// Copyright (c) 2026 Danylo Fitel

namespace LibSharp.Caching;

/// <summary>
/// Interface for a key-value cache.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
/// <remarks>
/// Eviction behaviour is implementation-defined. An implementation may retain every entry for its
/// own lifetime, in which case memory grows with the number of distinct keys requested and the
/// implementation is only suitable for bounded key spaces. Consult the concrete type before using
/// it with an unbounded key space.
/// </remarks>
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
