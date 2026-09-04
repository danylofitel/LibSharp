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
    /// <param name="timeProvider">
    /// (Optional) Time provider used to schedule the quiet-period timer. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public DebouncedAction(Action action, TimeSpan delay, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(action);
        Argument.GreaterThan(delay, TimeSpan.Zero);

        _action = action;
        _delay = delay;
        _timer = (timeProvider ?? TimeProvider.System).CreateTimer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Signals intent to invoke the action. Resets the quiet-period timer.
    /// The action will fire after the configured delay unless <see cref="Invoke"/> is called again first.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
    public void Invoke()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            _ = _timer.Change(_delay, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Disposes of the debounced action, cancelling any pending invocation.
    /// Blocks until any in-flight callback has completed.
    /// </summary>
    /// <remarks>
    /// Safe to call from inside the debounced action itself: that case returns without waiting,
    /// since the callback being waited for is the caller.
    /// </remarks>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _timer.Dispose();
        }

        // Disposing from inside the action would wait for the very callback making the call, which
        // never returns. Skip the drain in that case: that callback is already unwinding, and the
        // timer is stopped, so there is nothing left to wait for. The semaphore is then left to the
        // garbage collector, which costs nothing — its wait handle is never allocated, so it holds
        // no unmanaged resource.
        if (Volatile.Read(ref _callbackThreadId) == Environment.CurrentManagedThreadId)
        {
            return;
        }

        // Wait for any in-flight callback to finish, then release ownership of the slot.
        // _callbackRunning starts at 1 (idle). OnTimer claims it to 0 while running;
        // waiting here blocks until the callback releases it back to 1.
        _callbackRunning.Wait();
        _callbackRunning.Dispose();
    }

    private void OnTimer(object? state)
    {
        bool acquired;

        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            // Claim the execution slot while holding the lock so that the disposed-check
            // and the slot-claim are atomic with respect to Dispose. With a one-shot timer
            // this always succeeds, but the guard is defensive against unexpected races.
            acquired = _callbackRunning.Wait(0);
        }

        if (!acquired)
        {
            return;
        }

        try
        {
            Volatile.Write(ref _callbackThreadId, Environment.CurrentManagedThreadId);
            _action();
        }
        finally
        {
            Volatile.Write(ref _callbackThreadId, 0);
            _ = _callbackRunning.Release();
        }
    }

    private readonly Action _action;
    private readonly TimeSpan _delay;
    private readonly ITimer _timer;
    private readonly object _lock = new object();
    private readonly SemaphoreSlim _callbackRunning = new SemaphoreSlim(1, 1);

    private bool _isDisposed;

    // Managed id of the thread running the callback, or 0 when none is. Lets Dispose tell a call
    // made from inside the action from an ordinary one.
    private int _callbackThreadId;
}
