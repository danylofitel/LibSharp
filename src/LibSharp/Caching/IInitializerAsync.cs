// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching;

/// <summary>
/// Thread-safe lazy value initializer.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
public interface IInitializerAsync<T>
{
    /// <summary>
    /// Gets a value indicating whether the value has been created.
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// Gets the value, creates it if it has not been initialized.
    /// Thread-safe, only one successful factory result will ever be published.
    /// </summary>
    /// <param name="factory">Value factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value.</returns>
    /// <remarks>
    /// If the factory faults or is canceled, the value is not considered initialized and a later call may retry.
    /// Publication-only implementations may execute multiple factories concurrently, but only one successful result will be published.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the initializer has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the value is produced.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="factory"/> returns a null task.</exception>
    Task<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default);
}
