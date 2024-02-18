// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Caching
{
    /// <summary>
    /// A value wrapper, allows to swap the value atomically.
    /// </summary>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    internal class Box<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Box{TValue}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public Box(TValue value)
        {
            Value = value;
            Expiration = DateTime.MaxValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box{TValue}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="expiration">Expiration time.</param>
        public Box(TValue value, DateTime expiration)
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
