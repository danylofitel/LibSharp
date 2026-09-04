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
/// <para>
/// Should not be used with IDisposable value types since it does not dispose of expired values.
/// Values are replaced in place as they expire, and entries are retained for the lifetime of the
/// cache, so a disposable value leaks once per expiry per key. This type never disposes the value,
/// so disposal remains the caller's responsibility.
/// </para>
/// <para>
/// The value factory must not call <see cref="GetValue"/> on this same cache for the same key.
/// Doing so throws <see cref="InvalidOperationException"/>, the way <see cref="Lazy{T}"/> reports
/// recursive initialization. Re-entering for a <em>different</em> key is safe: each key gets its
/// own value cache with its own lock.
/// </para>
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
        Argument.NotNull(factory);
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = factory;
        _timeToLive = timeToLive;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueCache{TKey, TValue}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCache(Func<TKey, TValue> factory, Func<TKey, TValue, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory);
        Argument.NotNull(expirationFunction);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = factory;
        _expirationFunction = expirationFunction;
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
        Argument.NotNull(createFactory);
        Argument.NotNull(updateFactory);
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = createFactory;
        _updateFactory = updateFactory;
        _timeToLive = timeToLive;
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
        Argument.NotNull(createFactory);
        Argument.NotNull(updateFactory);
        Argument.NotNull(expirationFunction);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = createFactory;
        _updateFactory = updateFactory;
        _expirationFunction = expirationFunction;
    }

    /// <summary>
    /// Gets the number of entries the cache is holding.
    /// </summary>
    /// <remarks>
    /// Entries are never evicted, so this is the number of distinct keys ever requested, including
    /// those whose value has since expired. It is the measure to watch when confirming that a key
    /// space really is bounded.
    /// <para>
    /// Not free: reading it takes every bucket lock of the underlying <see cref="ConcurrentDictionary{TKey, TValue}"/>
    /// and so contends with concurrent writers. Sample it periodically; do not read it per request.
    /// </para>
    /// </remarks>
    public int Count => _cache.Count;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value factory reads the cache it is refreshing. Re-entering from the
    /// factory is not supported.
    /// </exception>
    /// <remarks>Re-entering for a different key is allowed.</remarks>
    public TValue GetValue(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // The factory is static and receives `this` as state, so a read allocates no delegate:
        // Roslyn caches only lambdas that capture nothing.
        Lazy<ValueCache<TValue>> lazyValueCache = _cache.GetOrAdd(
            key,
            static (cacheKey, self) => new Lazy<ValueCache<TValue>>(
                () => self.CreateValueCache(cacheKey),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        return lazyValueCache.Value.GetValue();
    }

    private ValueCache<TValue> CreateValueCache(TKey key)
    {
        if (_updateFactory is null)
        {
            return _timeToLive.HasValue
                ? new ValueCache<TValue>(() => _createFactory(key), _timeToLive.Value, _timeProvider)
                : new ValueCache<TValue>(() => _createFactory(key), value => _expirationFunction!(key, value), _timeProvider);
        }

        return _timeToLive.HasValue
            ? new ValueCache<TValue>(() => _createFactory(key), value => _updateFactory(key, value), _timeToLive.Value, _timeProvider)
            : new ValueCache<TValue>(() => _createFactory(key), value => _updateFactory(key, value), value => _expirationFunction!(key, value), _timeProvider);
    }

    private readonly ConcurrentDictionary<TKey, Lazy<ValueCache<TValue>>> _cache = new ConcurrentDictionary<TKey, Lazy<ValueCache<TValue>>>();
    private readonly TimeProvider _timeProvider;

    // Exactly one of _timeToLive / _expirationFunction is set by each constructor; _updateFactory
    // is null when the cache was created without an update factory. The forgiving access to
    // _expirationFunction in CreateValueCache is guarded by this invariant.
    private readonly Func<TKey, TValue> _createFactory;
    private readonly Func<TKey, TValue, TValue>? _updateFactory;
    private readonly TimeSpan? _timeToLive;
    private readonly Func<TKey, TValue, DateTime>? _expirationFunction;
}
