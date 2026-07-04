// Copyright (c) 2026 Danylo Fitel

using System;

namespace LibSharp.Caching;

/// <summary>
/// Thread-safe lazy value initializer.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// This is deliberately distinct from <see cref="IValueCache{T}"/> and <see cref="System.Lazy{T}"/>,
/// which capture the value factory once at construction. An initializer instead accepts the factory
/// at <see cref="GetValue(System.Func{T})"/> call time, for scenarios where the factory is not known
/// until the value is first needed. The factory is supplied per call, so different callers may pass
/// different factories; only the first one to run produces the retained value. Prefer
/// <see cref="IValueCache{T}"/> when the factory is fixed and known up front.
/// </remarks>
public interface IInitializer<T>
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
    /// <returns>The value.</returns>
    T GetValue(Func<T> factory);
}
