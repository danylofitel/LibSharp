// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Common
{
    /// <summary>
    /// A wrapper for any type that may or may not hold a value.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    public readonly struct Box<T> : IEquatable<T>, IEquatable<Box<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Box{T}"/> struct with the given value.
        /// </summary>
        /// <param name="value">The value to box.</param>
        public Box(T value)
        {
            Argument.NotNull(value, nameof(value));

            HasValue = true;
            m_value = value;
        }

        /// <summary>
        /// Gets a value indicating  whether the box has a value.
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
                    throw new InvalidOperationException("The box does not hold a value.");
                }

                return m_value;
            }
        }

        /// <inheritdoc/>
        public bool Equals(T other)
        {
            return (!HasValue && other is null)
                || (HasValue && Value.Equals(other));
        }

        /// <inheritdoc/>
        public bool Equals(Box<T> other)
        {
            return (!HasValue && !other.HasValue)
                || (HasValue && other.HasValue && Value.Equals(other.Value));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is T value)
            {
                return Equals(value);
            }
            else if (obj is Box<T> other)
            {
                return Equals(other);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HasValue ? Value.GetHashCode() : 0;
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True of the operands are equal, false otherwise.</returns>
        public static bool operator ==(Box<T> left, Box<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Not equals operator.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True of the operands are not equal, false otherwise.</returns>
        public static bool operator !=(Box<T> left, Box<T> right)
        {
            return !(left == right);
        }

        private readonly T m_value;
    }
}
