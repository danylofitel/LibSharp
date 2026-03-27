// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching
{
    [TestClass]
    public class ProactiveAsyncCacheUnitTests
    {
        [TestMethod]
        public void Constructor_ThrowsOnNullFactory()
        {
            _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ProactiveAsyncCache<int>(null, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10)).Dispose());
        }

        [TestMethod]
        public void Constructor_ThrowsOnZeroRefreshInterval()
        {
            _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.Zero, TimeSpan.Zero).Dispose());
        }

        [TestMethod]
        public void Constructor_ThrowsOnNegativePreFetchOffset()
        {
            _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(-1)).Dispose());
        }

        [TestMethod]
        public void Constructor_ThrowsWhenPreFetchOffsetExceedsRefreshInterval()
        {
            _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)).Dispose());
        }

        [TestMethod]
        public void Constructor_DoesNotStartBackgroundTask()
        {
            // Arrange
            int callCount = 0;
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    _ = Interlocked.Increment(ref callCount);
                    return Task.FromResult(42);
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            // Act — wait briefly to confirm nothing fires
            Thread.Sleep(50);

            // Assert
            Assert.AreEqual(0, callCount);
        }

        [TestMethod]
        public async Task Start_TriggersInitialFetch()
        {
            // Arrange
            int callCount = 0;
            using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    _ = Interlocked.Increment(ref callCount);
                    _ = fetchSignal.Release();
                    return Task.FromResult(42);
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            try
            {
                // Act
                cache.Start();
                bool fetched = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // Assert — Start should have triggered the initial fetch
                Assert.IsTrue(fetched, "Start did not trigger the initial fetch.");
                Assert.AreEqual(1, callCount);
                Assert.IsTrue(cache.HasValue);
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void HasValue_ReturnsFalseBeforeFirstFetch()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act & Assert
            Assert.IsFalse(cache.HasValue);
        }

        [TestMethod]
        public void Expiration_ReturnsNullBeforeFirstFetch()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act & Assert
            Assert.IsNull(cache.Expiration);
        }

        [TestMethod]
        public async Task GetValueAsync_ReturnsValueFromFactory()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            int value = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        public async Task GetValueAsync_ReturnsCachedValueOnSubsequentCalls()
        {
            // Arrange
            int callCount = 0;
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int result = Interlocked.Increment(ref callCount);
                    return Task.FromResult(result);
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            // Act
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            int second = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(1, first);
            Assert.AreEqual(1, second);
            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public async Task HasValue_ReturnsTrueAfterFetch()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            _ = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.IsTrue(cache.HasValue);
        }

        [TestMethod]
        public async Task Expiration_ReturnsValueAfterFetch()
        {
            // Arrange
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            _ = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.IsNotNull(cache.Expiration);
            Assert.IsTrue(cache.Expiration > DateTime.UtcNow);
        }

        [TestMethod]
        public void HasValue_ThrowsWhenDisposed()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
            cache.Dispose();

            // Act
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.HasValue);
        }

        [TestMethod]
        public void Expiration_ThrowsWhenDisposed()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
            cache.Dispose();

            // Act
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.Expiration);
        }

        [TestMethod]
        public async Task GetValueAsync_ThrowsWhenDisposed()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
            cache.Dispose();

            // Act
            _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await cache.GetValueAsync().ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DisposeAsync_CanBeCalledSafely()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            await cache.DisposeAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task GetValueAsync_WithReferenceType_ReturnsValue()
        {
            // Arrange
            using ProactiveAsyncCache<string> cache = new ProactiveAsyncCache<string>(_ => Task.FromResult("hello"), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act
            string value = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("hello", value);
        }

        [TestMethod]
        public void Start_CanBeCalledOnce()
        {
            // Arrange & Act
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
            cache.Start();
        }

        [TestMethod]
        public async Task Start_IsIdempotent()
        {
            // Arrange
            int callCount = 0;
            using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    _ = Interlocked.Increment(ref callCount);
                    _ = fetchSignal.Release();
                    return Task.FromResult(42);
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            try
            {
                // Act — calling Start multiple times should trigger exactly one fetch
                cache.Start();
                cache.Start();
                cache.Start();

                _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                await Task.Delay(50).ConfigureAwait(false);

                // Assert — factory called exactly once
                Assert.AreEqual(1, callCount);
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void Start_ThrowsWhenDisposed()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);
            cache.Dispose();

            // Act
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => cache.Start());
        }

        [TestMethod]
        public void Dispose_CanBeCalledTwice()
        {
            // Arrange
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(_ => Task.FromResult(42), TimeSpan.FromHours(1), TimeSpan.Zero);

            // Act — should not throw
            cache.Dispose();
            cache.Dispose();
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
            cache.Start();
            _ = await cache.GetValueAsync().ConfigureAwait(false);

            // Act — should stop the background task cleanly
            await cache.DisposeAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task BackgroundRefresh_RefreshesValueBeforeExpiration()
        {
            // Arrange
            int callCount = 0;
            using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int result = Interlocked.Increment(ref callCount);
                    _ = fetchSignal.Release();
                    return Task.FromResult(result);
                },
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(50));
            cache.Start();

            try
            {
                // Act — trigger the first fetch
                int first = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(1, first);

                // Consume the signal from the first fetch
                _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // Wait for the background pre-fetch to trigger
                bool refreshed = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                Assert.IsTrue(refreshed, "Background refresh did not occur within the expected time.");

                // Allow the snapshot to be updated
                await Task.Delay(50).ConfigureAwait(false);

                // Assert — value should have been refreshed
                int second = await cache.GetValueAsync().ConfigureAwait(false);
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

            // refreshInterval = 600ms, preFetchOffset = 400ms → background fires at ~200ms
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int result = Interlocked.Increment(ref callCount);
                    _ = fetchSignal.Release();
                    return Task.FromResult(result);
                },
                TimeSpan.FromMilliseconds(600),
                TimeSpan.FromMilliseconds(400));
            cache.Start();

            try
            {
                // Trigger the first fetch
                int first = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(1, first);
                _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // The background refresh should fire ~200ms after the first fetch
                DateTime beforeRefresh = DateTime.UtcNow;
                bool refreshed = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                Assert.IsTrue(refreshed, "Background refresh did not occur.");

                // The refresh should happen well before the 600ms expiration
                TimeSpan elapsed = DateTime.UtcNow - beforeRefresh;
                Assert.IsTrue(elapsed < TimeSpan.FromMilliseconds(500), $"Refresh took too long: {elapsed.TotalMilliseconds}ms.");
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task GetValueAsync_ReturnsStaleValueWhileRefreshing()
        {
            // Arrange
            int callCount = 0;
            TaskCompletionSource<int> secondFetchTcs = new TaskCompletionSource<int>();

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 1)
                    {
                        return Task.FromResult(100);
                    }

                    // Second fetch is slow — controlled by TCS
                    return secondFetchTcs.Task;
                },
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                allowStaleReads: true);

            // Act — get the first value
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(100, first);

            // Wait for expiration
            await Task.Delay(150).ConfigureAwait(false);

            // Get the value again — should return the stale value immediately,
            // not block on the slow second fetch
            int stale = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(100, stale);

            // Complete the background fetch
            secondFetchTcs.SetResult(200);
            await Task.Yield();

            // Now the refreshed value should be available
            int refreshed = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(200, refreshed);
        }

        [TestMethod]
        public async Task GetValueAsync_ReturnsStaleValueWhenFactoryFails()
        {
            // Arrange
            int callCount = 0;
            using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    _ = fetchSignal.Release();

                    if (count >= 2)
                    {
                        throw new InvalidOperationException("Factory error");
                    }

                    return Task.FromResult(42);
                },
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                allowStaleReads: true);

            // Act — get the first value
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(42, first);
            _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            // Wait for expiration
            await Task.Delay(150).ConfigureAwait(false);

            // Get the value again — should return the stale value, not throw
            int stale = await cache.GetValueAsync().ConfigureAwait(false);
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
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 2)
                    {
                        // Second fetch is very slow — simulates Cosmos DB latency spike
                        return slowFetchTcs.Task;
                    }

                    return Task.FromResult(count * 10);
                },
                TimeSpan.FromMilliseconds(300),
                TimeSpan.FromMilliseconds(150),
                allowStaleReads: true);
            cache.Start();

            try
            {
                // First fetch
                int first = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(10, first);

                // Wait past expiration (background pre-fetch triggers at ~150ms but is slow)
                await Task.Delay(400).ConfigureAwait(false);

                // Reader should get the stale value immediately, not block
                Task<int> readerTask = cache.GetValueAsync();
                Assert.IsTrue(readerTask.IsCompleted, "Reader should not block when stale value is available.");
                int stale = await readerTask.ConfigureAwait(false);
                Assert.AreEqual(10, stale);

                // Complete the slow fetch
                slowFetchTcs.SetResult(20);
                await Task.Yield();

                // Now the fresh value should be available
                int fresh = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(20, fresh);
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task GetValueAsync_ConcurrentCallers_ShareSingleFetch()
        {
            // Arrange
            int callCount = 0;
            TaskCompletionSource<int> fetchTcs = new TaskCompletionSource<int>();

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    _ = Interlocked.Increment(ref callCount);
                    return fetchTcs.Task;
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            // Act — start multiple concurrent calls
            Task<int> task1 = cache.GetValueAsync();
            Task<int> task2 = cache.GetValueAsync();
            Task<int> task3 = cache.GetValueAsync();

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

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) => fetchTcs.Task,
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

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
            int value = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        public async Task GetValueAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            TaskCompletionSource<int> fetchTcs = new TaskCompletionSource<int>();

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) => fetchTcs.Task,
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(
                () => cache.GetValueAsync(cts.Token)).ConfigureAwait(false);

            // Clean up the pending fetch
            fetchTcs.SetResult(42);
        }

        [TestMethod]
        public async Task GetValueAsync_FactoryThrows_CanRetrySuccessfully()
        {
            // Arrange
            int callCount = 0;

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                _ =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 1)
                    {
                        throw new InvalidOperationException("Transient failure");
                    }

                    return Task.FromResult(42);
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            // Act — first call fails
            _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => cache.GetValueAsync()).ConfigureAwait(false);

            // Second call should succeed with a new fetch
            int value = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(42, value);
            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public async Task BackgroundRefresh_FactoryThrows_CacheKeepsRunning()
        {
            // Arrange
            int callCount = 0;
            using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    _ = fetchSignal.Release();

                    if (count == 2)
                    {
                        throw new InvalidOperationException("Transient failure in background");
                    }

                    return Task.FromResult(count);
                },
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(50));
            cache.Start();

            try
            {
                // Trigger the first fetch
                int first = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(1, first);
                _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // The background will fail on the second fetch — wait for it
                _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // Wait for the background to retry (third fetch) which should succeed.
                // Retry delay = (refreshInterval - preFetchOffset) / 2 = 75ms.
                bool retried = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                Assert.IsTrue(retried, "Background did not retry after transient failure.");

                // Allow snapshot update
                await Task.Delay(50).ConfigureAwait(false);

                int value = await cache.GetValueAsync().ConfigureAwait(false);
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

            // refreshInterval = 5s, preFetchOffset = 4.5s → background fires at ~500ms
            // But GetValueAsync will keep the value fresh, so the background should skip.
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int result = Interlocked.Increment(ref callCount);
                    return Task.FromResult(result);
                },
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(100));
            cache.Start();

            try
            {
                // Trigger the first fetch
                int first = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(1, first);

                // Manually re-fetch right before the background would fire
                await Task.Delay(350).ConfigureAwait(false);

                // Force a fresh snapshot by waiting for expiration and re-fetching
                await Task.Delay(200).ConfigureAwait(false);
                int refreshed = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(2, refreshed);

                // Wait a bit and verify no extra fetches happened
                await Task.Delay(100).ConfigureAwait(false);
                Assert.AreEqual(2, callCount);
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task DisposeAsync_WhileCallerIsAwaitingFetch()
        {
            // Arrange
            TaskCompletionSource<int> fetchTcs = new TaskCompletionSource<int>();

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    // Register cancellation so the fetch completes when disposed
                    _ = ct.Register(() => fetchTcs.TrySetCanceled(ct));
                    return fetchTcs.Task;
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            // Start a GetValueAsync that will block on the slow factory
            Task<int> getTask = cache.GetValueAsync();

            // Act — dispose while the caller is still waiting
            await cache.DisposeAsync().ConfigureAwait(false);

            // Assert — the caller should observe a cancellation or ObjectDisposedException
            _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => getTask).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task BackgroundRefresh_OnErrorCallback_IsInvokedOnFailure()
        {
            // Arrange
            int callCount = 0;
            Exception capturedError = null;
            using SemaphoreSlim errorSignal = new SemaphoreSlim(0);
            using SemaphoreSlim fetchSignal = new SemaphoreSlim(0);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    _ = fetchSignal.Release();

                    if (count == 2)
                    {
                        throw new InvalidOperationException("Background failure");
                    }

                    return Task.FromResult(count);
                },
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(50),
                onBackgroundRefreshError: ex =>
                {
                    capturedError = ex;
                    _ = errorSignal.Release();
                });
            cache.Start();

            try
            {
                // Trigger the first fetch
                int first = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(1, first);
                _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // Wait for the background to fail on the second fetch
                _ = await fetchSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // Wait for the error callback to be invoked
                bool errorReceived = await errorSignal.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                Assert.IsTrue(errorReceived, "Error callback was not invoked.");
                Assert.IsNotNull(capturedError);
                _ = Assert.IsInstanceOfType<InvalidOperationException>(capturedError);
                Assert.AreEqual("Background failure", capturedError.Message);
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task GetValueAsync_WorksWithoutCallingStart()
        {
            // Arrange
            int callCount = 0;
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int result = Interlocked.Increment(ref callCount);
                    return Task.FromResult(result);
                },
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero);

            // Act — use the cache without ever calling Start
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(1, first);

            // Wait for expiration
            await Task.Delay(150).ConfigureAwait(false);

            // Default mode (allowStaleReads: false): blocks until new value is fetched
            int second = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(2, second);
            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public async Task GetValueAsync_AllowStaleReads_Disabled_BlocksOnExpiredValue()
        {
            // Arrange — default mode (allowStaleReads: false)
            int callCount = 0;
            TaskCompletionSource<int> secondFetchTcs = new TaskCompletionSource<int>();

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 1)
                    {
                        return Task.FromResult(100);
                    }

                    return secondFetchTcs.Task;
                },
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero);

            // First call
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(100, first);

            // Wait for expiration
            await Task.Delay(150).ConfigureAwait(false);

            // Second call — should block because allowStaleReads is false
            Task<int> blockedReader = cache.GetValueAsync();
            Assert.IsFalse(blockedReader.IsCompleted, "Reader should block when allowStaleReads is false.");

            // Complete the fetch
            secondFetchTcs.SetResult(200);
            int refreshed = await blockedReader.ConfigureAwait(false);
            Assert.AreEqual(200, refreshed);
        }

        [TestMethod]
        public async Task GetValueAsync_AllowStaleReads_Enabled_WorksWithoutCallingStart()
        {
            // Arrange
            int callCount = 0;
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int result = Interlocked.Increment(ref callCount);
                    return Task.FromResult(result);
                },
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                allowStaleReads: true);

            // Act — use the cache without ever calling Start
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(1, first);

            // Wait for expiration
            await Task.Delay(150).ConfigureAwait(false);

            // Stale-while-revalidate: returns stale value immediately
            int second = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(1, second);
            await Task.Yield();

            // After the background fetch completes, the refreshed value is available
            int third = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(2, third);
            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public void Constructor_ThrowsOnZeroRefreshTimeout()
        {
            _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new ProactiveAsyncCache<int>(
                    _ => Task.FromResult(42),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.Zero,
                    refreshTimeout: TimeSpan.Zero).Dispose());
        }

        [TestMethod]
        public void Constructor_ThrowsOnNegativeRefreshTimeout()
        {
            _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                new ProactiveAsyncCache<int>(
                    _ => Task.FromResult(42),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.Zero,
                    refreshTimeout: TimeSpan.FromSeconds(-1)).Dispose());
        }

        [TestMethod]
        public async Task RefreshTimeout_CancelsSlowFactory()
        {
            // Arrange — factory that never completes on its own
            int callCount = 0;

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                async (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 1)
                    {
                        return 42;
                    }

                    // Second call: simulate a hang that respects cancellation
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
                    return 99; // Never reached
                },
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                refreshTimeout: TimeSpan.FromMilliseconds(200));

            // Act — first call succeeds normally
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(42, first);

            // Wait for expiration
            await Task.Delay(150).ConfigureAwait(false);

            // Second call blocks on the pending fetch which will be cancelled by the
            // timeout, surfacing the cancellation to the caller.
            _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(
                () => cache.GetValueAsync()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RefreshTimeout_AllowStaleReads_ReturnsStaleWhileTimeoutFires()
        {
            // Arrange
            int callCount = 0;

            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                async (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 1)
                    {
                        return 100;
                    }

                    // Hang until cancelled by timeout
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
                    return 999;
                },
                TimeSpan.FromMilliseconds(100),
                TimeSpan.Zero,
                allowStaleReads: true,
                refreshTimeout: TimeSpan.FromMilliseconds(200));

            // First call succeeds
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(100, first);

            // Wait for expiration
            await Task.Delay(150).ConfigureAwait(false);

            // Stale read returns immediately, does not block
            int stale = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(100, stale);

            // Wait for the timeout to fire and cancel the hung fetch
            await Task.Delay(300).ConfigureAwait(false);

            // The fetch should have been cancelled; a new attempt can be made
            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public async Task RefreshTimeout_SuccessfulFetchWithinTimeout()
        {
            // Arrange — factory completes well within the timeout
            using ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                async (CancellationToken ct) =>
                {
                    await Task.Delay(50, ct).ConfigureAwait(false);
                    return 42;
                },
                TimeSpan.FromHours(1),
                TimeSpan.Zero,
                refreshTimeout: TimeSpan.FromSeconds(5));

            // Act
            int value = await cache.GetValueAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        [Timeout(30_000)]
        public async Task RefreshTimeout_BackgroundLoopSurvivesTimeoutAndRetries()
        {
            // Arrange — call 1 succeeds, call 2 hangs (times out), call 3 succeeds again.
            // Validates that a timeout does NOT kill the background refresh loop.
            int callCount = 0;
            TaskCompletionSource thirdCallCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                async (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);

                    if (count == 1)
                    {
                        return 10;
                    }

                    if (count == 2)
                    {
                        // Hang until cancelled by timeout
                        await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
                        return -1;
                    }

                    // Third+ call succeeds
                    _ = thirdCallCompleted.TrySetResult();
                    return 30;
                },
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(50),
                refreshTimeout: TimeSpan.FromMilliseconds(500));

            cache.Start();

            // Wait for initial fetch
            int first = await cache.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(10, first);

            // Wait for the background loop to trigger pre-fetch (call 2 — will timeout),
            // then retry (call 3 — will succeed). Retry delay = (refreshInterval - preFetchOffset) / 2 = 75ms.
            await thirdCallCompleted.Task.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);

            Assert.IsTrue(callCount >= 3, $"Expected at least 3 factory calls, got {callCount}.");

            await cache.DisposeAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(30_000)]
        public async Task Start_InitialFetchFails_RetriesUntilSuccess()
        {
            // Arrange — first call fails, second succeeds. Exercises the initial-fetch
            // retry loop in StartBackgroundRefresh, including the error callback and retry delay.
            int callCount = 0;
            Exception capturedError = null;
            using SemaphoreSlim errorSignal = new SemaphoreSlim(0);
            TaskCompletionSource firstValueReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 1)
                    {
                        throw new InvalidOperationException("Initial fetch failure");
                    }

                    _ = firstValueReady.TrySetResult();
                    return Task.FromResult(99);
                },
                // Retry delay = (refreshInterval - preFetchOffset) / 2 = 75ms.
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(50),
                onBackgroundRefreshError: ex =>
                {
                    capturedError = ex;
                    _ = errorSignal.Release();
                });

            cache.Start();

            try
            {
                // Wait for the error callback to fire
                bool errorReceived = await errorSignal.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                Assert.IsTrue(errorReceived, "Error callback was not invoked for initial fetch failure.");
                Assert.IsNotNull(capturedError);
                _ = Assert.IsInstanceOfType<InvalidOperationException>(capturedError);

                // Wait for the retry to succeed (retry delay ~75ms).
                await firstValueReady.Task.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                // The value should now be available
                int value = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(99, value);
                Assert.IsTrue(callCount >= 2);
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Timeout(30_000)]
        public async Task BackgroundRefresh_ErrorCallbackThrows_LoopContinues()
        {
            // Arrange — the error callback itself throws. The background loop must
            // swallow the callback exception and continue retrying.
            int callCount = 0;
            TaskCompletionSource thirdCallDone = new(TaskCreationOptions.RunContinuationsAsynchronously);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 2)
                    {
                        throw new InvalidOperationException("Transient failure");
                    }

                    if (count >= 3)
                    {
                        _ = thirdCallDone.TrySetResult();
                    }

                    return Task.FromResult(count);
                },
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(50),
                onBackgroundRefreshError: ex =>
                {
                    throw new InvalidOperationException("Callback threw!");
                });

            cache.Start();

            try
            {
                int first = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(1, first);

                // Wait for the background loop to fail (call 2), swallow the callback
                // exception, and retry (call 3).
                await thirdCallDone.Task.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                Assert.IsTrue(callCount >= 3, $"Expected at least 3 calls, got {callCount}.");
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task Dispose_WithBackgroundTaskRunning_CompletesCleanly()
        {
            // Arrange — exercises the sync Dispose() path with an active background task.
            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                _ => Task.FromResult(42),
                TimeSpan.FromHours(1),
                TimeSpan.Zero);

            cache.Start();
            _ = await cache.GetValueAsync().ConfigureAwait(false);

            // Act — sync dispose should cancel and wait for the background task
            cache.Dispose();

            // Assert — accessing the cache after Dispose should throw
            _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.HasValue);
        }

        [TestMethod]
        [Timeout(30_000)]
        public async Task Start_InitialFetchFails_ErrorCallbackThrows_LoopContinues()
        {
            // Arrange — initial fetch fails AND the error callback throws.
            // The background loop must survive both and eventually succeed.
            int callCount = 0;
            TaskCompletionSource secondCallDone = new(TaskCreationOptions.RunContinuationsAsynchronously);

            ProactiveAsyncCache<int> cache = new ProactiveAsyncCache<int>(
                (CancellationToken ct) =>
                {
                    int count = Interlocked.Increment(ref callCount);
                    if (count == 1)
                    {
                        throw new InvalidOperationException("Initial failure");
                    }

                    _ = secondCallDone.TrySetResult();
                    return Task.FromResult(77);
                },
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(50),
                onBackgroundRefreshError: ex =>
                {
                    throw new InvalidOperationException("Callback explodes");
                });

            cache.Start();

            try
            {
                await secondCallDone.Task.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);

                int value = await cache.GetValueAsync().ConfigureAwait(false);
                Assert.AreEqual(77, value);
                Assert.IsTrue(callCount >= 2);
            }
            finally
            {
                await cache.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
