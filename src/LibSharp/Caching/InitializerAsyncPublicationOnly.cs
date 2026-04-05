// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// Async initializer with LazyThreadSafetyMode.PublicationOnly.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// Should not be used with IDisposable or IAsyncDisposable value types since it does not dispose of values.
/// Concurrent callers may execute different factories more than once; only the first successfully published value is retained and returned to all callers.
/// Faulted or canceled attempts are not cached and may be retried by later callers.
/// </remarks>
public sealed class InitializerAsyncPublicationOnly<T> : IInitializerAsync<T>
{
    /// <inheritdoc/>
    public bool HasValue => m_value is not null;

    /// <inheritdoc/>
    public async Task<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        Argument.NotNull(factory, nameof(factory));

        if (!HasValue)
        {
            Task<T> factoryTask = factory(cancellationToken)
                ?? throw new InvalidOperationException("The value factory returned a null task.");
            T value = await factoryTask.ConfigureAwait(false);
            _ = Interlocked.CompareExchange(ref m_value, new ValueReference<T>(value), null);
        }

        return m_value.Value;
    }

    private volatile ValueReference<T> m_value;
}
