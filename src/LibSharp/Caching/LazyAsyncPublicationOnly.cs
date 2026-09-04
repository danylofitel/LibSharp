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
/// When callers race, every losing racer's value is dropped. A dropped value is disposed by default
/// if it implements <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>: the compare-exchange
/// that publishes the winner names the losers exactly, so a dropped value is known never to have
/// reached a caller, and nothing else could release it. Pass <c>disposeDroppedValues: false</c> to
/// leave dropped values alone.
/// </para>
/// <para>
/// Automatic disposal assumes the factory returns a freshly created instance that it exclusively
/// owns. A factory returning a shared instance is still safe — identity is checked, so the published
/// value is never disposed — but one returning distinct values that share an owned resource is not,
/// and should turn disposal off.
/// </para>
/// <para>
/// This type never disposes the published value, so its disposal remains the caller's responsibility.
/// </para>
/// </remarks>
public sealed class LazyAsyncPublicationOnly<T> : ILazyAsync<T>
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
    /// <param name="disposeDroppedValues">
    /// (Optional) Whether a value that loses the publication race is disposed when it implements
    /// <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>. Defaults to <c>true</c>, which is
    /// the safe choice: a dropped value reaches no caller, so nothing else can release it. Pass
    /// <c>false</c> when the factory returns values that share an owned resource, or that something
    /// else is responsible for.
    /// </param>
    public LazyAsyncPublicationOnly(Func<CancellationToken, Task<T>> factory, bool disposeDroppedValues = true)
    {
        Argument.NotNull(factory);

        _factory = factory;
        _disposeDroppedValues = disposeDroppedValues;
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

        // The exchange names the winner: null back means this call published, anything else is the
        // value that got there first, and this one was never handed to a caller.
        ValueReference<T>? published = Interlocked.CompareExchange(ref _value, new ValueReference<T>(value), null);
        if (published is null)
        {
            return value;
        }

        if (_disposeDroppedValues)
        {
            await DroppedValue.DisposeAsync(value, published.Value).ConfigureAwait(false);
        }

        return published.Value;
    }

    private readonly Func<CancellationToken, Task<T>>? _factory;
    private readonly bool _disposeDroppedValues;
    private volatile ValueReference<T>? _value;
}
