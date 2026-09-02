// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// Async in-memory value cache with ThreadSafetyMode.ExecutionAndPublication behavior.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// Should not be used with IDisposable value types since it does not dispose of expired values.
/// <para>
/// The value factory must not call <see cref="GetValueAsync"/> on this same cache and await the
/// result. The refresh task is published before the factory runs, so a factory whose synchronous
/// prologue re-enters simply joins that refresh rather than starting a recursive one; a factory
/// that <em>awaits</em> the nested read still deadlocks, waiting on the refresh it is itself running.
/// </para>
/// </remarks>
public sealed class ValueCacheAsync<T> : IValueCacheAsync<T>, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCacheAsync(Func<CancellationToken, Task<T>> factory, TimeSpan timeToLive, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory);
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero);

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_disposalToken = m_cts.Token;
        m_createFactory = factory;
        m_expirationFunction = _ => GetExpiration(timeToLive);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCacheAsync(Func<CancellationToken, Task<T>> factory, Func<T, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory);
        Argument.NotNull(expirationFunction);

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_disposalToken = m_cts.Token;
        m_createFactory = factory;
        m_expirationFunction = expirationFunction;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCacheAsync(Func<CancellationToken, Task<T>> createFactory, Func<T, CancellationToken, Task<T>> updateFactory, TimeSpan timeToLive, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(createFactory);
        Argument.NotNull(updateFactory);
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero);

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_disposalToken = m_cts.Token;
        m_createFactory = createFactory;
        m_updateFactory = updateFactory;
        m_expirationFunction = _ => GetExpiration(timeToLive);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCacheAsync(Func<CancellationToken, Task<T>> createFactory, Func<T, CancellationToken, Task<T>> updateFactory, Func<T, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(createFactory);
        Argument.NotNull(updateFactory);
        Argument.NotNull(expirationFunction);

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_disposalToken = m_cts.Token;
        m_createFactory = createFactory;
        m_updateFactory = updateFactory;
        m_expirationFunction = expirationFunction;
    }

    /// <inheritdoc/>
    public bool HasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_boxed is not null;
        }
    }

    /// <inheritdoc/>
    public DateTime? Expiration
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_boxed?.Expiration;
        }
    }

    /// <inheritdoc/>
    public ValueTask<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

        // Snapshot the volatile field once. ValueReference is immutable, so a non-null reference is
        // always a fully constructed, consistent object, and reading it once means the value returned
        // is the same one whose expiration was checked.
        ValueReference<T>? boxed = m_boxed;
        if (boxed is not null && UtcNow < boxed.Expiration)
        {
            // Hit: this method is deliberately not async, so the common path costs no allocation
            // and builds no state machine.
            return new ValueTask<T>(boxed.Value);
        }

        // Miss: this call has to wait for a refresh, so an already-cancelled token cancels it here.
        // WaitAsync in the slow path cannot be relied on for this — when the shared refresh has
        // already completed it hands back its result without ever consulting the token. Checked
        // after the fast path above, because a hit does no waiting and so has nothing to cancel.
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<T>(cancellationToken);
        }

        return RefreshAndGetValueAsync(cancellationToken);
    }

    private async ValueTask<T> RefreshAndGetValueAsync(CancellationToken cancellationToken)
    {
        Task<ValueReference<T>> refreshTask = GetOrCreateRefreshTask();

        // WaitAsync attaches this caller's cancellation to this caller's wait only. The refresh
        // itself runs on the disposal token, so a caller that gives up never cancels work other
        // callers are sharing, and never discards a value the cache was about to publish.
        try
        {
            ValueReference<T> refreshed = await refreshTask.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Never hand back a value produced after disposal. Dispose is synchronous and cannot
            // drain an in-flight refresh, and a factory that ignores its token can publish long
            // after Dispose returned.
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return refreshed.Value;
        }
        catch (OperationCanceledException) when (Volatile.Read(ref m_isDisposed) != 0 && !cancellationToken.IsCancellationRequested)
        {
            // The refresh was cancelled by disposal, not by this caller. Surface as
            // ObjectDisposedException so the two cases stay distinguishable.
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    // Returns a task for an in-progress or newly started refresh. At most one refresh is ever in
    // flight: concurrent callers join the same one instead of each invoking the factory.
    private Task<ValueReference<T>> GetOrCreateRefreshTask()
    {
        TaskCompletionSource<ValueReference<T>> tcs;

        lock (m_lock)
        {
            // Re-check disposal under the lock — closes the window between the caller's initial
            // check and acquiring the lock.
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            // Join a refresh that is already running.
            if (m_pendingRefresh is not null && !m_pendingRefresh.IsCompleted)
            {
                return m_pendingRefresh;
            }

            // Observe a completed faulted refresh so UnobservedTaskException never fires. The
            // continuation below covers the common case; this covers any gap.
            if (m_pendingRefresh?.IsFaulted == true)
            {
                _ = m_pendingRefresh!.Exception;
            }

            // Re-check freshness under the lock — a concurrent caller may have completed a refresh
            // between this caller's outer check and acquiring the lock.
            ValueReference<T>? boxed = m_boxed;
            if (boxed is not null && UtcNow < boxed.Expiration)
            {
                return Task.FromResult(boxed);
            }

            // Publish the task before invoking the factory. The lock is re-entrant on this thread,
            // so a factory whose synchronous prologue calls back in finds m_pendingRefresh already
            // set and joins it rather than recursing.
            tcs = new TaskCompletionSource<ValueReference<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_pendingRefresh = tcs.Task;

            // Proactively observe any fault, so a caller that abandoned its wait cannot leave an
            // unobserved exception behind.
            _ = m_pendingRefresh.ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        // Invoke the factory outside the lock, so it is never held across user code. RefreshAsync
        // routes every exception into the TCS, so this fire-and-forget task never faults.
        _ = RefreshAsync(tcs);

        return tcs.Task;
    }

    /// <summary>
    /// Disposes of the cache.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
        {
            return;
        }

        // Cancel before disposing: once cancellation has been requested a concurrent registration
        // runs its callback inline instead of being stored, which is what makes this order safe.
        try
        {
            m_cts.Cancel();
        }
        catch
        {
            // A throwing cancellation callback registered by the value factory must not prevent
            // disposal from completing.
        }

        m_cts.Dispose();
    }

    private DateTime UtcNow => m_timeProvider.GetUtcNow().UtcDateTime;

    private DateTime GetExpiration(TimeSpan timeToLive)
    {
        DateTime now = UtcNow;
        return timeToLive >= DateTime.MaxValue - now
            ? DateTime.MaxValue
            : now.Add(timeToLive);
    }

    private async Task RefreshAsync(TaskCompletionSource<ValueReference<T>> tcs)
    {
        try
        {
            // Snapshot once: the update factory must see the same previous value the decision to
            // use it was based on.
            ValueReference<T>? previous = m_boxed;

            T newValue;
            if (m_updateFactory is null || previous is null)
            {
                Task<T> createTask = m_createFactory(m_disposalToken)
                    ?? throw new InvalidOperationException("The value factory returned a null task.");
                newValue = await createTask.ConfigureAwait(false);
            }
            else
            {
                Task<T> updateTask = m_updateFactory(previous.Value, m_disposalToken)
                    ?? throw new InvalidOperationException("The update factory returned a null task.");
                newValue = await updateTask.ConfigureAwait(false);
            }

            ValueReference<T> refreshed = new ValueReference<T>(newValue, m_expirationFunction(newValue));

            // Volatile write — immediately visible to the lock-free read path.
            m_boxed = refreshed;
            _ = tcs.TrySetResult(refreshed);
        }
        catch (OperationCanceledException) when (m_disposalToken.IsCancellationRequested)
        {
            _ = tcs.TrySetCanceled(m_disposalToken);
        }
        catch (Exception ex)
        {
            _ = tcs.TrySetException(ex);
        }
    }

    private readonly object m_lock = new object();
    private readonly CancellationTokenSource m_cts = new CancellationTokenSource();

    // Captured at construction: reading m_cts.Token after Dispose would throw, and the refresh path
    // is fire-and-forget so it can still be running then.
    private readonly CancellationToken m_disposalToken;

    private readonly TimeProvider m_timeProvider;
    private readonly Func<CancellationToken, Task<T>> m_createFactory;
    private readonly Func<T, CancellationToken, Task<T>>? m_updateFactory;
    private readonly Func<T, DateTime> m_expirationFunction;

    private volatile ValueReference<T>? m_boxed;

    // Written and read only under m_lock.
    private Task<ValueReference<T>>? m_pendingRefresh;

    private int m_isDisposed;
}
