// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching;

/// <summary>
/// Tracks when something was last accessed, and parks a background worker while it goes unused.
/// </summary>
/// <remarks>
/// Knows nothing about caching: it answers "has anyone called in the last N, and if not, sleep
/// until someone does". <see cref="ProactiveAsyncCache{T}"/> uses it to suspend its refresh loop
/// once the cache falls out of use.
/// <para>
/// The two sides form a handshake across two fields, and getting that ordering right is the reason
/// this is its own type. <see cref="RecordAccess"/> stores the timestamp then reads the wake
/// signal; <see cref="WaitWhileIdleAsync"/> stores the wake signal then reads the timestamp.
/// Release/acquire (plain volatile) is not enough here — store-load reordering lets each side miss
/// the other's write, and the waiter would then sleep through the resumed activity. Both sides
/// fence with <see cref="Interlocked.Exchange(ref long, long)"/>, and the waiter never sleeps
/// without having published its signal behind that fence and re-checked idleness afterwards.
/// </para>
/// </remarks>
internal sealed class IdleTracker
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdleTracker"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider used to measure the idle window.</param>
    /// <param name="idleTimeout">How long without an access counts as idle. Must be positive.</param>
    public IdleTracker(TimeProvider timeProvider, TimeSpan idleTimeout)
    {
        m_timeProvider = timeProvider;
        m_idleTimeout = idleTimeout;

        // Construction counts as an access, so a tracker that is never touched still grants a full
        // idle window before it reports idle.
        m_lastAccessTicks = timeProvider.GetUtcNow().UtcDateTime.Ticks;
    }

    /// <summary>
    /// Gets a value indicating whether no access has been observed for at least the idle timeout.
    /// </summary>
    /// <remarks>
    /// A clock stepped backwards yields a negative difference and reads as active, which is the safe
    /// direction to fail. <c>Interlocked.Read</c> keeps the 64-bit load atomic on
    /// 32-bit runtimes; it is only ever called from the background worker, never from a hot path.
    /// </remarks>
    /// <returns><c>true</c> if the tracker is idle.</returns>
    public bool IsIdle()
    {
        return UtcNow.Ticks - Interlocked.Read(ref m_lastAccessTicks) >= m_idleTimeout.Ticks;
    }

    /// <summary>
    /// Stamps the access time and wakes the parked worker, if there is one.
    /// </summary>
    /// <param name="now">The current time, as read by the caller.</param>
    /// <remarks>
    /// The field keeps the newest access ever observed, never simply the last one written. A caller
    /// can be descheduled between reading the clock and arriving here, so a blind store would let a
    /// stale timestamp overwrite a newer one and bring the idle deadline forward.
    /// <para>
    /// This sits on the caller's hot path, so it stays as small as that allows: one uncontended
    /// read, one compare-exchange, and a read of a field that is null unless a worker is parked.
    /// The loop terminates promptly because <c>current</c> only ever moves forward.
    /// </para>
    /// <para>
    /// The compare-exchange carries the full fence the handshake with <c>m_signal</c> needs. When it
    /// is skipped, this call published nothing: the stored timestamp is already at least as recent
    /// as this access, and the caller that stored it ran the full fenced sequence itself, so there
    /// is nothing for a parked worker to miss.
    /// </para>
    /// </remarks>
    public void RecordAccess(DateTime now)
    {
        long nowTicks = now.Ticks;
        long current = Interlocked.Read(ref m_lastAccessTicks);
        while (current < nowTicks)
        {
            long observed = Interlocked.CompareExchange(ref m_lastAccessTicks, nowTicks, current);
            if (observed == current)
            {
                break;
            }

            current = observed;
        }

        TaskCompletionSource? signal = Volatile.Read(ref m_signal);
        if (signal is not null)
        {
            // Harmless if the signal has already been completed or abandoned.
            _ = signal.TrySetResult();
        }
    }

    /// <summary>
    /// Gets how long remains before the tracker becomes idle.
    /// </summary>
    /// <remarks>
    /// Lets a worker avoid sleeping past the point where it should park. Returns
    /// <see cref="TimeSpan.Zero"/> exactly when <see cref="IsIdle"/> is true, so a caller that
    /// sleeps for this and then re-checks never spins.
    /// </remarks>
    /// <returns>The time remaining, or <see cref="TimeSpan.Zero"/> if the tracker is already idle.</returns>
    public TimeSpan TimeUntilIdle()
    {
        long elapsed = UtcNow.Ticks - Interlocked.Read(ref m_lastAccessTicks);
        if (elapsed <= 0)
        {
            // Clock stepped backwards; report a full window rather than overflowing the subtraction.
            return m_idleTimeout;
        }

        long remaining = m_idleTimeout.Ticks - elapsed;
        return remaining > 0 ? TimeSpan.FromTicks(remaining) : TimeSpan.Zero;
    }

    /// <summary>
    /// Parks the caller for as long as the tracker is idle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token, typically signalled on disposal.</param>
    /// <returns>
    /// <c>true</c> once the tracker is active again, or immediately if it already was;
    /// <c>false</c> if <paramref name="cancellationToken"/> fired while parked, in which case the
    /// caller must exit its loop.
    /// </returns>
    /// <remarks>
    /// Signal-driven rather than polled, so a parked caller holds no timer and burns no CPU however
    /// long the idle period lasts, and resumes with no wake-up latency.
    /// </remarks>
    public async Task<bool> WaitWhileIdleAsync(CancellationToken cancellationToken)
    {
        while (IsIdle())
        {
            // RunContinuationsAsynchronously keeps the parked continuation off the thread of the
            // caller that wakes it: a reader must never end up running the background worker inline.
            TaskCompletionSource signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Publish behind a full fence, then re-check. Together with the matching fence in
            // RecordAccess this guarantees at least one side observes the other: either the caller
            // sees this signal and completes it, or the re-check below sees the caller's timestamp
            // and abandons the wait. Never await without both steps.
            _ = Interlocked.Exchange(ref m_signal, signal);

            try
            {
                if (!IsIdle())
                {
                    return true;
                }

                await signal.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                // Unpublish so an active tracker never keeps a stale signal, and the continuation
                // parked on it, alive. A caller still holding the old reference simply completes a
                // signal nobody is waiting on; the fenced re-publish above covers the next round.
                _ = Interlocked.Exchange(ref m_signal, null);
            }
        }

        return true;
    }

    private DateTime UtcNow => m_timeProvider.GetUtcNow().UtcDateTime;

    private readonly TimeProvider m_timeProvider;
    private readonly TimeSpan m_idleTimeout;

    // Ticks of the last recorded access, or of construction. Written by callers and read by the
    // parked worker without a lock; always through Interlocked, both to keep the 64-bit access
    // atomic on 32-bit runtimes and to fence the handshake with m_signal.
    private long m_lastAccessTicks;

    // Non-null only while a worker is parked in WaitWhileIdleAsync.
    private TaskCompletionSource? m_signal;
}
