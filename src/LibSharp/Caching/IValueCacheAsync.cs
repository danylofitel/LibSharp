// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching
{
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
        Task<T> GetValueAsync(CancellationToken cancellationToken = default);
    }
}
