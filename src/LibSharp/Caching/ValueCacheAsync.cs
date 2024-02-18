// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching
{
    /// <summary>
    /// Async in-memory value cache with ThreadSafetyMode.ExecutionAndPublication behavior.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <remarks>Should not be used with IDisposable value types since it does not dispose of expired values.</remarks>
    public class ValueCacheAsync<T> : IValueCacheAsync<T>, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">The value factory.</param>
        /// <param name="timeToLive">Cache time-to-live.</param>
        public ValueCacheAsync(Func<CancellationToken, Task<T>> factory, TimeSpan timeToLive)
        {
            Argument.NotNull(factory, nameof(factory));
            Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

            m_createFactory = factory;
            m_timeToLive = timeToLive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">The value factory.</param>
        /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
        public ValueCacheAsync(Func<CancellationToken, Task<T>> factory, Func<T, DateTime> expirationFunction)
        {
            Argument.NotNull(factory, nameof(factory));
            Argument.NotNull(expirationFunction, nameof(expirationFunction));

            m_createFactory = factory;
            m_expirationFunction = expirationFunction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
        /// </summary>
        /// <param name="createFactory">The creation factory.</param>
        /// <param name="updateFactory">The update factory.</param>
        /// <param name="timeToLive">Cache time-to-live.</param>
        public ValueCacheAsync(Func<CancellationToken, Task<T>> createFactory, Func<T, CancellationToken, Task<T>> updateFactory, TimeSpan timeToLive)
        {
            Argument.NotNull(createFactory, nameof(createFactory));
            Argument.NotNull(updateFactory, nameof(updateFactory));
            Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

            m_createFactory = createFactory;
            m_updateFactory = updateFactory;
            m_timeToLive = timeToLive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
        /// </summary>
        /// <param name="createFactory">The creation factory.</param>
        /// <param name="updateFactory">The update factory.</param>
        /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
        public ValueCacheAsync(Func<CancellationToken, Task<T>> createFactory, Func<T, CancellationToken, Task<T>> updateFactory, Func<T, DateTime> expirationFunction)
        {
            Argument.NotNull(createFactory, nameof(createFactory));
            Argument.NotNull(updateFactory, nameof(updateFactory));
            Argument.NotNull(expirationFunction, nameof(expirationFunction));

            m_createFactory = createFactory;
            m_updateFactory = updateFactory;
            m_expirationFunction = expirationFunction;
        }

        /// <inheritdoc/>
        public bool HasValue
        {
            get
            {
                ObjectDisposedException.ThrowIf(m_isDisposed, this);

                return m_boxed != null;
            }
        }

        /// <inheritdoc/>
        public DateTime? Expiration
        {
            get
            {
                ObjectDisposedException.ThrowIf(m_isDisposed, this);

                return m_boxed?.Expiration;
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(m_isDisposed, this);

            if (m_boxed == null || DateTime.UtcNow >= m_boxed.Expiration)
            {
                await m_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    if (m_boxed == null || DateTime.UtcNow >= m_boxed.Expiration)
                    {
                        await Refresh(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _ = m_semaphore.Release();
                }
            }

            return m_boxed.Value;
        }

        /// <summary>
        /// Disposes of the cache.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the cache.
        /// </summary>
        /// <param name="disposing">True if the method was called during disposal, false otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_isDisposed)
            {
                if (disposing)
                {
                    m_semaphore.Dispose();
                }

                m_isDisposed = true;
            }
        }

        /// <summary>
        /// Initializes or updates the cache.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task Refresh(CancellationToken cancellationToken)
        {
            T newValue;
            if (m_updateFactory == null || m_boxed == null)
            {
                newValue = await m_createFactory(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                newValue = await m_updateFactory(m_boxed.Value, cancellationToken).ConfigureAwait(false);
            }

            DateTime newExpiration;
            if (m_timeToLive.HasValue)
            {
                if (m_timeToLive.Value == TimeSpan.MaxValue)
                {
                    newExpiration = DateTime.MaxValue;
                }
                else
                {
                    newExpiration = DateTime.UtcNow.Add(m_timeToLive.Value);
                }
            }
            else
            {
                Debug.Assert(m_expirationFunction != null, "Expiration function cannot be null if time to live is null.");
                newExpiration = m_expirationFunction(newValue);
            }

            m_boxed = new Box<T>(newValue, newExpiration);
        }

        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);

        private readonly Func<CancellationToken, Task<T>> m_createFactory;
        private readonly Func<T, CancellationToken, Task<T>> m_updateFactory;
        private readonly TimeSpan? m_timeToLive;
        private readonly Func<T, DateTime> m_expirationFunction;

        private Box<T> m_boxed;

        private bool m_isDisposed;
    }
}
