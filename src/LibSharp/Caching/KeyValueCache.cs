// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Concurrent;
using System.Threading;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// In-memory key-value cache.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
/// <remarks>
/// Entries are never evicted from the cache.
/// This is by design for bounded key spaces.
/// Do not use with unbounded key spaces as memory will grow monotonically.
/// </remarks>
public sealed class KeyValueCache<TKey, TValue> : IKeyValueCache<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueCache{TKey, TValue}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">Value factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCache(Func<TKey, TValue> factory, TimeSpan timeToLive, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory, nameof(factory));
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_createFactory = factory;
        m_timeToLive = timeToLive;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueCache{TKey, TValue}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCache(Func<TKey, TValue> factory, Func<TKey, TValue, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory, nameof(factory));
        Argument.NotNull(expirationFunction, nameof(expirationFunction));

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_createFactory = factory;
        m_expirationFunction = expirationFunction;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueCache{TKey, TValue}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCache(Func<TKey, TValue> createFactory, Func<TKey, TValue, TValue> updateFactory, TimeSpan timeToLive, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(createFactory, nameof(createFactory));
        Argument.NotNull(updateFactory, nameof(updateFactory));
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

        m_timeProvider = timeProvider ?? TimeProvider.System;
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
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCache(Func<TKey, TValue> createFactory, Func<TKey, TValue, TValue> updateFactory, Func<TKey, TValue, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(createFactory, nameof(createFactory));
        Argument.NotNull(updateFactory, nameof(updateFactory));
        Argument.NotNull(expirationFunction, nameof(expirationFunction));

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_createFactory = createFactory;
        m_updateFactory = updateFactory;
        m_expirationFunction = expirationFunction;
    }

    /// <inheritdoc/>
    public TValue GetValue(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        Lazy<ValueCache<TValue>> lazyValueCache = m_cache.GetOrAdd(
            key,
            cacheKey => new Lazy<ValueCache<TValue>>(
                () => CreateValueCache(cacheKey),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return lazyValueCache.Value.GetValue();
    }

    private ValueCache<TValue> CreateValueCache(TKey key)
    {
        if (m_updateFactory is null)
        {
            return m_timeToLive.HasValue
                ? new ValueCache<TValue>(() => m_createFactory(key), m_timeToLive.Value, m_timeProvider)
                : new ValueCache<TValue>(() => m_createFactory(key), value => m_expirationFunction!(key, value), m_timeProvider);
        }

        return m_timeToLive.HasValue
            ? new ValueCache<TValue>(() => m_createFactory(key), value => m_updateFactory(key, value), m_timeToLive.Value, m_timeProvider)
            : new ValueCache<TValue>(() => m_createFactory(key), value => m_updateFactory(key, value), value => m_expirationFunction!(key, value), m_timeProvider);
    }

    private readonly ConcurrentDictionary<TKey, Lazy<ValueCache<TValue>>> m_cache = new ConcurrentDictionary<TKey, Lazy<ValueCache<TValue>>>();
    private readonly TimeProvider m_timeProvider;

    // Exactly one of m_timeToLive / m_expirationFunction is set by each constructor; m_updateFactory
    // is null when the cache was created without an update factory. The forgiving access to
    // m_expirationFunction in CreateValueCache is guarded by this invariant.
    private readonly Func<TKey, TValue> m_createFactory;
    private readonly Func<TKey, TValue, TValue>? m_updateFactory;
    private readonly TimeSpan? m_timeToLive;
    private readonly Func<TKey, TValue, DateTime>? m_expirationFunction;
}
