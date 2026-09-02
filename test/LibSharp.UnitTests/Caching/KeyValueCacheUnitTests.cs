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
public class KeyValueCacheUnitTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void KeyValueCache_TimeToLive_ValueNotExpired()
    {
        // Arrange
        Func<int, int> factory = Substitute.For<Func<int, int>>();
        _ = factory(Arg.Any<int>()).Returns(x => -((int)x[0]));

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(factory, TimeSpan.FromHours(1));

        // Assert
        _ = factory.Received(0)(Arg.Any<int>());

        // Act
        int value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(0)(2);

        // Act
        value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(0)(2);

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(1)(2);

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(1)(2);
    }

    [TestMethod]
    public void KeyValueCache_TimeToLive_ValueExpired()
    {
        // Arrange
        Func<int, int> factory = Substitute.For<Func<int, int>>();
        _ = factory(Arg.Any<int>()).Returns(x => -((int)x[0]));

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(factory, TimeSpan.Zero);

        // Assert
        _ = factory.Received(0)(Arg.Any<int>());

        // Act
        int value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(0)(2);

        // Act
        value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(2)(1);
        _ = factory.Received(0)(2);

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(2)(1);
        _ = factory.Received(1)(2);

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(2)(1);
        _ = factory.Received(2)(2);
    }

    [TestMethod]
    public void KeyValueCache_ExpirationFunction_ValueNotExpired()
    {
        // Arrange
        Func<int, int> factory = Substitute.For<Func<int, int>>();
        _ = factory(Arg.Any<int>()).Returns(x => -((int)x[0]));

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(factory, (_, _) => DateTime.UtcNow.AddHours(1));

        // Assert
        _ = factory.Received(0)(Arg.Any<int>());

        // Act
        int value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(0)(2);

        // Act
        value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(0)(2);

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(1)(2);

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(1)(2);
    }

    [TestMethod]
    public void KeyValueCache_ExpirationFunction_ValueExpired()
    {
        // Arrange
        Func<int, int> factory = Substitute.For<Func<int, int>>();
        _ = factory(Arg.Any<int>()).Returns(x => -((int)x[0]));

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(factory, (_, _) => DateTime.UtcNow);

        // Assert
        _ = factory.Received(0)(Arg.Any<int>());

        // Act
        int value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(1)(1);
        _ = factory.Received(0)(2);

        // Act
        value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = factory.Received(2)(1);
        _ = factory.Received(0)(2);

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(2)(1);
        _ = factory.Received(1)(2);

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = factory.Received(2)(1);
        _ = factory.Received(2)(2);
    }

    [TestMethod]
    public void KeyValueCache_UpdateFactory_TimeToLive_ValueNotExpired()
    {
        // Arrange
        Func<int, int> createFactory = Substitute.For<Func<int, int>>();
        _ = createFactory(Arg.Any<int>()).Returns(x => -((int)x[0]));

        Func<int, int, int> updateFactory = Substitute.For<Func<int, int, int>>();
        _ = updateFactory(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ((int)x[1]) * 10);

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(createFactory, updateFactory, TimeSpan.FromHours(1));

        // Assert
        _ = createFactory.Received(0)(Arg.Any<int>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        int value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(0)(2);
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(0)(2);
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(1)(2);
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(1)(2);
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());
    }

    [TestMethod]
    public async Task GetValue_ConcurrentCallersForSameKey_ShareSingleInitialization()
    {
        // Arrange
        using ManualResetEventSlim factoryStarted = new ManualResetEventSlim(false);
        using ManualResetEventSlim factoryGate = new ManualResetEventSlim(false);
        int callCount = 0;

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(
            key =>
            {
                _ = Interlocked.Increment(ref callCount);
                factoryStarted.Set();
                factoryGate.Wait(TestContext.CancellationToken);
                return key * 10;
            },
            TimeSpan.FromHours(1));

        Task<int>[] callers = new Task<int>[8];
        for (int i = 0; i < callers.Length; i++)
        {
            callers[i] = Task.Run(() => cache.GetValue(1), TestContext.CancellationToken);
        }

        factoryStarted.Wait(TestContext.CancellationToken);
        Assert.AreEqual(1, callCount);

        // Act
        factoryGate.Set();
        int[] results = await Task.WhenAll(callers).ConfigureAwait(false);

        // Assert
        CollectionAssert.AreEqual(new[] { 10, 10, 10, 10, 10, 10, 10, 10 }, results);
    }

    [TestMethod]
    public void KeyValueCache_UpdateFactory_TimeToLive_ValueExpired()
    {
        // Arrange
        Func<int, int> createFactory = Substitute.For<Func<int, int>>();
        _ = createFactory(Arg.Any<int>()).Returns(x => -((int)x[0]));

        Func<int, int, int> updateFactory = Substitute.For<Func<int, int, int>>();
        _ = updateFactory(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ((int)x[1]) * 10);

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(createFactory, updateFactory, TimeSpan.Zero);

        // Assert
        _ = createFactory.Received(0)(Arg.Any<int>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        int value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(0)(2);
        _ = updateFactory.Received(0)(1, Arg.Any<int>());
        _ = updateFactory.Received(0)(1, Arg.Any<int>());

        // Act
        value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-10, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(0)(2);
        _ = updateFactory.Received(1)(1, Arg.Any<int>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>());

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(1)(2);
        _ = updateFactory.Received(1)(1, Arg.Any<int>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>());

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-20, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(1)(2);
        _ = updateFactory.Received(1)(1, Arg.Any<int>());
        _ = updateFactory.Received(1)(2, Arg.Any<int>());
    }

    [TestMethod]
    public void KeyValueCache_UpdateFactory_ExpirationFunction_ValueNotExpired()
    {
        // Arrange
        Func<int, int> createFactory = Substitute.For<Func<int, int>>();
        _ = createFactory(Arg.Any<int>()).Returns(x => -((int)x[0]));

        Func<int, int, int> updateFactory = Substitute.For<Func<int, int, int>>();
        _ = updateFactory(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ((int)x[1]) * 10);

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(createFactory, updateFactory, (_, _) => DateTime.UtcNow.AddHours(1));

        // Assert
        _ = createFactory.Received(0)(Arg.Any<int>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        int value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(0)(2);
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(0)(2);
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(1)(2);
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(1)(2);
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());
    }

    [TestMethod]
    public void KeyValueCache_UpdateFactory_ExpirationFunction_ValueExpired()
    {
        // Arrange
        Func<int, int> createFactory = Substitute.For<Func<int, int>>();
        _ = createFactory(Arg.Any<int>()).Returns(x => -((int)x[0]));

        Func<int, int, int> updateFactory = Substitute.For<Func<int, int, int>>();
        _ = updateFactory(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ((int)x[1]) * 10);

        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(createFactory, updateFactory, (_, _) => DateTime.UtcNow);

        // Assert
        _ = createFactory.Received(0)(Arg.Any<int>());
        _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

        // Act
        int value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-1, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(0)(2);
        _ = updateFactory.Received(0)(1, Arg.Any<int>());
        _ = updateFactory.Received(0)(1, Arg.Any<int>());

        // Act
        value = cache.GetValue(1);

        // Assert
        Assert.AreEqual(-10, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(0)(2);
        _ = updateFactory.Received(1)(1, Arg.Any<int>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>());

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-2, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(1)(2);
        _ = updateFactory.Received(1)(1, Arg.Any<int>());
        _ = updateFactory.Received(0)(2, Arg.Any<int>());

        // Act
        value = cache.GetValue(2);

        // Assert
        Assert.AreEqual(-20, value);
        _ = createFactory.Received(1)(1);
        _ = createFactory.Received(1)(2);
        _ = updateFactory.Received(1)(1, Arg.Any<int>());
        _ = updateFactory.Received(1)(2, Arg.Any<int>());
    }

    [TestMethod]
    public void GetValue_WithFakeTimeProvider_ExpiresPerKey()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int calls = 0;
        KeyValueCache<string, int> cache = new KeyValueCache<string, int>(_ => ++calls, TimeSpan.FromMinutes(1), timeProvider);

        Assert.AreEqual(1, cache.GetValue("a")); // Factory invoked for "a".
        Assert.AreEqual(1, cache.GetValue("a")); // Cached.
        Assert.AreEqual(2, cache.GetValue("b")); // Factory invoked for "b".
        Assert.AreEqual(2, calls);

        timeProvider.Advance(TimeSpan.FromMinutes(1));
        Assert.AreEqual(3, cache.GetValue("a")); // "a" expired: factory invoked again.
        Assert.AreEqual(3, calls);
    }

    // ── Count ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Count_IsZeroBeforeAnyKeyIsRequested()
    {
        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(key => key, TimeSpan.FromHours(1));

        Assert.AreEqual(0, cache.Count);
    }

    [TestMethod]
    public void Count_TracksDistinctKeysRequested()
    {
        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(key => key, TimeSpan.FromHours(1));

        _ = cache.GetValue(1);
        Assert.AreEqual(1, cache.Count);

        _ = cache.GetValue(2);
        Assert.AreEqual(2, cache.Count);

        // Repeat reads of a known key add nothing.
        _ = cache.GetValue(1);
        _ = cache.GetValue(2);
        Assert.AreEqual(2, cache.Count);
    }

    [TestMethod]
    public void Count_StillCountsEntriesWhoseValueHasExpired()
    {
        // The point of exposing Count: entries are never evicted, so an expired value still
        // occupies an entry. This is the number that grows without bound on an unbounded key space.
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        KeyValueCache<int, int> cache = new KeyValueCache<int, int>(key => key, TimeSpan.FromMinutes(1), timeProvider);

        _ = cache.GetValue(1);
        _ = cache.GetValue(2);
        Assert.AreEqual(2, cache.Count);

        timeProvider.Advance(TimeSpan.FromHours(1));

        Assert.AreEqual(2, cache.Count, "Expired values must still be counted: nothing is evicted.");
    }
}
