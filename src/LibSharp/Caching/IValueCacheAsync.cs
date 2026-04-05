// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching;

/// <summary>
/// Interface for an async value cache.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
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
