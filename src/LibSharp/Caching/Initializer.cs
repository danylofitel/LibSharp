// Copyright (c) 2026 Danylo Fitel

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
        Argument.NotNull(factory);

        if (!m_hasValue)
        {
            lock (m_lock)
            {
                if (!m_hasValue)
                {
                    // The monitor is re-entrant, so a factory that reads this same initializer
                    // would re-enter here, still find no value, and call the factory again,
                    // recursing until the stack overflows and takes the process with it. Fail fast
                    // instead, the way Lazy<T> reports recursive initialization.
                    //
                    // m_isInitializing needs no synchronisation of its own: it is only ever touched
                    // under m_lock, and only the thread already holding that lock can observe it
                    // set. Any other thread is blocked at the lock and never sees it.
                    if (m_isInitializing)
                    {
                        throw new InvalidOperationException(
                            "The value factory attempted to read the initializer it is initializing. Re-entering an initializer from its own value factory is not supported.");
                    }

                    m_isInitializing = true;
                    try
                    {
                        m_instance = factory();
                        m_hasValue = true;
                    }
                    finally
                    {
                        // Reset even when the factory throws, so one failure does not wedge the
                        // initializer.
                        m_isInitializing = false;
                    }
                }

                return m_instance;
            }
        }

        return m_instance;
    }

    private readonly object m_lock = new object();

    // Only ever touched under m_lock.
    private bool m_isInitializing;
    private volatile bool m_hasValue;

    // Assigned before m_hasValue is set to true; only ever read after observing m_hasValue == true.
    private T m_instance = default!;
}
