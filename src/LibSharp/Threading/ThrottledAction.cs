// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using LibSharp.Common;

namespace LibSharp.Threading;

/// <summary>
/// Wraps an <see cref="Action"/> so that it executes at most once per interval.
/// </summary>
/// <remarks>
/// <para>
/// When <c>interval &gt; TimeSpan.Zero</c>: leading-edge time-based throttle.
/// The first call within any interval executes immediately; subsequent calls within
/// the same interval are dropped.
/// </para>
/// <para>
/// When <c>interval == TimeSpan.Zero</c>: at-most-one-concurrent-execution limiter.
/// If the action is already running on another thread, the incoming call is dropped.
/// </para>
/// </remarks>
public sealed class ThrottledAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottledAction"/> class.
    /// </summary>
    /// <param name="action">The action to throttle.</param>
    /// <param name="interval">
    /// The minimum time between executions, or <see cref="TimeSpan.Zero"/> for
    /// at-most-one-concurrent-execution behavior.
    /// </param>
    /// <param name="timeProvider">
    /// (Optional) Time provider used to measure the interval. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public ThrottledAction(Action action, TimeSpan interval, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(action);
        Argument.GreaterThanOrEqualTo(interval, TimeSpan.Zero);

        m_action = action;
        m_interval = interval;
        m_timeProvider = timeProvider ?? TimeProvider.System;

        // Clamp to long.MaxValue so an astronomically large interval (near TimeSpan.MaxValue) does not
        // overflow the double-to-long conversion — which would otherwise yield an undefined value and
        // break the throttle. long.MaxValue ticks simply means "effectively never fires again".
        double intervalTicks = Math.Round(interval.TotalSeconds * m_timeProvider.TimestampFrequency);
        m_intervalTicks = intervalTicks < long.MaxValue ? (long)intervalTicks : long.MaxValue;
    }

    /// <summary>
    /// Invokes the action if allowed by the throttle policy.
    /// Drops the call silently if the policy prevents execution.
    /// </summary>
    public void Invoke()
    {
        if (m_interval == TimeSpan.Zero)
        {
            InvokeMutex();
        }
        else
        {
            InvokeTimeBased();
        }
    }

    private void InvokeMutex()
    {
        if (Interlocked.CompareExchange(ref m_isRunning, 1, 0) != 0)
        {
            return;
        }

        try
        {
            m_action();
        }
        finally
        {
            Volatile.Write(ref m_isRunning, 0);
        }
    }

    private void InvokeTimeBased()
    {
        bool shouldInvoke;

        lock (m_lock)
        {
            long now = m_timeProvider.GetTimestamp();

            // The first invocation always fires; the timestamp origin is provider-defined and
            // must not be assumed to be far from zero (e.g. FakeTimeProvider starts near zero).
            shouldInvoke = !m_hasInvoked || now - m_lastInvocationTimestamp >= m_intervalTicks;
            if (shouldInvoke)
            {
                m_lastInvocationTimestamp = now;
                m_hasInvoked = true;
            }
        }

        if (shouldInvoke)
        {
            m_action();
        }
    }

    private readonly Action m_action;
    private readonly TimeSpan m_interval;
    private readonly TimeProvider m_timeProvider;
    private readonly long m_intervalTicks;
    private readonly object m_lock = new object();

    private long m_lastInvocationTimestamp;
    private bool m_hasInvoked;
    private int m_isRunning;
}
