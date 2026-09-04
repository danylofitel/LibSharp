// Copyright (c) 2026 Danylo Fitel

using System;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <inheritdoc/>
public sealed class Initializer<T> : IInitializer<T>
{
    /// <inheritdoc/>
    public bool HasValue => _hasValue;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the value factory reads the initializer it is initializing. Re-entering from the
    /// factory is not supported.
    /// </exception>
    public T GetValue(Func<T> factory)
    {
        Argument.NotNull(factory);

        if (!_hasValue)
        {
            lock (_lock)
            {
                if (!_hasValue)
                {
                    // The monitor is re-entrant, so a factory that reads this same initializer
                    // would re-enter here, still find no value, and call the factory again,
                    // recursing until the stack overflows and takes the process with it. Fail fast
                    // instead, the way Lazy<T> reports recursive initialization.
                    //
                    // _isInitializing needs no synchronisation of its own: it is only ever touched
                    // under _lock, and only the thread already holding that lock can observe it
                    // set. Any other thread is blocked at the lock and never sees it.
                    if (_isInitializing)
                    {
                        throw new InvalidOperationException(
                            "The value factory attempted to read the initializer it is initializing. Re-entering an initializer from its own value factory is not supported.");
                    }

                    _isInitializing = true;
                    try
                    {
                        _instance = factory();
                        _hasValue = true;
                    }
                    finally
                    {
                        // Reset even when the factory throws, so one failure does not wedge the
                        // initializer.
                        _isInitializing = false;
                    }
                }

                return _instance;
            }
        }

        return _instance;
    }

    private readonly object _lock = new object();

    // Only ever touched under _lock.
    private bool _isInitializing;
    private volatile bool _hasValue;

    // Assigned before _hasValue is set to true; only ever read after observing _hasValue == true.
    private T _instance = default!;
}
