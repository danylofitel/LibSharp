// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class InitializerAsyncPublicationOnlyUnitTests
{
    [TestMethod]
    public async Task FromValueFactory()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        InitializerAsyncPublicationOnly<int> lazy = new InitializerAsyncPublicationOnly<int>();

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Assert
            Assert.IsFalse(lazy.HasValue);
            _ = factory.DidNotReceive()(cancellationToken);

            Assert.AreEqual(5, await lazy.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

            Assert.IsTrue(lazy.HasValue);
            _ = factory.Received(1)(cancellationToken);

            Assert.AreEqual(5, await lazy.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
            Assert.AreEqual(5, await lazy.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));
            Assert.AreEqual(5, await lazy.GetValueAsync(factory, cancellationToken).ConfigureAwait(false));

            Assert.IsTrue(lazy.HasValue);
            _ = factory.Received(1)(cancellationToken);
        }
    }

    [TestMethod]
    public async Task GetValueAsync_FactoryReturningNullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        InitializerAsyncPublicationOnly<int> lazy = new InitializerAsyncPublicationOnly<int>();

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await lazy.GetValueAsync(_ => null, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallers_PublishSingleWinningValue()
    {
        // Arrange
        InitializerAsyncPublicationOnly<int> initializer = new InitializerAsyncPublicationOnly<int>();
        TaskCompletionSource<bool> bothFactoriesStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int executionCount = 0;

        async Task<int> Factory(CancellationToken cancellationToken)
        {
            int count = Interlocked.Increment(ref executionCount);
            if (count == 2)
            {
                _ = bothFactoriesStarted.TrySetResult(true);
            }

            _ = await bothFactoriesStarted.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return count;
        }

        // Act
        Task<int> first = Task.Run(() => initializer.GetValueAsync(Factory, CancellationToken.None), CancellationToken.None);
        Task<int> second = Task.Run(() => initializer.GetValueAsync(Factory, CancellationToken.None), CancellationToken.None);
        int[] results = await Task.WhenAll(first, second).ConfigureAwait(false);
        int published = await initializer.GetValueAsync(Factory, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(2, executionCount);
        Assert.AreEqual(results[0], results[1]);
        Assert.AreEqual(results[0], published);
        Assert.IsTrue(initializer.HasValue);
    }
}
