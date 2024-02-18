// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Caching
{
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
        /// Thread-safe, only one factory will ever be executed.
        /// </summary>
        /// <param name="factory">Value factory.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The value.</returns>
        Task<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default);
    }
}
