// Copyright (c) LibSharp. All rights reserved.

using System;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <inheritdoc/>
public sealed class Initializer<T> : IInitializer<T>
{
    /// <inheritdoc/>
    public bool HasValue => m_hasValue;

    /// <inheritdoc/>
    public T GetValue(Func<T> factory)
    {
        Argument.NotNull(factory, nameof(factory));

        if (!m_hasValue)
        {
            lock (m_lock)
            {
                if (!m_hasValue)
                {
                    m_instance = factory();
                    m_hasValue = true;
                }

                return m_instance;
            }
        }

        return m_instance;
    }

    private readonly object m_lock = new object();
    private volatile bool m_hasValue;
    private T m_instance;
}
