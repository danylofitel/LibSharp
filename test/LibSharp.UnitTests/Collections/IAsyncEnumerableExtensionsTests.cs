// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections;

[TestClass]
public class IAsyncEnumerableExtensionsTests
{
    // ── Chunk ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Chunk_NullSource_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => CollectAsync(((IAsyncEnumerable<int>)null).Chunk(1.0, _ => 1.0), TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Chunk_ZeroChunkWeight_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => CollectAsync(AsyncRange(0, 1, TestContext.CancellationToken).Chunk(0.0, _ => 1.0), TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Chunk_NullItemWeight_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => CollectAsync(AsyncRange(0, 1, TestContext.CancellationToken).Chunk(1.0, null), TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Chunk_EmptySequence_ReturnsZeroChunks()
    {
        List<List<int>> chunks = await CollectAsync(AsyncRange(0, 0, TestContext.CancellationToken).Chunk(1.0, _ => 0.0), TestContext.CancellationToken).ConfigureAwait(false);

        Assert.HasCount(0, chunks);
    }

    [TestMethod]
    public async Task Chunk_SingleItem_Returns1ChunkWith1Item()
    {
        List<List<string>> chunks = await CollectAsync(AsyncValues(TestContext.CancellationToken, "value").Chunk(100.0, _ => 1.0), TestContext.CancellationToken).ConfigureAwait(false);

        Assert.HasCount(1, chunks);
        Assert.HasCount(1, chunks[0]);
        Assert.AreEqual("value", chunks[0][0]);
    }

    [TestMethod]
    public async Task Chunk_MultipleItemsFittingIn1Chunk_Returns1ChunkWithAllItems()
    {
        List<List<int>> chunks = await CollectAsync(AsyncRange(0, 10, TestContext.CancellationToken).Chunk(20.0, _ => 1.0), TestContext.CancellationToken).ConfigureAwait(false);

        AssertChunk(chunks, 0, EnumerableRangeArray(0, 10));
        Assert.HasCount(1, chunks);
    }

    [TestMethod]
    public async Task Chunk_MultipleItemsFilling1Chunk_Returns1ChunkWithAllItems()
    {
        List<List<int>> chunks = await CollectAsync(AsyncRange(0, 10, TestContext.CancellationToken).Chunk(10.0, _ => 1.0), TestContext.CancellationToken).ConfigureAwait(false);

        AssertChunk(chunks, 0, EnumerableRangeArray(0, 10));
        Assert.HasCount(1, chunks);
    }

    [TestMethod]
    public async Task Chunk_MultipleItemsMakingMultipleFullChunks_ReturnsMultipleFullChunks()
    {
        List<List<int>> chunks = await CollectAsync(AsyncRepeatingModulo(100, 10, TestContext.CancellationToken).Chunk(10.0, _ => 1.0), TestContext.CancellationToken).ConfigureAwait(false);

        Assert.HasCount(10, chunks);

        for (int i = 0; i < 10; ++i)
        {
            AssertChunk(chunks, i, EnumerableRangeArray(0, 10));
        }
    }

    [TestMethod]
    public async Task Chunk_MultipleItemsMakingMultipleChunks_ReturnsMultipleChunks()
    {
        List<List<int>> chunks = await CollectAsync(AsyncRepeatingModulo(101, 10, TestContext.CancellationToken).Chunk(10.0, _ => 1.0), TestContext.CancellationToken).ConfigureAwait(false);

        Assert.HasCount(11, chunks);

        for (int i = 0; i < 10; ++i)
        {
            AssertChunk(chunks, i, EnumerableRangeArray(0, 10));
        }

        AssertChunk(chunks, 10, 0);
    }

