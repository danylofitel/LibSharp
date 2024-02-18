// Copyright (c) LibSharp. All rights reserved.

namespace LibSharp.Caching
{
    /// <summary>
    /// Interface for a key-value cache.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    public interface IKeyValueCache<TKey, TValue>
        where TKey : notnull
    {
        /// <summary>
        /// Gets the cached value for a given key.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <returns>The cached value.</returns>
        TValue GetValue(TKey key);
    }
}
