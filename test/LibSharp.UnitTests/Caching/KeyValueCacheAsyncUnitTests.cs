// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class KeyValueCacheAsyncUnitTests
{
    [TestMethod]
    public async Task GetValueAsync_ThrowsWhenDisposed()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> factory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(factory, TimeSpan.FromMinutes(1));
        cache.Dispose();

        // Act
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await cache.GetValueAsync(1, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GetValueAsync_CreateFactoryReturningNullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>((_, _) => null, TimeSpan.FromMinutes(1));

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await cache.GetValueAsync(1, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GetValueAsync_UpdateFactoryReturningNullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(
            (key, _) => Task.FromResult(-key),
            (_, _, _) => null,
            TimeSpan.Zero);

        Assert.AreEqual(-1, await cache.GetValueAsync(1, CancellationToken.None).ConfigureAwait(false));

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await cache.GetValueAsync(1, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallersForSameKey_ShareSingleCreate()
    {
        // Arrange
        TaskCompletionSource<bool> factoryStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> factoryTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        int callCount = 0;

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(
            (key, cancellationToken) =>
            {
                _ = Interlocked.Increment(ref callCount);
                _ = factoryStarted.TrySetResult(true);
                return factoryTcs.Task;
            },
            TimeSpan.FromHours(1));

        Task<int>[] callers = new Task<int>[8];
        for (int i = 0; i < callers.Length; i++)
        {
            callers[i] = cache.GetValueAsync(1, CancellationToken.None);
        }

        _ = await factoryStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.AreEqual(1, callCount);

        factoryTcs.SetResult(42);
        int[] results = await Task.WhenAll(callers).ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { 42, 42, 42, 42, 42, 42, 42, 42 }, results);
    }

    [TestMethod]
    public async Task GetValueAsync_DisposedWhileCreateFactoryIsInFlight_ThrowsObjectDisposedException()
    {
        // Arrange
        TaskCompletionSource<bool> factoryStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> factoryTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(
            (key, cancellationToken) =>
            {
                _ = factoryStarted.TrySetResult(true);
                return factoryTcs.Task;
            },
            TimeSpan.FromHours(1));

        Task<int> getTask = cache.GetValueAsync(1, CancellationToken.None);
        _ = await factoryStarted.Task.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        // Act
        cache.Dispose();
        factoryTcs.SetResult(42);

        // Assert
        _ = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => getTask).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task KeyValueCacheAsync_TimeToLive_ValueNotExpired()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> factory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        _ = factory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(factory, TimeSpan.FromHours(1));

        // Assert
        _ = factory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        int value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(1)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(1)(2, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task KeyValueCacheAsync_TimeToLive_ValueExpired()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> factory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        _ = factory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(factory, TimeSpan.Zero);

        // Assert
        _ = factory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        int value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(1)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(2)(2, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task KeyValueCacheAsync_ExpirationFunction_ValueNotExpired()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> factory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        _ = factory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(factory, (_, _) => DateTime.UtcNow.AddHours(1));

        // Assert
        _ = factory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        int value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(1)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(1)(2, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task KeyValueCacheAsync_ExpirationFunction_ValueExpired()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> factory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        _ = factory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(factory, (_, _) => DateTime.UtcNow);

        // Assert
        _ = factory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        int value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(0)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(1)(2, Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(2)(1, Arg.Any<CancellationToken>());
        _ = factory.Received(2)(2, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task KeyValueCacheAsync_UpdateFactory_TimeToLive_ValueNotExpired()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> createFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        _ = createFactory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

        Func<int, int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, int, CancellationToken, Task<int>>>();
        _ = updateFactory(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(((int)x[1]) * 10));

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(createFactory, updateFactory, TimeSpan.FromHours(1));

        // Assert
        _ = createFactory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        int value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(0)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(0)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(1)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(1)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task KeyValueCacheAsync_UpdateFactory_TimeToLive_ValueExpired()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> createFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        _ = createFactory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

        Func<int, int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, int, CancellationToken, Task<int>>>();
        _ = updateFactory(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(((int)x[1]) * 10));

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(createFactory, updateFactory, TimeSpan.Zero);

        // Assert
        _ = createFactory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        int value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(0)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-10, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(0)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(1)(1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(1)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(1)(1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-20, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(1)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(1)(1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(1)(2, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task KeyValueCacheAsync_UpdateFactory_ExpirationFunction_ValueNotExpired()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> createFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        _ = createFactory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

        Func<int, int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, int, CancellationToken, Task<int>>>();
        _ = updateFactory(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(((int)x[1]) * 10));

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(createFactory, updateFactory, (_, _) => DateTime.UtcNow.AddHours(1));

        // Assert
        _ = createFactory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        int value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(0)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(0)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(1)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(1)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task KeyValueCacheAsync_UpdateFactory_ExpirationFunction_ValueExpired()
    {
        // Arrange
        Func<int, CancellationToken, Task<int>> createFactory = Substitute.For<Func<int, CancellationToken, Task<int>>>();
        _ = createFactory(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(-((int)x[0])));

        Func<int, int, CancellationToken, Task<int>> updateFactory = Substitute.For<Func<int, int, CancellationToken, Task<int>>>();
        _ = updateFactory(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(x => Task.FromResult(((int)x[1]) * 10));

        using KeyValueCacheAsync<int, int> cache = new KeyValueCacheAsync<int, int>(createFactory, updateFactory, (_, _) => DateTime.UtcNow);

        // Assert
        _ = createFactory.Received(0)(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        int value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(0)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(1, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-10, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(0)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(1)(1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(1)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(1)(1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Act
        value = await cache.GetValueAsync(2, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(-20, value);
        _ = createFactory.Received(1)(1, Arg.Any<CancellationToken>());
        _ = createFactory.Received(1)(2, Arg.Any<CancellationToken>());
        _ = updateFactory.Received(1)(1, Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = updateFactory.Received(1)(2, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    public TestContext TestContext { get; set; }
}
