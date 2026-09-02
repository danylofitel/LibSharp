// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching;

/// <summary>
/// Thread-safe lazy value initializer.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// This is deliberately distinct from <see cref="IValueCacheAsync{T}"/>, which captures the value
/// factory once at construction. An initializer instead accepts the factory at
/// <see cref="GetValueAsync(System.Func{System.Threading.CancellationToken, System.Threading.Tasks.Task{T}}, System.Threading.CancellationToken)"/>
/// call time, for scenarios where the factory is not known until the value is first needed. The
/// factory is supplied per call, so different callers may pass different factories; only the first
/// successfully published value is retained. Prefer <see cref="IValueCacheAsync{T}"/> when the
/// factory is fixed and known up front.
/// </remarks>
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
    /// <remarks>
    /// Returns <see cref="System.Threading.Tasks.ValueTask{TResult}"/> because a cache read usually
    /// completes synchronously, and that path must not allocate. Observe the standard contract: await
    /// the result at most once, never concurrently, and call <c>AsTask</c> before handing it to
    /// <see cref="System.Threading.Tasks.Task.WhenAll(System.Threading.Tasks.Task[])"/> or storing it.
    /// </remarks>
    /// <remarks>
    /// The <paramref name="factory"/> keeps returning <see cref="Task{TResult}"/>: it performs the real
    /// work and never completes synchronously, so a value task there would be caller friction for no gain.
    /// </remarks>
    ValueTask<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default);
}
