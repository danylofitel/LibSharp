// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching;

/// <summary>
/// Tests for <see cref="IdleTracker"/>.
/// </summary>
/// <remarks>
/// Every test here is deterministic. <see cref="IdleTracker"/> owns no timer and no background
/// task, so its state transitions are driven entirely by explicit <c>RecordAccess</c> calls,
/// explicit clock advances, and awaiting the task <c>WaitWhileIdleAsync</c> returns. Nothing polls,
/// nothing sleeps, and no test needs a timeout.
/// </remarks>
[TestClass]
public class IdleTrackerUnitTests
{
    // ── IsIdle ────────────────────────────────────────────────────────────

    [TestMethod]
    public void IsIdle_IsFalseImmediatelyAfterConstruction()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        Assert.IsFalse(tracker.IsIdle(), "Construction counts as an access.");
    }

    [TestMethod]
    public void IsIdle_IsFalseOneTickBeforeTheWindowElapses()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.Advance(s_idleTimeout - TimeSpan.FromTicks(1));

        Assert.IsFalse(tracker.IsIdle());
    }

    [TestMethod]
    public void IsIdle_IsTrueExactlyAtTheWindowBoundary()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.Advance(s_idleTimeout);

        Assert.IsTrue(tracker.IsIdle(), "The boundary is inclusive: elapsed >= timeout is idle.");
    }

    [TestMethod]
    public void IsIdle_RecordAccessRestartsTheWindow()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.Advance(s_idleTimeout - TimeSpan.FromTicks(1));
        tracker.RecordAccess(timeProvider.GetUtcNow().UtcDateTime);

        // A full window minus a tick from the new access is still active.
        timeProvider.Advance(s_idleTimeout - TimeSpan.FromTicks(1));
        Assert.IsFalse(tracker.IsIdle(), "RecordAccess must restart the window from the access time.");

        timeProvider.Advance(TimeSpan.FromTicks(1));
        Assert.IsTrue(tracker.IsIdle());
    }

    [TestMethod]
    public void IsIdle_ClockSteppedBackwards_ReadsAsActive()
    {
        // A wall-clock jump backwards (an NTP correction, say) must fail toward "active" rather
        // than parking a cache that is in use.
        SettableTimeProvider timeProvider = new SettableTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.UtcNow = timeProvider.UtcNow.AddDays(-1);

        Assert.IsFalse(tracker.IsIdle());
    }

    [TestMethod]
    public void RecordAccess_OutOfOrderCalls_KeepTheNewestTimestamp()
    {
        // A caller can be descheduled between reading the clock and reaching RecordAccess, so a
        // stale timestamp can arrive after a newer one. It must not pull the idle deadline back.
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        DateTime older = timeProvider.GetUtcNow().UtcDateTime;
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        DateTime newer = timeProvider.GetUtcNow().UtcDateTime;

        tracker.RecordAccess(newer);
        tracker.RecordAccess(older);

        // The window must still be measured from the newer access.
        timeProvider.Advance(s_idleTimeout - TimeSpan.FromTicks(1));
        Assert.IsFalse(tracker.IsIdle(), "A late, older access must not bring the idle deadline forward.");

        timeProvider.Advance(TimeSpan.FromTicks(1));
        Assert.IsTrue(tracker.IsIdle());
    }

    // ── TimeUntilIdle ─────────────────────────────────────────────────────

    [TestMethod]
    public void TimeUntilIdle_IsTheFullWindowAfterConstruction()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        Assert.AreEqual(s_idleTimeout, tracker.TimeUntilIdle());
    }

    [TestMethod]
    public void TimeUntilIdle_ShrinksAsTheClockAdvances()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.Advance(TimeSpan.FromMinutes(2));

        Assert.AreEqual(s_idleTimeout - TimeSpan.FromMinutes(2), tracker.TimeUntilIdle());
    }

    [TestMethod]
    public void TimeUntilIdle_IsZeroExactlyWhenIdle()
    {
        // The refresh loop clamps its sleep to this value, so the two must agree: a non-zero result
        // while already idle would let the loop sleep on, and a zero result while still active
        // would let it spin.
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.Advance(s_idleTimeout - TimeSpan.FromTicks(1));
        Assert.IsFalse(tracker.IsIdle());
        Assert.AreEqual(TimeSpan.FromTicks(1), tracker.TimeUntilIdle());

        timeProvider.Advance(TimeSpan.FromTicks(1));
        Assert.IsTrue(tracker.IsIdle());
        Assert.AreEqual(TimeSpan.Zero, tracker.TimeUntilIdle());

        timeProvider.Advance(TimeSpan.FromHours(1));
        Assert.IsTrue(tracker.IsIdle());
        Assert.AreEqual(TimeSpan.Zero, tracker.TimeUntilIdle(), "It must never go negative.");
    }

    [TestMethod]
    public void TimeUntilIdle_ClockSteppedBackwards_ReportsAFullWindow()
    {
        // Guards the subtraction against overflow as much as it guards the behaviour.
        SettableTimeProvider timeProvider = new SettableTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        IdleTracker tracker = new IdleTracker(timeProvider, TimeSpan.MaxValue);

        timeProvider.UtcNow = timeProvider.UtcNow.AddDays(-1);

        Assert.AreEqual(TimeSpan.MaxValue, tracker.TimeUntilIdle());
    }

    // ── WaitWhileIdleAsync ────────────────────────────────────────────────

    [TestMethod]
    public async Task WaitWhileIdleAsync_CompletesSynchronouslyWhenActive()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        Task<bool> wait = tracker.WaitWhileIdleAsync(CancellationToken.None);

        Assert.IsTrue(wait.IsCompletedSuccessfully, "An active tracker must not park the caller at all.");
        Assert.IsTrue(await wait.ConfigureAwait(false));
    }

    [TestMethod]
    public async Task WaitWhileIdleAsync_ParksWhileIdle_AndResumesOnRecordAccess()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.Advance(s_idleTimeout);

        // The method runs synchronously up to its first await, so an idle tracker is guaranteed to
        // hand back an incomplete task here — no polling required to observe it.
        Task<bool> wait = tracker.WaitWhileIdleAsync(CancellationToken.None);
        Assert.IsFalse(wait.IsCompleted, "An idle tracker must park the caller.");

        // The clock is deliberately left frozen: only the recorded access can release the wait.
        tracker.RecordAccess(timeProvider.GetUtcNow().UtcDateTime);

        Assert.IsTrue(await wait.ConfigureAwait(false), "Recording an access must resume the parked caller.");
    }

    [TestMethod]
    public async Task WaitWhileIdleAsync_AccessRecordedBeforeParking_DoesNotPark()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.Advance(s_idleTimeout);
        tracker.RecordAccess(timeProvider.GetUtcNow().UtcDateTime);

        Task<bool> wait = tracker.WaitWhileIdleAsync(CancellationToken.None);

        Assert.IsTrue(wait.IsCompletedSuccessfully, "An access recorded before the wait must be observed by it.");
        Assert.IsTrue(await wait.ConfigureAwait(false));
    }

    [TestMethod]
    public async Task WaitWhileIdleAsync_CanParkAndResumeRepeatedly()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        // Exercises publishing and unpublishing the wake signal across several idle periods.
        for (int i = 0; i < 3; i++)
        {
            timeProvider.Advance(s_idleTimeout);

            Task<bool> wait = tracker.WaitWhileIdleAsync(CancellationToken.None);
            Assert.IsFalse(wait.IsCompleted, $"Expected to park on iteration {i}.");

            tracker.RecordAccess(timeProvider.GetUtcNow().UtcDateTime);

            Assert.IsTrue(await wait.ConfigureAwait(false), $"Expected to resume on iteration {i}.");
        }
    }

    [TestMethod]
    public async Task WaitWhileIdleAsync_ReturnsFalseWhenCancelledWhileParked()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);
        using CancellationTokenSource cts = new CancellationTokenSource();

        timeProvider.Advance(s_idleTimeout);

        Task<bool> wait = tracker.WaitWhileIdleAsync(cts.Token);
        Assert.IsFalse(wait.IsCompleted);

        await cts.CancelAsync().ConfigureAwait(false);

        Assert.IsFalse(await wait.ConfigureAwait(false), "Cancellation must tell the caller to exit its loop, not throw.");
    }

    [TestMethod]
    public async Task WaitWhileIdleAsync_AlreadyCancelledToken_ReturnsFalseWithoutThrowing()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);
        using CancellationTokenSource cts = new CancellationTokenSource();

        timeProvider.Advance(s_idleTimeout);
        await cts.CancelAsync().ConfigureAwait(false);

        Assert.IsFalse(await tracker.WaitWhileIdleAsync(cts.Token).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task WaitWhileIdleAsync_CancelledToken_WhileActive_ReturnsTrue()
    {
        // Pins the contract: cancellation is only observed while parked. Callers check their own
        // cancellation in their loop condition, so an active tracker never has to look at it.
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);
        using CancellationTokenSource cts = new CancellationTokenSource();

        await cts.CancelAsync().ConfigureAwait(false);

        Assert.IsTrue(await tracker.WaitWhileIdleAsync(cts.Token).ConfigureAwait(false));
    }

    // ── RecordAccess ──────────────────────────────────────────────────────

    [TestMethod]
    public void RecordAccess_WithNobodyParked_IsHarmless()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        IdleTracker tracker = new IdleTracker(timeProvider, s_idleTimeout);

        timeProvider.Advance(s_idleTimeout);
        tracker.RecordAccess(timeProvider.GetUtcNow().UtcDateTime);
        tracker.RecordAccess(timeProvider.GetUtcNow().UtcDateTime);

        Assert.IsFalse(tracker.IsIdle());
    }

    private static readonly TimeSpan s_idleTimeout = TimeSpan.FromMinutes(5);

    // FakeTimeProvider refuses to move backwards, so the backwards-clock case needs a provider
    // whose value can be set freely.
    private sealed class SettableTimeProvider : TimeProvider
    {
        public SettableTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow()
        {
            return UtcNow;
        }
    }
}
