// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class ValueCacheUnitTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void FromValueFactory_WithoutCallsToGetValue_DoesNotExecuteFactory()
    {
        // Arrange
        Func<int> factory = Substitute.For<Func<int>>();
        ValueCache<int> cache = new ValueCache<int>(factory, TimeSpan.FromMinutes(1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);
        _ = factory.DidNotReceive()();
    }

    [TestMethod]
    public void FromValueFactoryWithExpirationFunction_WithoutCallsToGetValue_DoesNotExecuteFactory()
    {
        // Arrange
        Func<int> factory = Substitute.For<Func<int>>();
        ValueCache<int> cache = new ValueCache<int>(factory, _ => DateTime.UtcNow.AddMinutes(1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);
        _ = factory.DidNotReceive()();
    }

    [TestMethod]
    public void FromUpdateFactory_WithoutCallsToGetValue_DoesNotExecuteFactory()
    {
        // Arrange
        Func<int> createFactory = Substitute.For<Func<int>>();
        Func<int, int> updateFactory = Substitute.For<Func<int, int>>();
        ValueCache<int> cache = new ValueCache<int>(createFactory, updateFactory, TimeSpan.FromMinutes(1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);
        _ = createFactory.DidNotReceive()();
        _ = updateFactory.DidNotReceive()(Arg.Any<int>());
    }

    [TestMethod]
    public void FromUpdateFactoryWithExpirationFunction_WithoutCallsToGetValue_DoesNotExecuteFactory()
    {
        // Arrange
        Func<int> createFactory = Substitute.For<Func<int>>();
        Func<int, int> updateFactory = Substitute.For<Func<int, int>>();
        ValueCache<int> cache = new ValueCache<int>(createFactory, updateFactory, _ => DateTime.UtcNow.AddMinutes(1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);
        _ = createFactory.DidNotReceive()();
        _ = updateFactory.DidNotReceive()(Arg.Any<int>());
    }

    [TestMethod]
    public async Task GetValue_ConcurrentCallers_ShareSingleInitialization()
    {
        // Arrange
        using ManualResetEventSlim factoryStarted = new ManualResetEventSlim(false);
        using ManualResetEventSlim factoryGate = new ManualResetEventSlim(false);
        int callCount = 0;

        ValueCache<int> cache = new ValueCache<int>(
            () =>
            {
                _ = Interlocked.Increment(ref callCount);
                factoryStarted.Set();
                factoryGate.Wait(TestContext.CancellationToken);
                return 42;
            },
            TimeSpan.FromHours(1));

        Task<int>[] callers = new Task<int>[8];
        for (int i = 0; i < callers.Length; i++)
        {
            callers[i] = Task.Run(cache.GetValue, TestContext.CancellationToken);
        }

        factoryStarted.Wait(TestContext.CancellationToken);
        Assert.AreEqual(1, callCount);

        // Act
        factoryGate.Set();
        int[] results = await Task.WhenAll(callers).ConfigureAwait(false);

        // Assert
        CollectionAssert.AreEqual(new[] { 42, 42, 42, 42, 42, 42, 42, 42 }, results);
    }

    [TestMethod]
    public void FromValueFactory_InitializesAndReturnsCachedValue()
    {
        // Arrange
        Func<int> factory = Substitute.For<Func<int>>();

        _ = factory().Returns(5);

        ValueCache<int> cache = new ValueCache<int>(factory, TimeSpan.FromMinutes(1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(5, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

        Assert.AreEqual(5, cache.GetValue());
        Assert.AreEqual(5, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
        _ = factory.Received(1)();
    }

    [TestMethod]
    public void FromValueFactoryWithExpirationFunction_InitializesAndReturnsCachedValue()
    {
        // Arrange
        Func<int> factory = Substitute.For<Func<int>>();

        _ = factory().Returns(5);

        ValueCache<int> cache = new ValueCache<int>(factory, _ => DateTime.UtcNow.AddMinutes(1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(5, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

        Assert.AreEqual(5, cache.GetValue());
        Assert.AreEqual(5, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
        _ = factory.Received(1)();
    }

    [TestMethod]
    public void FromUpdateFactory_InitializesAndReturnsCachedValue()
    {
        // Arrange
        Func<int> createFactory = Substitute.For<Func<int>>();

        _ = createFactory().Returns(5);

        Func<int, int> updateFactory = Substitute.For<Func<int, int>>();

        ValueCache<int> cache = new ValueCache<int>(createFactory, updateFactory, TimeSpan.FromMinutes(1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(5, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

        Assert.AreEqual(5, cache.GetValue());
        Assert.AreEqual(5, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
        _ = createFactory.Received(1)();
        _ = updateFactory.DidNotReceive()(Arg.Any<int>());
    }

    [TestMethod]
    public void FromUpdateFactoryWithExpirationFunction_InitializesAndReturnsCachedValue()
    {
        // Arrange
        Func<int> createFactory = Substitute.For<Func<int>>();

        _ = createFactory().Returns(5);

        Func<int, int> updateFactory = Substitute.For<Func<int, int>>();

        ValueCache<int> cache = new ValueCache<int>(createFactory, updateFactory, _ => DateTime.UtcNow.AddMinutes(1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(5, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

        Assert.AreEqual(5, cache.GetValue());
        Assert.AreEqual(5, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
        _ = createFactory.Received(1)();
        _ = updateFactory.DidNotReceive()(Arg.Any<int>());
    }

    [TestMethod]
    public void FromValueFactory_WhenCacheExpires_RefreshesCache()
    {
        // Arrange
        Func<int> factory = Substitute.For<Func<int>>();

        _ = factory().Returns(0, 1, 2, 3, 4);

        ValueCache<int> cache = new ValueCache<int>(factory, TimeSpan.Zero);

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(0, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);

        Assert.AreEqual(1, cache.GetValue());
        Assert.AreEqual(2, cache.GetValue());
        Assert.AreEqual(3, cache.GetValue());
        Assert.AreEqual(4, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);
        _ = factory.Received(5)();
    }

    [TestMethod]
    public void FromValueFactoryWithExpirationFunction_WhenCacheExpires_RefreshesCache()
    {
        // Arrange
        Func<int> factory = Substitute.For<Func<int>>();

        _ = factory().Returns(0, 1, 2, 3, 4);

        ValueCache<int> cache = new ValueCache<int>(factory, _ => DateTime.UtcNow.AddMinutes(-1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(0, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);

        Assert.AreEqual(1, cache.GetValue());
        Assert.AreEqual(2, cache.GetValue());
        Assert.AreEqual(3, cache.GetValue());
        Assert.AreEqual(4, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);
        _ = factory.Received(5)();
    }

    [TestMethod]
    public void FromUpdateFactory_WhenCacheExpires_RefreshesCache()
    {
        // Arrange
        Func<int> createFactory = Substitute.For<Func<int>>();

        _ = createFactory().Returns(0);

        Func<int, int> updateFactory = Substitute.For<Func<int, int>>();

        _ = updateFactory(Arg.Any<int>()).Returns(x => (int)x[0] + 1);

        ValueCache<int> cache = new ValueCache<int>(createFactory, updateFactory, TimeSpan.Zero);

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(0, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);

        Assert.AreEqual(1, cache.GetValue());
        Assert.AreEqual(2, cache.GetValue());
        Assert.AreEqual(3, cache.GetValue());
        Assert.AreEqual(4, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);
        _ = createFactory.Received(1)();
        _ = updateFactory.Received(4)(Arg.Any<int>());
    }

    [TestMethod]
    public void FromUpdateFactoryWithExpirationFunction_WhenCacheExpires_RefreshesCache()
    {
        // Arrange
        Func<int> createFactory = Substitute.For<Func<int>>();

        _ = createFactory().Returns(0);

        Func<int, int> updateFactory = Substitute.For<Func<int, int>>();

        _ = updateFactory(Arg.Any<int>()).Returns(x => (int)x[0] + 1);

        ValueCache<int> cache = new ValueCache<int>(createFactory, updateFactory, _ => DateTime.UtcNow.AddMinutes(-1));

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(0, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);

        Assert.AreEqual(1, cache.GetValue());
        Assert.AreEqual(2, cache.GetValue());
        Assert.AreEqual(3, cache.GetValue());
        Assert.AreEqual(4, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration <= DateTime.UtcNow);
        _ = createFactory.Received(1)();
        _ = updateFactory.Received(4)(Arg.Any<int>());
    }

    [TestMethod]
    public void FromValueFactory_InfiniteTimeToLive_DoesNotRefreshCache()
    {
        // Arrange
        Func<int> factory = Substitute.For<Func<int>>();

        _ = factory().Returns(0);

        ValueCache<int> cache = new ValueCache<int>(factory, TimeSpan.MaxValue);

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(0, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

        Assert.AreEqual(0, cache.GetValue());
        Assert.AreEqual(0, cache.GetValue());
        Assert.AreEqual(0, cache.GetValue());
        Assert.AreEqual(0, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
        _ = factory.Received(1)();
    }

    [TestMethod]
    public void FromUpdateFactory_InfiniteTimeToLive_DoesNotRefreshCache()
    {
        // Arrange
        Func<int> createFactory = Substitute.For<Func<int>>();

        _ = createFactory().Returns(0);

        Func<int, int> updateFactory = Substitute.For<Func<int, int>>();

        ValueCache<int> cache = new ValueCache<int>(createFactory, updateFactory, TimeSpan.MaxValue);

        // Assert
        Assert.IsFalse(cache.HasValue);
        Assert.IsNull(cache.Expiration);

        Assert.AreEqual(0, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);

        Assert.AreEqual(0, cache.GetValue());
        Assert.AreEqual(0, cache.GetValue());
        Assert.AreEqual(0, cache.GetValue());
        Assert.AreEqual(0, cache.GetValue());

        Assert.IsTrue(cache.HasValue);
        Assert.IsTrue(cache.Expiration >= DateTime.UtcNow);
        _ = createFactory.Received(1)();
        _ = updateFactory.DidNotReceive()(Arg.Any<int>());
    }
}
