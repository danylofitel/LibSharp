// Copyright (c) LibSharp. All rights reserved.

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
    public static IComparer<T> GetDefaultComparer<T>()
    {
        if (!typeof(IComparable<T>).IsAssignableFrom(typeof(T)) && !typeof(IComparable).IsAssignableFrom(typeof(T)))
        {
            throw new ArgumentException($"Type {typeof(T).FullName} does not implement IComparable<{typeof(T).FullName}> or IComparable.");
        }

        return Comparer<T>.Default;
    }
}
