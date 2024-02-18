// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibSharp.Common
{
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
            Type type = typeof(T);

            if (!type.GetInterfaces().Contains(typeof(IComparable<T>)))
            {
                throw new ArgumentException($"Type {type.FullName} does not implement IComparable<{type.FullName}>.");
            }

            return Comparer<T>.Default;
        }
    }
}
