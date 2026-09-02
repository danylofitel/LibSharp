// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// Async lazy with LazyThreadSafetyMode.ExecutionAndPublication.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
/// <remarks>
/// A successful initialization is cached permanently. Faulted or canceled attempts are not cached and may be retried by later callers.
/// <para>
/// Concurrent callers share a single factory execution rather than queueing behind a lock: the
/// initialization task is published before the factory runs, and each caller awaits it with its own
/// cancellation token. A caller that gives up cancels only its own wait, never the shared work.
/// Because the work is shared, the factory runs with <see cref="CancellationToken.None"/> — no one
/// caller's token may cancel an initialization the others are waiting on.
/// </para>
/// <para>
/// Unlike the PublicationOnly variant, no value is ever produced and then dropped: exactly one
/// factory execution succeeds, and its value is the one every caller receives. A disposable
/// <typeparamref name="T"/> is therefore usable here. This type never disposes the value, but it
/// never hides one from you either, so disposal remains the caller's responsibility.
/// </para>
/// </remarks>
public sealed class LazyAsyncExecutionAndPublication<T>
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
        Argument.NotNull(factory);

        m_hasValue = false;
        m_factory = factory;
    }

    /// <summary>
    /// Gets a value indicating whether the value has been initialized.
    /// </summary>
    public bool HasValue => m_hasValue;

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the value is produced.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the value factory returns a null task.</exception>
    public ValueTask<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        if (m_hasValue)
        {
            // Initialized: not async, so the common path costs no allocation and no state machine.
            return new ValueTask<T>(m_value);
        }

        // This call has to wait, so an already-cancelled token cancels it here. The wait below
        // cannot be relied on for it: a shared initialization that already completed hands back its
        // result without consulting the token.
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<T>(cancellationToken);
        }

        return InitializeAsync(cancellationToken);
    }

    private async ValueTask<T> InitializeAsync(CancellationToken cancellationToken)
    {
        // WaitAsync binds this caller's cancellation to this caller's wait only.
        return await GetOrCreateInitializationTask().WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    // Returns a task for an in-progress or newly started initialization. Exactly one factory
    // execution runs at a time; concurrent callers join it.
    private Task<T> GetOrCreateInitializationTask()
    {
        TaskCompletionSource<T> tcs;

        lock (m_lock)
        {
            if (m_pendingInitialization is not null && !m_pendingInitialization.IsCompleted)
            {
                return m_pendingInitialization;
            }

            // Observe a completed faulted attempt so UnobservedTaskException never fires. A faulted
            // attempt is not cached, so the next caller starts a fresh one.
            if (m_pendingInitialization?.IsFaulted == true)
            {
                _ = m_pendingInitialization!.Exception;
            }

            if (m_hasValue)
            {
                return Task.FromResult(m_value);
            }

            tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_pendingInitialization = tcs.Task;

            _ = m_pendingInitialization.ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        // Invoke the factory outside the lock, so it is never held across user code.
        _ = CompleteAsync(tcs);

        return tcs.Task;
    }

    private async Task CompleteAsync(TaskCompletionSource<T> tcs)
    {
        try
        {
            // m_factory is non-null whenever m_hasValue is false: the value constructor sets
            // m_hasValue to true, and the factory constructor sets m_factory.
            Task<T> factoryTask = m_factory!(CancellationToken.None)
                ?? throw new InvalidOperationException("The value factory returned a null task.");
            T value = await factoryTask.ConfigureAwait(false);

            m_value = value;
            m_hasValue = true;
            _ = tcs.TrySetResult(value);
        }
        catch (Exception ex)
        {
            _ = tcs.TrySetException(ex);
        }
    }

    private readonly object m_lock = new object();
    private readonly Func<CancellationToken, Task<T>>? m_factory;
    private volatile bool m_hasValue;

    // Assigned before m_hasValue is set to true; only ever read after observing m_hasValue == true.
    private T m_value = default!;

    // Written and read only under m_lock.
    private Task<T>? m_pendingInitialization;
}
