// Copyright (c) 2026 Danylo Fitel

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.Extensions.Time.Testing;
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
            _ = new ProactiveAsyncCache<int>(null!, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
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

        FieldInfo? retryDelayField = typeof(ProactiveAsyncCache<int>).GetField("m_retryDelay", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(retryDelayField, "Could not find m_retryDelay field.");
        TimeSpan retryDelay = (TimeSpan)retryDelayField!.GetValue(cache)!;

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
    public async Task GetValueAsync_FactoryReturningNullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => (Task<int>)null!, TimeSpan.FromHours(1), TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act & Assert
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => cache.GetValueAsync(TestContext.CancellationToken)).ConfigureAwait(false);
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
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task GetValueAsync_WithFakeTimeProvider_AllowStaleReads_Disabled_BlocksOnExpiredValue()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;
        TaskCompletionSource<int> secondFetchTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            TimeSpan.FromMinutes(1),
            TimeSpan.Zero,
            timeProvider: timeProvider);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(100, first);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        Task<int> blockedReader = cache.GetValueAsync(TestContext.CancellationToken);
        Assert.IsFalse(blockedReader.IsCompleted, "Reader should block when allowStaleReads is false.");

        secondFetchTcs.SetResult(200);
        int refreshed = await blockedReader.ConfigureAwait(false);
        Assert.AreEqual(200, refreshed);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task GetValueAsync_WithFakeTimeProvider_ReturnsStaleValueWhileRefreshing()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;
        TaskCompletionSource<int> secondFetchTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            TimeSpan.FromMinutes(1),
            TimeSpan.Zero,
            allowStaleReads: true,
            timeProvider: timeProvider);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(100, first);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        int stale = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(100, stale);

        secondFetchTcs.SetResult(200);
        await WaitUntilAsync(
            async () => await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false) == 200,
            TestContext.CancellationToken,
            "Expected the refreshed value to become visible after the second fetch completed.").ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task GetValueAsync_WithFakeTimeProvider_AllowStaleReads_ReturnsFreshValueWhenRefreshAlreadyCompleted()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;
        TaskCompletionSource secondFetchCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            (CancellationToken ct) =>
            {
                int value = Interlocked.Increment(ref callCount) * 100;
                if (value == 200)
                {
                    _ = secondFetchCompleted.TrySetResult();
                }

                return Task.FromResult(value);
            },
            TimeSpan.FromMinutes(1),
            TimeSpan.Zero,
            allowStaleReads: true,
            timeProvider: timeProvider);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(100, first);

        timeProvider.Advance(TimeSpan.FromMinutes(1));
        await secondFetchCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);

        int refreshed = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(200, refreshed);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task GetValueAsync_FactoryReadsCache_JoinsPendingFetchInsteadOfRecursing()
    {
        // Regression test: without the TCS early-publish fix, a factory that called
        // GetValueAsync in its synchronous prologue (before returning its Task) would
        // find m_pendingFetch unset, start a second CompleteAsync, and recurse
        // until a StackOverflowException.
        //
        // This test only covers the non-awaiting reentrant case: the factory issues the
        // nested read but returns immediately without awaiting it. The awaiting case
        // (factory awaits the nested GetValueAsync) would deadlock — that is a caller
        // bug and is intentionally out of scope.

        // Arrange
        StrongBox<ProactiveAsyncCache<int>> cacheBox = new StrongBox<ProactiveAsyncCache<int>>();
        TaskCompletionSource cacheReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int>? nestedReadTask = null;
        int callCount = 0;

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                _ = Interlocked.Increment(ref callCount);
                cacheReady.Task.GetAwaiter().GetResult();
                nestedReadTask = cacheBox.Value!.GetValueAsync(TestContext.CancellationToken);
                return Task.FromResult(42);
            },
            TimeSpan.FromHours(1),
            TimeSpan.Zero);
        cacheBox.Value = cache;
        cacheReady.SetResult();
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Act
        int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Assert — factory ran once; nested read joined the same fetch and got the same value.
        Assert.AreEqual(42, value);
        Assert.IsNotNull(nestedReadTask, "Expected the factory to issue a nested cache read.");
        Assert.AreEqual(42, await nestedReadTask.ConfigureAwait(false));
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task GetValueAsync_WithFakeTimeProvider_ReturnsStaleValueWhenFactoryFails()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                if (count >= 2)
                {
                    throw new InvalidOperationException("Factory error");
                }

                return Task.FromResult(42);
            },
            TimeSpan.FromMinutes(1),
            TimeSpan.Zero,
            allowStaleReads: true,
            timeProvider: timeProvider);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(42, first);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        int stale = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(42, stale);
        Assert.AreEqual(2, callCount);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task GetValueAsync_WithFakeTimeProvider_SlowFactory_ReadersNeverBlock()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;
        TaskCompletionSource<int> slowFetchTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                if (count == 2)
                {
                    return slowFetchTcs.Task;
                }

                return Task.FromResult(count * 10);
            },
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(50),
            allowStaleReads: true,
            timeProvider: timeProvider);

        try
        {
            int first = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(10, first);

            timeProvider.Advance(TimeSpan.FromMilliseconds(100));

            int stale = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(10, stale);
            Assert.AreEqual(2, callCount);

            slowFetchTcs.SetResult(20);
            await WaitUntilAsync(
                async () => await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false) == 20,
                TestContext.CancellationToken,
                "Expected readers to observe the refreshed value after the slow fetch completed.").ConfigureAwait(false);
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
    public async Task DisposeAsync_WhileCallerIsAwaitingFetch_WithDefaultToken_ThrowsObjectDisposedException()
    {
        // Arrange
        TaskCompletionSource<int> fetchTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                _ = ct.Register(() => fetchTcs.TrySetCanceled(ct));
                return fetchTcs.Task;
            },
            TimeSpan.FromHours(1),
            TimeSpan.Zero);

        Task<int> getTask = cache.GetValueAsync();

        // Act
        await cache.DisposeAsync().ConfigureAwait(false);

        // Assert
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => getTask).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task DisposeAsync_WhileInFlightFetchIsRunning_WaitsForFetchToComplete()
    {
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

        await factoryStarted.WaitAsync(TestContext.CancellationToken).ConfigureAwait(false);

        Task disposeTask = cache.DisposeAsync().AsTask();
        Assert.IsFalse(disposeTask.IsCompleted, "DisposeAsync should be blocked waiting for the in-flight fetch.");

        factoryTcs.SetResult(42);
        await disposeTask.ConfigureAwait(false);

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
            // (synchronous), so CompleteAsync writes m_snapshot before the semaphore
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
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task BackgroundRefresh_InitialFetchFails_RetriesUntilSuccess()
    {
        // Arrange — first call fails, second succeeds. Exercises the initial-fetch
        // retry loop in BackgroundRefreshAsync.
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

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task BackgroundRefresh_InitialFetchThrowsObjectDisposedException_RetriesUntilSuccess()
    {
        // Arrange
        int callCount = 0;
        TaskCompletionSource firstValueReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                int count = Interlocked.Increment(ref callCount);
                if (count == 1)
                {
                    throw new ObjectDisposedException("dependency");
                }

                _ = firstValueReady.TrySetResult();
                return Task.FromResult(99);
            },
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(20));

        try
        {
            await firstValueReady.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.CancellationToken).ConfigureAwait(false);

            int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
            Assert.AreEqual(99, value);
            Assert.IsGreaterThanOrEqualTo(2, callCount);
        }
        finally
        {
            await cache.DisposeAsync().ConfigureAwait(false);
        }
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task BackgroundRefresh_WithVeryLargeRefreshInterval_DoesNotFaultBackgroundTask()
    {
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            _ => Task.FromResult(42),
            TimeSpan.FromDays(1000),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        FieldInfo? backgroundTaskField = typeof(ProactiveAsyncCache<int>).GetField("m_backgroundTask", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(backgroundTaskField, "Could not find m_backgroundTask field.");

        int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Task backgroundTask = (Task)backgroundTaskField!.GetValue(cache)!;

        Assert.AreEqual(42, value);
        Assert.IsNotNull(backgroundTask);
        Assert.IsFalse(backgroundTask.IsFaulted, "Background task should keep running for very large refresh intervals.");
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task BackgroundRefresh_InitialFailure_WithVeryLargeRetryWindow_DoesNotFaultBackgroundTask()
    {
        using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            ct =>
            {
                _ = fetchSignal.Release();
                throw new InvalidOperationException("Initial fetch failure");
            },
            TimeSpan.FromDays(1000),
            TimeSpan.Zero);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        FieldInfo? backgroundTaskField = typeof(ProactiveAsyncCache<int>).GetField("m_backgroundTask", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(backgroundTaskField, "Could not find m_backgroundTask field.");

        bool initialFailureObserved = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);
        Task backgroundTask = (Task)backgroundTaskField!.GetValue(cache)!;

        Assert.IsTrue(initialFailureObserved, "Expected the initial background fetch to run and fail.");
        Assert.IsNotNull(backgroundTask);
        Assert.IsFalse(backgroundTask.IsFaulted, "Background task should remain active after a failed refresh with a very large retry window.");
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> conditionAsync, CancellationToken cancellationToken, string failureMessage)
    {
        for (int i = 0; i < 1000; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await conditionAsync().ConfigureAwait(false))
            {
                return;
            }

            await Task.Yield();
        }

        Assert.Fail(failureMessage);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task Constructor_WithFakeTimeProvider_ServesValue()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int calls = 0;
        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            _ => Task.FromResult(Interlocked.Increment(ref calls)),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(30),
            allowStaleReads: false,
            timeProvider: timeProvider);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        int value = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(1, value);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task GetValueAsync_WithFakeTimeProvider_AllowStaleReads_ReturnsStaleValueWhileRefreshing()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int calls = 0;
        TaskCompletionSource<int> refreshGate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
            _ => Interlocked.Increment(ref calls) == 1 ? Task.FromResult(1) : refreshGate.Task, // First fetch is fast; later fetches block.
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(10),
            allowStaleReads: true,
            timeProvider);
        await using ConfiguredAsyncDisposable d = cache.ConfigureAwait(false);

        // Initial fetch completes and serves value 1.
        int initial = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(1, initial);

        // Advance past expiration so the snapshot is stale and the next read triggers a refresh.
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        // With stale reads enabled the read returns the previous value immediately, without waiting
        // for the (still-blocked) refresh to complete.
        int stale = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        Assert.AreEqual(1, stale);

        // Unblock the refresh so disposal can drain the in-flight fetch cleanly.
        _ = refreshGate.TrySetResult(2);
    }

    public TestContext TestContext { get; set; }
}
