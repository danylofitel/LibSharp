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
public sealed class ProactiveAsyncCache<T> : IValueCacheAsync<T>, IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProactiveAsyncCache{T}"/> class.
    /// The background refresh loop starts immediately upon construction.
    /// </summary>
    /// <param name="valueFactory">
    /// The value factory. It must not call or await <see cref="GetValueAsync(System.Threading.CancellationToken)"/>
    /// on this same cache instance.
    /// </param>
    /// <param name="refreshInterval">The interval at which the cache should be refreshed.</param>
    /// <param name="preFetchOffset">The offset before the refresh interval to pre-fetch the value.</param>
    /// <param name="allowStaleReads">
    /// When <c>true</c>, readers receive the stale cached value immediately while a background
    /// refresh runs. When <c>false</c> (default), readers block until the refresh completes.
    /// </param>
    /// <remarks>
    /// The value factory is expected to be independent of this cache instance. Re-entering this same
    /// cache from inside the factory is unsupported and may deadlock if the factory awaits the nested read.
    /// </remarks>
    public ProactiveAsyncCache(
        Func<CancellationToken, Task<T>> valueFactory,
        TimeSpan refreshInterval,
        TimeSpan preFetchOffset,
        bool allowStaleReads = false)
    {
        Argument.NotNull(valueFactory, nameof(valueFactory));
        Argument.GreaterThan(refreshInterval, TimeSpan.Zero, nameof(refreshInterval));
        Argument.GreaterThanOrEqualTo(preFetchOffset, TimeSpan.Zero, nameof(preFetchOffset));
        Argument.LessThan(preFetchOffset, refreshInterval, nameof(preFetchOffset));

        m_cts = new CancellationTokenSource();
        m_lock = new object();
        m_fetchFunc = valueFactory;
        m_refreshInterval = refreshInterval;
        m_preFetchOffset = preFetchOffset;
        m_retryDelay = CalculateRetryDelay(refreshInterval, preFetchOffset);
        m_allowStaleReads = allowStaleReads;
        m_backgroundTask = Task.Run(BackgroundRefreshAsync);
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

        // Hot path: snapshot is fresh, no lock needed.
        // m_snapshot is volatile so the reference read is immediately visible across threads.
        // CacheSnapshot is an immutable record so a non-null reference is always a fully
        // constructed, consistent object.
        CacheSnapshot snapshot = m_snapshot;
        if (snapshot is not null && DateTime.UtcNow < snapshot.ExpirationTime)
        {
            return snapshot.Value;
        }

        // Snapshot is absent or expired — get or start a fetch.
        Task<CacheSnapshot> fetchTask = GetOrCreateFetchTask();

        if (snapshot is not null && m_allowStaleReads)
        {
            // Stale reads are allowed: return the stale value immediately while the refresh
            // runs in the background. This ensures readers never block after the initial fetch,
            // even when the factory is slow or temporarily failing.
            // If the factory completed synchronously (e.g. Task.FromResult), the fetch task
            // is already done and a fresher value is available — prefer it over the stale one.
            return fetchTask.IsCompletedSuccessfully ? fetchTask.Result.Value : snapshot.Value;
        }

        // Either no value at all (first call) or stale reads are disabled: wait for the fetch.
        // WaitAsync attaches caller cancellation without cancelling the underlying factory call,
        // so other callers sharing the same fetch task are unaffected.
        try
        {
            CacheSnapshot result = await fetchTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            return result.Value;
        }
        catch (OperationCanceledException) when (Volatile.Read(ref m_isDisposed) != 0 && !cancellationToken.IsCancellationRequested)
        {
            // The fetch was cancelled by disposal, not by the caller. Surface as
            // ObjectDisposedException so callers can distinguish the two cases.
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// If the value factory does not honour its <see cref="CancellationToken"/>, this method may
    /// block indefinitely. Add a timeout inside the factory to bound disposal time.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
        {
            return;
        }

        m_cts.Cancel();

        // Capture m_pendingFetch under lock. After m_isDisposed = 1, GetOrCreateFetchTask
        // throws ObjectDisposedException under the lock, so no new fetch can be created.
        // This single read therefore captures the last possible in-flight fetch.
        //
        // m_backgroundTask is always non-null (set in constructor) and does not need to be
        // read under lock — it is never reassigned after construction.
        Task<CacheSnapshot> pendingFetch;
        lock (m_lock)
        {
            pendingFetch = m_pendingFetch;
        }

        // Wait for the background loop to exit. When the background loop is itself awaiting
        // a fetch (not sleeping on the refresh timer), this implicitly drains that fetch too.
        try
        {
            await m_backgroundTask.ConfigureAwait(false);
        }
        catch
        {
            // Swallow — the loop is internally defensive and should not fault, but we must
            // never throw from DisposeAsync.
        }

        // Drain any independently-created fetch (e.g. one started by GetValueAsync while
        // the background loop was sleeping on the refresh timer). After m_backgroundTask
        // exits, no new fetches can be created, so this is the last possible in-flight one.
        if (pendingFetch is not null && !pendingFetch.IsCompleted)
        {
            try
            {
                _ = await pendingFetch.ConfigureAwait(false);
            }
            catch
            {
                // Intentionally swallowed.
            }
        }

        m_cts.Dispose();
    }

    // Returns a task representing an in-progress or newly started fetch. Callers should
    // await the returned task to get the refreshed snapshot.
    //
    // forceRefresh = false (GetValueAsync): fresh if now < expirationTime
    // forceRefresh = true  (background loop): fresh if now < expirationTime - preFetchOffset
    //   The tighter threshold prevents the background from duplicating a fetch that
    //   GetValueAsync just performed while the loop was sleeping.
    private Task<CacheSnapshot> GetOrCreateFetchTask(bool forceRefresh = false)
    {
        TaskCompletionSource<CacheSnapshot> tcs;

        lock (m_lock)
        {
            // Re-check disposal under lock — closes the window between the caller's
            // initial disposal check and acquiring the lock.
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            // Join an existing in-progress fetch.
            if (m_pendingFetch is not null && !m_pendingFetch.IsCompleted)
            {
                return m_pendingFetch;
            }

            // Observe any completed faulted fetch to suppress UnobservedTaskException.
            // (The ContinueWith below handles the common case; this covers any gap.)
            if (m_pendingFetch?.IsFaulted == true)
            {
                _ = m_pendingFetch.Exception;
            }

            // Re-check freshness under lock — a concurrent thread may have completed a
            // fetch between the caller's outer check and acquiring the lock.
            CacheSnapshot snapshot = m_snapshot;
            TimeSpan freshThreshold = forceRefresh ? m_preFetchOffset : TimeSpan.Zero;
            if (snapshot is not null && DateTime.UtcNow < snapshot.ExpirationTime - freshThreshold)
            {
                return Task.FromResult(snapshot);
            }

            // Publish a TCS task as m_pendingFetch *before* invoking the factory. This
            // closes the synchronous re-entrancy hole: if the factory's synchronous prologue
            // calls back into GetValueAsync (and therefore GetOrCreateFetchTask), the lock is
            // reentrant on the same thread and the reentrant call will find m_pendingFetch
            // already set, returning this task instead of starting a new recursive fetch.
            // Awaiting GetValueAsync on this same cache from inside the factory is still
            // unsupported and may deadlock; that misuse is documented on the constructor.
            tcs = new TaskCompletionSource<CacheSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_pendingFetch = tcs.Task;

            // Proactively observe any fault so UnobservedTaskException never fires, even
            // when a stale-read caller discards the fetch task reference and the factory
            // later fails.
            _ = m_pendingFetch.ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        // Invoke the factory outside the lock. CompleteAsync catches all exceptions and
        // routes them into the TCS, so the fire-and-forget task itself never faults.
        _ = CompleteAsync(tcs);

        return tcs.Task;
    }

    private async Task CompleteAsync(TaskCompletionSource<CacheSnapshot> tcs)
    {
        try
        {
            Task<T> fetchTask = m_fetchFunc(m_cts.Token)
                ?? throw new InvalidOperationException("The value factory returned a null task.");
            T value = await fetchTask.ConfigureAwait(false);

            // Clamp expiration to DateTime.MaxValue to avoid overflow when refreshInterval
            // is very large (e.g. TimeSpan.FromDays(1000)).
            DateTime now = DateTime.UtcNow;
            DateTime expiration = m_refreshInterval >= DateTime.MaxValue - now
                ? DateTime.MaxValue
                : now + m_refreshInterval;

            CacheSnapshot snapshot = new CacheSnapshot(value, expiration);
            // Volatile write — immediately visible to all threads reading m_snapshot on the hot path.
            m_snapshot = snapshot;
            _ = tcs.TrySetResult(snapshot);
        }
        catch (OperationCanceledException) when (m_cts.IsCancellationRequested)
        {
            _ = tcs.TrySetCanceled(m_cts.Token);
        }
        catch (Exception ex)
        {
            _ = tcs.TrySetException(ex);
        }
    }

    private async Task BackgroundRefreshAsync()
    {
        // Phase 1: initial fetch.
        // Retry until a value is obtained or the cache is disposed. A valid snapshot is
        // required before the timed refresh loop can compute meaningful delays.
        while (m_snapshot is null)
        {
            try
            {
                _ = await GetOrCreateFetchTask().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (m_cts.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (Volatile.Read(ref m_isDisposed) != 0)
            {
                return;
            }
            catch
            {
                // Transient factory failure; wait before retrying to avoid tight-looping.
                try
                {
                    await Task.Delay(Clamp(m_retryDelay), m_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        // Phase 2: periodic refresh.
        // Sleep until just before the current snapshot expires, then pre-fetch.
        while (!m_cts.Token.IsCancellationRequested)
        {
            try
            {
                // Anchor the delay to the snapshot's expiration time rather than using a
                // fixed interval, so scheduling jitter does not cause cumulative drift.
                CacheSnapshot snapshot = m_snapshot;
                TimeSpan delay = snapshot.ExpirationTime - m_preFetchOffset - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(Clamp(delay), m_cts.Token).ConfigureAwait(false);
                }

                // Re-read after sleeping: a concurrent GetValueAsync may have refreshed
                // the value while we were waiting, making our pre-fetch unnecessary.
                snapshot = m_snapshot;
                if (snapshot is not null && DateTime.UtcNow < snapshot.ExpirationTime - m_preFetchOffset)
                {
                    continue;
                }

                _ = await GetOrCreateFetchTask(forceRefresh: true).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (m_cts.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (Volatile.Read(ref m_isDisposed) != 0)
            {
                break;
            }
            catch
            {
                // Transient factory failure; wait before retrying to avoid tight-looping
                // when the snapshot has already expired.
                try
                {
                    await Task.Delay(Clamp(m_retryDelay), m_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    // Half the quiet window between fetches, clamped to at least one tick so retry
    // delays never collapse to zero when refreshInterval - preFetchOffset = 1 tick.
    private static TimeSpan CalculateRetryDelay(TimeSpan refreshInterval, TimeSpan preFetchOffset)
    {
        TimeSpan half = (refreshInterval - preFetchOffset) / 2;
        return half > TimeSpan.Zero ? half : TimeSpan.FromTicks(1);
    }

    private static TimeSpan Clamp(TimeSpan delay)
    {
        return delay <= s_maxDelay ? delay : s_maxDelay;
    }

    // Task.Delay internally converts TimeSpan to int milliseconds; clamp to avoid overflow
    // for refresh intervals longer than ~24.8 days. When the delay fires early, the loop
    // re-reads the snapshot and recomputes — it simply sleeps again and converges correctly.
    private static readonly TimeSpan s_maxDelay = TimeSpan.FromMilliseconds(int.MaxValue - 1);

    private readonly CancellationTokenSource m_cts;
    private readonly object m_lock;
    private readonly Func<CancellationToken, Task<T>> m_fetchFunc;
    private readonly TimeSpan m_refreshInterval;
    private readonly TimeSpan m_preFetchOffset;
    private readonly TimeSpan m_retryDelay;
    private readonly bool m_allowStaleReads;
    private readonly Task m_backgroundTask;

    // Volatile: the reference must be immediately visible to all threads because the hot
    // path in GetValueAsync (and HasValue/Expiration) reads it outside any lock. Since
    // CacheSnapshot is an immutable record, volatile on the reference alone is sufficient —
    // readers always see a fully constructed, consistent object.
    private volatile CacheSnapshot m_snapshot;

    // Written and read only under m_lock.
    private Task<CacheSnapshot> m_pendingFetch;

    private int m_isDisposed;

    private sealed record CacheSnapshot(T Value, DateTime ExpirationTime);
}