    [TestMethod]
    public async Task Chunk_RepeatedEnumerationOfChunkItems_Succeeds()
    {
        const int Iterations = 3;
        List<List<int>> chunks = await CollectAsync(AsyncRepeatingModulo(100, 10, TestContext.CancellationToken).Chunk(10.0, _ => 1.0), TestContext.CancellationToken).ConfigureAwait(false);

        Assert.HasCount(10, chunks);

        foreach (List<int> chunk in chunks)
        {
            for (int iteration = 0; iteration < Iterations; ++iteration)
            {
                int currentItem = 0;

                foreach (int item in chunk)
                {
                    Assert.AreEqual(currentItem, item);
                    ++currentItem;
                }

                Assert.AreEqual(10, currentItem);
            }
        }
    }

    [TestMethod]
    public async Task Chunk_SingleItemWeightMoreThanChunkWeight_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => CollectAsync(AsyncValues(TestContext.CancellationToken, 101.0).Chunk(100.0, x => x), TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Chunk_NegativeItemWeight_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => CollectAsync(AsyncValues(TestContext.CancellationToken, -1.0).Chunk(100.0, x => x), TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Chunk_PreCanceledToken_Throws()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => CollectAsync(AsyncRange(0, 5, TestContext.CancellationToken).Chunk(10.0, _ => 1.0), cts.Token)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Chunk_MidStreamCancellation_Throws()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => CollectAsync(AsyncRangeCancelMidStream(cts, 2, 0, 10, TestContext.CancellationToken).Chunk(10.0, _ => 1.0), cts.Token)).ConfigureAwait(false);
    }

    // ── FirstIndexOfAsync ─────────────────────────────────────────────────

