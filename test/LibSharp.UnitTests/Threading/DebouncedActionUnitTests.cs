// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Threading;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Threading;

[TestClass]
public class DebouncedActionUnitTests
{
    [TestMethod]
    public void Constructor_NullAction_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new DebouncedAction(null!, TimeSpan.FromMilliseconds(50)));
    }

    [TestMethod]
    public void Constructor_ZeroDelay_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new DebouncedAction(() => { }, TimeSpan.Zero));
    }

    [TestMethod]
    public void Constructor_NegativeDelay_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new DebouncedAction(() => { }, TimeSpan.FromMilliseconds(-1)));
    }

    [TestMethod]
    public void Invoke_SingleCall_FiresAfterDelay()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;
        using DebouncedAction debounced = new DebouncedAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(20), timeProvider);

        debounced.Invoke();
        timeProvider.Advance(TimeSpan.FromMilliseconds(20));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void Invoke_RapidCalls_FiresOnlyOnce()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;
        using DebouncedAction debounced = new DebouncedAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(100), timeProvider);

        for (int i = 0; i < 5; i++)
        {
            debounced.Invoke();
            timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        }

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));

        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void Invoke_TwoWavesSeparatedByDelay_FiresTwice()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;
        using DebouncedAction debounced = new DebouncedAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(20), timeProvider);

        debounced.Invoke();
        timeProvider.Advance(TimeSpan.FromMilliseconds(20));

        debounced.Invoke();
        timeProvider.Advance(TimeSpan.FromMilliseconds(20));

        Assert.AreEqual(2, callCount);
    }

    [TestMethod]
    public void Invoke_AfterDispose_Throws()
    {
        // Arrange
        DebouncedAction debounced = new DebouncedAction(() => { }, TimeSpan.FromMilliseconds(50));
        debounced.Dispose();

        // Act & Assert
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => debounced.Invoke());
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        DebouncedAction debounced = new DebouncedAction(() => { }, TimeSpan.FromMilliseconds(50));
        debounced.Dispose();
        debounced.Dispose();
    }

    [TestMethod]
    public void Dispose_CancelsPendingInvocation()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int callCount = 0;
        DebouncedAction debounced = new DebouncedAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(30), timeProvider);

        debounced.Invoke();
        debounced.Dispose();
        timeProvider.Advance(TimeSpan.FromMilliseconds(80));

        Assert.AreEqual(0, callCount);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task Dispose_WhileCallbackIsInFlight_ActionCompletesExactlyOnce()
    {
        using SemaphoreSlim actionStarted = new SemaphoreSlim(0, 1);
        using SemaphoreSlim actionGate = new SemaphoreSlim(0, 1);
        int callCount = 0;

        using DebouncedAction debounced = new DebouncedAction(
            () =>
            {
                _ = actionStarted.Release();
                actionGate.Wait(TestContext.CancellationToken);
                _ = Interlocked.Increment(ref callCount);
            },
            TimeSpan.FromMilliseconds(10));

        debounced.Invoke();
        bool started = await actionStarted.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken).ConfigureAwait(false);
        Assert.IsTrue(started, "Expected the debounced callback to start after the quiet period elapsed.");

        Task disposeTask = Task.Run(debounced.Dispose, TestContext.CancellationToken);

        _ = actionGate.Release();
        await disposeTask.ConfigureAwait(false);

        Assert.AreEqual(1, callCount);
    }

    public TestContext TestContext { get; set; }
}
