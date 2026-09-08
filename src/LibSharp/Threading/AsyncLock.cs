// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Threading;

/// <summary>
/// An async-compatible mutual exclusion lock.
/// </summary>
/// <remarks>
/// Not re-entrant: a caller that holds the lock must not call <see cref="AcquireAsync"/> again
/// before releasing it, or a deadlock will occur.
/// </remarks>
public sealed class AsyncLock : IDisposable
{
    /// <summary>
    /// A handle that releases the lock when disposed.
    /// </summary>
    public readonly struct Handle : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            _owner?.Release(_version);
        }

        internal Handle(AsyncLock owner, long version)
        {
            _owner = owner;
            _version = version;
        }

        private readonly AsyncLock? _owner;
        private readonly long _version;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLock"/> class.
    /// </summary>
    public AsyncLock()
    {
        _disposalToken = _disposalCts.Token;
    }

    /// <summary>
    /// Asynchronously acquires the lock.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Handle"/> that releases the lock when disposed.</returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was cancelled before the lock could be acquired.</exception>
    /// <remarks>
    /// Returns <see cref="ValueTask{TResult}"/> because an uncontended acquisition completes
    /// synchronously and must not allocate. Await the result at most once, and never concurrently.
    /// </remarks>
    public ValueTask<Handle> AcquireAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<Handle>(cancellationToken);
        }

        // Uncontended fast path. Wait(0) never blocks, so when the lock is free this method
        // allocates nothing and builds no state machine. The semaphore is never disposed by this class, so
        // this cannot throw ObjectDisposedException.
        // CancellationToken.None is deliberate: a zero timeout cannot block, so there is nothing for
        // a token to interrupt. Caller cancellation is already handled by the check above.
        if (_semaphore.Wait(0, CancellationToken.None))
        {
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                _ = _semaphore.Release();
                throw new ObjectDisposedException(GetType().Name);
            }

            return new ValueTask<Handle>(CreateHandle());
        }

        return AcquireContendedAsync(cancellationToken);
    }

    private async ValueTask<Handle> AcquireContendedAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.CanBeCanceled)
            {
                // Link caller cancellation with disposal cancellation so a blocked waiter wakes up for either signal.
                using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(_disposalToken, cancellationToken);
                await _semaphore.WaitAsync(linked.Token).ConfigureAwait(false);
            }
            else
            {
                // Fast path for default/non-cancelable token avoids linked CTS allocation.
                await _semaphore.WaitAsync(_disposalToken).ConfigureAwait(false);
            }

            // If disposal raced with a successful wait, release immediately and report disposal.
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                _ = _semaphore.Release();
                throw new ObjectDisposedException(GetType().Name);
            }

            return CreateHandle();
        }
        catch (OperationCanceledException) when (_disposalToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Disposal cancelled the wait — translate to ObjectDisposedException to match
            // the contract established by the disposal check in AcquireAsync.
            throw new ObjectDisposedException(GetType().Name);
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _isDisposed) != 0 && !cancellationToken.IsCancellationRequested)
        {
            // The disposal token source may be torn down while creating the linked token source.
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
            return;
        }

        // Cancel the disposal token so any thread blocked on WaitAsync wakes up
        // immediately with OperationCanceledException.
        // The semaphore is intentionally NOT disposed here: SemaphoreSlim.Dispose is not
        // safe to call concurrently with WaitAsync, and WaitAsync uses only managed
        // task-queue internals (no kernel handle), so GC reclamation is sufficient.
        _disposalCts.Cancel();
        _disposalCts.Dispose();
    }

    // Stamps each acquisition with a version the handle carries, so a handle can be matched against
    // the acquisition it came from. This is what keeps release idempotent without giving every
    // acquisition its own heap-allocated releaser.
    private Handle CreateHandle()
    {
        long version = Interlocked.Increment(ref _versionCounter);
        Volatile.Write(ref _activeVersion, version);
        return new Handle(this, version);
    }

    private void Release(long version)
    {
        // Only the handle for the acquisition still in force may release it, and only once. A copy
        // disposed a second time, or a handle left over from an earlier acquisition, no longer
        // matches and does nothing.
        if (Interlocked.CompareExchange(ref _activeVersion, 0L, version) != version)
        {
            return;
        }

        // Suppress ObjectDisposedException if a future implementation disposes semaphore
        // while critical sections are still unwinding.
        try { _ = _semaphore.Release(); }
        catch (ObjectDisposedException) { }
    }

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly CancellationTokenSource _disposalCts = new CancellationTokenSource();
    private readonly CancellationToken _disposalToken;

    private int _isDisposed;

    // Version 0 means no acquisition is in force, so the counter starts issuing at 1.
    private long _versionCounter;
    private long _activeVersion;
}
