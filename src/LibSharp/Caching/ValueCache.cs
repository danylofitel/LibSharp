// Copyright (c) 2026 Danylo Fitel

using System;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// Value cache with ThreadSafetyMode.ExecutionAndPublication.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>Should not be used with IDisposable value types since it does not dispose of expired values.</remarks>
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
        if (m_boxed is null || UtcNow >= m_boxed.Expiration)
        {
            lock (m_lock)
            {
                if (m_boxed is null || UtcNow >= m_boxed.Expiration)
                {
                    Refresh();
                }

                // Refresh guarantees m_boxed is non-null on return.
                return m_boxed!.Value;
            }
        }

        return m_boxed.Value;
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
}
