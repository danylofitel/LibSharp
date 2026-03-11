// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// An async cache that proactively refreshes its value in the background before it expires.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
public class ProactiveAsyncCache<T> : IValueCacheAsync<T>, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProactiveAsyncCache{T}"/> class.
    /// </summary>
    /// <param name="valueFactory">The value factory.</param>
    /// <param name="refreshInterval">The interval at which the cache should be refreshed.</param>
    /// <param name="preFetchOffset">The offset before the refresh interval to pre-fetch the value.</param>
    public ProactiveAsyncCache(
        Func<CancellationToken, Task<T>> valueFactory,
        TimeSpan refreshInterval,
        TimeSpan preFetchOffset)
    {
        Argument.NotNull(valueFactory, nameof(valueFactory));
        Argument.GreaterThan(refreshInterval, TimeSpan.Zero, nameof(refreshInterval));
        Argument.GreaterThanOrEqualTo(preFetchOffset, TimeSpan.Zero, nameof(preFetchOffset));
        Argument.LessThan(preFetchOffset, refreshInterval, nameof(preFetchOffset));

        m_cts = new CancellationTokenSource();
        m_semaphore = new SemaphoreSlim(1, 1);
        m_fetchFunc = valueFactory;
        m_refreshInterval = refreshInterval;
        m_preFetchOffset = preFetchOffset;
        m_backgroundTask = Task.Run(StartBackgroundRefresh);
    }

    /// <inheritdoc/>
    public bool HasValue
    {
        get
        {
            ObjectDisposedException.ThrowIf(m_isDisposed, this);

            return m_snapshot is not null;
        }
    }

    /// <inheritdoc/>
    public DateTime? Expiration
    {
        get
        {
            ObjectDisposedException.ThrowIf(m_isDisposed, this);

            return m_snapshot?.NextRefreshTime;
        }
    }

    /// <inheritdoc/>
    public async Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(m_isDisposed, this);

        CacheSnapshot snapshot = m_snapshot;
        if (snapshot is null || DateTime.UtcNow >= snapshot.NextRefreshTime)
        {
            await m_semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                snapshot = m_snapshot;
                if (snapshot is null || DateTime.UtcNow >= snapshot.NextRefreshTime)
                {
                    T value = await m_fetchFunc(cancellationToken).ConfigureAwait(false);
                    snapshot = new CacheSnapshot(value, DateTime.UtcNow + m_refreshInterval);
                    m_snapshot = snapshot;
                }

                return snapshot.Value;
            }
            finally
            {
                _ = m_semaphore.Release();
            }
        }

        return snapshot.Value;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the cache.
    /// </summary>
    /// <param name="disposing">True if the method was called during disposal, false otherwise.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!m_isDisposed)
        {
            m_isDisposed = true;

            if (disposing)
            {
                m_cts.Cancel();
                m_backgroundTask.GetAwaiter().GetResult();
                m_semaphore.Dispose();
                m_cts.Dispose();
            }
        }
    }

    /// <summary>
    /// Performs async disposal of the cache resources.
    /// </summary>
    /// <returns>A task representing the async dispose operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!m_isDisposed)
        {
            m_isDisposed = true;

            m_cts.Cancel();
            await m_backgroundTask.ConfigureAwait(false);
            m_semaphore.Dispose();
            m_cts.Dispose();
        }
    }

    private async Task StartBackgroundRefresh()
    {
        while (!m_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(m_refreshInterval - m_preFetchOffset, m_cts.Token).ConfigureAwait(false);
                await m_semaphore.WaitAsync(m_cts.Token).ConfigureAwait(false);
                try
                {
                    T value = await m_fetchFunc(m_cts.Token).ConfigureAwait(false);
                    m_snapshot = new CacheSnapshot(value, DateTime.UtcNow + m_refreshInterval);
                }
                finally
                {
                    _ = m_semaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Transient failure; will retry on next cycle
            }
        }
    }

    private readonly CancellationTokenSource m_cts;
    private readonly SemaphoreSlim m_semaphore;
    private readonly Func<CancellationToken, Task<T>> m_fetchFunc;
    private readonly TimeSpan m_refreshInterval;
    private readonly TimeSpan m_preFetchOffset;
    private readonly Task m_backgroundTask;
    private volatile CacheSnapshot m_snapshot;
    private volatile bool m_isDisposed;

    private sealed record CacheSnapshot(T Value, DateTime NextRefreshTime);
}
