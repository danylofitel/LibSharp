// Copyright (c) LibSharp. All rights reserved.

using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching
{
    /// <summary>
    /// Interface for an async key-value cache.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    public interface IKeyValueCacheAsync<TKey, TValue>
        where TKey : notnull
    {
        /// <summary>
        /// Gets the cached value for a given key.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value.</returns>
        Task<TValue> GetValueAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
