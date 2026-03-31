// Copyright (c) LibSharp. All rights reserved.

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
            m_releaser?.Release();
        }

        private Handle(Releaser releaser)
        {
            m_releaser = releaser;
        }

        internal static Handle Create(SemaphoreSlim semaphore)
        {
            return new Handle(new Releaser(semaphore));
        }

        private readonly Releaser m_releaser;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLock"/> class.
    /// </summary>
    public AsyncLock()
    {
        m_disposalToken = m_disposalCts.Token;
    }

    /// <summary>
    /// Asynchronously acquires the lock.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Handle"/> that releases the lock when disposed.</returns>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was cancelled before the lock could be acquired.</exception>
    public async Task<Handle> AcquireAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

        try
        {
            if (cancellationToken.CanBeCanceled)
            {
                // Link caller cancellation with disposal cancellation so a blocked waiter wakes up for either signal.
                using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(m_disposalToken, cancellationToken);
                await m_semaphore.WaitAsync(linked.Token).ConfigureAwait(false);
            }
            else
            {
                // Fast path for default/non-cancelable token avoids linked CTS allocation.
                await m_semaphore.WaitAsync(m_disposalToken).ConfigureAwait(false);
            }

            // If disposal raced with a successful wait, release immediately and report disposal.
            if (Volatile.Read(ref m_isDisposed) != 0)
            {
                _ = m_semaphore.Release();
                throw new ObjectDisposedException(GetType().Name);
            }

            return Handle.Create(m_semaphore);
        }
        catch (OperationCanceledException) when (m_disposalToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Disposal cancelled the wait — translate to ObjectDisposedException to match
            // the contract established by the check at the top of this method.
            throw new ObjectDisposedException(GetType().Name);
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref m_isDisposed) != 0 && !cancellationToken.IsCancellationRequested)
        {
            // The disposal token source may be torn down while creating the linked token source.
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
        {
            return;
        }

        // Cancel the disposal token so any thread blocked on WaitAsync wakes up
        // immediately with OperationCanceledException.
        // The semaphore is intentionally NOT disposed here: SemaphoreSlim.Dispose is not
        // safe to call concurrently with WaitAsync, and WaitAsync uses only managed
        // task-queue internals (no kernel handle), so GC reclamation is sufficient.
        m_disposalCts.Cancel();
        m_disposalCts.Dispose();
    }

    private sealed class Releaser
    {
        public Releaser(SemaphoreSlim semaphore)
        {
            m_semaphore = semaphore;
        }

        public void Release()
        {
            // Keep release idempotent across copied Handle structs.
            if (Interlocked.Exchange(ref m_isReleased, 1) != 0)
            {
                return;
            }

            // Suppress ObjectDisposedException if a future implementation disposes semaphore
            // while critical sections are still unwinding.
            try { _ = m_semaphore.Release(); }
            catch (ObjectDisposedException) { }
        }

        private readonly SemaphoreSlim m_semaphore;

        private int m_isReleased;
    }

    private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
    private readonly CancellationTokenSource m_disposalCts = new CancellationTokenSource();
    private readonly CancellationToken m_disposalToken;

    private int m_isDisposed;
}
