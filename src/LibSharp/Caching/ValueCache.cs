// Copyright (c) 2026 Danylo Fitel

using System;
using System.Diagnostics.CodeAnalysis;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// Value cache with ThreadSafetyMode.ExecutionAndPublication.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// Should not be used with IDisposable value types since it does not dispose of expired values.
/// <para>
/// The value factory must not call <see cref="GetValue"/> on this same cache. Doing so throws
/// <see cref="InvalidOperationException"/>, the way <see cref="Lazy{T}"/> reports recursive
/// initialization, rather than recursing until the stack overflows.
/// </para>
/// </remarks>
public sealed class ValueCache<T> : IValueCache<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCache{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCache(Func<T> factory, TimeSpan timeToLive, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory);
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = factory;
        _expirationFunction = _ => GetExpiration(timeToLive);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCache{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCache(Func<T> factory, Func<T, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(factory);
        Argument.NotNull(expirationFunction);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = factory;
        _expirationFunction = expirationFunction;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCache{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCache(Func<T> createFactory, Func<T, T> updateFactory, TimeSpan timeToLive, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(createFactory);
        Argument.NotNull(updateFactory);
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = createFactory;
        _updateFactory = updateFactory;
        _expirationFunction = _ => GetExpiration(timeToLive);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCache{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCache(Func<T> createFactory, Func<T, T> updateFactory, Func<T, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(createFactory);
        Argument.NotNull(updateFactory);
        Argument.NotNull(expirationFunction);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _createFactory = createFactory;
        _updateFactory = updateFactory;
        _expirationFunction = expirationFunction;
    }

    /// <inheritdoc/>
    public bool HasValue => _boxed is not null;

    /// <inheritdoc/>
    public DateTime? Expiration => _boxed?.Expiration;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value factory reads the cache it is refreshing. Re-entering from the
    /// factory is not supported.
    /// </exception>
    public T GetValue()
    {
        // Snapshot the volatile field once. ValueReference is immutable, so a non-null reference is
        // always a fully constructed, consistent object, and reading it once means the value returned
        // is the same one whose expiration was checked.
        ValueReference<T>? boxed = _boxed;
        if (boxed is null || UtcNow >= boxed.Expiration)
        {
            lock (_lock)
            {
                boxed = _boxed;
                if (boxed is null || UtcNow >= boxed.Expiration)
                {
                    // The monitor is re-entrant, so a factory that reads this same cache would
                    // re-enter here, still find no published value, and call the factory again,
                    // recursing until the stack overflows and takes the process with it. Fail fast
                    // instead, the way Lazy<T> reports recursive initialization.
                    //
                    // _isRefreshing needs no synchronisation of its own: it is only ever touched
                    // under _lock, and only the thread already holding that lock can observe it
                    // set. Any other thread is blocked at the lock and never sees it.
                    if (_isRefreshing)
                    {
                        throw new InvalidOperationException(
                            "The value factory attempted to read the cache it is refreshing. Re-entering a cache from its own value factory is not supported.");
                    }

                    _isRefreshing = true;
                    try
                    {
                        Refresh();
                    }
                    finally
                    {
                        // Reset even when the factory throws, so one failure does not wedge the cache.
                        _isRefreshing = false;
                    }

                    boxed = _boxed;
                }

                return boxed.Value;
            }
        }

        return boxed.Value;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    private DateTime GetExpiration(TimeSpan timeToLive)
    {
        DateTime now = UtcNow;
        return timeToLive >= DateTime.MaxValue - now
            ? DateTime.MaxValue
            : now.Add(timeToLive);
    }

    [MemberNotNull(nameof(_boxed))]
    private void Refresh()
    {
        T newValue;
        if (_updateFactory is null || _boxed is null)
        {
            newValue = _createFactory();
        }
        else
        {
            newValue = _updateFactory(_boxed.Value);
        }

        DateTime newExpiration = _expirationFunction(newValue);

        _boxed = new ValueReference<T>(newValue, newExpiration);
    }

    private readonly object _lock = new object();
    private readonly TimeProvider _timeProvider;

    private readonly Func<T> _createFactory;
    private readonly Func<T, T>? _updateFactory;
    private readonly Func<T, DateTime> _expirationFunction;

    private volatile ValueReference<T>? _boxed;

    // Guards against a value factory re-entering this cache. Written and read only under _lock.
    private bool _isRefreshing;
}
