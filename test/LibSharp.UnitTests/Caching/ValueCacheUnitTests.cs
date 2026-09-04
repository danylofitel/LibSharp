// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class ValueCacheUnitTests
{
    // MSTest assigns this by property injection after construction. The initializer states that
    // explicitly: without it the compiler reports CS8618, which the normal build suppresses but
    // `dotnet format` does not, and its code-fix pass then makes the property nullable and breaks
    // every TestContext.CancellationToken use.
    public TestContext TestContext { get; set; } = null!;

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

    [TestMethod]
    public void GetValue_WithFakeTimeProvider_ExpiresDeterministically()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int calls = 0;
        ValueCache<int> cache = new ValueCache<int>(() => ++calls, TimeSpan.FromMinutes(1), timeProvider);

        Assert.AreEqual(1, cache.GetValue()); // Factory invoked.
        Assert.AreEqual(1, cache.GetValue()); // Cached.
        Assert.AreEqual(1, calls);

        timeProvider.Advance(TimeSpan.FromSeconds(59));
        Assert.AreEqual(1, cache.GetValue()); // Still fresh.
        Assert.AreEqual(1, calls);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        Assert.AreEqual(2, cache.GetValue()); // Expired: factory invoked again.
        Assert.AreEqual(2, calls);
    }

    // ── Factory re-entrancy ───────────────────────────────────────────────

    [TestMethod]
    public void GetValue_FactoryReadsSameCache_ThrowsInsteadOfRecursing()
    {
        // Without the guard the re-entrant read finds no published value, calls the factory again,
        // and recurses until the stack overflows — which kills the process rather than the call.
        ValueCache<int>? cache = null;
        cache = new ValueCache<int>(() => cache!.GetValue(), TimeSpan.FromHours(1));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = cache.GetValue());
    }

    [TestMethod]
    public void GetValue_AfterReentrancyIsRejected_CacheStillWorks()
    {
        // The guard must be reset even though the factory threw, or one bad call wedges the cache.
        ValueCache<int>? cache = null;
        bool reenter = true;
        cache = new ValueCache<int>(
            () =>
            {
                if (reenter)
                {
                    reenter = false;
                    return cache!.GetValue();
                }

                return 42;
            },
            TimeSpan.FromHours(1));

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = cache.GetValue());

        Assert.AreEqual(42, cache.GetValue());
    }

    [TestMethod]
    public void GetValue_FactoryThrows_DoesNotWedgeTheCache()
    {
        ValueCache<int>? cache = null;
        bool fail = true;
        cache = new ValueCache<int>(
            () =>
            {
                if (fail)
                {
                    fail = false;
                    throw new InvalidTimeZoneException("boom");
                }

                return 7;
            },
            TimeSpan.FromHours(1));

        _ = Assert.ThrowsExactly<InvalidTimeZoneException>(() => _ = cache!.GetValue());

        Assert.AreEqual(7, cache!.GetValue());
    }
}
