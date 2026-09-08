// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching;

/// <summary>
/// Behaviour every <see cref="ILazyAsync{T}"/> implementation must share, run once against each of
/// them. The two differ in how many times a factory may run under concurrency; everything asserted
/// here holds for both.
/// </summary>
public abstract class LazyAsyncContractTests
{
    protected abstract ILazyAsync<int> Create(Func<CancellationToken, Task<int>> factory);

    [TestMethod]
    public void HasValue_BeforeFirstCall_IsFalse()
    {
        Assert.IsFalse(Create(_ => Task.FromResult(42)).HasValue);
    }

    [TestMethod]
    public async Task GetValueAsync_ReturnsFactoryValue_AndSetsHasValue()
    {
        ILazyAsync<int> lazy = Create(_ => Task.FromResult(42));

        Assert.AreEqual(42, await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        Assert.IsTrue(lazy.HasValue);
    }

    [TestMethod]
    public async Task SequentialReads_InvokeFactoryOnce()
    {
        int calls = 0;
        ILazyAsync<int> lazy = Create((CancellationToken token) =>
        {
            _ = Interlocked.Increment(ref calls);
            return Task.FromResult(42);
        });

        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(42, await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        }

        Assert.AreEqual(1, Volatile.Read(ref calls));
    }

    [TestMethod]
    public async Task AlreadyCancelledToken_BeforeInitialization_Cancels()
    {
        // The factory deliberately ignores its token: every implementation must still honour an
        // already-cancelled token rather than leaving it to the factory.
        ILazyAsync<int> lazy = Create(_ => Task.FromResult(42));

        using CancellationTokenSource cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            async () => _ = await lazy.GetValueAsync(cancelled.Token).ConfigureAwait(false)).ConfigureAwait(false);
        Assert.IsFalse(lazy.HasValue);
    }

    [TestMethod]
    public async Task CancelledToken_AfterInitialization_StillReturnsValue()
    {
        // An initialized read does no waiting, so there is nothing for the token to cancel.
        ILazyAsync<int> lazy = Create(_ => Task.FromResult(42));
        _ = await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false);

        using CancellationTokenSource cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.AreEqual(42, await lazy.GetValueAsync(cancelled.Token).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task FactoryReturningNullTask_Throws()
    {
        ILazyAsync<int> lazy = Create(_ => null!);

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => _ = await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FailedAttempt_IsNotCached_AndNextCallRetries()
    {
        int calls = 0;
        ILazyAsync<int> lazy = Create((CancellationToken token) =>
            Interlocked.Increment(ref calls) == 1
                ? Task.FromException<int>(new InvalidOperationException("boom"))
                : Task.FromResult(7));

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => _ = await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
        Assert.IsFalse(lazy.HasValue);

        Assert.AreEqual(7, await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        Assert.IsTrue(lazy.HasValue);
    }

    public TestContext TestContext { get; set; } = null!;
}

[TestClass]
public class LazyAsyncContract_ExecutionAndPublication : LazyAsyncContractTests
{
    protected override ILazyAsync<int> Create(Func<CancellationToken, Task<int>> factory)
    {
        return new LazyAsyncExecutionAndPublication<int>(factory);
    }
}

[TestClass]
public class LazyAsyncContract_PublicationOnly : LazyAsyncContractTests
{
    protected override ILazyAsync<int> Create(Func<CancellationToken, Task<int>> factory)
    {
        return new LazyAsyncPublicationOnly<int>(factory);
    }
}
