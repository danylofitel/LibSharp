// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching
{
    /// <summary>
    /// Async initializer with LazyThreadSafetyMode.ExecutionAndPublication.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <remarks>Should not be used with IDisposable or IAsyncDisposable value types since it does not dispose of values.</remarks>
    public class InitializerAsyncExecutionAndPublication<T> : IInitializerAsync<T>, IDisposable
    {
        /// <inheritdoc/>
        public bool HasValue
        {
            get
            {
                if (m_isDisposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                return m_hasValue;
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
        {
            if (m_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (!m_hasValue)
            {
                await m_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (!m_hasValue)
                    {
                        m_value = await factory(cancellationToken).ConfigureAwait(false);
                        m_hasValue = true;
                    }
                }
                finally
                {
                    _ = m_semaphore.Release();
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
            if (!m_isDisposed)
            {
                m_semaphore.Dispose();
                m_isDisposed = true;
            }
        }

        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);

        private bool m_hasValue;
        private T m_value;
        private bool m_isDisposed;
    }
}
