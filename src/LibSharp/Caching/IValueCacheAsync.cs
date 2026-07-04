// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching;

/// <summary>
/// Interface for an async value cache.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// This interface does not extend <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/> because
/// not every implementation owns resources. Some implementations do, however, and expose those
/// interfaces on the concrete type (for example <see cref="ValueCacheAsync{T}"/> is
/// <see cref="IDisposable"/> and <see cref="ProactiveAsyncCache{T}"/> is
/// <see cref="IAsyncDisposable"/>). When you hold a concrete cache, check for those interfaces and
/// dispose accordingly; when you hold only <see cref="IValueCacheAsync{T}"/>, the owner of the
/// instance is responsible for its disposal.
/// </remarks>
public interface IValueCacheAsync<T>
{
    /// <summary>
    /// Gets a value indicating whether the cache has been initialized.
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// Gets the expiration time of the current value.
    /// </summary>
    DateTime? Expiration { get; }

    /// <summary>
    /// Gets the cached value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the cache has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the value is produced.</exception>
    Task<T> GetValueAsync(CancellationToken cancellationToken = default);
}
