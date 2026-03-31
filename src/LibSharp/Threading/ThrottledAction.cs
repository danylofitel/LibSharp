// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Diagnostics;
using System.Threading;
using LibSharp.Common;

namespace LibSharp.Threading
{
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
        public ThrottledAction(Action action, TimeSpan interval)
        {
            Argument.NotNull(action, nameof(action));
            Argument.GreaterThanOrEqualTo(interval, TimeSpan.Zero, nameof(interval));

            m_action = action;
            m_interval = interval;
            m_intervalTicks = (long)(interval.TotalSeconds * Stopwatch.Frequency);
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
                long now = Stopwatch.GetTimestamp();
                shouldInvoke = now - m_lastInvocationTimestamp >= m_intervalTicks;
                if (shouldInvoke)
                {
                    m_lastInvocationTimestamp = now;
                }
            }

            if (shouldInvoke)
            {
                m_action();
            }
        }

        private readonly Action m_action;
        private readonly TimeSpan m_interval;
        private readonly long m_intervalTicks;
        private readonly object m_lock = new object();

        private long m_lastInvocationTimestamp;
        private int m_isRunning;
    }
}
