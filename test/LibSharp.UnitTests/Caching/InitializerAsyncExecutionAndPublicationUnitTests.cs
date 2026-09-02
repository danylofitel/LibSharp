// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class InitializerAsyncExecutionAndPublicationUnitTests
{

    [TestMethod]
    public async Task FromValueFactory()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Assert
            Assert.IsFalse(initializer.HasValue);
            _ = factory.DidNotReceive()(cancellationToken);

            Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

            Assert.IsTrue(initializer.HasValue);
            _ = factory.Received(1)(Arg.Any<CancellationToken>());

            Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
            Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
            Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

            Assert.IsTrue(initializer.HasValue);
            _ = factory.Received(1)(Arg.Any<CancellationToken>());
        }
        
    }

    [TestMethod]
    public async Task FromValueFactory_TokenCanceled_Throws()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            cancellationTokenSource.Cancel();

            // Act
            _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await initializer.GetValueAsync(factory, cancellationTokenSource.Token).ConfigureAwait(false)).ConfigureAwait(false);
        }
        
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallers_OnlyOneFactoryExecutes()
    {
        // Arrange
        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();
        TaskCompletionSource<bool> factoryStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFactory = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int executionCount = 0;

        async Task<int> Factory(CancellationToken cancellationToken)
        {
            _ = Interlocked.Increment(ref executionCount);
            _ = factoryStarted.TrySetResult(true);
            _ = await releaseFactory.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return 42;
        }

        Task<int>[] callers = new Task<int>[8];
        for (int i = 0; i < callers.Length; i++)
        {
            callers[i] = Task.Run(() => initializer.GetValueAsync(Factory, CancellationToken.None).AsTask(), CancellationToken.None);
        }

        _ = await factoryStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        releaseFactory.SetResult(true);
        int[] results = await Task.WhenAll(callers).ConfigureAwait(false);

        // Assert
        CollectionAssert.AreEqual(new[] { 42, 42, 42, 42, 42, 42, 42, 42 }, results);
        Assert.AreEqual(1, executionCount);
        Assert.IsTrue(initializer.HasValue);
    }

    [TestMethod]
    public async Task GetValueAsync_FactoryFailure_DoesNotCacheFailure()
    {
        // Arrange
        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();
        int attemptCount = 0;

        async Task<int> Factory(CancellationToken cancellationToken)
        {
            await Task.Yield();
            return Interlocked.Increment(ref attemptCount) switch
            {
                1 => throw new InvalidOperationException("boom"),
                _ => 42,
            };
        }

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await initializer.GetValueAsync(Factory, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
        int value = await initializer.GetValueAsync(Factory, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(42, value);
        Assert.AreEqual(2, attemptCount);
        Assert.IsTrue(initializer.HasValue);
    }

    [TestMethod]
    public async Task GetValueAsync_FactoryReturningNullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await initializer.GetValueAsync(_ => null!, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    // ── Shared-initialization contract ────────────────────────────────────

    [TestMethod]
    public async Task GetValueAsync_ValueFactoryDoesNotReceiveTheCallersToken()
    {
        // Arrange — the initialization is shared, so it must not be cancellable by whichever
        // caller happened to trigger it.
        CancellationToken observed = default;
        using CancellationTokenSource callerCts = new CancellationTokenSource();

        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();

        // Act
        _ = await initializer.GetValueAsync(
            cancellationToken =>
            {
                observed = cancellationToken;
                return Task.FromResult(42);
            },
            callerCts.Token).ConfigureAwait(false);

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

        Task<int> Factory(CancellationToken cancellationToken)
        {
            _ = Interlocked.Increment(ref calls);
            _ = factoryStarted.TrySetResult(true);
            return factoryTcs.Task;
        }

        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();

        using CancellationTokenSource impatientCts = new CancellationTokenSource();
        Task<int> impatient = initializer.GetValueAsync(Factory, impatientCts.Token).AsTask();
        Task<int> patient = initializer.GetValueAsync(Factory, CancellationToken.None).AsTask();

        _ = await factoryStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        // Act — the first caller gives up waiting.
        await impatientCts.CancelAsync().ConfigureAwait(false);
        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => impatient).ConfigureAwait(false);

        // Assert — the initialization survived and still serves the caller that waited.
        factoryTcs.SetResult(42);
        Assert.AreEqual(42, await patient.ConfigureAwait(false));
        Assert.AreEqual(1, Volatile.Read(ref calls), "Both callers must share a single factory execution.");
        Assert.IsTrue(initializer.HasValue);
    }
}
