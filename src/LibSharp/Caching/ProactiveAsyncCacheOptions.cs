// Copyright (c) 2026 Danylo Fitel

using System;

namespace LibSharp.Caching;

/// <summary>
/// Configuration for <see cref="ProactiveAsyncCache{T}"/>.
/// </summary>
/// <remarks>
/// Settings live here rather than on the constructor so that adding one later is not a binary
/// breaking change: an optional constructor parameter is baked into every call site, whereas a new
/// property is not.
/// <para>
/// Values are validated and copied when the cache is constructed, so mutating an instance
/// afterwards has no effect on a cache already built from it, and one instance may be reused.
/// </para>
/// </remarks>
public sealed class ProactiveAsyncCacheOptions
{
    /// <summary>
    /// Gets how long a fetched value stays fresh. Required, and must be positive.
    /// </summary>
    public required TimeSpan RefreshInterval { get; init; }

    /// <summary>
    /// Gets how long before expiration the background loop refreshes the value.
    /// </summary>
    /// <remarks>
    /// Must be at least zero and less than <see cref="RefreshInterval"/>. A larger offset gives the
    /// value factory more time to complete before readers would otherwise start waiting; zero means
    /// the refresh is scheduled for the moment of expiration. Defaults to zero.
    /// </remarks>
    public TimeSpan PreFetchOffset { get; init; }

    /// <summary>
    /// Gets what happens when a read arrives and the cached value has expired.
    /// </summary>
    /// <remarks>Defaults to <see cref="StaleReadPolicy.Wait"/>.</remarks>
    public StaleReadPolicy StaleReads { get; init; }

    /// <summary>
    /// Gets how long the cache may go unread before its background refresh loop suspends itself.
    /// </summary>
    /// <remarks>
    /// While suspended the loop holds no timer and consumes no CPU, and the next read resumes it
    /// immediately. Only reads count as activity, not <see cref="ProactiveAsyncCache{T}.HasValue"/>
    /// or <see cref="ProactiveAsyncCache{T}.Expiration"/>. Choose a value comfortably larger than
    /// <see cref="RefreshInterval"/>: a shorter one lets the cache fall idle between consecutive
    /// reads, degrading it to on-demand refresh. Must be positive when set. Defaults to
    /// <c>null</c>, meaning the loop never suspends.
    /// </remarks>
    public TimeSpan? IdleTimeout { get; init; }

    /// <summary>
    /// Gets how long a single value factory invocation may run before it is cancelled.
    /// </summary>
    /// <remarks>
    /// The factory receives a token that is cancelled once this elapses, and the attempt is recorded
    /// as a failure with a <see cref="TimeoutException"/>. This also bounds
    /// <see cref="ProactiveAsyncCache{T}.DisposeAsync"/>, which otherwise waits as long as the
    /// factory takes. It can only help if the factory honours its token; one that ignores
    /// cancellation still runs to completion. Must be positive when set. Defaults to <c>null</c>,
    /// meaning no timeout.
    /// </remarks>
    public TimeSpan? FetchTimeout { get; init; }

    /// <summary>
    /// Gets the time provider used to measure expiration and to schedule background refreshes.
    /// </summary>
    /// <remarks>Defaults to <see cref="System.TimeProvider.System"/>.</remarks>
    public TimeProvider? TimeProvider { get; init; }
}
