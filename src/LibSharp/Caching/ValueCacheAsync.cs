// Copyright (c) LibSharp. All rights reserved.

using System;
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
            m_expirationFunction = _ => GetExpiration(timeToLive);
            m_disposalToken = m_disposalCts.Token;
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
            m_disposalToken = m_disposalCts.Token;
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
            m_expirationFunction = _ => GetExpiration(timeToLive);
            m_disposalToken = m_disposalCts.Token;
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
            m_disposalToken = m_disposalCts.Token;
        }

        /// <inheritdoc/>
        public bool HasValue
        {
            get
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

                return m_boxed is not null;
            }
        }

        /// <inheritdoc/>
        public DateTime? Expiration
        {
            get
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

                return m_boxed?.Expiration;
            }
        }

        /// <inheritdoc/>
        public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            if (m_boxed is null || DateTime.UtcNow >= m_boxed.Expiration)
            {
                try
                {
                    // Link the caller's token with the disposal token so that pending waiters
                    // are unblocked immediately when the cache is disposed.
                    using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(m_disposalToken, cancellationToken);
                    await m_semaphore.WaitAsync(linked.Token).ConfigureAwait(false);

                    try
                    {
                        if (m_boxed is null || DateTime.UtcNow >= m_boxed.Expiration)
                        {
                            await Refresh(cancellationToken).ConfigureAwait(false);
                        }

                        return m_boxed.Value;
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

        private static DateTime GetExpiration(TimeSpan timeToLive)
        {
            DateTime now = DateTime.UtcNow;
            return timeToLive >= DateTime.MaxValue - now
                ? DateTime.MaxValue
                : now.Add(timeToLive);
        }

        private async Task Refresh(CancellationToken cancellationToken)
        {
            T newValue;
            if (m_updateFactory is null || m_boxed is null)
            {
                newValue = await m_createFactory(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                newValue = await m_updateFactory(m_boxed.Value, cancellationToken).ConfigureAwait(false);
            }

            DateTime newExpiration = m_expirationFunction(newValue);

            m_boxed = new ValueReference<T>(newValue, newExpiration);
        }

        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource m_disposalCts = new CancellationTokenSource();
        private readonly CancellationToken m_disposalToken;

        private readonly Func<CancellationToken, Task<T>> m_createFactory;
        private readonly Func<T, CancellationToken, Task<T>> m_updateFactory;
        private readonly Func<T, DateTime> m_expirationFunction;

        private volatile ValueReference<T> m_boxed;

        private int m_isDisposed;
    }
}
