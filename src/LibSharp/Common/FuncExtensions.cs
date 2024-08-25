// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;

/// <summary>
/// Function extensions.
/// </summary>
public static class FuncExtensions
{
    /// <summary>
    /// Runs a task with timeout.
    /// </summary>
    /// <param name="task">The task to run.</param>
    /// <param name="timeout">Timeout.</param>
    /// <param name="cancellationToken">(Optional) Task cancellation token.</param>
    /// <returns>Task result.</returns>
    /// <exception cref="OperationCanceledException">If the task cancellation token is canceled or the timeout is reached.</exception>
    public static async Task RunWithTimeout(this Func<CancellationToken, Task> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        Argument.NotNull(task, nameof(task));
        Argument.GreaterThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout));

        using CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
        using CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);
        await task(combinedTokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a task with timeout.
    /// </summary>
    /// <typeparam name="T">Task return type.</typeparam>
    /// <param name="task">The task to run.</param>
    /// <param name="timeout">Timeout.</param>
    /// <param name="cancellationToken">(Optional) Task cancellation token.</param>
    /// <returns>Task result.</returns>
    /// <exception cref="OperationCanceledException">If the task cancellation token is canceled or the timeout is reached.</exception>
    public static async Task<T> RunWithTimeout<T>(this Func<CancellationToken, Task<T>> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        Argument.NotNull(task, nameof(task));
        Argument.GreaterThanOrEqualTo(timeout, TimeSpan.Zero, nameof(timeout));

        using CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
        using CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);
        return await task(combinedTokenSource.Token).ConfigureAwait(false);
    }
}
