// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Caching
{
    /// <summary>
    /// Interface for a value cache.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    public interface IValueCache<T>
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
        /// <returns>The cached value.</returns>
        T GetValue();
    }
}
