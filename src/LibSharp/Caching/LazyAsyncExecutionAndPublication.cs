// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching
{
    /// <summary>
    /// Async lazy with LazyThreadSafetyMode.ExecutionAndPublication.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <remarks>Should not be used with IDisposable or IAsyncDisposable value types since it does not dispose of values.</remarks>
    public class LazyAsyncExecutionAndPublication<T> : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LazyAsyncExecutionAndPublication{T}"/> class from a value.
        /// </summary>
        /// <param name="value">The value to hold.</param>
        public LazyAsyncExecutionAndPublication(T value)
        {
            Argument.NotNull(value, nameof(value));

            m_hasValue = true;
            m_value = value;
            m_disposalToken = m_disposalCts.Token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyAsyncExecutionAndPublication{T}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">The value factory.</param>
        public LazyAsyncExecutionAndPublication(Func<CancellationToken, Task<T>> factory)
        {
            Argument.NotNull(factory, nameof(factory));

            m_hasValue = false;
            m_factory = factory;
            m_disposalToken = m_disposalCts.Token;
        }

        /// <summary>
        /// Gets a value indicating whether the value has been initialized.
        /// </summary>
        public bool HasValue
        {
            get
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

                return m_hasValue;
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The value.</returns>
        public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            if (!m_hasValue)
            {
                try
                {
                    // Link the caller's token with the disposal token so that pending waiters
                    // are unblocked immediately when the lazy is disposed.
                    using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(m_disposalToken, cancellationToken);
                    await m_semaphore.WaitAsync(linked.Token).ConfigureAwait(false);

                    try
                    {
                        if (!m_hasValue)
                        {
                            m_value = await m_factory(cancellationToken).ConfigureAwait(false);
                            m_hasValue = true;
                        }

                        return m_value;
                    }
                    finally
                    {
                        // Disposal may have happened between WaitAsync and here; Release()
                        // throws ObjectDisposedException in that case, which we suppress since
                        // the semaphore is already gone and no waiters remain.
                        try { _ = m_semaphore.Release(); }
                        catch (ObjectDisposedException) { }
                    }
                }
                catch (OperationCanceledException) when (m_disposalToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    // Disposal cancelled the wait — honour the same ObjectDisposedException
                    // contract as the check at the top of this method.
                    throw new ObjectDisposedException(GetType().Name);
                }
            }

            return m_value;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the value.
        /// </summary>
        /// <param name="disposing">True if the method was called by Dispose(), false if by the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
            {
                return;
            }

            if (disposing)
            {
                // Cancel first so any thread blocked on WaitAsync wakes up with
                // OperationCanceledException before the semaphore is torn down.
                m_disposalCts.Cancel();
                m_disposalCts.Dispose();
                m_semaphore.Dispose();
            }
        }

        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource m_disposalCts = new CancellationTokenSource();
        private readonly CancellationToken m_disposalToken;

        private readonly Func<CancellationToken, Task<T>> m_factory;
        private volatile bool m_hasValue;
        private T m_value;

        private int m_isDisposed;
    }
}
