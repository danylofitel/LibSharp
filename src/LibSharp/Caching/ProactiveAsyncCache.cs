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
    /// <param name="valueFactory">The value factory.</param>
    /// <param name="refreshInterval">The interval at which the cache should be refreshed.</param>
    /// <param name="preFetchOffset">The offset before the refresh interval to pre-fetch the value.</param>
    /// <param name="allowStaleReads">
    /// When <c>true</c>, readers receive the stale cached value immediately while a background
    /// refresh runs. When <c>false</c> (default), readers block until the refresh completes.
    /// </param>
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
        m_backgroundTask = Task.Run(BackgroundRefresh);
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
        try
        {
            CacheSnapshot result = await fetchTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            return result.Value;
        }
        catch (OperationCanceledException) when (Volatile.Read(ref m_isDisposed) != 0 && !cancellationToken.IsCancellationRequested)
        {
            // The fetch was cancelled by disposal, not by the caller. Surface this as
            // ObjectDisposedException so callers can distinguish the two cases.
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// If the value factory does not honour its <see cref="CancellationToken"/>, this method may block
    /// indefinitely. Add a timeout inside the factory to bound the wait.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
        {
            return;
        }

        m_cts.Cancel();

        // Read m_pendingFetch under lock: GetValueAsync can create a fetch independently of
        // the background loop (e.g. when AllowStaleReads is true and the loop is sleeping).
        // After m_isDisposed = 1 no new fetch can be created (GetOrCreateFetchTask checks
        // disposal under the lock), so this single read captures the last possible in-flight fetch.
        Task<CacheSnapshot> pendingFetch;
        lock (m_lock)
        {
            pendingFetch = m_pendingFetch;
        }

        try
        {
            await m_backgroundTask.ConfigureAwait(false);
        }
        catch
        {
            // Intentionally swallowed — the refresh loop is internally defensive and should
            // not fault, but we must never throw from a disposal method.
        }

        if (pendingFetch is not null && !pendingFetch.IsCompleted)
        {
            // Wait for any in-flight fetch to drain. The factory may still be running even
            // after the background loop exits (e.g. the loop was sleeping and the CTS cancel
            // woke it before the factory completed).
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

            // Observe any completed faulted task to prevent TaskScheduler.UnobservedTaskException.
            if (m_pendingFetch?.IsFaulted == true)
            {
                _ = m_pendingFetch.Exception;
            }

            // Re-check freshness under lock — another thread may have completed a fetch
            // between the caller's outer check and acquiring the lock.
            // The background loop uses a tighter threshold (ExpirationTime - preFetchOffset)
            // so it can refresh before the value actually expires.
            CacheSnapshot snapshot = m_snapshot;
            TimeSpan freshThreshold = forceRefresh ? m_preFetchOffset : TimeSpan.Zero;
            if (snapshot is not null && DateTime.UtcNow < snapshot.ExpirationTime - freshThreshold)
            {
                return Task.FromResult(snapshot);
            }

            // FetchAndUpdateAsync runs synchronously until its first await. If the value
            // factory returns a completed task (e.g. Task.FromResult(x)), the entire fetch
            // including the snapshot write runs under this lock. This is intentional —
            // the lock correctly protects invariants — but means concurrent GetValueAsync
            // callers block for the duration of any synchronous work in the factory.
            m_pendingFetch = FetchAndUpdateAsync();

            // Proactively observe any fault so TaskScheduler.UnobservedTaskException never
            // fires, even when a stale-read caller discards the fetch task reference.
            _ = m_pendingFetch.ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return m_pendingFetch;
        }
    }

    private async Task<CacheSnapshot> FetchAndUpdateAsync()
    {
        T value = await m_fetchFunc(m_cts.Token).ConfigureAwait(false);
        CacheSnapshot snapshot = new CacheSnapshot(value, DateTime.UtcNow + m_refreshInterval);
        m_snapshot = snapshot;
        return snapshot;
    }

    private async Task BackgroundRefresh()
    {
        // Phase 1: initial fetch — retry until a value is obtained or the cache is disposed.
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
            catch (ObjectDisposedException)
            {
                return;
            }
            catch
            {
                // Transient failure; wait before retrying to avoid tight-looping.
                try
                {
                    await Task.Delay(m_retryDelay, m_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        // Phase 2: periodic refresh — pre-fetch before each expiry.
        while (!m_cts.Token.IsCancellationRequested)
        {
            try
            {
                CacheSnapshot snapshot = m_snapshot;

                // Compute delay anchored to the snapshot's expiration time, eliminating
                // cumulative drift that a fixed-interval delay would introduce.
                TimeSpan delay = snapshot.ExpirationTime - m_preFetchOffset - DateTime.UtcNow;
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

                _ = await GetOrCreateFetchTask(forceRefresh: true).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (m_cts.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                // Transient failure; wait before retrying to avoid tight-looping
                // when the snapshot is already expired.
                try
                {
                    await Task.Delay(m_retryDelay, m_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private static TimeSpan CalculateRetryDelay(TimeSpan refreshInterval, TimeSpan preFetchOffset)
    {
        // Half the quiet window between fetches, clamped to at least one tick so retry
        // delays never collapse to zero on very small refresh windows.
        TimeSpan delay = (refreshInterval - preFetchOffset) / 2;
        return delay > TimeSpan.Zero ? delay : TimeSpan.FromTicks(1);
    }

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
    // CacheSnapshot is an immutable record, volatile on the reference alone is sufficient
    // for safe publication — readers always see a fully constructed, consistent snapshot.
    private volatile CacheSnapshot m_snapshot;
    private Task<CacheSnapshot> m_pendingFetch; // under m_lock

    private int m_isDisposed;

    private sealed record CacheSnapshot(T Value, DateTime ExpirationTime);
}
