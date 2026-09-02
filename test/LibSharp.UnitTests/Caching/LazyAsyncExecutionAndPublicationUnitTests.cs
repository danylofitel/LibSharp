// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class LazyAsyncExecutionAndPublicationUnitTests
{

    [TestMethod]
    public async Task FromValue()
    {
        // Arrange
        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(5);

        // Assert
        Assert.IsTrue(lazy.HasValue);
        Assert.AreEqual(5, await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        
    }

    [TestMethod]
    public async Task FromNullValue_HasValueIsTrue_ReturnsNull()
    {
        // Arrange — null is a legitimate value to cache; HasValue reflects whether the
        // lazy has been initialised, not whether the contained value is non-null.
        LazyAsyncExecutionAndPublication<string> lazy = new LazyAsyncExecutionAndPublication<string>((string)null!);

        // Assert
        Assert.IsTrue(lazy.HasValue);
        Assert.IsNull(await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        
    }

    [TestMethod]
    public async Task FromFactory_NullResult_HasValueIsTrue_ReturnsNull()
    {
        // Arrange
        LazyAsyncExecutionAndPublication<string> lazy = new LazyAsyncExecutionAndPublication<string>(_ => Task.FromResult<string>(null!));

        // Assert
        Assert.IsFalse(lazy.HasValue);
        Assert.IsNull(await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        Assert.IsTrue(lazy.HasValue);
        
    }

    [TestMethod]
    public async Task FromValue_CanceledToken_Succeeds()
    {
        // Arrange
        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(5);

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            cancellationTokenSource.Cancel();

            // Assert
            Assert.IsTrue(lazy.HasValue);
            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false));
        }
        
    }

    [TestMethod]
    public async Task FromValueFactory()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory);

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Assert
            Assert.IsFalse(lazy.HasValue);
            _ = factory.DidNotReceive()(cancellationToken);

            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));

            Assert.IsTrue(lazy.HasValue);
            _ = factory.Received(1)(Arg.Any<CancellationToken>());

            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));

            Assert.IsTrue(lazy.HasValue);
            _ = factory.Received(1)(Arg.Any<CancellationToken>());
        }
        
    }

    [TestMethod]
    public async Task FromValueFactory_TokenCanceled_Throws()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory);

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            cancellationTokenSource.Cancel();

            // Act
            _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false)).ConfigureAwait(false);
        }
        
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallers_OnlyOneFactoryExecutes()
    {
        // Arrange
        TaskCompletionSource<bool> factoryStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFactory = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int executionCount = 0;

        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(
            async cancellationToken =>
            {
                _ = Interlocked.Increment(ref executionCount);
                _ = factoryStarted.TrySetResult(true);
                _ = await releaseFactory.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                return 42;
            });

        Task<int>[] callers = new Task<int>[8];
        for (int i = 0; i < callers.Length; i++)
        {
            callers[i] = Task.Run(() => lazy.GetValueAsync(CancellationToken.None).AsTask(), CancellationToken.None);
        }

        _ = await factoryStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.AreEqual(1, executionCount);

        // Act
        releaseFactory.SetResult(true);
        int[] results = await Task.WhenAll(callers).ConfigureAwait(false);

        // Assert
        CollectionAssert.AreEqual(new[] { 42, 42, 42, 42, 42, 42, 42, 42 }, results);
        Assert.IsTrue(lazy.HasValue);
    }

    [TestMethod]
    public async Task FromFactory_NullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(_ => null!);

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await lazy.GetValueAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    public TestContext TestContext { get; set; }

    // ── Shared-initialization contract ────────────────────────────────────

    [TestMethod]
    public async Task GetValueAsync_ValueFactoryDoesNotReceiveTheCallersToken()
    {
        // Arrange — the initialization is shared, so it must not be cancellable by whichever
        // caller happened to trigger it.
        CancellationToken observed = default;
        using CancellationTokenSource callerCts = new CancellationTokenSource();

        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(
            cancellationToken =>
            {
                observed = cancellationToken;
                return Task.FromResult(42);
            });

        // Act
        _ = await lazy.GetValueAsync(callerCts.Token).ConfigureAwait(false);

        // Assert
        Assert.AreNotEqual(callerCts.Token, observed, "The value factory must not receive the caller's token.");

        await callerCts.CancelAsync().ConfigureAwait(false);
        Assert.IsFalse(observed.IsCancellationRequested, "Cancelling the caller must not cancel the factory's token.");
    }

    [TestMethod]
    public async Task GetValueAsync_OneCallerCancelling_LeavesTheSharedInitializationRunning()
    {
        // Arrange
        TaskCompletionSource<bool> factoryStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> factoryTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;

        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(
            cancellationToken =>
            {
                _ = Interlocked.Increment(ref calls);
                _ = factoryStarted.TrySetResult(true);
                return factoryTcs.Task;
            });

        using CancellationTokenSource impatientCts = new CancellationTokenSource();
        Task<int> impatient = lazy.GetValueAsync(impatientCts.Token).AsTask();
        Task<int> patient = lazy.GetValueAsync(CancellationToken.None).AsTask();

        _ = await factoryStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        // Act — the first caller gives up waiting.
        await impatientCts.CancelAsync().ConfigureAwait(false);
        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => impatient).ConfigureAwait(false);

        // Assert — the initialization survived and still serves the caller that waited.
        factoryTcs.SetResult(42);
        Assert.AreEqual(42, await patient.ConfigureAwait(false));
        Assert.AreEqual(1, Volatile.Read(ref calls), "Both callers must share a single factory execution.");
        Assert.IsTrue(lazy.HasValue);
    }
}
