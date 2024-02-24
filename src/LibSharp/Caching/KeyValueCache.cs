// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Concurrent;
using LibSharp.Common;

namespace LibSharp.Caching
{
    /// <summary>
    /// In-memory key-value cache.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    public class KeyValueCache<TKey, TValue> : IKeyValueCache<TKey, TValue>
        where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueCache{TKey, TValue}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">Value factory.</param>
        /// <param name="timeToLive">Cache time-to-live.</param>
        public KeyValueCache(Func<TKey, TValue> factory, TimeSpan timeToLive)
        {
            Argument.NotNull(factory, nameof(factory));
            Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

            m_createFactory = factory;
            m_timeToLive = timeToLive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueCache{TKey, TValue}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">The value factory.</param>
        /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
        public KeyValueCache(Func<TKey, TValue> factory, Func<TKey, TValue, DateTime> expirationFunction)
        {
            Argument.NotNull(factory, nameof(factory));
            Argument.NotNull(expirationFunction, nameof(expirationFunction));

            m_createFactory = factory;
            m_expirationFunction = expirationFunction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueCache{TKey, TValue}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
        /// </summary>
        /// <param name="createFactory">The creation factory.</param>
        /// <param name="updateFactory">The update factory.</param>
        /// <param name="timeToLive">Cache time-to-live.</param>
        public KeyValueCache(Func<TKey, TValue> createFactory, Func<TKey, TValue, TValue> updateFactory, TimeSpan timeToLive)
        {
            Argument.NotNull(createFactory, nameof(createFactory));
            Argument.NotNull(updateFactory, nameof(updateFactory));
            Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

            m_createFactory = createFactory;
            m_updateFactory = updateFactory;
            m_timeToLive = timeToLive;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueCache{TKey, TValue}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
        /// </summary>
        /// <param name="createFactory">The creation factory.</param>
        /// <param name="updateFactory">The update factory.</param>
        /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
        public KeyValueCache(Func<TKey, TValue> createFactory, Func<TKey, TValue, TValue> updateFactory, Func<TKey, TValue, DateTime> expirationFunction)
        {
            Argument.NotNull(createFactory, nameof(createFactory));
            Argument.NotNull(updateFactory, nameof(updateFactory));
            Argument.NotNull(expirationFunction, nameof(expirationFunction));

            m_createFactory = createFactory;
            m_updateFactory = updateFactory;
            m_expirationFunction = expirationFunction;
        }

        /// <inheritdoc/>
        public TValue GetValue(TKey key)
        {
            Argument.NotNull(key, nameof(key));

            ValueCache<TValue> valueCache = m_cache.GetOrAdd(key, CreateValueCache);

            return valueCache.GetValue();
        }

        private ValueCache<TValue> CreateValueCache(TKey key)
        {
            if (m_updateFactory is null)
            {
                return m_timeToLive.HasValue
                    ? new ValueCache<TValue>(() => m_createFactory(key), m_timeToLive.Value)
                    : new ValueCache<TValue>(() => m_createFactory(key), value => m_expirationFunction(key, value));
            }

            return m_timeToLive.HasValue
                ? new ValueCache<TValue>(() => m_createFactory(key), value => m_updateFactory(key, value), m_timeToLive.Value)
                : new ValueCache<TValue>(() => m_createFactory(key), value => m_updateFactory(key, value), value => m_expirationFunction(key, value));
        }

        private readonly ConcurrentDictionary<TKey, ValueCache<TValue>> m_cache = new ConcurrentDictionary<TKey, ValueCache<TValue>>();

        private readonly Func<TKey, TValue> m_createFactory;
        private readonly Func<TKey, TValue, TValue> m_updateFactory;
        private readonly TimeSpan? m_timeToLive;
        private readonly Func<TKey, TValue, DateTime> m_expirationFunction;
    }
}
