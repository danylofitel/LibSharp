// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class LazyAsyncPublicationOnlyUnitTests
{
    [TestMethod]
    public async Task FromValue()
    {
        // Arrange
        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(5);

        // Assert
        Assert.IsTrue(lazy.HasValue);
        Assert.AreEqual(5, await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task FromValue_CanceledToken_Succeeds()
    {
        // Arrange
        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(5);

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            cancellationTokenSource.Cancel();

            // Assert
            Assert.IsTrue(lazy.HasValue);
            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false));
        }
    }

    [TestMethod]
    public async Task FromNullValue_HasValueIsTrue_ReturnsNull()
    {
        // Arrange — null is a legitimate value to cache; HasValue reflects whether the
        // wrapper has been initialised, not whether the contained value is non-null.
        LazyAsyncPublicationOnly<string> lazy = new LazyAsyncPublicationOnly<string>((string)null);

        // Assert
        Assert.IsTrue(lazy.HasValue);
        Assert.IsNull(await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task FromFactory_NullResult_HasValueIsTrue_ReturnsNull()
    {
        // Arrange
        LazyAsyncPublicationOnly<string> lazy = new LazyAsyncPublicationOnly<string>(_ => Task.FromResult<string>(null));

        // Assert
        Assert.IsFalse(lazy.HasValue);
        Assert.IsNull(await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        Assert.IsTrue(lazy.HasValue);
    }

    [TestMethod]
    public async Task FromValueFactory()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(factory);

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

    [TestMethod]
    public async Task FromFactory_NullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(_ => null);

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await lazy.GetValueAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallers_PublishSingleWinningValue()
    {
        // Arrange
        TaskCompletionSource<bool> bothFactoriesStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int executionCount = 0;

        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(async cancellationToken =>
        {
            int count = Interlocked.Increment(ref executionCount);
            if (count == 2)
            {
                _ = bothFactoriesStarted.TrySetResult(true);
            }

            _ = await bothFactoriesStarted.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return count;
        });

        // Act
        Task<int> first = Task.Run(() => lazy.GetValueAsync(CancellationToken.None), CancellationToken.None);
        Task<int> second = Task.Run(() => lazy.GetValueAsync(CancellationToken.None), CancellationToken.None);
        int[] results = await Task.WhenAll(first, second).ConfigureAwait(false);
        int published = await lazy.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(2, executionCount);
        Assert.AreEqual(results[0], results[1]);
        Assert.AreEqual(results[0], published);
        Assert.IsTrue(lazy.HasValue);
    }

    public TestContext TestContext { get; set; }
}
