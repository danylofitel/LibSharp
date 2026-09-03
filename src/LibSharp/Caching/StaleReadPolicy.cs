// Copyright (c) 2026 Danylo Fitel

using System;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// What a cache should do when a read arrives and the cached value has expired.
/// </summary>
/// <remarks>
/// A closed set of cases rather than an enum, because <see cref="ServeStaleUpTo"/> carries a bound.
/// An enum plus a separate maximum-age setting would allow combinations that mean nothing, such as
/// a bound alongside <see cref="Wait"/>; here they cannot be expressed.
/// <para>
/// The default value is <see cref="Wait"/>, so an unset policy is the conservative one.
/// </para>
/// </remarks>
public readonly struct StaleReadPolicy : IEquatable<StaleReadPolicy>
{
    private StaleReadPolicy(bool servesStale, TimeSpan? maxStaleness)
    {
        ServesStale = servesStale;
        MaxStaleness = maxStaleness;
    }

    /// <summary>
    /// Gets the policy that makes readers wait for a fresh value once the cached one has expired.
    /// </summary>
    /// <remarks>
    /// This is the default. Readers never receive an out-of-date value, at the cost of blocking for
    /// as long as the value factory takes, and of surfacing its failures directly to callers.
    /// </remarks>
    public static StaleReadPolicy Wait => default;

    /// <summary>
    /// Gets the policy that serves the expired value immediately, however old it is, while a
    /// refresh runs in the background.
    /// </summary>
    /// <remarks>
    /// Readers never block after the first successful fetch, but a value factory that keeps failing
    /// leaves them receiving an arbitrarily old value with no error to indicate it.
    /// <see cref="ProactiveAsyncCache{T}.LastRefreshException"/> and
    /// <see cref="ProactiveAsyncCache{T}.LastSuccessfulRefresh"/> are then the only signal. Prefer
    /// <see cref="ServeStaleUpTo"/> where unbounded age is not acceptable.
    /// </remarks>
    public static StaleReadPolicy ServeStale => new StaleReadPolicy(true, null);

    /// <summary>
    /// Creates a policy that serves an expired value for up to <paramref name="maxStaleness"/> past
    /// its expiration, and makes readers wait for a fresh value beyond that.
    /// </summary>
    /// <param name="maxStaleness">
    /// How long past its expiration a value may still be served. Measured from the expiration, not
    /// from when the value was produced, so the oldest value a reader can receive is the cache's
    /// refresh interval plus this bound.
    /// </param>
    /// <returns>The policy.</returns>
    /// <remarks>
    /// The middle ground: absorb a brief outage without blocking readers, but stop serving data that
    /// has aged past what the caller can tolerate. Once the bound is passed the cache behaves as
    /// <see cref="Wait"/> does, so readers block and a persistent failure reaches them as an
    /// exception instead of being hidden behind an ever-older value.
    /// </remarks>
    public static StaleReadPolicy ServeStaleUpTo(TimeSpan maxStaleness)
    {
        Argument.GreaterThan(maxStaleness, TimeSpan.Zero);

        return new StaleReadPolicy(true, maxStaleness);
    }

    /// <summary>
    /// Gets a value indicating whether an expired value may be served at all.
    /// </summary>
    public bool ServesStale { get; }

    /// <summary>
    /// Gets how long past expiration a value may be served, or <c>null</c> when unbounded or when
    /// stale reads are not permitted at all.
    /// </summary>
    public TimeSpan? MaxStaleness { get; }

    /// <summary>
    /// Determines whether two policies are equal.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><c>true</c> if the policies are equal.</returns>
    public static bool operator ==(StaleReadPolicy left, StaleReadPolicy right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two policies are different.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><c>true</c> if the policies are different.</returns>
    public static bool operator !=(StaleReadPolicy left, StaleReadPolicy right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(StaleReadPolicy other)
    {
        return ServesStale == other.ServesStale && MaxStaleness == other.MaxStaleness;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is StaleReadPolicy other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(ServesStale, MaxStaleness);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (!ServesStale)
        {
            return nameof(Wait);
        }

        return MaxStaleness is TimeSpan maxStaleness
            ? $"{nameof(ServeStaleUpTo)}({maxStaleness})"
            : nameof(ServeStale);
    }
}
