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
    /// bounding the work, and only the first can be guaranteed from the outside. Abandoned work
    /// keeps a usable token until it finishes, so it can still observe its own cancellation.
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

        CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout, provider);
        CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        Task work;
        try
        {
            work = task(linkedSource.Token)
                ?? throw new InvalidOperationException("The task factory returned a null task.");
        }
        catch
        {
            // Nothing took the token, so nothing else can be holding the sources.
            linkedSource.Dispose();
            timeoutSource.Dispose();
            throw;
        }

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
        finally
        {
            Release(work, linkedSource, timeoutSource);
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
    /// bounding the work, and only the first can be guaranteed from the outside. Abandoned work
    /// keeps a usable token until it finishes, so it can still observe its own cancellation.
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

        CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout, provider);
        CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        Task<T> work;
        try
        {
            work = task(linkedSource.Token)
                ?? throw new InvalidOperationException("The task factory returned a null task.");
        }
        catch
        {
            // Nothing took the token, so nothing else can be holding the sources.
            linkedSource.Dispose();
            timeoutSource.Dispose();
            throw;
        }

        try
        {
            return await work.WaitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (IsTimeout(timeoutSource, cancellationToken))
        {
            throw new TimeoutException($"The operation did not complete within {timeout}.");
        }
        finally
        {
            Release(work, linkedSource, timeoutSource);
        }
    }

    // True when the timeout is what ended the wait. A caller who cancelled gets their own
    // OperationCanceledException, which takes precedence when both fired. This runs as an exception
    // filter, ahead of the finally below, so the sources are still alive when it reads them.
    private static bool IsTimeout(CancellationTokenSource timeoutSource, CancellationToken cancellationToken)
    {
        return timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
    }

    // The sources belong to whoever still needs the token. Once the work has finished nobody does,
    // so they are released here. Work that outlived the wait still holds the linked token, and a
    // token whose source has been disposed throws from WaitHandle, so ownership passes to the work.
    private static void Release(Task work, CancellationTokenSource linkedSource, CancellationTokenSource timeoutSource)
    {
        if (work.IsCompleted)
        {
            // Observe a fault the caller may not have awaited, which would otherwise surface as
            // UnobservedTaskException during finalization, attributed to nothing in particular.
            _ = work.Exception;

            linkedSource.Dispose();
            timeoutSource.Dispose();
            return;
        }

        _ = work.ContinueWith(
            static (completed, state) =>
            {
                _ = completed.Exception;

                (CancellationTokenSource Linked, CancellationTokenSource Timeout) sources =
                    ((CancellationTokenSource, CancellationTokenSource))state!;

                sources.Linked.Dispose();
                sources.Timeout.Dispose();
            },
            (linkedSource, timeoutSource),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
