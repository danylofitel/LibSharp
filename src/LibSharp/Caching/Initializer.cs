// Copyright (c) LibSharp. All rights reserved.

using System;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <inheritdoc/>
public sealed class Initializer<T> : IInitializer<T>
{
    /// <inheritdoc/>
    public bool HasValue
    {
        get => m_hasValue;
        private set => m_hasValue = value;
    }

    /// <inheritdoc/>
    public T GetValue(Func<T> factory)
    {
        Argument.NotNull(factory, nameof(factory));

        if (!HasValue)
        {
            lock (m_lock)
            {
                if (!HasValue)
                {
                    m_instance = factory();
                    HasValue = true;
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
