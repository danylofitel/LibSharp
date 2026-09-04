// Copyright (c) 2026 Danylo Fitel

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
/// Concurrent callers may execute the factory more than once; only the first successfully published value is retained and returned to all callers.
/// Faulted or canceled attempts are not cached and may be retried by later callers.
/// <para>
/// Should not be used with <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/> value types.
/// When callers race, every losing racer's value is dropped without being disposed, and no caller
/// ever sees it to dispose it itself.
/// </para>
/// </remarks>
public sealed class LazyAsyncPublicationOnly<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LazyAsyncPublicationOnly{T}"/> class from a value.
    /// </summary>
    /// <param name="value">The value to hold.</param>
    public LazyAsyncPublicationOnly(T value)
    {
        _value = new ValueReference<T>(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyAsyncPublicationOnly{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    public LazyAsyncPublicationOnly(Func<CancellationToken, Task<T>> factory)
    {
        Argument.NotNull(factory);

        _factory = factory;
    }

    /// <summary>
    /// Gets a value indicating whether the value has been initialized.
    /// </summary>
    public bool HasValue => _value is not null;

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before a published value is produced.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the value factory returns a null task.</exception>
    public ValueTask<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ValueReference<T>? value = _value;
        if (value is not null)
        {
            return new ValueTask<T>(value.Value);
        }

        return InitializeAsync(cancellationToken);
    }

    private async ValueTask<T> InitializeAsync(CancellationToken cancellationToken)
    {
        // _factory is non-null whenever _value is null: the value constructor publishes
        // _value, and the factory constructor sets _factory.
        Task<T> factoryTask = _factory!(cancellationToken)
            ?? throw new InvalidOperationException("The value factory returned a null task.");
        T value = await factoryTask.ConfigureAwait(false);
        _ = Interlocked.CompareExchange(ref _value, new ValueReference<T>(value), null);

        // _value is non-null here: this call published it, or a concurrent caller won the race.
        return _value!.Value;
    }

    private readonly Func<CancellationToken, Task<T>>? _factory;
    private volatile ValueReference<T>? _value;
}
