// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using LibSharp.Common;

namespace LibSharp.Threading;

/// <summary>
/// Wraps an <see cref="Action"/> so that it fires only after a quiet period has elapsed
/// since the last call to <see cref="Invoke"/>.
/// </summary>
/// <remarks>
/// Each call to <see cref="Invoke"/> resets the timer. The underlying action runs on a
/// ThreadPool thread once the delay expires without another <see cref="Invoke"/> call.
/// <para>
/// <see cref="Dispose"/> blocks until any in-flight callback has completed, so it is safe
/// to call immediately after the last <see cref="Invoke"/> call.
/// </para>
/// <para>
/// Do not call <see cref="Dispose"/> from within the debounced callback itself. Doing so can
/// deadlock because disposal waits for callback completion.
/// </para>
/// </remarks>
public sealed class DebouncedAction : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebouncedAction"/> class.
    /// </summary>
    /// <param name="action">The action to debounce.</param>
    /// <param name="delay">The quiet period that must elapse before the action fires.</param>
    public DebouncedAction(Action action, TimeSpan delay)
    {
        Argument.NotNull(action, nameof(action));
        Argument.GreaterThan(delay, TimeSpan.Zero, nameof(delay));

        m_action = action;
        m_delay = delay;
        m_timer = new Timer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Signals intent to invoke the action. Resets the quiet-period timer.
    /// The action will fire after the configured delay unless <see cref="Invoke"/> is called again first.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
    public void Invoke()
    {
        lock (m_lock)
        {
            ObjectDisposedException.ThrowIf(m_isDisposed, this);

            _ = m_timer.Change(m_delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Disposes of the debounced action, cancelling any pending invocation.
    /// Blocks until any in-flight callback has completed.
    /// </summary>
    public void Dispose()
    {
        lock (m_lock)
        {
            if (m_isDisposed)
            {
                return;
            }

            m_isDisposed = true;
            m_timer.Dispose();
        }

        // Wait for any in-flight callback to finish, then release ownership of the slot.
        // m_callbackRunning starts at 1 (idle). OnTimer claims it to 0 while running;
        // waiting here blocks until the callback releases it back to 1.
        m_callbackRunning.Wait();
        m_callbackRunning.Dispose();
    }

    private void OnTimer(object state)
    {
        bool acquired;

        lock (m_lock)
        {
            if (m_isDisposed)
            {
                return;
            }

            // Claim the execution slot while holding the lock so that the disposed-check
            // and the slot-claim are atomic with respect to Dispose. With a one-shot timer
            // this always succeeds, but the guard is defensive against unexpected races.
            acquired = m_callbackRunning.Wait(0);
        }

        if (!acquired)
        {
            return;
        }

        try
        {
            m_action();
        }
        finally
        {
            _ = m_callbackRunning.Release();
        }
    }

    private readonly Action m_action;
    private readonly TimeSpan m_delay;
    private readonly Timer m_timer;
    private readonly object m_lock = new object();
    private readonly SemaphoreSlim m_callbackRunning = new SemaphoreSlim(1, 1);

    private bool m_isDisposed;
}
