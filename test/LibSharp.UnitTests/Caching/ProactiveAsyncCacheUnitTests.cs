// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class ProactiveAsyncCacheUnitTests
{
    // ── Constructor validation ────────────────────────────────────────────

    [TestMethod]
    public void Constructor_ThrowsOnNullFactory()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ProactiveAsyncCache<int>(null, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
        });
    }

    [TestMethod]
    public void Constructor_ThrowsOnZeroRefreshInterval()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.Zero, TimeSpan.Zero);
        });
    }

    [TestMethod]
    public void Constructor_ThrowsOnNegativePreFetchOffset()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(-1));
        });
    }

    [TestMethod]
    public void Constructor_ThrowsWhenPreFetchOffsetExceedsRefreshInterval()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
        });
    }

    [TestMethod]
    public async Task Constructor_WithSmallestValidRefreshWindow_ClampsRetryDelayToPositive()
    {
        // Arrange — refreshInterval=2 ticks, preFetchOffset=1 tick; (2-1)/2=0, must clamp to 1.
        TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            _ => tcs.Task,
            TimeSpan.FromTicks(2),
            TimeSpan.FromTicks(1));
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        FieldInfo retryDelayField = typeof(ProactiveAsyncCache<int>).GetField("m_retryDelay", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(retryDelayField, "Could not find m_retryDelay field.");
        TimeSpan retryDelay = (TimeSpan)retryDelayField.GetValue(cache);

        Assert.IsTrue(retryDelay > TimeSpan.Zero, $"Expected a positive retry delay, but got {retryDelay}.");

        tcs.SetResult(0);
    }

    // ── HasValue / Expiration ─────────────────────────────────────────────

    [TestMethod]
    public async Task HasValue_ReturnsFalseBeforeFirstFetch()
    {
        // Arrange — factory blocks so HasValue is guaranteed false on the first read.
        TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            _ => tcs.Task,
            TimeSpan.FromHours(1),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        Assert.IsFalse(cache.HasValue);

        tcs.SetResult(42);
    }

    [TestMethod]
    public async Task Expiration_ReturnsNullBeforeFirstFetch()
    {
        // Arrange — factory blocks so Expiration is guaranteed null on the first read.
        TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            _ => tcs.Task,
            TimeSpan.FromHours(1),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        Assert.IsNull(cache.Expiration);

        tcs.SetResult(42);
    }

    [TestMethod]
    public async Task HasValue_ReturnsTrueAfterFetch()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act
        _ = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.IsTrue(cache.HasValue);
    }

    [TestMethod]
    public async Task Expiration_ReturnsValueAfterFetch()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act
        _ = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(cache.Expiration);
        Assert.IsTrue(cache.Expiration > DateTime.UtcNow);
    }

    [TestMethod]
    public async Task Expiration_WithVeryLargeRefreshInterval_ClampsToDateTimeMaxValue()
    {
        // Arrange
        TimeSpan refreshInterval = DateTime.MaxValue - DateTime.UtcNow;
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), refreshInterval, TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act
        int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(42, value);
        Assert.AreEqual(DateTime.MaxValue, cache.Expiration);
    }

    [TestMethod]
    public async Task HasValue_ThrowsWhenDisposed()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
        await cache.DisposeAsync().ConfigureAwait(false);

        // Act & Assert
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.HasValue);
    }

    [TestMethod]
    public async Task Expiration_ThrowsWhenDisposed()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
        await cache.DisposeAsync().ConfigureAwait(false);

        // Act & Assert
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.Expiration);
    }

    // ── GetValueAsync ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetValueAsync_ReturnsValueFromFactory()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act
        int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public async Task GetValueAsync_ReturnsCachedValueOnSubsequentCalls()
    {
        // Arrange
        int callCount = 0;
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int result = Interlocked.Increment(ref callCount);
                return Task.FromResult(result);
            },
            TimeSpan.FromHours(1),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act
        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        int second = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, first);
        Assert.AreEqual(1, second);
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task GetValueAsync_WithReferenceType_ReturnsValue()
    {
        // Arrange
        ProactiveAsyncCache<string> cache = new ProactiveAsyncCache<string>(_ => Task.FromResult("hello"), TimeSpan.FromHours(1), TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act
        string value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual("hello", value);
    }

    [TestMethod]
    public async Task GetValueAsync_ThrowsWhenDisposed()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
        await cache.DisposeAsync().ConfigureAwait(false);

        // Act & Assert
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            async () => await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallers_ShareSingleFetch()
    {
        // Arrange
        int callCount = 0;
        TaskCompletionSource<int> fetchTcs = new TaskCompletionSource<int>();

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                _ = Interlocked.Increment(ref callCount);
                return fetchTcs.Task;
            },
            TimeSpan.FromHours(1),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act — start multiple concurrent calls
        Task<int> task1 = cache.GetValueAsync(TestContext.CancellationToken);
        Task<int> task2 = cache.GetValueAsync(TestContext.CancellationToken);
        Task<int> task3 = cache.GetValueAsync(TestContext.CancellationToken);

        // Complete the single shared fetch
        fetchTcs.SetResult(42);
        int[] results = await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);

        // Assert — factory called exactly once
        Assert.AreEqual(42, results[0]);
        Assert.AreEqual(42, results[1]);
        Assert.AreEqual(42, results[2]);
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task GetValueAsync_CallerCancellation_DoesNotCancelFetch()
    {
        // Arrange
        TaskCompletionSource<int> fetchTcs = new TaskCompletionSource<int>();

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct => fetchTcs.Task,
            TimeSpan.FromHours(1),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        using CancellationTokenSource callerCts = new CancellationTokenSource();

        // Act — start a fetch and then cancel the caller's token
        Task<int> getTask = cache.GetValueAsync(callerCts.Token);
        callerCts.Cancel();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => getTask).ConfigureAwait(false);

        // Complete the underlying fetch (it was NOT cancelled)
        fetchTcs.SetResult(42);

        // Allow the async continuation to run
        await Task.Yield();

        // Assert — a new caller gets the completed value
        int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public async Task GetValueAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        TaskCompletionSource<int> fetchTcs = new TaskCompletionSource<int>();

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct => fetchTcs.Task,
            TimeSpan.FromHours(1),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => cache.GetValueAsync(cts.Token)).ConfigureAwait(false);

        // Clean up the pending fetch
        fetchTcs.SetResult(42);
    }

    [TestMethod]
    public async Task GetValueAsync_AllowStaleReads_Disabled_BlocksOnExpiredValue()
    {
        // Arrange — default mode (allowStaleReads: false)
        int callCount = 0;
        TaskCompletionSource<int> secondFetchTcs = new TaskCompletionSource<int>();

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                if (count == 1)
                {
                    return Task.FromResult(100);
                }

                return secondFetchTcs.Task;
            },
            TimeSpan.FromMilliseconds(50),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // First call
        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(100, first);

        // Wait for expiration
        await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);

        // Second call — should block because allowStaleReads is false
        Task<int> blockedReader = cache.GetValueAsync(TestContext.CancellationToken);
        Assert.IsFalse(blockedReader.IsCompleted, "Reader should block when allowStaleReads is false.");

        // Complete the fetch
        secondFetchTcs.SetResult(200);
        int refreshed = await blockedReader.ConfigureAwait(false);
        Assert.AreEqual(200, refreshed);
    }

    [TestMethod]
    public async Task GetValueAsync_ReturnsStaleValueWhileRefreshing()
    {
        // Arrange
        int callCount = 0;
        TaskCompletionSource<int> secondFetchTcs = new TaskCompletionSource<int>();

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                if (count == 1)
                {
                    return Task.FromResult(100);
                }

                // Second fetch is slow — controlled by TCS
                return secondFetchTcs.Task;
            },
            TimeSpan.FromMilliseconds(50),
            TimeSpan.Zero,
            allowStaleReads: true);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act — get the first value
        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(100, first);

        // Wait for expiration
        await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);

        // Get the value again — should return the stale value immediately,
        // not block on the slow second fetch
        int stale = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(100, stale);

        // Complete the background fetch
        secondFetchTcs.SetResult(200);
        await Task.Yield();

        // Now the refreshed value should be available
        int refreshed = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(200, refreshed);
    }

    [TestMethod]
    public async Task GetValueAsync_AllowStaleReads_ReturnsFreshValueWhenRefreshAlreadyCompleted()
    {
        // Arrange
        int callCount = 0;

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct => Task.FromResult(Interlocked.Increment(ref callCount) * 100),
            TimeSpan.FromMilliseconds(50),
            TimeSpan.Zero,
            allowStaleReads: true);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(100, first);

        await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);

        // Act — the synchronous factory lets the refresh complete before GetValueAsync
        // reaches the stale-read branch.
        int refreshed = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(200, refreshed);
    }

    [TestMethod]
    public async Task GetValueAsync_ReturnsStaleValueWhenFactoryFails()
    {
        // Arrange
        int callCount = 0;
        using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                _ = fetchSignal.Release();

                if (count >= 2)
                {
                    throw new InvalidOperationException("Factory error");
                }

                return Task.FromResult(42);
            },
            TimeSpan.FromMilliseconds(50),
            TimeSpan.Zero,
            allowStaleReads: true);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act — get the first value
        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(42, first);
        _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);

        // Wait for expiration
        await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);

        // Get the value again — should return the stale value, not throw
        int stale = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(42, stale);
    }

    [TestMethod]
    public async Task GetValueAsync_SlowFactory_ReadersNeverBlock()
    {
        // Simulates refreshInterval=2s, preFetchOffset=1s with a factory that
        // sometimes takes longer than the pre-fetch window.
        int callCount = 0;
        TaskCompletionSource<int> slowFetchTcs = new TaskCompletionSource<int>();

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                if (count == 2)
                {
                    // Second fetch is very slow — simulates Cosmos DB latency spike
                    return slowFetchTcs.Task;
                }

                return Task.FromResult(count * 10);
            },
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(50),
            allowStaleReads: true);

        try
        {
            // First fetch
            int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(10, first);

            // Wait past expiration (background pre-fetch triggers at ~50ms but is slow)
            await Task.Delay(120, TestContext.CancellationToken).ConfigureAwait(false);

            // Reader should get the stale value immediately, not block
            Task<int> readerTask = cache.GetValueAsync(TestContext.CancellationToken);
            Assert.IsTrue(readerTask.IsCompleted, "Reader should not block when stale value is available.");
            int stale = await readerTask.ConfigureAwait(false);
            Assert.AreEqual(10, stale);

            // Complete the slow fetch
            slowFetchTcs.SetResult(20);
            await Task.Yield();

            // Now the fresh value should be available
            int fresh = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(20, fresh);
        }
        finally
        {
            await cache.DisposeAsync().ConfigureAwait(false);
        }
    }

    // ── Disposal ──────────────────────────────────────────────────────────

    [TestMethod]
    public async Task DisposeAsync_CanBeCalledSafely()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

        // Act
        await cache.DisposeAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DisposeAsync_CanBeCalledTwice()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

        // Act — should not throw
        await cache.DisposeAsync().ConfigureAwait(false);
        await cache.DisposeAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DisposeAsync_WithBackgroundTaskRunning()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
        _ = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Act — should stop the background task cleanly
        await cache.DisposeAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DisposeAsync_WithBackgroundTaskRunning_CompletesCleanly()
    {
        // Arrange — exercises the DisposeAsync() path with an active background task.
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            _ => Task.FromResult(42),
            TimeSpan.FromHours(1),
            TimeSpan.Zero);
        _ = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Act — async dispose should cancel and wait for the background task
        await cache.DisposeAsync().ConfigureAwait(false);

        // Assert — accessing the cache after DisposeAsync should throw
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.HasValue);
    }

    [TestMethod]
    public async Task DisposeAsync_WhileCallerIsAwaitingFetch()
    {
        // Arrange
        TaskCompletionSource<int> fetchTcs = new TaskCompletionSource<int>();

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                // Register cancellation so the fetch completes when disposed
                _ = ct.Register(() => fetchTcs.TrySetCanceled(ct));
                return fetchTcs.Task;
            },
            TimeSpan.FromHours(1),
            TimeSpan.Zero);

        // Start a GetValueAsync that will block on the slow factory
        Task<int> getTask = cache.GetValueAsync(TestContext.CancellationToken);

        // Act — dispose while the caller is still waiting
        await cache.DisposeAsync().ConfigureAwait(false);

        // Assert — the caller observes ObjectDisposedException because the cache was disposed
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => getTask).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DisposeAsync_WhileInFlightFetchIsRunning_WaitsForFetchToComplete()
    {
        // Verifies the m_pendingFetch drain path in DisposeAsync. The background loop starts
        // an initial fetch immediately; if the factory ignores cancellation, DisposeAsync
        // must block until the factory completes rather than returning while m_snapshot is
        // still being mutated.
        using SemaphoreSlim factoryStarted = new SemaphoreSlim(0, 1);
        TaskCompletionSource<int> factoryTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        int factoryCompleteCount = 0;

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            async ct =>
            {
                _ = factoryStarted.Release();
                // Deliberately ignore ct: the factory must complete after DisposeAsync
                // is blocking on m_pendingFetch, regardless of CTS cancellation.
                int value = await factoryTcs.Task.ConfigureAwait(false);
                _ = Interlocked.Increment(ref factoryCompleteCount);
                return value;
            },
            TimeSpan.FromHours(1),
            TimeSpan.Zero);

        // Wait for the background loop's initial fetch to start.
        await factoryStarted.WaitAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Start disposal — it must block waiting for the in-flight fetch.
        Task disposeTask = cache.DisposeAsync().AsTask();

        // Give disposal time to reach its await on m_pendingFetch. Without the drain,
        // it would complete immediately (<1ms); with it, it must block here.
        await Task.Delay(50, TestContext.CancellationToken).ConfigureAwait(false);
        Assert.IsFalse(disposeTask.IsCompleted, "DisposeAsync should be blocked waiting for the in-flight fetch.");

        // Unblock the factory — disposal can now drain and complete.
        factoryTcs.SetResult(42);
        await disposeTask.ConfigureAwait(false);

        // Assert — factory ran and wrote its result before DisposeAsync returned.
        Assert.AreEqual(1, factoryCompleteCount);
    }

    // ── Background refresh ────────────────────────────────────────────────

    [TestMethod]
    public async Task BackgroundRefresh_RefreshesValueBeforeExpiration()
    {
        // Arrange
        int callCount = 0;
        using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int result = Interlocked.Increment(ref callCount);
                _ = fetchSignal.Release();
                return Task.FromResult(result);
            },
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(20));

        try
        {
            // Act — trigger the first fetch
            int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(1, first);

            // Consume the signal from the first fetch
            _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);

            // Wait for the background pre-fetch to trigger
            bool refreshed = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);
            Assert.IsTrue(refreshed, "Background refresh did not occur within the expected time.");

            // Assert — value should have been refreshed. The factory uses Task.FromResult
            // (synchronous), so FetchAndUpdateAsync writes m_snapshot before the semaphore
            // waiter is scheduled. The snapshot is already updated when WaitAsync returns.
            int second = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(2, second);
        }
        finally
        {
            await cache.DisposeAsync().ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task BackgroundRefresh_WithPreFetchOffset_RefreshesEarly()
    {
        // Arrange
        int callCount = 0;
        using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

        // refreshInterval = 200ms, preFetchOffset = 100ms → background fires at ~100ms
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int result = Interlocked.Increment(ref callCount);
                _ = fetchSignal.Release();
                return Task.FromResult(result);
            },
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(100));

        try
        {
            // Trigger the first fetch
            int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(1, first);
            _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);

            // The background refresh should fire ~200ms after the first fetch
            DateTime beforeRefresh = DateTime.UtcNow;
            bool refreshed = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);
            Assert.IsTrue(refreshed, "Background refresh did not occur.");

            // The refresh should happen well before the 600ms expiration
            TimeSpan elapsed = DateTime.UtcNow - beforeRefresh;
            Assert.IsLessThan(TimeSpan.FromMilliseconds(500), elapsed, $"Refresh took too long: {elapsed.TotalMilliseconds}ms.");
        }
        finally
        {
            await cache.DisposeAsync().ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task BackgroundRefresh_FactoryThrows_CacheKeepsRunning()
    {
        // Arrange
        int callCount = 0;
        using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                _ = fetchSignal.Release();

                if (count == 2)
                {
                    throw new InvalidOperationException("Transient failure in background");
                }

                return Task.FromResult(count);
            },
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(20));

        try
        {
            // Trigger the first fetch
            int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(1, first);
            _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);

            // The background will fail on the second fetch — wait for it
            _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);

            // Wait for the background to retry (third fetch) which should succeed.
            // Retry delay = (refreshInterval - preFetchOffset) / 2 = 40ms.
            bool retried = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(10), TestContext.CancellationToken).ConfigureAwait(false);
            Assert.IsTrue(retried, "Background did not retry after transient failure.");

            // Allow snapshot update
            await Task.Delay(30, TestContext.CancellationToken).ConfigureAwait(false);

            int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(3, value);
        }
        finally
        {
            await cache.DisposeAsync().ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task BackgroundRefresh_SkipsWhenValueIsFresh()
    {
        // Arrange
        int callCount = 0;

        // refreshInterval = 200ms, preFetchOffset = 40ms → background fires at ~160ms.
        // But GetValueAsync will keep the value fresh, so the background should skip.
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int result = Interlocked.Increment(ref callCount);
                return Task.FromResult(result);
            },
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(40));

        try
        {
            // Trigger the first fetch
            int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(1, first);

            // Wait until just before the background fires at ~160ms
            await Task.Delay(120, TestContext.CancellationToken).ConfigureAwait(false);

            // Wait for background refresh to complete and value to be updated
            await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);
            int refreshed = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(2, refreshed);

            // Wait a bit and verify no extra fetches happened
            await Task.Delay(50, TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(2, callCount);
        }
        finally
        {
            await cache.DisposeAsync().ConfigureAwait(false);
        }
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task BackgroundRefresh_InitialFetchFails_RetriesUntilSuccess()
    {
        // Arrange — first call fails, second succeeds. Exercises the initial-fetch
        // retry loop in StartBackgroundRefresh.
        int callCount = 0;
        TaskCompletionSource firstValueReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                if (count == 1)
                {
                    throw new InvalidOperationException("Initial fetch failure");
                }

                _ = firstValueReady.TrySetResult();
                return Task.FromResult(99);
            },
            // Retry delay = (refreshInterval - preFetchOffset) / 2 = 40ms.
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(20));

        try
        {
            // Wait for the retry to succeed.
            await firstValueReady.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.CancellationToken).ConfigureAwait(false);

            // The value should now be available
            int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(99, value);
            Assert.IsGreaterThanOrEqualTo(2, callCount);
        }
        finally
        {
            await cache.DisposeAsync().ConfigureAwait(false);
        }
    }

    public TestContext TestContext { get; set; }
}
