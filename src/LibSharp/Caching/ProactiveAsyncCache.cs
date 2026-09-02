// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// An async cache that proactively refreshes its value in the background before it expires.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// Disposal is mandatory. The background refresh loop keeps the instance rooted for its entire
/// lifetime, so an instance that is never disposed is never garbage collected and keeps invoking
/// the value factory forever — both a managed leak and a continuous load on whatever the factory
/// calls. Always dispose the cache via <see cref="DisposeAsync"/> when it is no longer needed.
/// An <c>idleTimeout</c> stops the factory calls once the cache falls out of use, but it does not
/// release the instance — disposal is still mandatory.
/// <para>
/// Should not be used with <see cref="IDisposable"/> value types since it does not dispose of
/// replaced values. This cache sheds values more readily than the others in this namespace: the
/// background loop replaces the current value once per refresh interval whether or not anything
/// ever reads it, so a disposable <typeparamref name="T"/> leaks on every refresh rather than only
/// on demand.
/// </para>
/// </remarks>
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
    /// Either way, the first read blocks until the initial fetch completes, because there is no
    /// prior value to serve.
    /// </param>
    /// <param name="timeProvider">
    /// (Optional) Time provider used to measure expiration and schedule background refreshes.
    /// Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    /// <param name="idleTimeout">
    /// (Optional) When set, the background refresh loop suspends itself once no call to
    /// <see cref="GetValueAsync(System.Threading.CancellationToken)"/> has been made for this long,
    /// so a cache that has fallen out of use stops invoking the value factory. While suspended the
    /// loop holds no timer and consumes no CPU; the next
    /// <see cref="GetValueAsync(System.Threading.CancellationToken)"/> resumes it immediately.
    /// That call is served from the cached value if it has not yet expired, and otherwise fetches
    /// on the caller's behalf exactly as the first-ever read does, so the cost of having gone idle
    /// is at most one reactive fetch when usage resumes.
    /// Only <see cref="GetValueAsync(System.Threading.CancellationToken)"/> counts as activity;
    /// <see cref="HasValue"/> and <see cref="Expiration"/> are metadata probes and do not.
    /// Construction counts as activity, so a newly created cache always gets a full idle window in
    /// which to perform its initial fetch.
    /// Choose a value comfortably larger than <paramref name="refreshInterval"/>: a shorter one lets
    /// the cache fall idle between consecutive reads, which degrades it to on-demand refresh.
    /// Defaults to <c>null</c>, meaning the loop never suspends.
    /// </param>
    /// <remarks>
    /// The value factory is expected to be independent of this cache instance. Re-entering this same
    /// cache from inside the factory is unsupported and may deadlock if the factory awaits the nested read.
    /// </remarks>
    public ProactiveAsyncCache(
        Func<CancellationToken, Task<T>> valueFactory,
        TimeSpan refreshInterval,
        TimeSpan preFetchOffset,
        bool allowStaleReads = false,
        TimeProvider? timeProvider = null,
        TimeSpan? idleTimeout = null)
    {
        Argument.NotNull(valueFactory);
        Argument.GreaterThan(refreshInterval, TimeSpan.Zero);
        Argument.GreaterThanOrEqualTo(preFetchOffset, TimeSpan.Zero);
        Argument.LessThan(preFetchOffset, refreshInterval);
        if (idleTimeout.HasValue)
        {
            Argument.GreaterThan(idleTimeout.Value, TimeSpan.Zero, nameof(idleTimeout));
        }

        m_cts = new CancellationTokenSource();
        m_lock = new object();
        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_fetchFunc = valueFactory;
        m_refreshInterval = refreshInterval;
        m_preFetchOffset = preFetchOffset;
        m_retryDelay = CalculateRetryDelay(refreshInterval, preFetchOffset);
        m_allowStaleReads = allowStaleReads;
        m_idleTracker = idleTimeout.HasValue ? new IdleTracker(m_timeProvider, idleTimeout.Value) : null;
        m_backgroundTask = Task.Run(BackgroundRefreshAsync);
    }

    /// <inheritdoc/>
    public bool HasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);
            return m_state.Snapshot is not null;
        }
    }

    /// <inheritdoc/>
    public DateTime? Expiration
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);
            return m_state.Snapshot?.ExpiresAt;
        }
    }

    /// <summary>
    /// Gets the exception from the most recent failed refresh, or <c>null</c> if the cache is healthy.
    /// </summary>
    /// <remarks>
    /// Cleared by the next successful refresh, so a non-null value means the cache is failing
    /// <em>now</em>, not that it failed at some point. With stale reads enabled this is the only
    /// signal that the value being served has stopped being updated, since readers see no error.
    /// <para>
    /// This and the two properties below are each sampled independently from an internally
    /// consistent state object, so a pair of reads may straddle a refresh. Treat them as a health
    /// signal rather than a transactional snapshot.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the cache has been disposed.</exception>
    public Exception? LastRefreshException
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);
            return m_state.Failure?.Exception;
        }
    }

    /// <summary>
    /// Gets the number of consecutive failed refreshes since the last successful one.
    /// </summary>
    /// <remarks>
    /// Zero when the cache is healthy. A value that keeps climbing is the signal to alert on: it
    /// distinguishes a single transient failure from a dependency that is genuinely down.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the cache has been disposed.</exception>
    public int ConsecutiveRefreshFailures
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);
            return m_state.Failure?.ConsecutiveCount ?? 0;
        }
    }

    /// <summary>
    /// Gets the time at which the current value was produced, or <c>null</c> before the first
    /// successful refresh.
    /// </summary>
    /// <remarks>
    /// Measured with the cache's <see cref="TimeProvider"/>. Together with
    /// <see cref="ConsecutiveRefreshFailures"/> this gives the age of what is actually being served,
    /// which is what matters when stale reads are enabled and the value could be arbitrarily old.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the cache has been disposed.</exception>
    public DateTime? LastSuccessfulRefresh
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);
            return m_state.Snapshot?.CreatedAt;
        }
    }

    /// <inheritdoc/>
    public ValueTask<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

        DateTime now = UtcNow;

        // Record the access before looking at the snapshot, so the background loop's idle deadline
        // reflects this call whether or not the cached value turns out to be fresh. Skipped
        // entirely when no idle timeout is configured, leaving the default hot path exactly as it
        // was: a single volatile reference read, with no interlocked operation and no shared cache
        // line to dirty.
        if (m_idleTracker is not null)
        {
            m_idleTracker.RecordAccess(now);
        }

        // Hot path: snapshot is fresh, no lock needed.
        // m_state is volatile so the reference read is immediately visible across threads, and the
        // whole state is immutable, so this single read gives a consistent view.
        CacheSnapshot? snapshot = m_state.Snapshot;
        if (snapshot is not null && now < snapshot.ExpiresAt)
        {
            // Hit: this method is deliberately not async, so the common path costs no allocation
            // and builds no state machine.
            return new ValueTask<T>(snapshot.Value);
        }

        // Miss: this call has to wait for a refresh, so an already-cancelled token cancels it here.
        // WaitAsync in the slow path cannot be relied on for this — when the shared refresh has
        // already completed it hands back its result without ever consulting the token. Checked
        // after the fast path above, because a hit does no waiting and so has nothing to cancel.
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<T>(cancellationToken);
        }

        return FetchValueAsync(snapshot, cancellationToken);
    }

    // Slow path: no value yet, or the value has expired.
    private async ValueTask<T> FetchValueAsync(CacheSnapshot? snapshot, CancellationToken cancellationToken)
    {
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

        // Cancel runs any cancellation callbacks the factory registered on the token it was
        // handed; a throwing callback is surfaced by Cancel as an AggregateException and must
        // not prevent disposal from completing.
        try
        {
            m_cts.Cancel();
        }
        catch
        {
            // Intentionally swallowed — DisposeAsync must never throw.
        }

        // Capture m_pendingFetch under lock. After m_isDisposed = 1, GetOrCreateFetchTask
        // throws ObjectDisposedException under the lock, so no new fetch can be created.
        // This single read therefore captures the last possible in-flight fetch.
        //
        // m_backgroundTask is always non-null (set in constructor) and does not need to be
        // read under lock — it is never reassigned after construction.
        Task<CacheSnapshot>? pendingFetch;
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
    // backgroundRefresh = false (GetValueAsync): fresh if now < expiresAt
    // backgroundRefresh = true  (background loop): fresh if now < expiresAt - preFetchOffset
    //   The tighter threshold prevents the background from duplicating a fetch that
    //   GetValueAsync just performed while the loop was sleeping.
    // The flag also exempts the loop from the reader-side failure backoff below: the loop already
    // paces its own retries with m_retryDelay, so suppressing it there would only stall it further.
    private Task<CacheSnapshot> GetOrCreateFetchTask(bool backgroundRefresh = false)
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
                _ = m_pendingFetch!.Exception;
            }

            // Re-check freshness under lock — a concurrent thread may have completed a
            // fetch between the caller's outer check and acquiring the lock.
            CacheState state = m_state;
            CacheSnapshot? snapshot = state.Snapshot;
            TimeSpan freshThreshold = backgroundRefresh ? m_preFetchOffset : TimeSpan.Zero;
            if (snapshot is not null && UtcNow < snapshot.ExpiresAt - freshThreshold)
            {
                return Task.FromResult(snapshot);
            }

            // Negative caching. Without this, a faulted fetch is IsCompleted, so the next read
            // starts a fresh factory call with no delay at all: a dependency failing fast turns a
            // multi-minute refresh interval into a retry per read, hammering a service that is very
            // likely failing fast because it is already overloaded. Within m_retryDelay of a
            // failure, replay the stored exception instead of calling the factory again.
            FetchFailure? failure = state.Failure;
            if (!backgroundRefresh && failure is not null && UtcNow - failure.Time < m_retryDelay)
            {
                Task<CacheSnapshot> suppressed = Task.FromException<CacheSnapshot>(failure.Exception);

                // Observe it here: a stale-read caller discards this task without awaiting it, and
                // an unobserved faulted task would raise UnobservedTaskException on finalization.
                _ = suppressed.Exception;

                return suppressed;
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
            DateTime now = UtcNow;
            DateTime expiration = m_refreshInterval >= DateTime.MaxValue - now
                ? DateTime.MaxValue
                : now + m_refreshInterval;

            CacheSnapshot snapshot = new CacheSnapshot(value, now, expiration);

            // Volatile write — immediately visible on the hot path. Success clears any recorded
            // failure, so the backoff and the failure counters reset together.
            m_state = new CacheState(snapshot, null);
            _ = tcs.TrySetResult(snapshot);
        }
        catch (OperationCanceledException) when (m_cts.IsCancellationRequested)
        {
            // Disposal, not a factory failure — deliberately not recorded as one.
            _ = tcs.TrySetCanceled(m_cts.Token);
        }
        catch (Exception ex)
        {
            // Keep whatever value we already had; only the failure part of the state changes.
            CacheState previous = m_state;
            int consecutive = (previous.Failure?.ConsecutiveCount ?? 0) + 1;
            m_state = previous with { Failure = new FetchFailure(ex, UtcNow, consecutive) };

            _ = tcs.TrySetException(ex);
        }
    }

    private async Task BackgroundRefreshAsync()
    {
        // Phase 1: initial fetch.
        // Retry until a value is obtained or the cache is disposed. A valid snapshot is
        // required before the timed refresh loop can compute meaningful delays.
        while (m_state.Snapshot is null)
        {
            // Nobody has asked for a value since construction: suspend instead of retrying the
            // factory forever. A reader arriving meanwhile fetches reactively and wakes this loop;
            // falling straight through to GetOrCreateFetchTask afterwards is harmless, because it
            // re-checks freshness under the lock and hands back that reader's snapshot unchanged.
            if (!await WaitWhileIdleAsync().ConfigureAwait(false))
            {
                return;
            }

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
                    await Task.Delay(Clamp(m_retryDelay), m_timeProvider, m_cts.Token).ConfigureAwait(false);
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
                // Suspend instead of scheduling around an expiration nobody is waiting on. The
                // snapshot is then left to expire; the read that ends the idle period pays for one
                // reactive fetch, which is exactly the trade this option makes.
                if (!await WaitWhileIdleAsync().ConfigureAwait(false))
                {
                    break;
                }

                // Anchor the delay to the snapshot's expiration time rather than using a
                // fixed interval, so scheduling jitter does not cause cumulative drift.
                // Phase 1 guarantees a snapshot exists on entry; the null guard is defensive
                // (a null snapshot simply falls through to an immediate refresh).
                CacheSnapshot? snapshot = m_state.Snapshot;
                if (snapshot is not null)
                {
                    TimeSpan delay = snapshot.ExpiresAt - m_preFetchOffset - UtcNow;

                    // Never sleep past the moment the cache goes idle. Without this the loop would
                    // hold its refresh timer for the rest of the interval after falling idle —
                    // up to a full refreshInterval of staying scheduled for a pre-fetch it is
                    // going to skip anyway. Waking at the earlier of the two deadlines lets the
                    // idle check below park it and release the timer promptly.
                    // TimeUntilIdle is zero exactly when IsIdle is true, so clamping only to a
                    // strictly positive value keeps this from ever collapsing into a spin.
                    TimeSpan? untilIdle = m_idleTracker?.TimeUntilIdle();
                    if (untilIdle is TimeSpan remaining && remaining > TimeSpan.Zero && remaining < delay)
                    {
                        delay = remaining;
                    }

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(Clamp(delay), m_timeProvider, m_cts.Token).ConfigureAwait(false);
                    }
                }

                // The cache may have fallen idle while we slept: skip this refresh and let the top
                // of the next iteration suspend the loop.
                if (m_idleTracker?.IsIdle() is true)
                {
                    continue;
                }

                // Re-read after sleeping: a concurrent GetValueAsync may have refreshed
                // the value while we were waiting, making our pre-fetch unnecessary.
                snapshot = m_state.Snapshot;
                if (snapshot is not null && UtcNow < snapshot.ExpiresAt - m_preFetchOffset)
                {
                    continue;
                }

                _ = await GetOrCreateFetchTask(backgroundRefresh: true).ConfigureAwait(false);
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
                    await Task.Delay(Clamp(m_retryDelay), m_timeProvider, m_cts.Token).ConfigureAwait(false);
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

    // Suspends the background refresh loop for as long as the cache is idle. Returns true once the
    // cache is active again, and false when it was disposed while suspended, in which case the
    // caller must exit its loop. A no-op when no idle timeout is configured.
    private async Task<bool> WaitWhileIdleAsync()
    {
        return m_idleTracker is null
            || await m_idleTracker.WaitWhileIdleAsync(m_cts.Token).ConfigureAwait(false);
    }

    private DateTime UtcNow => m_timeProvider.GetUtcNow().UtcDateTime;

    // Task.Delay internally converts TimeSpan to int milliseconds; clamp to avoid overflow
    // for refresh intervals longer than ~24.8 days. When the delay fires early, the loop
    // re-reads the snapshot and recomputes — it simply sleeps again and converges correctly.
    private static readonly TimeSpan s_maxDelay = TimeSpan.FromMilliseconds(int.MaxValue - 1);

    private readonly CancellationTokenSource m_cts;
    private readonly object m_lock;
    private readonly TimeProvider m_timeProvider;
    private readonly Func<CancellationToken, Task<T>> m_fetchFunc;
    private readonly TimeSpan m_refreshInterval;
    private readonly TimeSpan m_preFetchOffset;
    private readonly TimeSpan m_retryDelay;
    private readonly bool m_allowStaleReads;
    private readonly IdleTracker? m_idleTracker;
    private readonly Task m_backgroundTask;

    // Everything the cache knows, in one immutable object published by a single reference swap.
    // Volatile: the hot path in GetValueAsync (and HasValue/Expiration) reads it outside any lock,
    // and because CacheState and everything it holds are immutable records, volatile on the
    // reference alone is enough — readers always see a fully constructed, consistent object.
    //
    // Never null, so the read path needs no null check of its own. Only CompleteAsync writes it,
    // and those writes are serialised: a new fetch cannot be created until the previous one has
    // completed, and the state is published before the task completes. A plain store is therefore
    // sufficient even for the consecutive-failure count, which needs no interlocked increment.
    private volatile CacheState m_state = new CacheState(null, null);

    // Written and read only under m_lock.
    private Task<CacheSnapshot>? m_pendingFetch;

    private int m_isDisposed;

    private sealed record CacheState(CacheSnapshot? Snapshot, FetchFailure? Failure);

    // CreatedAt is stored rather than derived from ExpiresAt: expiration is clamped to
    // DateTime.MaxValue for very large refresh intervals, so subtracting the interval back off it
    // would not recover the real production time.
    private sealed record CacheSnapshot(T Value, DateTime CreatedAt, DateTime ExpiresAt);

    private sealed record FetchFailure(Exception Exception, DateTime Time, int ConsecutiveCount);
}
