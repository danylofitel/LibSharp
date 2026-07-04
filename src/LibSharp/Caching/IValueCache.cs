// Copyright (c) 2026 Danylo Fitel

using System;

namespace LibSharp.Caching;

/// <summary>
/// Interface for a value cache.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// This interface does not extend <see cref="IDisposable"/> because not every implementation owns
/// resources. Some implementations do, however, and expose <see cref="IDisposable"/> (or
/// <see cref="IAsyncDisposable"/>) on the concrete type. When you hold a concrete cache, check for
/// those interfaces and dispose accordingly; when you hold only <see cref="IValueCache{T}"/>, the
/// owner of the instance is responsible for its disposal.
/// </remarks>
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
