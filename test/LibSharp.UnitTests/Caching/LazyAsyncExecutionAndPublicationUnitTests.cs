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
    public void HasValue_ThrowsWhenDisposed()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory);
        lazy.Dispose();

        // Act
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = lazy.HasValue);
    }

    [TestMethod]
    public async Task GetValueAsync_ThrowsWhenDisposed()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();
        LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory);
        lazy.Dispose();

        // Act
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await lazy.GetValueAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FromValue()
    {
        // Arrange
        using (LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(5))
        {
            // Assert
            Assert.IsTrue(lazy.HasValue);
            Assert.AreEqual(5, await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        }
    }

    [TestMethod]
    public async Task FromNullValue_HasValueIsTrue_ReturnsNull()
    {
        // Arrange — null is a legitimate value to cache; HasValue reflects whether the
        // lazy has been initialised, not whether the contained value is non-null.
        using (LazyAsyncExecutionAndPublication<string> lazy = new LazyAsyncExecutionAndPublication<string>((string)null))
        {
            // Assert
            Assert.IsTrue(lazy.HasValue);
            Assert.IsNull(await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        }
    }

    [TestMethod]
    public async Task FromFactory_NullResult_HasValueIsTrue_ReturnsNull()
    {
        // Arrange
        using (LazyAsyncExecutionAndPublication<string> lazy = new LazyAsyncExecutionAndPublication<string>(_ => Task.FromResult<string>(null)))
        {
            // Assert
            Assert.IsFalse(lazy.HasValue);
            Assert.IsNull(await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
            Assert.IsTrue(lazy.HasValue);
        }
    }

    [TestMethod]
    public async Task FromValue_CanceledToken_Succeeds()
    {
        // Arrange
        using (LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(5))
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                // Assert
                Assert.IsTrue(lazy.HasValue);
                Assert.AreEqual(5, await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false));
            }
        }
    }

    [TestMethod]
    public async Task FromValueFactory()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        using (LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory))
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                // Assert
                Assert.IsFalse(lazy.HasValue);
                _ = factory.DidNotReceive()(cancellationToken);

                Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));

                Assert.IsTrue(lazy.HasValue);
                _ = factory.Received(1)(cancellationToken);

                Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
                Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
                Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));

                Assert.IsTrue(lazy.HasValue);
                _ = factory.Received(1)(cancellationToken);
            }
        }
    }

    [TestMethod]
    public async Task FromValueFactory_TokenCanceled_Throws()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        using (LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(factory))
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                // Act
                _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallers_OnlyOneFactoryExecutes()
    {
        // Arrange
        TaskCompletionSource<bool> factoryStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFactory = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int executionCount = 0;

        using LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(
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
            callers[i] = Task.Run(() => lazy.GetValueAsync(CancellationToken.None), CancellationToken.None);
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
    public async Task GetValueAsync_DisposedWhileFactoryIsInFlight_ThrowsObjectDisposedException()
    {
        // Arrange
        TaskCompletionSource<bool> factoryStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> factoryTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        using LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(
            cancellationToken =>
            {
                _ = factoryStarted.TrySetResult(true);
                return factoryTcs.Task;
            });

        Task<int> getTask = lazy.GetValueAsync(CancellationToken.None);
        _ = await factoryStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        lazy.Dispose();
        factoryTcs.SetResult(42);

        // Assert
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => getTask).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FromFactory_NullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        using LazyAsyncExecutionAndPublication<int> lazy = new LazyAsyncExecutionAndPublication<int>(_ => null);

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await lazy.GetValueAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    public TestContext TestContext { get; set; }
}
