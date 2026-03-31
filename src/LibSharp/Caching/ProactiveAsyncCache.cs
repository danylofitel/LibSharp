// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// An async cache that proactively refreshes its value in the background before it expires.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
public sealed class ProactiveAsyncCache<T> : IValueCacheAsync<T>, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProactiveAsyncCache{T}"/> class.
    /// </summary>
    /// <param name="valueFactory">The value factory.</param>
    /// <param name="refreshInterval">The interval at which the cache should be refreshed.</param>
    /// <param name="preFetchOffset">The offset before the refresh interval to pre-fetch the value.</param>
    /// <param name="options">Optional configuration. When <c>null</c>, defaults are used (auto-start enabled, stale reads disabled).</param>
    public ProactiveAsyncCache(
        Func<CancellationToken, Task<T>> valueFactory,
        TimeSpan refreshInterval,
        TimeSpan preFetchOffset,
        ProactiveAsyncCacheOptions options = null)
    {
        Argument.NotNull(valueFactory, nameof(valueFactory));
        Argument.GreaterThan(refreshInterval, TimeSpan.Zero, nameof(refreshInterval));
        Argument.GreaterThanOrEqualTo(preFetchOffset, TimeSpan.Zero, nameof(preFetchOffset));
        Argument.LessThan(preFetchOffset, refreshInterval, nameof(preFetchOffset));

        options ??= new ProactiveAsyncCacheOptions();

        if (options.RefreshTimeout.HasValue)
        {
            Argument.GreaterThan(options.RefreshTimeout.Value, TimeSpan.Zero, nameof(options.RefreshTimeout));
        }

        m_cts = new CancellationTokenSource();
        m_lock = new object();
        m_firstValueSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        m_fetchFunc = valueFactory;
        m_refreshInterval = refreshInterval;
        m_preFetchOffset = preFetchOffset;
        m_allowStaleReads = options.AllowStaleReads;
        m_refreshTimeout = options.RefreshTimeout;
        m_onBackgroundRefreshError = options.OnBackgroundRefreshError;

        if (options.AutoStart)
        {
            Start();
        }
    }

    /// <summary>
    /// Starts the background refresh loop, including the initial fetch. Can be called multiple
    /// times safely; only the first call has any effect. The background task runs on the thread pool.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

        if (Interlocked.Exchange(ref m_isStarted, 1) != 0)
        {
            return;
        }

        lock (m_lock)
        {
            // Re-check disposal under lock to close the race with DisposeAsync/Dispose.
            // Without this, Dispose could read m_backgroundTask (null), return, dispose
            // the CTS, and then this write starts a task against an already-disposed CTS.
            if (Volatile.Read(ref m_isDisposed) != 0)
            {
                return;
            }

            m_backgroundTask = Task.Run(StartBackgroundRefresh);
        }
    }

    /// <inheritdoc/>
    public bool HasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_snapshot is not null;
        }
    }

    /// <inheritdoc/>
    public DateTime? Expiration
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_snapshot?.ExpirationTime;
        }
    }

    /// <inheritdoc/>
    public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

        CacheSnapshot snapshot = m_snapshot;
        if (snapshot is not null && DateTime.UtcNow < snapshot.ExpirationTime)
        {
            return snapshot.Value;
        }

        Task<CacheSnapshot> fetchTask = GetOrCreateFetchTask();

        if (snapshot is not null && m_allowStaleReads)
        {
            // Stale but non-null and stale reads are allowed: return the stale value
            // immediately. This ensures readers never block after the initial fetch, even
            // when the factory is slow or temporarily failing.
            return snapshot.Value;
        }

        // Either no value at all (first call) or stale reads are disabled.
        CacheSnapshot result = await fetchTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
        {
            return;
        }

        m_cts.Cancel();

        // Read m_backgroundTask under lock to synchronize with Start(), which writes it
        // under the same lock. Without this, we could read null, return, and dispose the
        // CTS while Start() is about to launch (or has just launched) the background task.
        Task backgroundTask;
        lock (m_lock)
        {
            backgroundTask = m_backgroundTask;
        }

        if (backgroundTask is not null)
        {
            await backgroundTask.ConfigureAwait(false);
        }

        m_cts.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
        {
            return;
        }

        m_cts.Cancel();

        // Read m_backgroundTask under lock to synchronize with Start() — see DisposeAsync.
        Task backgroundTask;
        lock (m_lock)
        {
            backgroundTask = m_backgroundTask;
        }

        if (backgroundTask is null)
        {
            m_cts.Dispose();
            return;
        }

        if (backgroundTask.IsCompleted)
        {
            backgroundTask.GetAwaiter().GetResult();
            m_cts.Dispose();
            return;
        }

        // Dispose() is best-effort: if the background task is still draining after cancellation,
        // finish disposal asynchronously rather than blocking the caller thread. Use DisposeAsync
        // when the caller needs to await full shutdown.
        _ = backgroundTask.ContinueWith(
            static (task, state) => CompleteDeferredDispose(task, (CancellationTokenSource)state),
            m_cts,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    // Half the quiet window between fetches: always positive (constructor enforces
    // preFetchOffset < refreshInterval), scales with the configured interval, and
    // is always less than refreshInterval so the background loop retries before expiration.
    private TimeSpan RetryDelay => (m_refreshInterval - m_preFetchOffset) / 2;

    private Task<CacheSnapshot> GetOrCreateFetchTask(bool forceRefresh = false)
    {
        lock (m_lock)
        {
            // Re-check disposal under lock to close the race between the caller's
            // disposal check and this point.
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            if (m_pendingFetch is not null && !m_pendingFetch.IsCompleted)
            {
                return m_pendingFetch;
            }

            // Observe any faulted/cancelled completed task to prevent
            // TaskScheduler.UnobservedTaskException from firing.
            if (m_pendingFetch is not null && m_pendingFetch.IsFaulted)
            {
                _ = m_pendingFetch.Exception;
            }

            if (!forceRefresh)
            {
                // Re-check snapshot freshness under lock — another thread may have
                // completed a fetch between our outer check and acquiring the lock.
                CacheSnapshot snapshot = m_snapshot;
                if (snapshot is not null && DateTime.UtcNow < snapshot.ExpirationTime)
                {
                    return Task.FromResult(snapshot);
                }
            }
            else
            {
                // Background pre-fetch: skip if a concurrent GetValueAsync already
                // refreshed the value while we were waiting to acquire the lock.
                CacheSnapshot snapshot = m_snapshot;
                if (snapshot is not null && DateTime.UtcNow < snapshot.ExpirationTime - m_preFetchOffset)
                {
                    return Task.FromResult(snapshot);
                }
            }

            // FetchAndUpdateAsync runs synchronously until its first await. If the value
            // factory returns a completed task (e.g. Task.FromResult(x)), the entire fetch
            // including the snapshot write runs under this lock. This is intentional —
            // the lock correctly protects invariants — but means concurrent GetValueAsync
            // callers block for the duration of any synchronous work in the factory.
            // If this becomes a bottleneck, wrapping in Task.Run would release the lock
            // immediately at the cost of an extra thread-pool hop.
            m_pendingFetch = FetchAndUpdateAsync();
            return m_pendingFetch;
        }
    }

    private async Task<CacheSnapshot> FetchAndUpdateAsync()
    {
        CancellationToken token;
        CancellationTokenSource timeoutCts = null;

        try
        {
            if (m_refreshTimeout.HasValue)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(m_cts.Token);
                timeoutCts.CancelAfter(m_refreshTimeout.Value);
                token = timeoutCts.Token;
            }
            else
            {
                token = m_cts.Token;
            }

            T value = await m_fetchFunc(token).ConfigureAwait(false);
            CacheSnapshot snapshot = new CacheSnapshot(value, DateTime.UtcNow + m_refreshInterval);
            m_snapshot = snapshot;
            _ = m_firstValueSignal.TrySetResult();
            return snapshot;
        }
        finally
        {
            timeoutCts?.Dispose();
        }
    }

    private async Task StartBackgroundRefresh()
    {
        try
        {
            // Perform the initial fetch so the cache is warm without waiting for
            // the first GetValueAsync call. Retry until it succeeds or the cache
            // is disposed, because the refresh loop below requires a valid snapshot.
            while (!m_firstValueSignal.Task.IsCompleted)
            {
                try
                {
                    _ = await GetOrCreateFetchTask().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (m_cts.IsCancellationRequested)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    try
                    {
                        m_onBackgroundRefreshError?.Invoke(ex);
                    }
                    catch
                    {
                        // Never let a callback exception crash the background loop.
                    }

                    // Wait before retrying to avoid tight-looping on persistent failures.
                    try
                    {
                        await Task.Delay(RetryDelay, m_cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }

            // Refresh loop.
            while (!m_cts.Token.IsCancellationRequested)
            {
                try
                {
                    CacheSnapshot snapshot = m_snapshot;

                    // Compute delay anchored to the snapshot's expiration time, eliminating
                    // cumulative drift that a fixed-interval delay would introduce.
                    DateTime preFetchTime = snapshot.ExpirationTime - m_preFetchOffset;
                    TimeSpan delay = preFetchTime - DateTime.UtcNow;

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, m_cts.Token).ConfigureAwait(false);
                    }

                    // After waking up, check if the value is already fresh (e.g. a concurrent
                    // GetValueAsync call may have refreshed it while we were waiting).
                    snapshot = m_snapshot;
                    if (snapshot is not null && DateTime.UtcNow < snapshot.ExpirationTime - m_preFetchOffset)
                    {
                        continue;
                    }

                    Task<CacheSnapshot> fetchTask = GetOrCreateFetchTask(forceRefresh: true);
                    _ = await fetchTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (m_cts.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    try
                    {
                        m_onBackgroundRefreshError?.Invoke(ex);
                    }
                    catch
                    {
                        // Never let a callback exception crash the background loop.
                    }

                    // Transient failure; wait before retrying to avoid tight-looping
                    // when the snapshot is already expired.
                    try
                    {
                        await Task.Delay(RetryDelay, m_cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Disposal cancelled the first-value wait; exit gracefully.
        }
        catch (ObjectDisposedException)
        {
            // Cache was disposed before the first value arrived.
        }
    }

    private static void CompleteDeferredDispose(Task backgroundTask, CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            if (backgroundTask.IsFaulted)
            {
                _ = backgroundTask.Exception;
            }
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }

    private readonly CancellationTokenSource m_cts;
    private readonly object m_lock;
    private readonly TaskCompletionSource m_firstValueSignal;
    private readonly Func<CancellationToken, Task<T>> m_fetchFunc;
    private readonly TimeSpan m_refreshInterval;
    private readonly TimeSpan m_preFetchOffset;
    private readonly bool m_allowStaleReads;
    private readonly TimeSpan? m_refreshTimeout;
    private readonly Action<Exception> m_onBackgroundRefreshError;

    // Volatile: the reference must be immediately visible to all threads because the hot
    // path in GetValueAsync (and HasValue/Expiration) reads it outside any lock. Since
    // CacheSnapshot is an immutable record, volatile on the reference alone is sufficient
    // for safe publication — readers always see a fully constructed, consistent snapshot.
    private volatile CacheSnapshot m_snapshot;
    private Task<CacheSnapshot> m_pendingFetch;
    private Task m_backgroundTask;
    private int m_isDisposed;
    private int m_isStarted;

    private sealed record CacheSnapshot
    {
        public CacheSnapshot(T value, DateTime expirationTime)
        {
            Value = value;
            ExpirationTime = expirationTime;
        }

        public T Value { get; }

        public DateTime ExpirationTime { get; }
    }
}
