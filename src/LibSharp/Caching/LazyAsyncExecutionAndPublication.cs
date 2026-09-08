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
/// <typeparamref name="T"/> is therefore usable here. This type never disposes the value, so
/// disposal remains the caller's responsibility.
/// </para>
/// </remarks>
public sealed class LazyAsyncExecutionAndPublication<T> : ILazyAsync<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LazyAsyncExecutionAndPublication{T}"/> class from a value.
    /// </summary>
    /// <param name="value">The value to hold.</param>
    public LazyAsyncExecutionAndPublication(T value)
    {
        _hasValue = true;
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyAsyncExecutionAndPublication{T}"/> class from a value factory.
    /// </summary>
    /// <param name="factory">The value factory.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is <c>null</c>.</exception>
    public LazyAsyncExecutionAndPublication(Func<CancellationToken, Task<T>> factory)
    {
        Argument.NotNull(factory);

        _hasValue = false;
        _factory = factory;
    }

    /// <summary>
    /// Gets a value indicating whether the value has been initialized.
    /// </summary>
    public bool HasValue => _hasValue;

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value.</returns>
    /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the value is produced.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the value factory returns a null task.</exception>
    public ValueTask<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        if (_hasValue)
        {
            // Initialized: not async, so the common path costs no allocation and no state machine.
            return new ValueTask<T>(_value);
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

        lock (_lock)
        {
            if (_pendingInitialization is not null && !_pendingInitialization.IsCompleted)
            {
                return _pendingInitialization;
            }

            // Observe a completed faulted attempt so UnobservedTaskException never fires. A faulted
            // attempt is not cached, so the next caller starts a fresh one.
            if (_pendingInitialization is { IsFaulted: true })
            {
                _ = _pendingInitialization.Exception;
            }

            if (_hasValue)
            {
                return Task.FromResult(_value);
            }

            tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingInitialization = tcs.Task;

            _ = _pendingInitialization.ContinueWith(
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
            // _factory is non-null whenever _hasValue is false: the value constructor sets
            // _hasValue to true, and the factory constructor sets _factory.
            Task<T> factoryTask = _factory!(CancellationToken.None)
                ?? throw new InvalidOperationException("The value factory returned a null task.");
            T value = await factoryTask.ConfigureAwait(false);

            _value = value;
            _hasValue = true;
            _ = tcs.TrySetResult(value);
        }
        catch (Exception ex)
        {
            _ = tcs.TrySetException(ex);
        }
    }

    private readonly object _lock = new object();
    private readonly Func<CancellationToken, Task<T>>? _factory;
    private volatile bool _hasValue;

    // Assigned before _hasValue is set to true; only ever read after observing _hasValue == true.
    private T _value = default!;

    // Written and read only under _lock.
    private Task<T>? _pendingInitialization;
}
