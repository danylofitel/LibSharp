// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching
{
    /// <summary>
    /// Async in-memory key-value cache.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    public class KeyValueCacheAsync<TKey, TValue> : IKeyValueCacheAsync<TKey, TValue>, IDisposable
        where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueCacheAsync{TKey, TValue}"/> class from a value factory.
        /// </summary>
        /// <param name="factory">Value factory.</param>
        /// <param name="timeToLive">Cache time-to-live.</param>
        public KeyValueCacheAsync(Func<TKey, CancellationToken, Task<TValue>> factory, TimeSpan timeToLive)
        {
            Argument.NotNull(factory, nameof(factory));
            Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

            m_factory = factory;
            m_timeToLive = timeToLive;
        }

        /// <inheritdoc/>
        public async Task<TValue> GetValueAsync(TKey key, CancellationToken cancellationToken = default)
        {
            Argument.NotNull(key, nameof(key));

            if (m_isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            /* ValueCacheAsync is disposable, so we should to call Dispose() on every created instance.
             *
             * GetOrAdd function of ConcurrentDictionary allows multiple invocations of the factory function
             * when concurrent calls are made for the same key.
             *
             * Those additional instances will not be disposed of if they implement IDisposable.
             *
             * Wrapping individual value caches in Lazy avoids that. This call to GetOrAdd only initializes the key-value pair,
             * where value is a Lazy that has not been instantiated yet.
             *
             * This will not invoke the factory method yet.
             */
            Lazy<ValueCacheAsync<TValue>> lazyValueCache = m_cache.GetOrAdd(
                key,
                cacheKey => new Lazy<ValueCacheAsync<TValue>>(
                    () => new ValueCacheAsync<TValue>((token) => m_factory(cacheKey, token), m_timeToLive),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            /*
             * Now that the value cache for the key has been initialized with a single instance,
             * get the value cache from the Lazy wrapper.
             *
             * This call will create the value cache object if it has not been created yet.
             *
             * This will not invoke the factory method yet.
             */
            ValueCacheAsync<TValue> valueCache = lazyValueCache.Value;

            /*
             * Delegate the call to the value cache instance for the given key.
             *
             * This will invoke the factory method if the value has not been initialized yet or if it has expired.
             */
            return await valueCache.GetValueAsync(cancellationToken).ConfigureAwait(false);
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
                    foreach (Lazy<ValueCacheAsync<TValue>> cache in m_cache.Values)
                    {
                        if (cache.IsValueCreated)
                        {
                            cache.Value.Dispose();
                        }
                    }
                }

                m_isDisposed = true;
            }
        }

        private readonly ConcurrentDictionary<TKey, Lazy<ValueCacheAsync<TValue>>> m_cache = new ConcurrentDictionary<TKey, Lazy<ValueCacheAsync<TValue>>>();

        private readonly Func<TKey, CancellationToken, Task<TValue>> m_factory;
        private readonly TimeSpan m_timeToLive;

        private bool m_isDisposed;
    }
}
