// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Common
{
    /// <summary>
    /// Utilities for verification of function arguments.
    /// </summary>
    public static class Argument
    {
        /// <summary>
        /// Verifies that the argument is an instance of the given type.
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="equalValue">Required argument value.</param>
        /// <param name="name">Argument name.</param>
        /// <typeparam name="T">Argument type.</typeparam>
        public static void EqualTo<T>(T value, T equalValue, string name)
        {
            if (value == null && equalValue == null)
            {
                return;
            }

            if (value == null && equalValue != null)
            {
                throw new ArgumentNullException(name, $"Argument is null and is not equal to {equalValue}");
            }

            if (!value.Equals(equalValue))
            {
                throw new ArgumentException(name, $"{value} must be equal to {equalValue}.");
            }
        }

        /// <summary>
        /// Verifies that the argument is greater than the specified value.
        /// </summary>
        /// <typeparam name="T">Argument type.</typeparam>
        /// <param name="value">Argument value.</param>
        /// <param name="minValueExclusive">Minimal value exclusive.</param>
        /// <param name="name">Argument name.</param>
        public static void GreaterThan<T>(T value, T minValueExclusive, string name)
            where T : IComparable<T>
        {
            NotNull(value, name);

            if (value.CompareTo(minValueExclusive) <= 0)
            {
                throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than {minValueExclusive}.");
            }
        }

        /// <summary>
        /// Verifies that the argument is greater than or equal to the specified value.
        /// </summary>
        /// <typeparam name="T">Argument type.</typeparam>
        /// <param name="value">Argument value.</param>
        /// <param name="minValueInclusive">Minimal value inclusive.</param>
        /// <param name="name">Argument name.</param>
        public static void GreaterThanOrEqualTo<T>(T value, T minValueInclusive, string name)
            where T : IComparable<T>
        {
            NotNull(value, name);

            if (value.CompareTo(minValueInclusive) < 0)
            {
                throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than or equal to {minValueInclusive}.");
            }
        }

        /// <summary>
        /// Verifies that the argument is less than the specified value.
        /// </summary>
        /// <typeparam name="T">Argument type.</typeparam>
        /// <param name="value">Argument value.</param>
        /// <param name="maxValueExclusive">Maximal value exclusive.</param>
        /// <param name="name">Argument name.</param>
        public static void LessThan<T>(T value, T maxValueExclusive, string name)
            where T : IComparable<T>
        {
            NotNull(value, name);

            if (value.CompareTo(maxValueExclusive) >= 0)
            {
                throw new ArgumentOutOfRangeException(name, value, $"{name} must be less than {maxValueExclusive}.");
            }
        }

        /// <summary>
        /// Verifies that the argument is less than or equal to the specified value.
        /// </summary>
        /// <typeparam name="T">Argument type.</typeparam>
        /// <param name="value">Argument value.</param>
        /// <param name="maxValueInclusive">Maximal value inclusive.</param>
        /// <param name="name">Argument name.</param>
        public static void LessThanOrEqualTo<T>(T value, T maxValueInclusive, string name)
            where T : IComparable<T>
        {
            NotNull(value, name);

            if (value.CompareTo(maxValueInclusive) > 0)
            {
                throw new ArgumentOutOfRangeException(name, value, $"{name} must be less than or equal to {maxValueInclusive}.");
            }
        }

        /// <summary>
        /// Verifies that the argument is an instance of the given type.
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="notEqualValue">Prohibited argument value.</param>
        /// <param name="name">Argument name.</param>
        /// <typeparam name="T">Argument type.</typeparam>
        public static void NotEqualTo<T>(T value, T notEqualValue, string name)
        {
            if (value == null && notEqualValue == null)
            {
                throw new ArgumentNullException(name);
            }

            if (value == null || notEqualValue == null)
            {
                return;
            }

            if (value.Equals(notEqualValue))
            {
                throw new ArgumentException(name, $"{value} must not be equal to {notEqualValue}.");
            }
        }

        /// <summary>
        /// Verifies that the argument is not null.
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="name">Argument name.</param>
        public static void NotNull(object value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Verifies that the argument is not null or empty.
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="name">Argument name.</param>
        public static void NotNullOrEmpty(string value, string name)
        {
            NotNull(value, name);

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"{name} must not be null or empty.", name);
            }
        }

        /// <summary>
        /// Verifies that the argument is not null or white space.
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="name">Argument name.</param>
        public static void NotNullOrWhiteSpace(string value, string name)
        {
            NotNull(value, name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name} must not be null or white space.", name);
            }
        }

        /// <summary>
        /// Verifies that the argument is an instance of the given type.
        /// </summary>
        /// <param name="value">Argument value.</param>
        /// <param name="type">Required argument type.</param>
        /// <param name="name">Argument name.</param>
        public static void OfType(object value, Type type, string name)
        {
            NotNull(value, name);
            NotNull(type, nameof(type));

            if (!type.IsInstanceOfType(value))
            {
                throw new ArgumentException($"{name} must be of type {type.FullName}.");
            }
        }
    }
}
