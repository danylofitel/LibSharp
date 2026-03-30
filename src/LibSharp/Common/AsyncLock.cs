// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Common
{
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
                // Suppress SemaphoreFullException on double-dispose and ObjectDisposedException
                // if the semaphore is torn down while the critical section is still running.
                try { _ = m_semaphore?.Release(); }
                catch (SemaphoreFullException) { }
                catch (ObjectDisposedException) { }
            }

            internal Handle(SemaphoreSlim semaphore)
            {
                m_semaphore = semaphore;
            }

            private readonly SemaphoreSlim m_semaphore;
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
                // Link the caller's token with the disposal token so that pending waiters
                // are unblocked immediately when the lock is disposed.
                using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(m_disposalToken, cancellationToken);
                await m_semaphore.WaitAsync(linked.Token).ConfigureAwait(false);
                return new Handle(m_semaphore);
            }
            catch (OperationCanceledException) when (m_disposalToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Disposal cancelled the wait — translate to ObjectDisposedException to match
                // the contract established by the check at the top of this method.
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

        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource m_disposalCts = new CancellationTokenSource();
        private readonly CancellationToken m_disposalToken;

        private int m_isDisposed;
    }
}
