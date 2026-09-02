// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;
using LibSharp.Threading;

namespace LibSharp.Caching;

/// <summary>
/// Async initializer with LazyThreadSafetyMode.ExecutionAndPublication.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// Should not be used with IDisposable or IAsyncDisposable value types since it does not dispose of values.
/// A successful initialization is cached permanently. Faulted or canceled attempts are not cached and may be retried by later callers.
/// </remarks>
public sealed class InitializerAsyncExecutionAndPublication<T> : IInitializerAsync<T>, IDisposable
{
    /// <inheritdoc/>
    public bool HasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_hasValue;
        }
    }

    /// <inheritdoc/>
    public ValueTask<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        Argument.NotNull(factory);

        ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

        if (m_hasValue)
        {
            return new ValueTask<T>(m_value);
        }

        return InitializeAsync(factory, cancellationToken);
    }

    private async ValueTask<T> InitializeAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        using (await m_lock.AcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!m_hasValue)
            {
                Task<T> factoryTask = factory(cancellationToken)
                    ?? throw new InvalidOperationException("The value factory returned a null task.");
                m_value = await factoryTask.ConfigureAwait(false);
                m_hasValue = true;
            }

            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_value;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref m_isDisposed, 1) != 0)
        {
            return;
        }

        m_lock.Dispose();
    }

    private readonly AsyncLock m_lock = new AsyncLock();
    private volatile bool m_hasValue;

    // Assigned before m_hasValue is set to true; only ever read after observing m_hasValue == true.
    private T m_value = default!;
    private int m_isDisposed;
}
