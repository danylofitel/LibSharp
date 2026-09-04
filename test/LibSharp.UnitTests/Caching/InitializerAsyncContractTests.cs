// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching;

/// <summary>
/// Behaviour every <see cref="IInitializerAsync{T}"/> implementation must share, run once against
/// each of them. The two implementations differ in how many times a factory may run under
/// concurrency; everything asserted here holds for both.
/// </summary>
public abstract class InitializerAsyncContractTests
{
    protected abstract IInitializerAsync<int> CreateInitializer();

    [TestMethod]
    public void HasValue_BeforeFirstCall_IsFalse()
    {
        Assert.IsFalse(CreateInitializer().HasValue);
    }

    [TestMethod]
    public async Task GetValueAsync_ReturnsFactoryValue_AndSetsHasValue()
    {
        IInitializerAsync<int> initializer = CreateInitializer();

        Assert.AreEqual(42, await initializer.GetValueAsync(_ => Task.FromResult(42), TestContext.CancellationToken).ConfigureAwait(false));
        Assert.IsTrue(initializer.HasValue);
    }

    [TestMethod]
    public async Task SequentialReads_InvokeFactoryOnce()
    {
        IInitializerAsync<int> initializer = CreateInitializer();
        int calls = 0;
        Task<int> Factory(CancellationToken token)
        {
            _ = Interlocked.Increment(ref calls);
            return Task.FromResult(42);
        }

        for (int i = 0; i < 5; i++)
        {
            Assert.AreEqual(42, await initializer.GetValueAsync(Factory, TestContext.CancellationToken).ConfigureAwait(false));
        }

        Assert.AreEqual(1, Volatile.Read(ref calls));
    }

    [TestMethod]
    public async Task NullFactory_Throws()
    {
        IInitializerAsync<int> initializer = CreateInitializer();

        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => _ = await initializer.GetValueAsync(null!, TestContext.CancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FactoryReturningNullTask_Throws()
    {
        IInitializerAsync<int> initializer = CreateInitializer();

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => _ = await initializer.GetValueAsync(_ => null!, TestContext.CancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FailedAttempt_IsNotCached_AndNextCallRetries()
    {
        IInitializerAsync<int> initializer = CreateInitializer();

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => _ = await initializer.GetValueAsync(
                _ => Task.FromException<int>(new InvalidOperationException("boom")),
                TestContext.CancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        Assert.IsFalse(initializer.HasValue);

        // A failure must not poison the initializer.
        Assert.AreEqual(7, await initializer.GetValueAsync(_ => Task.FromResult(7), TestContext.CancellationToken).ConfigureAwait(false));
        Assert.IsTrue(initializer.HasValue);
    }

    [TestMethod]
    public async Task CancelledToken_AfterInitialization_StillReturnsValue()
    {
        // An initialized read does no waiting, so there is nothing for the token to cancel.
        IInitializerAsync<int> initializer = CreateInitializer();
        _ = await initializer.GetValueAsync(_ => Task.FromResult(42), TestContext.CancellationToken).ConfigureAwait(false);

        using CancellationTokenSource cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.AreEqual(42, await initializer.GetValueAsync(_ => Task.FromResult(99), cancelled.Token).ConfigureAwait(false));
    }

    public TestContext TestContext { get; set; } = null!;
}

[TestClass]
public class InitializerAsyncContract_ExecutionAndPublication : InitializerAsyncContractTests
{
    protected override IInitializerAsync<int> CreateInitializer()
    {
        return new InitializerAsyncExecutionAndPublication<int>();
    }
}

[TestClass]
public class InitializerAsyncContract_PublicationOnly : InitializerAsyncContractTests
{
    protected override IInitializerAsync<int> CreateInitializer()
    {
        return new InitializerAsyncPublicationOnly<int>();
    }
}
