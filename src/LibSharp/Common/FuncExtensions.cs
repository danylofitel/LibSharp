// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibSharp.Common;

/// <summary>
/// Function extensions.
/// </summary>
public static class FuncExtensions
{
    /// <summary>
    /// Runs a task, giving up on it once the timeout elapses.
    /// </summary>
    /// <param name="task">The task to run.</param>
    /// <param name="timeout">How long to wait. Must be positive.</param>
    /// <param name="timeProvider">(Optional) Time provider used to measure the timeout. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <param name="cancellationToken">(Optional) Cancellation token.</param>
    /// <returns>A task that completes when the work does, or faults once the timeout elapses.</returns>
    /// <exception cref="TimeoutException">The timeout elapsed before the work completed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="task"/> returned a null task.</exception>
    /// <remarks>
    /// The caller is released when the timeout elapses whether or not the work cooperates. The work
    /// is also handed a token that is cancelled at the same moment, giving it the chance to stop;
    /// work that ignores that token keeps running unobserved in the background, and only the caller
    /// is released. Cancelling that way is the difference between bounding <em>this call</em> and
    /// bounding the work, and only the first can be guaranteed from the outside.
    /// <para>
    /// A timeout and a cancellation are reported differently on purpose &#8212; <see cref="TimeoutException"/>
    /// against <see cref="OperationCanceledException"/> &#8212; so a caller can tell which happened,
    /// including when work that honours its token throws on the way out.
    /// </para>
    /// </remarks>
    public static async Task RunWithTimeout(
        this Func<CancellationToken, Task> task,
        TimeSpan timeout,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        Argument.NotNull(task);
        Argument.GreaterThan(timeout, TimeSpan.Zero);

        TimeProvider provider = timeProvider ?? TimeProvider.System;

        using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout, provider);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        Task work = task(linkedSource.Token)
            ?? throw new InvalidOperationException("The task factory returned a null task.");

        ObserveFaults(work);

        try
        {
            // Waiting on the token is what bounds this call: it returns when the token fires, even
            // if the work never does.
            await work.WaitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (IsTimeout(timeoutSource, cancellationToken))
        {
            throw new TimeoutException($"The operation did not complete within {timeout}.");
        }
    }

    /// <summary>
    /// Runs a task, giving up on it once the timeout elapses.
    /// </summary>
    /// <typeparam name="T">Task return type.</typeparam>
    /// <param name="task">The task to run.</param>
    /// <param name="timeout">How long to wait. Must be positive.</param>
    /// <param name="timeProvider">(Optional) Time provider used to measure the timeout. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <param name="cancellationToken">(Optional) Cancellation token.</param>
    /// <returns>The task result.</returns>
    /// <exception cref="TimeoutException">The timeout elapsed before the work completed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="task"/> returned a null task.</exception>
    /// <remarks>
    /// The caller is released when the timeout elapses whether or not the work cooperates. The work
    /// is also handed a token that is cancelled at the same moment, giving it the chance to stop;
    /// work that ignores that token keeps running unobserved in the background, and only the caller
    /// is released. Cancelling that way is the difference between bounding <em>this call</em> and
    /// bounding the work, and only the first can be guaranteed from the outside.
    /// <para>
    /// A timeout and a cancellation are reported differently on purpose &#8212; <see cref="TimeoutException"/>
    /// against <see cref="OperationCanceledException"/> &#8212; so a caller can tell which happened,
    /// including when work that honours its token throws on the way out.
    /// </para>
    /// </remarks>
    public static async Task<T> RunWithTimeout<T>(
        this Func<CancellationToken, Task<T>> task,
        TimeSpan timeout,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        Argument.NotNull(task);
        Argument.GreaterThan(timeout, TimeSpan.Zero);

        TimeProvider provider = timeProvider ?? TimeProvider.System;

        using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout, provider);
        using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        Task<T> work = task(linkedSource.Token)
            ?? throw new InvalidOperationException("The task factory returned a null task.");

        ObserveFaults(work);

        try
        {
            return await work.WaitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (IsTimeout(timeoutSource, cancellationToken))
        {
            throw new TimeoutException($"The operation did not complete within {timeout}.");
        }
    }

    // True when the timeout is what ended the wait. A caller who cancelled gets their own
    // OperationCanceledException, which takes precedence when both fired.
    private static bool IsTimeout(CancellationTokenSource timeoutSource, CancellationToken cancellationToken)
    {
        return timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
    }

    // Abandoned work is never awaited, so a fault on it would surface as UnobservedTaskException
    // during finalization, attributed to nothing in particular.
    private static void ObserveFaults(Task work)
    {
        _ = work.ContinueWith(
            static t => _ = t.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
