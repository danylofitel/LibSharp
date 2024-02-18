// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Caching
{
    /// <summary>
    /// A value wrapper, allows to swap the value atomically.
    /// </summary>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    internal class ValueReference<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueReference{TValue}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ValueReference(TValue value)
        {
            Value = value;
            Expiration = DateTime.MaxValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueReference{TValue}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="expiration">Expiration time.</param>
        public ValueReference(TValue value, DateTime expiration)
        {
            Value = value;
            Expiration = expiration;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public TValue Value { get; }

        /// <summary>
        /// Gets the expiration time.
        /// </summary>
        public DateTime Expiration { get; }
    }
}
