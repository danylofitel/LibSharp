// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching;

/// <summary>
/// A value produced once, asynchronously, and reused thereafter.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// The common shape of every asynchronously produced value in this namespace: the lazies, which
/// produce a value once and keep it, and the caches, which additionally expire and replace it.
/// <see cref="IValueCacheAsync{T}"/> extends this with an expiration time, so code that only needs
/// the value can accept this interface and take either.
/// <para>
/// This interface does not extend <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/>
/// because not every implementation owns resources. Some do, and expose those interfaces on the
/// concrete type; when you hold only this interface, the owner of the instance is responsible for
/// disposing it.
/// </para>
/// </remarks>
public interface ILazyAsync<T>
{
    /// <summary>
    /// Gets a value indicating whether the value has been produced.
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// Gets the value, producing it if it is not already available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the implementation has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the value is produced.</exception>
    /// <remarks>
    /// Returns <see cref="System.Threading.Tasks.ValueTask{TResult}"/> because a read usually
    /// completes synchronously, and that path must not allocate. Observe the standard contract: await
    /// the result at most once, never concurrently, and call <c>AsTask</c> before handing it to
    /// <see cref="System.Threading.Tasks.Task.WhenAll(System.Threading.Tasks.Task[])"/> or storing it.
    /// </remarks>
    ValueTask<T> GetValueAsync(CancellationToken cancellationToken = default);
}
