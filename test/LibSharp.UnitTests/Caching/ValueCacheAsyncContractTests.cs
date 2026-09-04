// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching;

/// <summary>
/// Behaviour every <see cref="IValueCacheAsync{T}"/> implementation must share, run once against
/// each of them. Per-implementation tests cover what is specific to one; this covers the contract
/// callers program against when they hold the interface rather than a concrete type.
/// </summary>
public abstract class ValueCacheAsyncContractTests
{
    /// <summary>Creates a cache whose value stays fresh for the whole test.</summary>
    protected abstract IValueCacheAsync<int> CreateCache(Func<CancellationToken, Task<int>> factory, TimeProvider timeProvider);

    /// <summary>Disposes a cache created by <see cref="CreateCache"/>.</summary>
    protected abstract ValueTask DisposeCacheAsync(IValueCacheAsync<int> cache);

    [TestMethod]
    public async Task GetValueAsync_ReturnsFactoryValue()
    {
        IValueCacheAsync<int> cache = CreateCache(_ => Task.FromResult(42), new FakeTimeProvider());
        try
        {
            Assert.AreEqual(42, await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        }
        finally
        {
            await DisposeCacheAsync(cache).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task AfterFirstRead_HasValueAndExpirationArePopulated()
    {
        IValueCacheAsync<int> cache = CreateCache(_ => Task.FromResult(42), new FakeTimeProvider());
        try
        {
            _ = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

            Assert.IsTrue(cache.HasValue);
            Assert.IsNotNull(cache.Expiration);
        }
        finally
        {
            await DisposeCacheAsync(cache).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task RepeatedReadsWithinLifetime_InvokeFactoryOnce()
    {
        int calls = 0;
        IValueCacheAsync<int> cache = CreateCache(
            (CancellationToken token) =>
            {
                _ = Interlocked.Increment(ref calls);
                return Task.FromResult(42);
            },
            new FakeTimeProvider());
        try
        {
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(42, await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
            }

            Assert.AreEqual(1, Volatile.Read(ref calls));
        }
        finally
        {
            await DisposeCacheAsync(cache).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task ConcurrentFirstReads_ShareASingleFactoryInvocation()
    {
        int calls = 0;
        TaskCompletionSource gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IValueCacheAsync<int> cache = CreateCache(
            async (CancellationToken token) =>
            {
                _ = Interlocked.Increment(ref calls);
                await gate.Task.ConfigureAwait(false);
                return 42;
            },
            new FakeTimeProvider());
        try
        {
            Task<int>[] readers = new Task<int>[8];
            for (int i = 0; i < readers.Length; i++)
            {
                readers[i] = cache.GetValueAsync(TestContext.CancellationToken).AsTask();
            }

            gate.SetResult();
            int[] results = await Task.WhenAll(readers).ConfigureAwait(false);

            CollectionAssert.AreEqual(new[] { 42, 42, 42, 42, 42, 42, 42, 42 }, results);
            Assert.AreEqual(1, Volatile.Read(ref calls));
        }
        finally
        {
            await DisposeCacheAsync(cache).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task CancelledToken_OnWarmCache_StillReturnsValue()
    {
        // A hit does no waiting, so there is nothing for the token to cancel. Both implementations
        // deliberately serve it rather than throwing.
        IValueCacheAsync<int> cache = CreateCache(_ => Task.FromResult(42), new FakeTimeProvider());
        try
        {
            _ = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

            using CancellationTokenSource cancelled = new CancellationTokenSource();
            cancelled.Cancel();

            Assert.AreEqual(42, await cache.GetValueAsync(cancelled.Token).ConfigureAwait(false));
        }
        finally
        {
            await DisposeCacheAsync(cache).ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task AfterDisposal_MembersThrowObjectDisposed()
    {
        IValueCacheAsync<int> cache = CreateCache(_ => Task.FromResult(42), new FakeTimeProvider());
        _ = await cache.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);
        await DisposeCacheAsync(cache).ConfigureAwait(false);

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.HasValue);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.Expiration);
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cache.GetValueAsync(CancellationToken.None));
    }

    public TestContext TestContext { get; set; } = null!;
}

[TestClass]
public class ValueCacheAsyncContract_ValueCacheAsync : ValueCacheAsyncContractTests
{
    protected override IValueCacheAsync<int> CreateCache(Func<CancellationToken, Task<int>> factory, TimeProvider timeProvider)
    {
        return new ValueCacheAsync<int>(factory, TimeSpan.FromHours(1), timeProvider);
    }

    protected override ValueTask DisposeCacheAsync(IValueCacheAsync<int> cache)
    {
        ((ValueCacheAsync<int>)cache).Dispose();
        return ValueTask.CompletedTask;
    }
}

[TestClass]
public class ValueCacheAsyncContract_ProactiveAsyncCache : ValueCacheAsyncContractTests
{
    protected override IValueCacheAsync<int> CreateCache(Func<CancellationToken, Task<int>> factory, TimeProvider timeProvider)
    {
        return new ProactiveAsyncCache<int>(
            factory,
            new ProactiveAsyncCacheOptions
            {
                RefreshInterval = TimeSpan.FromHours(1),
                PreFetchOffset = TimeSpan.Zero,
                TimeProvider = timeProvider,
            });
    }

    protected override ValueTask DisposeCacheAsync(IValueCacheAsync<int> cache)
    {
        return ((ProactiveAsyncCache<int>)cache).DisposeAsync();
    }
}
