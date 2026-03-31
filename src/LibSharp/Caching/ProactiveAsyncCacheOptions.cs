// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Caching
{
    /// <summary>
    /// Configuration options for <see cref="ProactiveAsyncCache{T}"/>.
    /// </summary>
    public sealed record ProactiveAsyncCacheOptions
    {
        /// <summary>
        /// Gets a value indicating whether the cache should start the background refresh loop
        /// immediately upon construction. Defaults to <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Set to <c>false</c> when you need to defer startup until after construction, for example
        /// when wiring up dependencies in a DI container. Call <see cref="ProactiveAsyncCache{T}.Start"/>
        /// explicitly to begin the background loop.
        /// </remarks>
        public bool AutoStart { get; init; } = true;

        /// <summary>
        /// Gets a value indicating whether readers receive the stale cached value immediately while a
        /// background refresh runs. When <c>false</c> (default), readers block until the refresh completes.
        /// </summary>
        public bool AllowStaleReads { get; init; } = false;

        /// <summary>
        /// Gets an optional timeout applied to each individual refresh operation.
        /// When set, a refresh that exceeds this duration is cancelled.
        /// Must be greater than <see cref="TimeSpan.Zero"/> if provided.
        /// </summary>
        public TimeSpan? RefreshTimeout { get; init; }

        /// <summary>
        /// Gets an optional callback invoked when a background refresh fails
        /// (excluding disposal-triggered cancellation).
        /// </summary>
        public Action<Exception> OnBackgroundRefreshError { get; init; }
    }
}
