// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;
using LibSharp.Threading;

namespace LibSharp.Caching;

/// <summary>
/// Async lazy with LazyThreadSafetyMode.ExecutionAndPublication.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// Should not be used with IDisposable or IAsyncDisposable value types since it does not dispose of values.
/// A successful initialization is cached permanently. Faulted or canceled attempts are not cached and may be retried by later callers.
/// </remarks>
public sealed class LazyAsyncExecutionAndPublication<T> : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LazyAsyncExecutionAndPublication{T}"/> class from a value.
    /// </summary>
    /// <param name="value">The value to hold.</param>
    public LazyAsyncExecutionAndPublication(T value)
    {
        m_hasValue = true;
        m_value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyAsyncExecutionAndPublication{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    public LazyAsyncExecutionAndPublication(Func<CancellationToken, Task<T>> factory)
    {
        Argument.NotNull(factory, nameof(factory));

        m_hasValue = false;
        m_factory = factory;
    }

    /// <summary>
    /// Gets a value indicating whether the value has been initialized.
    /// </summary>
    public bool HasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

            return m_hasValue;
        }
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the value is produced.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the value factory returns a null task.</exception>
    public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

        if (!m_hasValue)
        {
            using (await m_lock.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!m_hasValue)
                {
                    Task<T> factoryTask = m_factory(cancellationToken)
                        ?? throw new InvalidOperationException("The value factory returned a null task.");
                    m_value = await factoryTask.ConfigureAwait(false);
                    m_hasValue = true;
                }

                ObjectDisposedException.ThrowIf(Volatile.Read(ref m_isDisposed) != 0, this);

                return m_value;
            }
        }

        return m_value;
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
    private readonly Func<CancellationToken, Task<T>> m_factory;
    private volatile bool m_hasValue;
    private T m_value;

    private int m_isDisposed;
}
