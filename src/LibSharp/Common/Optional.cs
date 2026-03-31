// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;

namespace LibSharp.Common
{
    /// <summary>
    /// A wrapper for any type that may or may not hold a value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    public readonly struct Optional<T> : IEquatable<T>, IEquatable<Optional<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Optional{T}"/> struct with the given value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public Optional(T value)
        {
            HasValue = true;
            m_value = value;
        }

        /// <summary>
        /// Gets a value indicating whether the optional has a value.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the value if it exists, throws an exception if it doesn't.
        /// </summary>
        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("The optional does not hold a value.");
                }

                return m_value;
            }
        }

        /// <summary>
        /// Returns the value if it exists, or <paramref name="fallback"/> if it doesn't.
        /// </summary>
        /// <param name="fallback">The fallback value. Defaults to <c>default(T)</c>.</param>
        public T GetValueOrDefault(T fallback = default)
        {
            return HasValue ? m_value : fallback;
        }

        /// <summary>
        /// Returns true and sets <paramref name="value"/> to the wrapped value if the optional has one;
        /// otherwise returns false and sets <paramref name="value"/> to <c>default(T)</c>.
        /// </summary>
        public bool TryGetValue(out T value)
        {
            value = m_value;
            return HasValue;
        }

        /// <inheritdoc/>
        public bool Equals(T other)
        {
            return HasValue && EqualityComparer<T>.Default.Equals(m_value, other);
        }

        /// <inheritdoc/>
        public bool Equals(Optional<T> other)
        {
            if (HasValue != other.HasValue)
            {
                return false;
            }

            return !HasValue || EqualityComparer<T>.Default.Equals(m_value, other.m_value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is Optional<T> other)
            {
                return Equals(other);
            }

            if (!HasValue)
            {
                return false;
            }

            if (m_value is null)
            {
                return obj is null;
            }

            return obj is T value && EqualityComparer<T>.Default.Equals(m_value, value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HasValue ? (m_value is null ? 0 : m_value.GetHashCode()) : 0;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return HasValue ? (m_value?.ToString() ?? string.Empty) : string.Empty;
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True of the operands are equal, false otherwise.</returns>
        public static bool operator ==(Optional<T> left, Optional<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Not equals operator.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True of the operands are not equal, false otherwise.</returns>
        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !(left == right);
        }

        private readonly T m_value;
    }
}
