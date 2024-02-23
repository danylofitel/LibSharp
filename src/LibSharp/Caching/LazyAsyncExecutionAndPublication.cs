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
        }

        /// <summary>
        /// Gets a value indicating whether the value has been initialized.
        /// </summary>
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

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The value.</returns>
        public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        {
            if (!m_hasValue)
            {
                await m_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (!m_hasValue)
                    {
                        m_value = await m_factory(cancellationToken).ConfigureAwait(false);
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

        private readonly Func<CancellationToken, Task<T>> m_factory;
        private bool m_hasValue;
        private T m_value;

        private bool m_isDisposed;
    }
}
