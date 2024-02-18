// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Caching
{
    /// <inheritdoc/>
    public class Initializer<T> : IInitializer<T>
    {
        /// <inheritdoc/>
        public bool HasValue { get; private set; }

        /// <inheritdoc/>
        public T GetValue(Func<T> factory)
        {
            if (!HasValue)
            {
                lock (m_lock)
                {
                    if (!HasValue)
                    {
                        m_instance = factory();
                        HasValue = true;
                    }
                }
            }

            return m_instance;
        }

        private readonly object m_lock = new object();
        private T m_instance;
    }
}
