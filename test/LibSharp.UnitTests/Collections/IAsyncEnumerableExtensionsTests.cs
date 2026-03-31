// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections
{
    [TestClass]
    public class IAsyncEnumerableExtensionsTests
    {
        // ── FirstIndexOfAsync ─────────────────────────────────────────────────

        [TestMethod]
        public async Task FirstIndexOfAsync_NullSource_Throws()
        {
            _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => ((IAsyncEnumerable<int>)null).FirstIndexOfAsync(_ => true)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_NullPredicate_Throws()
        {
            _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => AsyncRange(0, 5).FirstIndexOfAsync(null)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_EmptySequence_ReturnsMinusOne()
        {
            int index = await AsyncRange(0, 0).FirstIndexOfAsync(_ => true).ConfigureAwait(false);

            Assert.AreEqual(-1, index);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_NoMatch_ReturnsMinusOne()
        {
            int index = await AsyncRange(0, 5).FirstIndexOfAsync(x => x > 100).ConfigureAwait(false);

            Assert.AreEqual(-1, index);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_FirstElement_ReturnsZero()
        {
            int index = await AsyncRange(0, 5).FirstIndexOfAsync(x => x == 0).ConfigureAwait(false);

            Assert.AreEqual(0, index);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_LastElement_ReturnsLastIndex()
        {
            int index = await AsyncRange(0, 5).FirstIndexOfAsync(x => x == 4).ConfigureAwait(false);

            Assert.AreEqual(4, index);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_MultipleMatches_ReturnsFirstIndex()
        {
            // Sequence: 0 1 2 3 4 — even numbers at indices 0, 2, 4
            int index = await AsyncRange(0, 5).FirstIndexOfAsync(x => x % 2 == 0).ConfigureAwait(false);

            Assert.AreEqual(0, index);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_StopsAtFirstMatch()
        {
            // The sequence throws if enumeration continues past index 2.
            // Without early termination the test would throw instead of returning 2.
            int index = await AsyncSequenceThrowingAfterIndex(2, 0, 1, 2, 3, 4).FirstIndexOfAsync(x => x == 2).ConfigureAwait(false);

            Assert.AreEqual(2, index);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_PreCanceledToken_Throws()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => AsyncRange(0, 5).FirstIndexOfAsync(_ => false, cts.Token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task FirstIndexOfAsync_MidStreamCancellation_Throws()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => AsyncRangeCancelMidStream(cts, 2, 0, 10).FirstIndexOfAsync(_ => false, cts.Token)).ConfigureAwait(false);
        }

        // ── LastIndexOfAsync ──────────────────────────────────────────────────

        [TestMethod]
        public async Task LastIndexOfAsync_NullSource_Throws()
        {
            _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => ((IAsyncEnumerable<int>)null).LastIndexOfAsync(_ => true)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task LastIndexOfAsync_NullPredicate_Throws()
        {
            _ = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => AsyncRange(0, 5).LastIndexOfAsync(null)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task LastIndexOfAsync_EmptySequence_ReturnsMinusOne()
        {
            int index = await AsyncRange(0, 0).LastIndexOfAsync(_ => true).ConfigureAwait(false);

            Assert.AreEqual(-1, index);
        }

        [TestMethod]
        public async Task LastIndexOfAsync_NoMatch_ReturnsMinusOne()
        {
            int index = await AsyncRange(0, 5).LastIndexOfAsync(x => x > 100).ConfigureAwait(false);

            Assert.AreEqual(-1, index);
        }

        [TestMethod]
        public async Task LastIndexOfAsync_SingleMatch_ReturnsItsIndex()
        {
            int index = await AsyncRange(0, 5).LastIndexOfAsync(x => x == 3).ConfigureAwait(false);

            Assert.AreEqual(3, index);
        }

        [TestMethod]
        public async Task LastIndexOfAsync_MultipleMatches_ReturnsLastIndex()
        {
            // Sequence: 0 1 2 3 4 — even numbers at indices 0, 2, 4
            int index = await AsyncRange(0, 5).LastIndexOfAsync(x => x % 2 == 0).ConfigureAwait(false);

            Assert.AreEqual(4, index);
        }

        [TestMethod]
        public async Task LastIndexOfAsync_AllMatch_ReturnsLastIndex()
        {
            int index = await AsyncRange(0, 5).LastIndexOfAsync(_ => true).ConfigureAwait(false);

            Assert.AreEqual(4, index);
        }

        [TestMethod]
        public async Task LastIndexOfAsync_PreCanceledToken_Throws()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => AsyncRange(0, 5).LastIndexOfAsync(_ => false, cts.Token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task LastIndexOfAsync_MidStreamCancellation_Throws()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => AsyncRangeCancelMidStream(cts, 2, 0, 10).LastIndexOfAsync(_ => false, cts.Token)).ConfigureAwait(false);
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
    }
}
