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
        public async Task Dispose_IdempotentMultipleCalls_DoesNotThrow()
        {
            // Arrange
            AsyncLock asyncLock = new AsyncLock();
            _ = await asyncLock.AcquireAsync().ConfigureAwait(false);

            // Act
            asyncLock.Dispose();
            asyncLock.Dispose();
        }
    }
}
