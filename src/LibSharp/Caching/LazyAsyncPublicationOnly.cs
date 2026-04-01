// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// Async lazy with LazyThreadSafetyMode.PublicationOnly.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// Should not be used with IDisposable or IAsyncDisposable value types since it does not dispose of values.
/// Concurrent callers may execute the factory more than once; only the first successfully published value is retained and returned to all callers.
/// Faulted or canceled attempts are not cached and may be retried by later callers.
/// </remarks>
public sealed class LazyAsyncPublicationOnly<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LazyAsyncPublicationOnly{T}"/> class from a value.
    /// </summary>
    /// <param name="value">The value to hold.</param>
    public LazyAsyncPublicationOnly(T value)
    {
        m_value = new ValueReference<T>(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyAsyncPublicationOnly{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    public LazyAsyncPublicationOnly(Func<CancellationToken, Task<T>> factory)
    {
        Argument.NotNull(factory, nameof(factory));

        m_factory = factory;
    }

    /// <summary>
    /// Gets a value indicating whether the value has been initialized.
    /// </summary>
    public bool HasValue => m_value is not null;

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before a published value is produced.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the value factory returns a null task.</exception>
    public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        if (!HasValue)
        {
            Task<T> factoryTask = m_factory(cancellationToken)
                ?? throw new InvalidOperationException("The value factory returned a null task.");
            T value = await factoryTask.ConfigureAwait(false);
            _ = Interlocked.CompareExchange(ref m_value, new ValueReference<T>(value), null);
        }

        return m_value.Value;
    }

    private readonly Func<CancellationToken, Task<T>> m_factory;
    private volatile ValueReference<T> m_value;
}
