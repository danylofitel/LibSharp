// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Threading;

[TestClass]
public class ThrottledActionUnitTests
{
    [TestMethod]
    public void Constructor_NullAction_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new ThrottledAction(null, TimeSpan.FromMilliseconds(50)));
    }

    [TestMethod]
    public void Constructor_ZeroInterval_DoesNotThrow()
    {
        _ = new ThrottledAction(() => { }, TimeSpan.Zero);
    }

    [TestMethod]
    public void Constructor_NegativeInterval_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new ThrottledAction(() => { }, TimeSpan.FromMilliseconds(-1)));
    }

    [TestMethod]
    public void Invoke_FirstCall_ExecutesImmediately()
    {
        // Arrange
        int callCount = 0;
        ThrottledAction throttled = new ThrottledAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromSeconds(10));

        // Act
        throttled.Invoke();

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void Invoke_SecondCallWithinInterval_IsDropped()
    {
        // Arrange
        int callCount = 0;
        ThrottledAction throttled = new ThrottledAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromSeconds(10));

        // Act
        throttled.Invoke();
        throttled.Invoke();
        throttled.Invoke();

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task Invoke_CallAfterIntervalElapses_ExecutesAgain()
    {
        // Arrange
        int callCount = 0;
        ThrottledAction throttled = new ThrottledAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(20));

        // Act
        throttled.Invoke();
        await Task.Delay(60, TestContext.CancellationToken).ConfigureAwait(false);
        throttled.Invoke();

        // Assert
        Assert.AreEqual(2, callCount);
    }

    [TestMethod]
    public async Task Invoke_RapidCallsThenWait_ExecutesTwice()
    {
        // Arrange
        int callCount = 0;
        ThrottledAction throttled = new ThrottledAction(() => Interlocked.Increment(ref callCount), TimeSpan.FromMilliseconds(20));

        // Act — first call fires, rapid follow-ups are dropped
        for (int i = 0; i < 5; i++)
        {
            throttled.Invoke();
        }

        await Task.Delay(60, TestContext.CancellationToken).ConfigureAwait(false);

        // After interval, call fires again
        throttled.Invoke();

        // Assert
        Assert.AreEqual(2, callCount);
    }

    // ── Zero interval (mutex) ─────────────────────────────────────────────

    [TestMethod]
    public void ZeroInterval_Invoke_WhenIdle_Executes()
    {
        // Arrange
        int callCount = 0;
        ThrottledAction throttled = new ThrottledAction(() => Interlocked.Increment(ref callCount), TimeSpan.Zero);

        // Act
        throttled.Invoke();

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void ZeroInterval_Invoke_SequentialCalls_AllExecute()
    {
        // With zero interval and no concurrency, sequential calls all run.
        int callCount = 0;
        ThrottledAction throttled = new ThrottledAction(() => Interlocked.Increment(ref callCount), TimeSpan.Zero);

        throttled.Invoke();
        throttled.Invoke();
        throttled.Invoke();

        Assert.AreEqual(3, callCount);
    }

    [TestMethod]
    public async Task ZeroInterval_Invoke_ConcurrentCall_IsDropped()
    {
        // Arrange — action blocks until signalled, simulating a long-running action
        using SemaphoreSlim actionStarted = new SemaphoreSlim(0, 1);
        using SemaphoreSlim actionGate = new SemaphoreSlim(0, 1);
        int callCount = 0;

        ThrottledAction throttled = new ThrottledAction(
            () =>
            {
                _ = Interlocked.Increment(ref callCount);
                _ = actionStarted.Release();
                actionGate.Wait(TestContext.CancellationToken);
            },
            TimeSpan.Zero);

        // Start the first invocation on a background thread
        Task first = Task.Run(() => throttled.Invoke(), TestContext.CancellationToken);

        // Wait until the action has actually started
        await actionStarted.WaitAsync(TestContext.CancellationToken).ConfigureAwait(false);

        // This call should be dropped because the first is still running
        throttled.Invoke();

        // Unblock the first invocation
        _ = actionGate.Release();
        await first.ConfigureAwait(false);

        // Assert — only the first call ran; the concurrent one was dropped
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task ZeroInterval_Invoke_AfterFirstCompletes_ExecutesAgain()
    {
        // Arrange
        using SemaphoreSlim actionGate = new SemaphoreSlim(0, 1);
        int callCount = 0;

        ThrottledAction throttled = new ThrottledAction(
            () =>
            {
                _ = Interlocked.Increment(ref callCount);
                actionGate.Wait(TestContext.CancellationToken);
            },
            TimeSpan.Zero);

        // First invocation
        Task first = Task.Run(() => throttled.Invoke(), TestContext.CancellationToken);
        _ = actionGate.Release();
        await first.ConfigureAwait(false);

        // Second invocation after first has finished
        Task second = Task.Run(() => throttled.Invoke(), TestContext.CancellationToken);
        _ = actionGate.Release();
        await second.ConfigureAwait(false);

        Assert.AreEqual(2, callCount);
    }

    public TestContext TestContext { get; set; }
}
