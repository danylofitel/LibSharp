// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;

namespace LibSharp.Common;

/// <summary>
/// Extension methods for Type.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Gets the default comparer for the type.
    /// </summary>
    /// <typeparam name="T">The comparable type.</typeparam>
    /// <returns>Default comparer for the type.</returns>
    /// <remarks>
    /// Fails immediately for a type that cannot be ordered, rather than letting
    /// <see cref="Comparer{T}.Default"/> throw later on its first comparison, which is far from the
    /// mistake. The check is resolved once per closed generic type, not on every call.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="T"/> implements neither <see cref="IComparable{T}"/> nor
    /// <see cref="IComparable"/>.
    /// </exception>
    public static IComparer<T> GetDefaultComparer<T>()
    {
        if (!ComparableInfo<T>.s_isComparable)
        {
            throw new InvalidOperationException(
                $"Type {typeof(T).FullName} does not implement IComparable<{typeof(T).FullName}> or IComparable.");
        }

        return Comparer<T>.Default;
    }

    /// <summary>
    /// Per-type data resolved once, on first use of each closed generic type.
    /// </summary>
    private static class ComparableInfo<T>
    {
        public static readonly bool s_isComparable =
            typeof(IComparable<T>).IsAssignableFrom(typeof(T)) || typeof(IComparable).IsAssignableFrom(typeof(T));
    }
}
