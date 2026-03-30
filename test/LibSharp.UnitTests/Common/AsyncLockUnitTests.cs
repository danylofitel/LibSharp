// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class AsyncLockUnitTests
    {
        [TestMethod]
        public async Task AcquireAsync_ThrowsWhenDisposed()
        {
            // Arrange
            AsyncLock asyncLock = new AsyncLock();
            asyncLock.Dispose();

            // Act
            _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
                await asyncLock.AcquireAsync().ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AcquireAsync_CanceledToken_Throws()
        {
            // Arrange
            using (AsyncLock asyncLock = new AsyncLock())
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.Cancel();

                // Act
                _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
                    await asyncLock.AcquireAsync(cts.Token).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task AcquireAsync_HandleDispose_ReleasesLock()
        {
            // Arrange — acquire and release, then acquire again to prove the lock was released.
            using (AsyncLock asyncLock = new AsyncLock())
            {
                using (await asyncLock.AcquireAsync().ConfigureAwait(false))
                {
                    // first acquisition
                }

                // Act — second acquisition should not block
                using (await asyncLock.AcquireAsync().ConfigureAwait(false))
                {
                    // second acquisition succeeded
                }
            }
        }

        [TestMethod]
        public async Task AcquireAsync_MutualExclusion_SecondCallerWaitsForFirst()
        {
            // Arrange
            using (AsyncLock asyncLock = new AsyncLock())
            {
                TaskCompletionSource<bool> insideLock = new TaskCompletionSource<bool>();
                TaskCompletionSource<bool> releaseLock = new TaskCompletionSource<bool>();

                // First acquirer holds the lock until signalled.
                Task firstTask = Task.Run(async () =>
                {
                    using (await asyncLock.AcquireAsync().ConfigureAwait(false))
                    {
                        insideLock.SetResult(true);
                        _ = await releaseLock.Task.ConfigureAwait(false);
                    }
                });

                // Wait until the first task is holding the lock.
                _ = await insideLock.Task.ConfigureAwait(false);

                // Act — second acquirer must block.
                bool secondCompleted = false;
                Task secondTask = Task.Run(async () =>
                {
                    using (await asyncLock.AcquireAsync().ConfigureAwait(false))
                    {
                        secondCompleted = true;
                    }
                });

                // The second task should not have completed yet.
                await Task.Delay(50).ConfigureAwait(false);
                Assert.IsFalse(secondCompleted);

                // Release the first lock.
                releaseLock.SetResult(true);
                await Task.WhenAll(firstTask, secondTask).ConfigureAwait(false);

                Assert.IsTrue(secondCompleted);
            }
        }

        [TestMethod]
        public async Task AcquireAsync_DisposedWhileWaiting_ThrowsObjectDisposedException()
        {
            // Arrange
            using (AsyncLock asyncLock = new AsyncLock())
            using (CancellationTokenSource holderCts = new CancellationTokenSource())
            {
                TaskCompletionSource<bool> insideLock = new TaskCompletionSource<bool>();

                // First task holds the lock until holderCts is cancelled.
                Task firstTask = Task.Run(async () =>
                {
                    using (await asyncLock.AcquireAsync().ConfigureAwait(false))
                    {
                        insideLock.SetResult(true);

                        try
                        {
                            await Task.Delay(Timeout.Infinite, holderCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when the test cancels the holder.
                        }
                    }
                });

                _ = await insideLock.Task.ConfigureAwait(false);

                // Second task is blocked waiting to acquire.
                Task<ObjectDisposedException> secondTask = Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
                {
                    using (await asyncLock.AcquireAsync().ConfigureAwait(false))
                    {
                        Assert.Fail("Should not reach here.");
                    }
                });

                // Act — dispose the lock while the second task is waiting.
                asyncLock.Dispose();

                // Assert — second task surfaces ObjectDisposedException.
                _ = await secondTask.ConfigureAwait(false);

                // Clean up — cancel the holder so firstTask can exit.
                holderCts.Cancel();
                await firstTask.ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task AcquireAsync_CanceledWhileWaiting_ThrowsOperationCanceledException()
        {
            // Arrange
            using (AsyncLock asyncLock = new AsyncLock())
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                TaskCompletionSource<bool> insideLock = new TaskCompletionSource<bool>();
                TaskCompletionSource<bool> releaseLock = new TaskCompletionSource<bool>();

                // First task holds the lock.
                Task firstTask = Task.Run(async () =>
                {
                    using (await asyncLock.AcquireAsync().ConfigureAwait(false))
                    {
                        insideLock.SetResult(true);
                        _ = await releaseLock.Task.ConfigureAwait(false);
                    }
                });

                _ = await insideLock.Task.ConfigureAwait(false);

                // Second task waits with a token we will cancel.
                // SemaphoreSlim.WaitAsync throws OperationCanceledException (not the derived
                // TaskCanceledException) when cancelled mid-wait via a linked token.
                Task<OperationCanceledException> secondTask = Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
                    await asyncLock.AcquireAsync(cts.Token).ConfigureAwait(false));

                // Act — cancel while the second task is blocked.
                cts.Cancel();
                _ = await secondTask.ConfigureAwait(false);

                // Clean up — release the first lock so firstTask can exit.
                releaseLock.SetResult(true);
                await firstTask.ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task AcquireAsync_HighContention_OnlyOneCallerInsideCriticalSection()
        {
            // Arrange
            using (AsyncLock asyncLock = new AsyncLock())
            {
                const int workerCount = 24;
                const int iterationsPerWorker = 150;

                int insideCount = 0;
                int maxInsideCount = 0;
                int enteredCount = 0;

                Task[] workers = new Task[workerCount];

                // Act
                for (int i = 0; i < workers.Length; i++)
                {
                    workers[i] = Task.Run(async () =>
                    {
                        for (int j = 0; j < iterationsPerWorker; j++)
                        {
                            using (await asyncLock.AcquireAsync().ConfigureAwait(false))
                            {
                                int currentInsideCount = Interlocked.Increment(ref insideCount);
                                _ = Interlocked.Increment(ref enteredCount);

                                int observedMax;
                                do
                                {
                                    observedMax = Volatile.Read(ref maxInsideCount);
                                    if (currentInsideCount <= observedMax)
                                    {
                                        break;
                                    }
                                }
                                while (Interlocked.CompareExchange(ref maxInsideCount, currentInsideCount, observedMax) != observedMax);

                                await Task.Yield();

                                _ = Interlocked.Decrement(ref insideCount);
                            }
                        }
                    });
                }

                await Task.WhenAll(workers).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(workerCount * iterationsPerWorker, enteredCount);
                Assert.AreEqual(1, maxInsideCount);
            }
        }

        [TestMethod]
        public async Task Dispose_WithManyPendingWaiters_AllWaitersUnblocked()
        {
            // Arrange
            AsyncLock asyncLock = new AsyncLock();
            AsyncLock.Handle holder = await asyncLock.AcquireAsync().ConfigureAwait(false);

            const int waiterCount = 64;
            Task[] waiterTasks = new Task[waiterCount];

            for (int i = 0; i < waiterCount; i++)
            {
                waiterTasks[i] = Task.Run(async () =>
                {
                    _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
                        await asyncLock.AcquireAsync().ConfigureAwait(false)).ConfigureAwait(false);
                });
            }

            // Allow waiters to start blocking.
            await Task.Delay(50).ConfigureAwait(false);

            // Act
            asyncLock.Dispose();

            // Assert
            await AwaitWithTimeout(Task.WhenAll(waiterTasks), 3000).ConfigureAwait(false);

            // Cleanup
            holder.Dispose();
        }

        [TestMethod]
        public void Handle_DefaultInstance_DisposeDoesNotThrow()
        {
            // Arrange — default(Handle) has a null semaphore; Dispose must be safe to call.
            AsyncLock.Handle handle = default;

            // Act
            handle.Dispose();
        }

        [TestMethod]
        public async Task Handle_DoubleDispose_DoesNotThrow()
        {
            // Arrange
            using (AsyncLock asyncLock = new AsyncLock())
            {
                AsyncLock.Handle handle = await asyncLock.AcquireAsync().ConfigureAwait(false);

                // Act — dispose twice; second call must not throw even though the
                // semaphore count would exceed its maximum.
                handle.Dispose();
                handle.Dispose();
            }
        }

        [TestMethod]
        public async Task Handle_DoubleDispose_DoesNotReleaseExtraPermit()
        {
            // Arrange
            using (AsyncLock asyncLock = new AsyncLock())
            {
                AsyncLock.Handle firstHandle = await asyncLock.AcquireAsync().ConfigureAwait(false);
                firstHandle.Dispose();

                AsyncLock.Handle secondHandle = await asyncLock.AcquireAsync().ConfigureAwait(false);
                Task<AsyncLock.Handle> thirdAcquireTask = asyncLock.AcquireAsync();

                // Act + Assert — third acquisition should remain blocked while second holds lock.
                await Task.Delay(50).ConfigureAwait(false);
                Assert.IsFalse(thirdAcquireTask.IsCompleted);

                // A second dispose on firstHandle must not release another permit.
                firstHandle.Dispose();
                await Task.Delay(50).ConfigureAwait(false);
                Assert.IsFalse(thirdAcquireTask.IsCompleted);

                // Once the current holder releases, the third waiter can proceed.
                secondHandle.Dispose();
                AsyncLock.Handle thirdHandle = await thirdAcquireTask.ConfigureAwait(false);
                thirdHandle.Dispose();
            }
        }

        [TestMethod]
        public async Task Handle_CopiedStruct_DisposeFromBothCopies_ReleasesOnlyOnce()
        {
            // Arrange
            using (AsyncLock asyncLock = new AsyncLock())
            {
                AsyncLock.Handle originalHandle = await asyncLock.AcquireAsync().ConfigureAwait(false);
                AsyncLock.Handle copiedHandle = originalHandle;
                originalHandle.Dispose();

                AsyncLock.Handle secondHandle = await asyncLock.AcquireAsync().ConfigureAwait(false);
                Task<AsyncLock.Handle> thirdAcquireTask = asyncLock.AcquireAsync();

                await Task.Delay(50).ConfigureAwait(false);
                Assert.IsFalse(thirdAcquireTask.IsCompleted);

                // Disposing a copied handle must not release a second permit.
                copiedHandle.Dispose();

                await Task.Delay(50).ConfigureAwait(false);
                Assert.IsFalse(thirdAcquireTask.IsCompleted);

                secondHandle.Dispose();
                AsyncLock.Handle thirdHandle = await thirdAcquireTask.ConfigureAwait(false);
                thirdHandle.Dispose();
            }
        }

        [TestMethod]
        public async Task AcquireAsync_DisposeRace_DoesNotSurfaceOperationCanceledException()
        {
            // Arrange + Act + Assert
            for (int i = 0; i < 400; i++)
            {
                AsyncLock asyncLock = new AsyncLock();

                Task<AsyncLock.Handle> acquireTask = Task.Run(async () =>
                    await asyncLock.AcquireAsync().ConfigureAwait(false));

                asyncLock.Dispose();

                try
                {
                    AsyncLock.Handle handle = await acquireTask.ConfigureAwait(false);
                    handle.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Expected for disposal races.
                }
                catch (OperationCanceledException exception)
                {
                    Assert.Fail($"Unexpected cancellation type from disposal race: {exception.GetType().Name}.");
                }
            }
        }

        [TestMethod]
        public async Task Dispose_IdempotentMultipleCalls_DoesNotThrow()
        {
            // Arrange
            AsyncLock asyncLock = new AsyncLock();
            _ = await asyncLock.AcquireAsync().ConfigureAwait(false);

            // Act
            asyncLock.Dispose();
            asyncLock.Dispose();
        }

        private static async Task AwaitWithTimeout(Task task, int timeoutMs)
        {
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, task))
            {
                Assert.Fail($"Operation timed out after {timeoutMs}ms.");
            }

            await task.ConfigureAwait(false);
        }
    }
}
