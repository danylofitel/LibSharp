// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;
using LibSharp.Threading;

namespace LibSharp.Caching;

/// <summary>
/// Async in-memory value cache with ThreadSafetyMode.ExecutionAndPublication behavior.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>Should not be used with IDisposable value types since it does not dispose of expired values.</remarks>
public sealed class ValueCacheAsync<T> : IValueCacheAsync<T>, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    public ValueCacheAsync(Func<CancellationToken, Task<T>> factory, TimeSpan timeToLive)
    {
        Argument.NotNull(factory, nameof(factory));
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

        m_createFactory = factory;
        m_expirationFunction = _ => GetExpiration(timeToLive);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    public ValueCacheAsync(Func<CancellationToken, Task<T>> factory, Func<T, DateTime> expirationFunction)
    {
        Argument.NotNull(factory, nameof(factory));
        Argument.NotNull(expirationFunction, nameof(expirationFunction));

        m_createFactory = factory;
        m_expirationFunction = expirationFunction;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="timeToLive">Cache time-to-live.</param>
    public ValueCacheAsync(Func<CancellationToken, Task<T>> createFactory, Func<T, CancellationToken, Task<T>> updateFactory, TimeSpan timeToLive)
    {
        Argument.NotNull(createFactory, nameof(createFactory));
        Argument.NotNull(updateFactory, nameof(updateFactory));
        Argument.GreaterThanOrEqualTo(timeToLive, TimeSpan.Zero, nameof(timeToLive));

        m_createFactory = createFactory;
        m_updateFactory = updateFactory;
        m_expirationFunction = _ => GetExpiration(timeToLive);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCacheAsync{T}"/> class from a creation factory, used to initialize the cache, and update factory, used to refresh it.
    /// </summary>
    /// <param name="createFactory">The creation factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <param name="expirationFunction">Function to calculate expiration of a value.</param>
    public ValueCacheAsync(Func<CancellationToken, Task<T>> createFactory, Func<T, CancellationToken, Task<T>> updateFactory, Func<T, DateTime> expirationFunction)
    {
        Argument.NotNull(createFactory, nameof(createFactory));
        Argument.NotNull(updateFactory, nameof(updateFactory));
        Argument.NotNull(expirationFunction, nameof(expirationFunction));

        m_createFactory = createFactory;
        m_updateFactory = updateFactory;
        m_expirationFunction = expirationFunction;
    }

    /// <inheritdoc/>
    public bool HasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_boxed is not null;
        }
    }

    /// <inheritdoc/>
    public DateTime? Expiration
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_boxed?.Expiration;
        }
    }

    /// <inheritdoc/>
    public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

        if (m_boxed is null || DateTime.UtcNow >= m_boxed.Expiration)
        {
            using (await m_lock.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                if (m_boxed is null || DateTime.UtcNow >= m_boxed.Expiration)
                {
                    await Refresh(cancellationToken).ConfigureAwait(false);
                }

                return m_boxed.Value;
            }
        }

        return m_boxed.Value;
    }

    /// <summary>
    /// Disposes of the cache.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
        {
            return;
        }

        m_lock.Dispose();
    }

    private static DateTime GetExpiration(TimeSpan timeToLive)
    {
        DateTime now = DateTime.UtcNow;
        return timeToLive >= DateTime.MaxValue - now
            ? DateTime.MaxValue
            : now.Add(timeToLive);
    }

    private async Task Refresh(CancellationToken cancellationToken)
    {
        T newValue;
        if (m_updateFactory is null || m_boxed is null)
        {
            Task<T> createTask = m_createFactory(cancellationToken)
                ?? throw new InvalidOperationException("The value factory returned a null task.");
            newValue = await createTask.ConfigureAwait(false);
        }
        else
        {
            Task<T> updateTask = m_updateFactory(m_boxed.Value, cancellationToken)
                ?? throw new InvalidOperationException("The update factory returned a null task.");
            newValue = await updateTask.ConfigureAwait(false);
        }

        DateTime newExpiration = m_expirationFunction(newValue);

        m_boxed = new ValueReference<T>(newValue, newExpiration);
    }

    private readonly AsyncLock m_lock = new AsyncLock();
    private readonly Func<CancellationToken, Task<T>> m_createFactory;
    private readonly Func<T, CancellationToken, Task<T>> m_updateFactory;
    private readonly Func<T, DateTime> m_expirationFunction;

    private volatile ValueReference<T> m_boxed;

    private int m_isDisposed;
}