    [TestMethod]
    public async Task FirstIndexOfAsync_NullSource_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => ((IAsyncEnumerable<int>)null).FirstIndexOfAsync(_ => true, TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_NullPredicate_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => AsyncRange(0, 5, TestContext.CancellationToken).FirstIndexOfAsync(null, TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_EmptySequence_ReturnsMinusOne()
    {
        int index = await AsyncRange(0, 0, TestContext.CancellationToken).FirstIndexOfAsync(_ => true, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(-1, index);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_NoMatch_ReturnsMinusOne()
    {
        int index = await AsyncRange(0, 5, TestContext.CancellationToken).FirstIndexOfAsync(x => x > 100, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(-1, index);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_FirstElement_ReturnsZero()
    {
        int index = await AsyncRange(0, 5, TestContext.CancellationToken).FirstIndexOfAsync(x => x == 0, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(0, index);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_LastElement_ReturnsLastIndex()
    {
        int index = await AsyncRange(0, 5, TestContext.CancellationToken).FirstIndexOfAsync(x => x == 4, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(4, index);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_MultipleMatches_ReturnsFirstIndex()
    {
        // Sequence: 0 1 2 3 4 — even numbers at indices 0, 2, 4
        int index = await AsyncRange(0, 5, TestContext.CancellationToken).FirstIndexOfAsync(x => x % 2 == 0, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(0, index);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_StopsAtFirstMatch()
    {
        // The sequence throws if enumeration continues past index 2.
        // Without early termination the test would throw instead of returning 2.
        int index = await AsyncSequenceThrowingAfterIndex(2, 0, 1, 2, 3, 4).FirstIndexOfAsync(x => x == 2, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(2, index);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_PreCanceledToken_Throws()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => AsyncRange(0, 5, TestContext.CancellationToken).FirstIndexOfAsync(_ => false, cts.Token)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FirstIndexOfAsync_MidStreamCancellation_Throws()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => AsyncRangeCancelMidStream(cts, 2, 0, 10, TestContext.CancellationToken).FirstIndexOfAsync(_ => false, cts.Token)).ConfigureAwait(false);
    }

    // ── LastIndexOfAsync ──────────────────────────────────────────────────

    [TestMethod]
    public async Task LastIndexOfAsync_NullSource_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => ((IAsyncEnumerable<int>)null).LastIndexOfAsync(_ => true, TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LastIndexOfAsync_NullPredicate_Throws()
    {
        _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            () => AsyncRange(0, 5, TestContext.CancellationToken).LastIndexOfAsync(null, TestContext.CancellationToken)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LastIndexOfAsync_EmptySequence_ReturnsMinusOne()
    {
        int index = await AsyncRange(0, 0, TestContext.CancellationToken).LastIndexOfAsync(_ => true, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(-1, index);
    }

    [TestMethod]
    public async Task LastIndexOfAsync_NoMatch_ReturnsMinusOne()
    {
        int index = await AsyncRange(0, 5, TestContext.CancellationToken).LastIndexOfAsync(x => x > 100, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(-1, index);
    }

    [TestMethod]
    public async Task LastIndexOfAsync_SingleMatch_ReturnsItsIndex()
    {
        int index = await AsyncRange(0, 5, TestContext.CancellationToken).LastIndexOfAsync(x => x == 3, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(3, index);
    }

    [TestMethod]
    public async Task LastIndexOfAsync_MultipleMatches_ReturnsLastIndex()
    {
        // Sequence: 0 1 2 3 4 — even numbers at indices 0, 2, 4
        int index = await AsyncRange(0, 5, TestContext.CancellationToken).LastIndexOfAsync(x => x % 2 == 0, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(4, index);
    }

    [TestMethod]
    public async Task LastIndexOfAsync_AllMatch_ReturnsLastIndex()
    {
        int index = await AsyncRange(0, 5, TestContext.CancellationToken).LastIndexOfAsync(_ => true, TestContext.CancellationToken).ConfigureAwait(false);

        Assert.AreEqual(4, index);
    }

    [TestMethod]
    public async Task LastIndexOfAsync_PreCanceledToken_Throws()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => AsyncRange(0, 5, TestContext.CancellationToken).LastIndexOfAsync(_ => false, cts.Token)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LastIndexOfAsync_MidStreamCancellation_Throws()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();

        _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => AsyncRangeCancelMidStream(cts, 2, 0, 10, TestContext.CancellationToken).LastIndexOfAsync(_ => false, cts.Token)).ConfigureAwait(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async IAsyncEnumerable<int> AsyncRange(
        int start,
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return start + i;
        }
    }

    private static async IAsyncEnumerable<T> AsyncValues<T>(
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        params T[] values)
    {
        for (int i = 0; i < values.Length; ++i)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return values[i];
        }
    }

    private static async IAsyncEnumerable<int> AsyncRepeatingModulo(
        int count,
        int modulo,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; ++i)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return i % modulo;
        }
    }

    private static async IAsyncEnumerable<int> AsyncSequenceThrowingAfterIndex(
        int throwAfterIndex,
        params int[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > throwAfterIndex)
            {
                throw new InvalidOperationException("Enumeration should have stopped before this element.");
            }

            await Task.Yield();
            yield return values[i];
        }
    }

    private static async IAsyncEnumerable<int> AsyncRangeCancelMidStream(
        CancellationTokenSource cts,
        int cancelAfterElements,
        int start,
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (i == cancelAfterElements - 1)
            {
                cts.Cancel();
            }

            await Task.Yield();
            yield return start + i;
        }
    }

    private static async Task<List<T>> CollectAsync<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        List<T> results = new List<T>();

        await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            results.Add(item);
        }

        return results;
    }

    private static int[] EnumerableRangeArray(int start, int count)
    {
        int[] values = new int[count];

        for (int i = 0; i < count; ++i)
        {
            values[i] = start + i;
        }

        return values;
    }

    private static void AssertChunk(IReadOnlyList<List<int>> chunks, int chunkIndex, params int[] expected)
    {
        Assert.HasCount(expected.Length, chunks[chunkIndex]);

        for (int i = 0; i < expected.Length; ++i)
        {
            Assert.AreEqual(expected[i], chunks[chunkIndex][i]);
        }
    }

    public TestContext TestContext { get; set; }
}
