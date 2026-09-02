// Copyright (c) 2026 Danylo Fitel

using System;
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

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_createFactory = factory;
        m_expirationFunction = _ => GetExpiration(timeToLive);
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

        m_timeProvider = timeProvider ?? TimeProvider.System;
        m_createFactory = factory;
        m_expirationFunction = expirationFunction;
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

        m_timeProvider = timeProvider ?? TimeProvider.System;
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
    /// <param name="timeProvider">(Optional) Time provider used for expiration. Defaults to <see cref="TimeProvider.System"/>.</param>
    public ValueCache(Func<T> createFactory, Func<T, T> updateFactory, Func<T, DateTime> expirationFunction, TimeProvider? timeProvider = null)
    {
        Argument.NotNull(createFactory);
        Argument.NotNull(updateFactory);
        Argument.NotNull(expirationFunction);

        m_timeProvider = timeProvider ?? TimeProvider.System;
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
        // Snapshot the volatile field once. ValueReference is immutable, so a non-null reference is
        // always a fully constructed, consistent object, and reading it once means the value returned
        // is the same one whose expiration was checked.
        ValueReference<T>? boxed = m_boxed;
        if (boxed is null || UtcNow >= boxed.Expiration)
        {
            lock (m_lock)
            {
                boxed = m_boxed;
                if (boxed is null || UtcNow >= boxed.Expiration)
                {
                    // The monitor is re-entrant, so a factory that reads this same cache would
                    // re-enter here, still find no published value, and call the factory again,
                    // recursing until the stack overflows and takes the process with it. Fail fast
                    // instead, the way Lazy<T> reports recursive initialization.
                    //
                    // m_isRefreshing needs no synchronisation of its own: it is only ever touched
                    // under m_lock, and only the thread already holding that lock can observe it
                    // set. Any other thread is blocked at the lock and never sees it.
                    if (m_isRefreshing)
                    {
                        throw new InvalidOperationException(
                            "The value factory attempted to read the cache it is refreshing. Re-entering a cache from its own value factory is not supported.");
                    }

                    m_isRefreshing = true;
                    try
                    {
                        Refresh();
                    }
                    finally
                    {
                        // Reset even when the factory throws, so one failure does not wedge the cache.
                        m_isRefreshing = false;
                    }

                    boxed = m_boxed;
                }

                // Refresh guarantees m_boxed is non-null on return.
                return boxed!.Value;
            }
        }

        return boxed.Value;
    }

    private DateTime UtcNow => m_timeProvider.GetUtcNow().UtcDateTime;

    private DateTime GetExpiration(TimeSpan timeToLive)
    {
        DateTime now = UtcNow;
        return timeToLive >= DateTime.MaxValue - now
            ? DateTime.MaxValue
            : now.Add(timeToLive);
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
    private readonly TimeProvider m_timeProvider;

    private readonly Func<T> m_createFactory;
    private readonly Func<T, T>? m_updateFactory;
    private readonly Func<T, DateTime> m_expirationFunction;

    private volatile ValueReference<T>? m_boxed;

    // Guards against a value factory re-entering this cache. Written and read only under m_lock.
    private bool m_isRefreshing;
}
