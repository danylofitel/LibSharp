// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Common
{
    /// <summary>
    /// Int extensions.
    /// </summary>
    public static class IntExtensions
    {
        /// <summary>
        /// Converts an integer to an enum value if possible.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="value">Integer value.</param>
        /// <param name="result">Enum value.</param>
        /// <returns>True if the value is defined and was successfully converted, false otherwise.</returns>
        public static bool TryConvertToEnum<T>(this int value, out T result)
            where T : struct, Enum
        {
            if (Enum.IsDefined(typeof(T), value))
            {
                result = (T)(object)value;
                return true;
            }

            result = default;
            return false;
        }
    }
}
