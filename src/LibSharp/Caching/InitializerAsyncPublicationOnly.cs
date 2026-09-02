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
/// Concurrent callers may execute different factories more than once; only the first successfully published value is retained and returned to all callers.
/// Faulted or canceled attempts are not cached and may be retried by later callers.
/// <para>
/// Should not be used with <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/> value types.
/// When callers race, every losing racer's value is dropped without being disposed, and no caller
/// ever sees it to dispose it itself.
/// </para>
/// </remarks>
public sealed class InitializerAsyncPublicationOnly<T> : IInitializerAsync<T>
{
    /// <inheritdoc/>
    public bool HasValue => m_value is not null;

    /// <inheritdoc/>
    public ValueTask<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        Argument.NotNull(factory);

        ValueReference<T>? value = m_value;
        if (value is not null)
        {
            return new ValueTask<T>(value.Value);
        }

        return InitializeAsync(factory, cancellationToken);
    }

    private async ValueTask<T> InitializeAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        Task<T> factoryTask = factory(cancellationToken)
            ?? throw new InvalidOperationException("The value factory returned a null task.");
        T value = await factoryTask.ConfigureAwait(false);
        _ = Interlocked.CompareExchange(ref m_value, new ValueReference<T>(value), null);

        // m_value is non-null here: this call published it, or a concurrent caller won the race.
        return m_value!.Value;
    }

    private volatile ValueReference<T>? m_value;
}
