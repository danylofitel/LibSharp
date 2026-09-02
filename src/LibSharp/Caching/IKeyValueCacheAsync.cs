// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching;

/// <summary>
/// Interface for an async key-value cache.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
/// <remarks>
/// Eviction behaviour is implementation-defined. An implementation may retain every entry for its
/// own lifetime, in which case memory grows with the number of distinct keys requested and the
/// implementation is only suitable for bounded key spaces. Consult the concrete type before using
/// it with an unbounded key space.
/// </remarks>
public interface IKeyValueCacheAsync<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets the cached value for a given key.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the cache has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the value is produced.</exception>
    /// <remarks>
    /// Returns <see cref="System.Threading.Tasks.ValueTask{TResult}"/> because a cache read usually
    /// completes synchronously, and that path must not allocate. Observe the standard contract: await
    /// the result at most once, never concurrently, and call <c>AsTask</c> before handing it to
    /// <see cref="System.Threading.Tasks.Task.WhenAll(System.Threading.Tasks.Task[])"/> or storing it.
    /// </remarks>
    ValueTask<TValue> GetValueAsync(TKey key, CancellationToken cancellationToken = default);
}
