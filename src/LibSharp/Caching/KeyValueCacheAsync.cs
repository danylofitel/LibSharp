// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// Async in-memory key-value cache.
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
/// cache, so a disposable value leaks once per expiry per key. (The internal per-key caches are
/// disposed with this instance; the values they hold are not.)
/// </para>
/// <para>
/// The value factory must not call <see cref="GetValueAsync"/> on this same cache for the same key
/// and await the result. The per-key lock is held across the factory call and is not re-entrant, so
/// that deadlocks. Re-entering for a <em>different</em> key is safe.
/// </para>
/// </remarks>
public sealed class KeyValueCacheAsync<TKey, TValue> : IKeyValueCacheAsync<TKey, TValue>, IDisposable
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueCacheAsync{TKey, TValue}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">Value factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCacheAsync(Func<TKey, CancellationToken, Task<TValue>> factory, TimeSpan timeToLive, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory);
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = factory;
        _timeToLive = timeToLive;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueCacheAsync{TKey, TValue}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCacheAsync(Func<TKey, CancellationToken, Task<TValue>> factory, Func<TKey, TValue, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory);
        Argument.NotNull(expirationFunction);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = factory;
        _expirationFunction = expirationFunction;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueCacheAsync{TKey, TValue}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCacheAsync(Func<TKey, CancellationToken, Task<TValue>> createFactory, Func<TKey, TValue, CancellationToken, Task<TValue>> updateFactory, TimeSpan timeToLive, TimeProvider? timeProvider = null)
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
    /// Initializes a new instance of the <see cref="KeyValueCacheAsync{TKey, TValue}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public KeyValueCacheAsync(Func<TKey, CancellationToken, Task<TValue>> createFactory, Func<TKey, TValue, CancellationToken, Task<TValue>> updateFactory, Func<TKey, TValue, DateTime> expirationFunction, TimeProvider? timeProvider = null)
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
    /// <exception cref="ObjectDisposedException">Thrown if the cache has been disposed.</exception>
    public int Count
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);

            return _cache.Count;
        }
    }

    /// <inheritdoc/>
    public ValueTask<TValue> GetValueAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);

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
        // The factory is static and receives `this` as state, so a read allocates no delegate:
        // Roslyn caches only lambdas that capture nothing.
        Lazy<ValueCacheAsync<TValue>> lazyValueCache = _cache.GetOrAdd(
            key,
            static (cacheKey, self) => new Lazy<ValueCacheAsync<TValue>>(
                () => self.CreateValueCache(cacheKey),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        // Re-check after GetOrAdd to avoid leaking entries added concurrently with Dispose.
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) != 0, this);

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
         * Final disposal check after evaluating the Lazy. If Dispose ran between the
         * previous check and here — either because the Lazy had IsValueCreated==false
         * when Dispose iterated _cache, or because ConcurrentDictionary's enumerator
         * snapshot missed this entry — this ValueCacheAsync will not be cleaned up by
         * the Dispose path. Detect that case and dispose it ourselves to prevent a leak.
        */
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            valueCache.Dispose();
            throw new ObjectDisposedException(GetType().Name);
        }

        /*
         * Delegate the call to the value cache instance for the given key.
         *
         * This will invoke the factory method if the value has not been initialized yet or if it has expired.
         */
        // Returned directly rather than awaited: this method has no work of its own after the
        // delegation, so not being async saves a second state machine and a second allocation
        // on top of whatever the per-key cache does.
        return valueCache.GetValueAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes of the cache.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
            return;
        }

        foreach (Lazy<ValueCacheAsync<TValue>> cache in _cache.Values)
        {
            if (cache.IsValueCreated)
            {
                cache.Value.Dispose();
            }
        }

        _cache.Clear();
    }

    private ValueCacheAsync<TValue> CreateValueCache(TKey key)
    {
        if (_updateFactory is null)
        {
            return _timeToLive.HasValue
                ? new ValueCacheAsync<TValue>((token) => _createFactory(key, token), _timeToLive.Value, _timeProvider)
                : new ValueCacheAsync<TValue>((token) => _createFactory(key, token), value => _expirationFunction!(key, value), _timeProvider);
        }

        return _timeToLive.HasValue
            ? new ValueCacheAsync<TValue>((token) => _createFactory(key, token), (value, token) => _updateFactory(key, value, token), _timeToLive.Value, _timeProvider)
            : new ValueCacheAsync<TValue>((token) => _createFactory(key, token), (value, token) => _updateFactory(key, value, token), value => _expirationFunction!(key, value), _timeProvider);
    }

    private readonly ConcurrentDictionary<TKey, Lazy<ValueCacheAsync<TValue>>> _cache = new ConcurrentDictionary<TKey, Lazy<ValueCacheAsync<TValue>>>();
    private readonly TimeProvider _timeProvider;

    // Exactly one of _timeToLive / _expirationFunction is set by each constructor; _updateFactory
    // is null when the cache was created without an update factory. The forgiving access to
    // _expirationFunction in CreateValueCache is guarded by this invariant.
    private readonly Func<TKey, CancellationToken, Task<TValue>> _createFactory;
    private readonly Func<TKey, TValue, CancellationToken, Task<TValue>>? _updateFactory;
    private readonly TimeSpan? _timeToLive;
    private readonly Func<TKey, TValue, DateTime>? _expirationFunction;

    private int _isDisposed;
}
