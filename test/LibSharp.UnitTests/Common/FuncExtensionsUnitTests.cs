// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Common;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common;

[TestClass]
public class FuncExtensionsUnitTests
{
    [TestMethod]
    public async Task RunWithTimeout_WithoutReturnType_Test()
    {
        // Arrange
        int result = 0;
        Func<CancellationToken, Task> task = async cancellationToken =>
        {
            await Task.CompletedTask.ConfigureAwait(false);
            result = 99;
        };

        // Act
        await task.RunWithTimeout(TimeSpan.FromSeconds(1), cancellationToken: CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(99, result);
    }

    [TestMethod]
    public async Task RunWithTimeout_WithReturnType_Test()
    {
        // Arrange
        Func<CancellationToken, Task<int>> task = async cancellationToken =>
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return 99;
        };

        // Act
        int result = await task.RunWithTimeout(TimeSpan.FromSeconds(1), cancellationToken: CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(99, result);
    }

    [TestMethod]
    public async Task RunWithTimeout_WithoutReturnType_ZeroTimeout_Throws()
    {
        // A zero timeout would create an already-expired CancellationToken, so it must be rejected.
        // RunWithTimeout is async, so argument validation runs inside the state machine; we must await.
        Func<CancellationToken, Task> task = _ => Task.CompletedTask;
        _ = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => task.RunWithTimeout(TimeSpan.Zero)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RunWithTimeout_WithReturnType_ZeroTimeout_Throws()
    {
        // A zero timeout would create an already-expired CancellationToken, so it must be rejected.
        // RunWithTimeout is async, so argument validation runs inside the state machine; we must await.
        Func<CancellationToken, Task<int>> task = _ => Task.FromResult(0);
        _ = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => task.RunWithTimeout(TimeSpan.Zero)).ConfigureAwait(false);
    }

    // -- Timeout enforcement -----------------------------------------------

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_WorkIgnoresItsToken_StillReleasesTheCaller()
    {
        // The point of the method: bounding this call cannot depend on the work cooperating.
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        TaskCompletionSource<int> never = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<CancellationToken, Task<int>> task = _ => never.Task;

        Task<int> call = task.RunWithTimeout(TimeSpan.FromSeconds(30), timeProvider);

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        _ = await Assert.ThrowsExactlyAsync<TimeoutException>(() => call).ConfigureAwait(false);

        // Leave nothing dangling for the test host.
        _ = never.TrySetResult(0);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_WorkHonoursItsToken_StillReportsATimeout()
    {
        // Cooperative work throws OperationCanceledException on the way out; that must not be
        // mistaken for the caller having cancelled.
        FakeTimeProvider timeProvider = new FakeTimeProvider();

        Func<CancellationToken, Task<int>> task = async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return 1;
        };

        Task<int> call = task.RunWithTimeout(TimeSpan.FromSeconds(30), timeProvider);

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        _ = await Assert.ThrowsExactlyAsync<TimeoutException>(() => call).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_CallerCancels_ReportsCancellationNotTimeout()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        using CancellationTokenSource callerCts = new CancellationTokenSource();
        TaskCompletionSource<int> never = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<CancellationToken, Task<int>> task = _ => never.Task;

        Task<int> call = task.RunWithTimeout(TimeSpan.FromHours(1), timeProvider, callerCts.Token);

        await callerCts.CancelAsync().ConfigureAwait(false);

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => call).ConfigureAwait(false);

        _ = never.TrySetResult(0);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_WorkCompletesInTime_ReturnsItsResult()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();

        Func<CancellationToken, Task<int>> task = _ => Task.FromResult(42);

        Assert.AreEqual(42, await task.RunWithTimeout(TimeSpan.FromSeconds(30), timeProvider).ConfigureAwait(false));
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_WorkFaults_PropagatesItsException()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();

        Func<CancellationToken, Task<int>> task = _ => throw new InvalidTimeZoneException("boom");

        _ = await Assert.ThrowsExactlyAsync<InvalidTimeZoneException>(
            () => task.RunWithTimeout(TimeSpan.FromSeconds(30), timeProvider)).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_NullTask_ThrowsInvalidOperationException()
    {
        Func<CancellationToken, Task<int>> task = _ => null!;

        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => task.RunWithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_WithoutReturnType_WorkIgnoresItsToken_StillReleasesTheCaller()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        TaskCompletionSource never = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<CancellationToken, Task> task = _ => never.Task;

        Task call = task.RunWithTimeout(TimeSpan.FromSeconds(30), timeProvider);

        timeProvider.Advance(TimeSpan.FromSeconds(31));

        _ = await Assert.ThrowsExactlyAsync<TimeoutException>(() => call).ConfigureAwait(false);

        _ = never.TrySetResult();
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_AbandonedWork_KeepsAUsableToken()
    {
        // The work outlives the call, so it still holds the linked token. Releasing the token
        // source when the caller stops waiting would make WaitHandle throw underneath it.
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        TaskCompletionSource never = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken captured = default;

        Func<CancellationToken, Task> task = cancellationToken =>
        {
            captured = cancellationToken;
            return never.Task;
        };

        Task call = task.RunWithTimeout(TimeSpan.FromSeconds(30), timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(31));

        _ = await Assert.ThrowsExactlyAsync<TimeoutException>(() => call).ConfigureAwait(false);

        Assert.IsTrue(captured.IsCancellationRequested);
        Assert.IsNotNull(captured.WaitHandle, "The abandoned work must still be able to use its token.");

        // Registering must not throw either; the registration itself is a struct, so there is
        // nothing to assert about it beyond the call completing.
        captured.Register(static () => { }).Dispose();

        _ = never.TrySetResult();
    }

    [TestMethod]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task RunWithTimeout_WorkCompletingSynchronously_Succeeds()
    {
        // A synchronously completed task makes any completion continuation run the moment it is
        // attached, which must not disturb the token this call is still using.
        FakeTimeProvider timeProvider = new FakeTimeProvider();

        Func<CancellationToken, Task<int>> task = _ => Task.FromResult(7);

        Assert.AreEqual(7, await task.RunWithTimeout(TimeSpan.FromSeconds(30), timeProvider).ConfigureAwait(false));
    }
}
