// Copyright (c) LibSharp. All rights reserved.

using System;
using LibSharp.Common;

namespace LibSharp.Caching
{
    /// <summary>
    /// Value cache with ThreadSafetyMode.ExecutionAndPublication.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <remarks>Should not be used with IDisposable value types since it does not dispose of expired values.</remarks>
    public class ValueCache<T> : IValueCache<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCache{T}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">The value factory.</param>
        /// <param name="timeToLive">Cache time-to-live.</param>
        public ValueCache(Func<T> factory, TimeSpan timeToLive)
        {
            Argument.NotNull(factory, nameof(factory));
            Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

            m_createFactory = factory;
            m_expirationFunction = _ => GetExpiration(timeToLive);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCache{T}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">The value factory.</param>
        /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
        public ValueCache(Func<T> factory, Func<T, DateTime> expirationFunction)
        {
            Argument.NotNull(factory, nameof(factory));
            Argument.NotNull(expirationFunction, nameof(expirationFunction));

            m_createFactory = factory;
            m_expirationFunction = expirationFunction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCache{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
        /// </summary>
        /// <param name="createFactory">The creation factory.</param>
        /// <param name="updateFactory">The update factory.</param>
        /// <param name="timeToLive">Cache time-to-live.</param>
        public ValueCache(Func<T> createFactory, Func<T, T> updateFactory, TimeSpan timeToLive)
        {
            Argument.NotNull(createFactory, nameof(createFactory));
            Argument.NotNull(updateFactory, nameof(updateFactory));
            Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

            m_createFactory = createFactory;
            m_updateFactory = updateFactory;
            m_expirationFunction = _ => GetExpiration(timeToLive);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCache{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
        /// </summary>
        /// <param name="createFactory">The creation factory.</param>
        /// <param name="updateFactory">The update factory.</param>
        /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
        public ValueCache(Func<T> createFactory, Func<T, T> updateFactory, Func<T, DateTime> expirationFunction)
        {
            Argument.NotNull(createFactory, nameof(createFactory));
            Argument.NotNull(updateFactory, nameof(updateFactory));
            Argument.NotNull(expirationFunction, nameof(expirationFunction));

            m_createFactory = createFactory;
            m_updateFactory = updateFactory;
            m_expirationFunction = expirationFunction;
        }

        /// <inheritdoc/>
        public bool HasValue => m_boxed is not null;

        /// <inheritdoc/>
        public DateTime? Expiration => m_boxed?.Expiration;

        /// <inheritdoc/>
        public T GetValue()
        {
            if (m_boxed is null || DateTime.UtcNow >= m_boxed.Expiration)
            {
                lock (m_lock)
                {
                    if (m_boxed is null || DateTime.UtcNow >= m_boxed.Expiration)
                    {
                        Refresh();
                    }
                }
            }

            return m_boxed.Value;
        }

        private static DateTime GetExpiration(TimeSpan timeToLive)
        {
            return timeToLive == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.UtcNow.Add(timeToLive);
        }

        private void Refresh()
        {
            T newValue;
            if (m_updateFactory is null || m_boxed is null)
            {
                newValue = m_createFactory();
            }
            else
            {
                newValue = m_updateFactory(m_boxed.Value);
            }

            DateTime newExpiration = m_expirationFunction(newValue);

            m_boxed = new ValueReference<T>(newValue, newExpiration);
        }

        private readonly object m_lock = new object();

        private readonly Func<T> m_createFactory;
        private readonly Func<T, T> m_updateFactory;
        private readonly Func<T, DateTime> m_expirationFunction;

        private ValueReference<T> m_boxed;
    }
}
