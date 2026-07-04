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
    public async Task Invoke_SingleCall_FiresAfterDelay_RealTime()
    {
        // Arrange
        int callCount = 0;
        using DebouncedAction debounced = new DebouncedAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(20));

        // Act
        debounced.Invoke();
        await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task Invoke_RapidCalls_FiresOnlyOnce_RealTime()
    {
        // Arrange
        int callCount = 0;
        using DebouncedAction debounced = new DebouncedAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(100));

        // Act — 5 rapid calls, only the last should trigger
        for (int i = 0; i < 5; i++)
        {
            debounced.Invoke();
            await Task.Delay(10, TestContext.CancellationToken).ConfigureAwait(false);
        }

        await Task.Delay(150, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task Invoke_TwoWavesSeparatedByDelay_FiresTwice_RealTime()
    {
        // Arrange
        int callCount = 0;
        using DebouncedAction debounced = new DebouncedAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(20));

        // Act — first wave
        debounced.Invoke();
        await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);

        // Second wave
        debounced.Invoke();
        await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert
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
    public async Task Dispose_CancelsPendingInvocation_RealTime()
    {
        // Arrange
        int callCount = 0;
        DebouncedAction debounced = new DebouncedAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(30));

        // Act
        debounced.Invoke();
        debounced.Dispose();
        await Task.Delay(80, TestContext.CancellationToken).ConfigureAwait(false);

        // Assert — the timer was cancelled so the action should not have fired
        Assert.AreEqual(0, callCount);
    }

    [TestMethod]
    public async Task Dispose_WhileCallbackIsInFlight_ActionCompletesExactlyOnce_RealTime()
    {
        // Arrange — action signals when it starts, then blocks until released
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

        // Let the timer fire and wait until the action is holding the lock
        debounced.Invoke();
        await actionStarted.WaitAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // Dispose while the action is still running — must block until action completes
        Task disposeTask = Task.Run(debounced.Dispose, TestContext.CancellationToken);

        // Unblock the action
        _ = actionGate.Release();
        await disposeTask.ConfigureAwait(false);

        // Assert — action ran exactly once; no post-dispose invocation
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void Invoke_WithFakeTimeProvider_FiresAfterQuietPeriod()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int count = 0;
        using DebouncedAction debounced = new DebouncedAction(() => count++, TimeSpan.FromMilliseconds(300), timeProvider);

        debounced.Invoke();
        timeProvider.Advance(TimeSpan.FromMilliseconds(200)); // Quiet period not yet elapsed.
        Assert.AreEqual(0, count);

        debounced.Invoke(); // Resets the quiet period.
        timeProvider.Advance(TimeSpan.FromMilliseconds(200)); // 200 ms since the reset: not yet.
        Assert.AreEqual(0, count);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100)); // 300 ms since the reset: fires.
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void Invoke_WithFakeTimeProvider_RapidCalls_FiresOnce()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int count = 0;
        using DebouncedAction debounced = new DebouncedAction(() => count++, TimeSpan.FromMilliseconds(100), timeProvider);

        for (int i = 0; i < 5; i++)
        {
            debounced.Invoke();
            timeProvider.Advance(TimeSpan.FromMilliseconds(10)); // Each call resets before the quiet period elapses.
        }

        Assert.AreEqual(0, count);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100)); // Quiet period after the last call elapses.
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void Invoke_WithFakeTimeProvider_TwoWaves_FiresTwice()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int count = 0;
        using DebouncedAction debounced = new DebouncedAction(() => count++, TimeSpan.FromMilliseconds(50), timeProvider);

        debounced.Invoke();
        timeProvider.Advance(TimeSpan.FromMilliseconds(50)); // First wave fires.
        Assert.AreEqual(1, count);

        debounced.Invoke();
        timeProvider.Advance(TimeSpan.FromMilliseconds(50)); // Second wave fires.
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void Dispose_WithFakeTimeProvider_CancelsPendingInvocation()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        int count = 0;
        DebouncedAction debounced = new DebouncedAction(() => count++, TimeSpan.FromMilliseconds(50), timeProvider);

        debounced.Invoke();
        debounced.Dispose(); // Cancels the pending timer.
        timeProvider.Advance(TimeSpan.FromMilliseconds(100)); // Would have fired, but the timer was disposed.

        Assert.AreEqual(0, count);
    }

    public TestContext TestContext { get; set; }
}
