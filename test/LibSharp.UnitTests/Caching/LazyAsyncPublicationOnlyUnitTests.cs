// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class LazyAsyncPublicationOnlyUnitTests
{
    [TestMethod]
    public async Task FromValue()
    {
        // Arrange
        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(5);

        // Assert
        Assert.IsTrue(lazy.HasValue);
        Assert.AreEqual(5, await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task FromValue_CanceledToken_Succeeds()
    {
        // Arrange
        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(5);

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            cancellationTokenSource.Cancel();

            // Assert
            Assert.IsTrue(lazy.HasValue);
            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationTokenSource.Token).ConfigureAwait(false));
        }
    }

    [TestMethod]
    public async Task FromNullValue_HasValueIsTrue_ReturnsNull()
    {
        // Arrange — null is a legitimate value to cache; HasValue reflects whether the
        // wrapper has been initialised, not whether the contained value is non-null.
        LazyAsyncPublicationOnly<string> lazy = new LazyAsyncPublicationOnly<string>((string)null!);

        // Assert
        Assert.IsTrue(lazy.HasValue);
        Assert.IsNull(await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task FromFactory_NullResult_HasValueIsTrue_ReturnsNull()
    {
        // Arrange
        LazyAsyncPublicationOnly<string> lazy = new LazyAsyncPublicationOnly<string>(_ => Task.FromResult<string>(null!));

        // Assert
        Assert.IsFalse(lazy.HasValue);
        Assert.IsNull(await lazy.GetValueAsync(TestContext.CancellationToken).ConfigureAwait(false));
        Assert.IsTrue(lazy.HasValue);
    }

    [TestMethod]
    public async Task FromValueFactory()
    {
        // Arrange
        Func<CancellationToken, Task<int>> factory = Substitute.For<Func<CancellationToken, Task<int>>>();

        _ = factory(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(factory);

        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Assert
            Assert.IsFalse(lazy.HasValue);
            _ = factory.DidNotReceive()(cancellationToken);

            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));

            Assert.IsTrue(lazy.HasValue);
            _ = factory.Received(1)(cancellationToken);

            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));
            Assert.AreEqual(5, await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false));

            Assert.IsTrue(lazy.HasValue);
            _ = factory.Received(1)(cancellationToken);
        }
    }

    [TestMethod]
    public async Task FromFactory_NullTask_ThrowsInvalidOperationException()
    {
        // Arrange
        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(_ => null!);

        // Act
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await lazy.GetValueAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GetValueAsync_ConcurrentCallers_PublishSingleWinningValue()
    {
        // Arrange
        TaskCompletionSource<bool> bothFactoriesStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int executionCount = 0;

        LazyAsyncPublicationOnly<int> lazy = new LazyAsyncPublicationOnly<int>(async cancellationToken =>
        {
            int count = Interlocked.Increment(ref executionCount);
            if (count == 2)
            {
                _ = bothFactoriesStarted.TrySetResult(true);
            }

            _ = await bothFactoriesStarted.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return count;
        });

        // Act
        Task<int> first = Task.Run(() => lazy.GetValueAsync(CancellationToken.None).AsTask(), CancellationToken.None);
        Task<int> second = Task.Run(() => lazy.GetValueAsync(CancellationToken.None).AsTask(), CancellationToken.None);
        int[] results = await Task.WhenAll(first, second).ConfigureAwait(false);
        int published = await lazy.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(2, executionCount);
        Assert.AreEqual(results[0], results[1]);
        Assert.AreEqual(results[0], published);
        Assert.IsTrue(lazy.HasValue);
    }

    [TestMethod]
    public async Task DroppedValue_IsDisposed_AndPublishedValueIsNot()
    {
        // Arrange — both racers start before either can publish, so the race is forced, not timed.
        List<Tracked> created = new List<Tracked>();
        TaskCompletionSource gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        LazyAsyncPublicationOnly<Tracked> lazy = new LazyAsyncPublicationOnly<Tracked>(async _ =>
        {
            Tracked tracked = new Tracked();
            lock (created)
            {
                created.Add(tracked);
            }

            await gate.Task.ConfigureAwait(false);
            return tracked;
        });

        // Act — the factory runs synchronously up to the gate, so both have registered by now.
        Task<Tracked> first = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        Task<Tracked> second = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        gate.SetResult();
        Tracked firstValue = await first.ConfigureAwait(false);
        Tracked secondValue = await second.ConfigureAwait(false);

        // Assert — whichever won, exactly the other one is disposed.
        Assert.AreEqual(2, created.Count);
        Assert.AreSame(firstValue, secondValue);
        Assert.IsFalse(firstValue.Disposed, "the published value must never be disposed");

        Tracked dropped = created[0] == firstValue ? created[1] : created[0];
        Assert.IsTrue(dropped.Disposed, "the dropped value must be disposed");
    }

    [TestMethod]
    public async Task DroppedValue_DisposalDisabled_LeavesItAlone()
    {
        // Arrange
        List<Tracked> created = new List<Tracked>();
        TaskCompletionSource gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        LazyAsyncPublicationOnly<Tracked> lazy = new LazyAsyncPublicationOnly<Tracked>(
            async _ =>
            {
                Tracked tracked = new Tracked();
                lock (created)
                {
                    created.Add(tracked);
                }

                await gate.Task.ConfigureAwait(false);
                return tracked;
            },
            disposeDroppedValues: false);

        // Act
        Task<Tracked> first = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        Task<Tracked> second = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        gate.SetResult();
        _ = await first.ConfigureAwait(false);
        _ = await second.ConfigureAwait(false);

        // Assert
        Assert.AreEqual(2, created.Count);
        Assert.IsFalse(created[0].Disposed);
        Assert.IsFalse(created[1].Disposed);
    }

    [TestMethod]
    public async Task DroppedValue_SharedInstance_IsNeverDisposed()
    {
        // Arrange — a factory handing back one shared instance: the loser's value IS the winner's,
        // so disposing it would destroy the published value.
        // Disposed at scope exit, after the assertion: the point is that the library does not.
        using Tracked shared = new Tracked();
        TaskCompletionSource gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        LazyAsyncPublicationOnly<Tracked> lazy = new LazyAsyncPublicationOnly<Tracked>(async _ =>
        {
            await gate.Task.ConfigureAwait(false);
            return shared;
        });

        // Act
        Task<Tracked> first = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        Task<Tracked> second = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        gate.SetResult();
        Tracked firstValue = await first.ConfigureAwait(false);
        _ = await second.ConfigureAwait(false);

        // Assert
        Assert.AreSame(shared, firstValue);
        Assert.IsFalse(shared.Disposed, "identity must be checked before disposing a dropped value");
    }

    [TestMethod]
    public async Task DroppedValue_AsyncDisposable_IsPreferredOverDisposable()
    {
        // Arrange
        List<BothDisposable> created = new List<BothDisposable>();
        TaskCompletionSource gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        LazyAsyncPublicationOnly<BothDisposable> lazy = new LazyAsyncPublicationOnly<BothDisposable>(async _ =>
        {
            BothDisposable value = new BothDisposable();
            lock (created)
            {
                created.Add(value);
            }

            await gate.Task.ConfigureAwait(false);
            return value;
        });

        // Act
        Task<BothDisposable> first = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        Task<BothDisposable> second = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        gate.SetResult();
        BothDisposable firstValue = await first.ConfigureAwait(false);
        _ = await second.ConfigureAwait(false);

        // Assert
        BothDisposable dropped = created[0] == firstValue ? created[1] : created[0];
        Assert.IsTrue(dropped.AsyncDisposed);
        Assert.IsFalse(dropped.SyncDisposed, "IAsyncDisposable must win when a type implements both");
    }

    [TestMethod]
    public async Task DroppedValue_DisposalThrows_DoesNotSurfaceToCaller()
    {
        // Arrange — cleanup of a value the caller never saw must not fail the caller's own call.
        TaskCompletionSource gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        LazyAsyncPublicationOnly<ThrowingDisposable> lazy = new LazyAsyncPublicationOnly<ThrowingDisposable>(async _ =>
        {
            ThrowingDisposable value = new ThrowingDisposable();
            await gate.Task.ConfigureAwait(false);
            return value;
        });

        // Act
        Task<ThrowingDisposable> first = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        Task<ThrowingDisposable> second = lazy.GetValueAsync(TestContext.CancellationToken).AsTask();
        gate.SetResult();

        // Assert
        Assert.IsNotNull(await first.ConfigureAwait(false));
        Assert.IsNotNull(await second.ConfigureAwait(false));
    }

    private sealed class Tracked : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class BothDisposable : IDisposable, IAsyncDisposable
    {
        public bool SyncDisposed { get; private set; }

        public bool AsyncDisposed { get; private set; }

        public void Dispose()
        {
            SyncDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return default;
        }
    }

    private sealed class ThrowingDisposable : IDisposable
    {
        public void Dispose()
        {
            throw new InvalidOperationException("disposal failed");
        }
    }

    // MSTest assigns this by property injection after construction. The initializer states that
    // explicitly: without it the compiler reports CS8618, which the normal build suppresses but
    // `dotnet format` does not, and its code-fix pass then makes the property nullable and breaks
    // every TestContext.CancellationToken use.
    public TestContext TestContext { get; set; } = null!;
}
