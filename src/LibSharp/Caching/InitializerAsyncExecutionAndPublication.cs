// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

namespace LibSharp.Caching;

/// <summary>
/// Async initializer with LazyThreadSafetyMode.ExecutionAndPublication.
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
/// When callers race, the factory supplied by whichever caller starts the initialization is the one
/// that runs; the others receive its result without their own factory being invoked.
/// </para>
/// <para>
/// Unlike the PublicationOnly variant, no value is ever produced and then dropped: exactly one
/// factory execution succeeds, and its value is the one every caller receives. A disposable
/// <typeparamref name="T"/> is therefore usable here. This type never disposes the value, so
/// disposal remains the caller's responsibility.
/// </para>
/// </remarks>
public sealed class InitializerAsyncExecutionAndPublication<T> : IInitializerAsync<T>
{
    /// <inheritdoc/>
    public bool HasValue => _hasValue;

    /// <inheritdoc/>
    public ValueTask<T> GetValueAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        Argument.NotNull(factory);

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

        return InitializeAsync(factory, cancellationToken);
    }

    private async ValueTask<T> InitializeAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        // WaitAsync binds this caller's cancellation to this caller's wait only.
        return await GetOrCreateInitializationTask(factory).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    // Returns a task for an in-progress or newly started initialization. Exactly one factory
    // execution runs at a time; concurrent callers join it.
    private Task<T> GetOrCreateInitializationTask(Func<CancellationToken, Task<T>> factory)
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
        _ = CompleteAsync(factory, tcs);

        return tcs.Task;
    }

    private async Task CompleteAsync(Func<CancellationToken, Task<T>> factory, TaskCompletionSource<T> tcs)
    {
        try
        {
            Task<T> factoryTask = factory(CancellationToken.None)
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
    private volatile bool _hasValue;

    // Assigned before _hasValue is set to true; only ever read after observing _hasValue == true.
    private T _value = default!;

    // Written and read only under _lock.
    private Task<T>? _pendingInitialization;
}
