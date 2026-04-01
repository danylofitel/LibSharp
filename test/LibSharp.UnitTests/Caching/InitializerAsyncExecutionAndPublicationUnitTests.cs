// Copyright (c) LibSharp. All rights reserved.

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
    public void HasValue_ThrowsWhenDisposed()
    {
        // Arrange
        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();
        initializer.Dispose();

        // Act
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = initializer.HasValue);
    }

    [TestMethod]
    public async Task GetValueAsync_ThrowsWhenDisposed()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
        InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();
        initializer.Dispose();

        // Act
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await initializer.GetValueAsync(factory, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FromValueFactory()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        using (InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>())
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                // Assert
                Assert.IsFalse(initializer.HasValue);
                _ = factory.DidNotReceive()(cancellationToken);

                Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

                Assert.IsTrue(initializer.HasValue);
                _ = factory.Received(1)(cancellationToken);

                Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
                Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
                Assert.AreEqual(5, await initializer.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

                Assert.IsTrue(initializer.HasValue);
                _ = factory.Received(1)(cancellationToken);
            }
        }
    }

    [TestMethod]
    public async Task FromValueFactory_TokenCanceled_Throws()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        using (InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>())
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                // Act
                _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await initializer.GetValueAsync(factory, cancellationTokenSource.Token).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallers_OnlyOneFactoryExecutes()
    {
        // Arrange
        using InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();
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
            callers[i] = Task.Run(() => initializer.GetValueAsync(Factory, CancellationToken.None), CancellationToken.None);
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
        using InitializerAsyncExecutionAndPublication<int> initializer = new InitializerAsyncExecutionAndPublication<int>();
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
}
