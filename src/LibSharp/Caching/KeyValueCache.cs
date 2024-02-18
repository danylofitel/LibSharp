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

            m_factory = factory;
            m_timeToLive = timeToLive;
        }

        /// <inheritdoc/>
        public TValue GetValue(TKey key)
        {
            Argument.NotNull(key, nameof(key));

            ValueCache<TValue> valueCache = m_cache.GetOrAdd(
                key,
                cacheKey => new ValueCache<TValue>(() => m_factory(cacheKey), m_timeToLive));

            return valueCache.GetValue();
        }

        private readonly ConcurrentDictionary<TKey, ValueCache<TValue>> m_cache = new ConcurrentDictionary<TKey, ValueCache<TValue>>();

        private readonly Func<TKey, TValue> m_factory;
        private readonly TimeSpan m_timeToLive;
    }
}
