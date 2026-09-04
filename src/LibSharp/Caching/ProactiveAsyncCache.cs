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
    /// Initializes a new instance of the <see cref="ProactiveAsyncCache{T}"/> class with default
    /// options. The background refresh loop starts immediately.
    /// </summary>
    /// <param name="valueFactory">
    /// The value factory. It must not call or await <see cref="GetValueAsync(CancellationToken)"/>
    /// on this same cache instance.
    /// </param>
    /// <param name="refreshInterval">How long a fetched value stays fresh. Must be positive.</param>
    /// <param name="preFetchOffset">
    /// How long before expiration the background loop refreshes the value. Must be at least zero and
    /// less than <paramref name="refreshInterval"/>.
    /// </param>
    /// <remarks>
    /// A convenience for the common case. This overload will never gain further parameters —
    /// everything else is configured through <see cref="ProactiveAsyncCacheOptions"/>, so that new
    /// settings can be added without breaking existing callers.
    /// </remarks>
    public ProactiveAsyncCache(
        Func<CancellationToken, Task<T>> valueFactory,
        TimeSpan refreshInterval,
        TimeSpan preFetchOffset)
        : this(
            valueFactory,
            new ProactiveAsyncCacheOptions { RefreshInterval = refreshInterval, PreFetchOffset = preFetchOffset })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProactiveAsyncCache{T}"/> class.
    /// The background refresh loop starts immediately.
    /// </summary>
    /// <param name="valueFactory">
    /// The value factory. It must not call or await <see cref="GetValueAsync(CancellationToken)"/>
    /// on this same cache instance.
    /// </param>
    /// <param name="options">
    /// Cache configuration. Validated and copied here, so later changes to the instance do not
    /// affect this cache.
    /// </param>
    /// <remarks>
    /// The value factory is expected to be independent of this cache instance. Re-entering this same
    /// cache from inside the factory is unsupported and deadlocks if the factory awaits the nested read.
    /// </remarks>
    public ProactiveAsyncCache(Func<CancellationToken, Task<T>> valueFactory, ProactiveAsyncCacheOptions options)
    {
        Argument.NotNull(valueFactory);
        Argument.NotNull(options);
        Argument.GreaterThan(options.RefreshInterval, TimeSpan.Zero, nameof(options.RefreshInterval));
        Argument.GreaterThanOrEqualTo(options.PreFetchOffset, TimeSpan.Zero, nameof(options.PreFetchOffset));
        Argument.LessThan(options.PreFetchOffset, options.RefreshInterval, nameof(options.PreFetchOffset));

        if (options.IdleTimeout.HasValue)
        {
            Argument.GreaterThan(options.IdleTimeout.Value, TimeSpan.Zero, nameof(options.IdleTimeout));
        }

        if (options.FetchTimeout.HasValue)
        {
            Argument.GreaterThan(options.FetchTimeout.Value, TimeSpan.Zero, nameof(options.FetchTimeout));
        }

        _cts = new CancellationTokenSource();
        _lock = new object();
        _timeProvider = options.TimeProvider ?? TimeProvider.System;
        _fetchFunc = valueFactory;
        _refreshInterval = options.RefreshInterval;
        _preFetchOffset = options.PreFetchOffset;
        _retryDelay = CalculateRetryDelay(options.RefreshInterval, options.PreFetchOffset);
        _staleReads = options.StaleReads;
        _fetchTimeout = options.FetchTimeout;
        _idleTracker = options.IdleTimeout.HasValue ? new IdleTracker(_timeProvider, options.IdleTimeout.Value) : null;
        _backgroundTask = Task.Run(BackgroundRefreshAsync);
    }

    /// <inheritdoc/>
    public bool HasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);
            return _state.Snapshot is not null;
        }
    }

    /// <inheritdoc/>
    public DateTime? Expiration
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);
            return _state.Snapshot?.ExpiresAt;
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
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);
            return _state.Failure?.Exception;
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
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);
            return _state.Failure?.ConsecutiveCount ?? 0;
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
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);
            return _state.Snapshot?.CreatedAt;
        }
    }

    /// <inheritdoc/>
    public ValueTask<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);

        DateTime now = UtcNow;

        // Record the access before looking at the snapshot, so the background loop's idle deadline
        // reflects this call whether or not the cached value turns out to be fresh. Skipped
        // entirely when no idle timeout is configured, so the default read path stays a single
        // volatile reference read with no interlocked operation and no shared cache line to dirty.
        if (_idleTracker is not null)
        {
            _idleTracker.RecordAccess(now);
        }

        // Hot path: snapshot is fresh, no lock needed.
        // _state is volatile so the reference read is immediately visible across threads, and the
        // whole state is immutable, so this single read gives a consistent view.
        CacheSnapshot? snapshot = _state.Snapshot;
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

    // True when an expired value may still be handed to a reader: the policy permits it, and the
    // value has not aged past the policy's bound. Staleness is measured from expiration, not from
    // production, so the oldest value a reader can see is refreshInterval + MaxStaleness.
    private bool CanServeStale(CacheSnapshot snapshot)
    {
        if (!_staleReads.ServesStale)
        {
            return false;
        }

        return _staleReads.MaxStaleness is not TimeSpan maxStaleness
            || UtcNow - snapshot.ExpiresAt <= maxStaleness;
    }

    // Slow path: no value yet, or the value has expired.
    private async ValueTask<T> FetchValueAsync(CacheSnapshot? snapshot, CancellationToken cancellationToken)
    {
        // Snapshot is absent or expired — get or start a fetch.
        Task<CacheSnapshot> fetchTask = GetOrCreateFetchTask();

        if (snapshot is not null && CanServeStale(snapshot))
        {
            // Serve the expired value immediately while the refresh runs in the background, so
            // readers never block after the initial fetch even when the factory is slow or failing.
            // If the factory completed synchronously (e.g. Task.FromResult), the fetch task is
            // already done and a fresher value is available — prefer it over the stale one.
            return fetchTask.IsCompletedSuccessfully ? fetchTask.Result.Value : snapshot.Value;
        }

        // Either stale reads are off, or this value has aged past the configured bound. Falling
        // through means waiting for the refresh, which is also what surfaces a persistent failure
        // to the caller instead of hiding it behind an ever-older value.

        // Either no value at all (first call) or stale reads are disabled: wait for the fetch.
        // WaitAsync attaches caller cancellation without cancelling the underlying factory call,
        // so other callers sharing the same fetch task are unaffected.
        try
        {
            CacheSnapshot result = await fetchTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            return result.Value;
        }
        catch (OperationCanceledException) when (Volatile.Read(ref _isDisposed) != 0 && !cancellationToken.IsCancellationRequested)
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
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
            return;
        }

        // Cancel runs any cancellation callbacks the factory registered on the token it was
        // handed; a throwing callback is surfaced by Cancel as an AggregateException and must
        // not prevent disposal from completing.
        try
        {
            _cts.Cancel();
        }
        catch
        {
            // Intentionally swallowed — DisposeAsync must never throw.
        }

        // Capture _pendingFetch under lock. After _isDisposed = 1, GetOrCreateFetchTask
        // throws ObjectDisposedException under the lock, so no new fetch can be created.
        // This single read therefore captures the last possible in-flight fetch.
        //
        // _backgroundTask is always non-null (set in constructor) and does not need to be
        // read under lock — it is never reassigned after construction.
        Task<CacheSnapshot>? pendingFetch;
        lock (_lock)
        {
            pendingFetch = _pendingFetch;
        }

        // Wait for the background loop to exit. When the background loop is itself awaiting
        // a fetch (not sleeping on the refresh timer), this implicitly drains that fetch too.
        try
        {
            await _backgroundTask.ConfigureAwait(false);
        }
        catch
        {
            // Swallow — the loop is internally defensive and should not fault, but we must
            // never throw from DisposeAsync.
        }

        // Drain any independently-created fetch (e.g. one started by GetValueAsync while
        // the background loop was sleeping on the refresh timer). After _backgroundTask
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

        _cts.Dispose();
    }

    // Returns a task representing an in-progress or newly started fetch. Callers should
    // await the returned task to get the refreshed snapshot.
    //
    // backgroundRefresh = false (GetValueAsync): fresh if now < expiresAt
    // backgroundRefresh = true  (background loop): fresh if now < expiresAt - preFetchOffset
    //   The tighter threshold prevents the background from duplicating a fetch that
    //   GetValueAsync just performed while the loop was sleeping.
    // The flag also exempts the loop from the reader-side failure backoff below: the loop already
    // paces its own retries with _retryDelay, so suppressing it there would only stall it further.
    private Task<CacheSnapshot> GetOrCreateFetchTask(bool backgroundRefresh = false)
    {
        TaskCompletionSource<CacheSnapshot> tcs;

        lock (_lock)
        {
            // Re-check disposal under lock — closes the window between the caller's
            // initial disposal check and acquiring the lock.
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);

            // Join an existing in-progress fetch.
            if (_pendingFetch is not null && !_pendingFetch.IsCompleted)
            {
                return _pendingFetch;
            }

            // Observe any completed faulted fetch to suppress UnobservedTaskException.
            // (The ContinueWith below handles the common case; this covers any gap.)
            if (_pendingFetch is { IsFaulted: true })
            {
                _ = _pendingFetch.Exception;
            }

            // Re-check freshness under lock — a concurrent thread may have completed a
            // fetch between the caller's outer check and acquiring the lock.
            CacheState state = _state;
            CacheSnapshot? snapshot = state.Snapshot;
            TimeSpan freshThreshold = backgroundRefresh ? _preFetchOffset : TimeSpan.Zero;
            if (snapshot is not null && UtcNow < snapshot.ExpiresAt - freshThreshold)
            {
                return Task.FromResult(snapshot);
            }

            // Negative caching. A faulted fetch is a completed task and so is never joined, which
            // leaves every read free to start a fresh factory call: a dependency failing fast would
            // then be retried once per read, hammering a service that is very likely failing fast
            // because it is already overloaded. Within _retryDelay of a failure the stored
            // exception is replayed instead.
            FetchFailure? failure = state.Failure;
            if (!backgroundRefresh && failure is not null && UtcNow - failure.FailedAt < _retryDelay)
            {
                Task<CacheSnapshot> suppressed = Task.FromException<CacheSnapshot>(failure.Exception);

                // Observe it here: a stale-read caller discards this task without awaiting it, and
                // an unobserved faulted task would raise UnobservedTaskException on finalization.
                _ = suppressed.Exception;

                return suppressed;
            }

            // Publish a TCS task as _pendingFetch *before* invoking the factory. This
            // closes the synchronous re-entrancy hole: if the factory's synchronous prologue
            // calls back into GetValueAsync (and therefore GetOrCreateFetchTask), the lock is
            // reentrant on the same thread and the reentrant call will find _pendingFetch
            // already set, returning this task instead of starting a new recursive fetch.
            // Awaiting GetValueAsync on this same cache from inside the factory is still
            // unsupported and may deadlock; that misuse is documented on the constructor.
            tcs = new TaskCompletionSource<CacheSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingFetch = tcs.Task;

            // Proactively observe any fault so UnobservedTaskException never fires, even
            // when a stale-read caller discards the fetch task reference and the factory
            // later fails.
            _ = _pendingFetch.ContinueWith(
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
            T value = await InvokeFactoryAsync().ConfigureAwait(false);

            // Clamp expiration to DateTime.MaxValue to avoid overflow when refreshInterval
            // is very large (e.g. TimeSpan.FromDays(1000)).
            DateTime now = UtcNow;
            DateTime expiration = _refreshInterval >= DateTime.MaxValue - now
                ? DateTime.MaxValue
                : now + _refreshInterval;

            CacheSnapshot snapshot = new CacheSnapshot(value, now, expiration);

            // Volatile write — immediately visible on the hot path. Success clears any recorded
            // failure, so the backoff and the failure counters reset together.
            _state = new CacheState(snapshot, null);
            _ = tcs.TrySetResult(snapshot);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Disposal, not a factory failure — deliberately not recorded as one.
            _ = tcs.TrySetCanceled(_cts.Token);
        }
        catch (Exception ex)
        {
            // Everything else, including a fetch timeout, counts as a failure: it feeds the
            // reader-side backoff and the diagnostics alike.
            RecordFailure(ex, tcs);
        }
    }

    // Invokes the value factory, bounded by the configured fetch timeout if there is one.
    private async Task<T> InvokeFactoryAsync()
    {
        if (_fetchTimeout is not TimeSpan fetchTimeout)
        {
            Task<T> untimedTask = _fetchFunc(_cts.Token)
                ?? throw new InvalidOperationException("The value factory returned a null task.");
            return await untimedTask.ConfigureAwait(false);
        }

        // Two sources rather than CancelAfter: the timeout must run on the cache's TimeProvider so
        // it is testable and consistent with every other deadline here, and CancelAfter always uses
        // the system clock. The linked source lets disposal cancel the factory as well.
        using CancellationTokenSource timeoutSource = new CancellationTokenSource(fetchTimeout, _timeProvider);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutSource.Token);

        try
        {
            Task<T> fetchTask = _fetchFunc(linkedSource.Token)
                ?? throw new InvalidOperationException("The value factory returned a null task.");
            return await fetchTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !_cts.IsCancellationRequested)
        {
            // Report a timeout as a timeout, not as a cancellation the caller never asked for.
            throw new TimeoutException(
                $"The value factory did not complete within the configured fetch timeout of {fetchTimeout}.");
        }
    }

    private void RecordFailure(Exception exception, TaskCompletionSource<CacheSnapshot> tcs)
    {
        // Keep whatever value we already had; only the failure part of the state changes.
        CacheState previous = _state;
        int consecutive = (previous.Failure?.ConsecutiveCount ?? 0) + 1;
        _state = previous with { Failure = new FetchFailure(exception, UtcNow, consecutive) };

        _ = tcs.TrySetException(exception);
    }

    private async Task BackgroundRefreshAsync()
    {
        // Phase 1: initial fetch.
        // Retry until a value is obtained or the cache is disposed. A valid snapshot is
        // required before the timed refresh loop can compute meaningful delays.
        while (_state.Snapshot is null)
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
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (Volatile.Read(ref _isDisposed) != 0)
            {
                return;
            }
            catch
            {
                // Transient factory failure; wait before retrying to avoid tight-looping.
                try
                {
                    await Task.Delay(Clamp(_retryDelay), _timeProvider, _cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        // Phase 2: periodic refresh.
        // Sleep until just before the current snapshot expires, then pre-fetch.
        while (!_cts.Token.IsCancellationRequested)
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
                CacheSnapshot? snapshot = _state.Snapshot;
                if (snapshot is not null)
                {
                    TimeSpan delay = snapshot.ExpiresAt - _preFetchOffset - UtcNow;

                    // Wake at the earlier of the pre-fetch point and the idle deadline, so the idle
                    // check below can park the loop and release its timer the moment the cache falls
                    // out of use rather than at the end of the interval.
                    // TimeUntilIdle is zero exactly when IsIdle is true, so clamping only to a
                    // strictly positive value keeps this from ever collapsing into a spin.
                    TimeSpan? untilIdle = _idleTracker?.TimeUntilIdle();
                    if (untilIdle is TimeSpan remaining && remaining > TimeSpan.Zero && remaining < delay)
                    {
                        delay = remaining;
                    }

                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(Clamp(delay), _timeProvider, _cts.Token).ConfigureAwait(false);
                    }
                }

                // The cache may have fallen idle while we slept: skip this refresh and let the top
                // of the next iteration suspend the loop.
                if (_idleTracker?.IsIdle() is true)
                {
                    continue;
                }

                // Re-read after sleeping: a concurrent GetValueAsync may have refreshed
                // the value while we were waiting, making our pre-fetch unnecessary.
                snapshot = _state.Snapshot;
                if (snapshot is not null && UtcNow < snapshot.ExpiresAt - _preFetchOffset)
                {
                    continue;
                }

                _ = await GetOrCreateFetchTask(backgroundRefresh: true).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (Volatile.Read(ref _isDisposed) != 0)
            {
                break;
            }
            catch
            {
                // Transient factory failure; wait before retrying to avoid tight-looping
                // when the snapshot has already expired.
                try
                {
                    await Task.Delay(Clamp(_retryDelay), _timeProvider, _cts.Token).ConfigureAwait(false);
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
        return _idleTracker is null
            || await _idleTracker.WaitWhileIdleAsync(_cts.Token).ConfigureAwait(false);
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    // Task.Delay internally converts TimeSpan to int milliseconds; clamp to avoid overflow
    // for refresh intervals longer than ~24.8 days. When the delay fires early, the loop
    // re-reads the snapshot and recomputes — it simply sleeps again and converges correctly.
    private static readonly TimeSpan s_maxDelay = TimeSpan.FromMilliseconds(int.MaxValue - 1);

    private readonly CancellationTokenSource _cts;
    private readonly object _lock;
    private readonly TimeProvider _timeProvider;
    private readonly Func<CancellationToken, Task<T>> _fetchFunc;
    private readonly TimeSpan _refreshInterval;
    private readonly TimeSpan _preFetchOffset;
    private readonly TimeSpan _retryDelay;
    private readonly StaleReadPolicy _staleReads;
    private readonly TimeSpan? _fetchTimeout;
    private readonly IdleTracker? _idleTracker;
    private readonly Task _backgroundTask;

    // Everything the cache knows, in one immutable object published by a single reference swap.
    // Volatile: the hot path in GetValueAsync (and HasValue/Expiration) reads it outside any lock,
    // and because CacheState and everything it holds are immutable records, volatile on the
    // reference alone is enough — readers always see a fully constructed, consistent object.
    //
    // Never null, so the read path needs no null check of its own. Only CompleteAsync writes it,
    // and those writes are serialised: a new fetch cannot be created until the previous one has
    // completed, and the state is published before the task completes. A plain store is therefore
    // sufficient even for the consecutive-failure count, which needs no interlocked increment.
    private volatile CacheState _state = new CacheState(null, null);

    // Written and read only under _lock.
    private Task<CacheSnapshot>? _pendingFetch;

    private int _isDisposed;

    private sealed record CacheState(CacheSnapshot? Snapshot, FetchFailure? Failure);

    // CreatedAt is stored rather than derived from ExpiresAt: expiration is clamped to
    // DateTime.MaxValue for very large refresh intervals, so subtracting the interval back off it
    // would not recover the real production time.
    private sealed record CacheSnapshot(T Value, DateTime CreatedAt, DateTime ExpiresAt);

    private sealed record FetchFailure(Exception Exception, DateTime FailedAt, int ConsecutiveCount);
}
