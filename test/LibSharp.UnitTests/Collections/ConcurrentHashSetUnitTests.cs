// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections
{
    [TestClass]
    public class ConcurrentHashSetUnitTests
    {
        // ── Constructors ──────────────────────────────────────────────────────

        [TestMethod]
        public void Constructor_Default_IsEmpty()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();

            Assert.AreEqual(0, set.Count);
        }

        [TestMethod]
        public void Constructor_WithComparer_NullComparer_Throws()
        {
            _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
                _ = new ConcurrentHashSet<string>((IEqualityComparer<string>)null));
        }

        [TestMethod]
        public void Constructor_WithComparer_UsesComparer()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
            _ = set.Add("Hello");

            Assert.IsTrue(set.Contains("hello"));
            Assert.IsTrue(set.Contains("HELLO"));
        }

        [TestMethod]
        public void Constructor_WithCollection_NullCollection_Throws()
        {
            _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
                _ = new ConcurrentHashSet<int>((IEnumerable<int>)null));
        }

        [TestMethod]
        public void Constructor_WithCollection_ContainsElements()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

            Assert.AreEqual(3, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
            Assert.IsTrue(set.Contains(3));
        }

        [TestMethod]
        public void Constructor_WithCollection_DuplicatesAreDeduped()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 1, 2, 2, 3 });

            Assert.AreEqual(3, set.Count);
        }

        [TestMethod]
        public void Constructor_WithCollectionAndComparer_NullCollection_Throws()
        {
            _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
                _ = new ConcurrentHashSet<string>(null, StringComparer.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void Constructor_WithCollectionAndComparer_NullComparer_Throws()
        {
            _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
                _ = new ConcurrentHashSet<string>(Array.Empty<string>(), null));
        }

        [TestMethod]
        public void Constructor_WithCollectionAndComparer_UsesComparer()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(
                new[] { "Hello", "World" },
                StringComparer.OrdinalIgnoreCase);

            Assert.IsTrue(set.Contains("hello"));
            Assert.IsTrue(set.Contains("world"));
        }

        // ── Add ───────────────────────────────────────────────────────────────

        [TestMethod]
        public void Add_NewElement_ReturnsTrueAndIncreasesCount()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();

            bool added = set.Add(42);

            Assert.IsTrue(added);
            Assert.AreEqual(1, set.Count);
        }

        [TestMethod]
        public void Add_DuplicateElement_ReturnsFalseAndCountUnchanged()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();
            _ = set.Add(42);

            bool added = set.Add(42);

            Assert.IsFalse(added);
            Assert.AreEqual(1, set.Count);
        }

        [TestMethod]
        public void ICollectionAdd_NewElement_IncreasesCount()
        {
            ICollection<int> set = new ConcurrentHashSet<int>();

            set.Add(42);

            Assert.AreEqual(1, set.Count);
        }

        [TestMethod]
        public void ICollectionAdd_DuplicateElement_CountUnchanged()
        {
            ICollection<int> set = new ConcurrentHashSet<int>();
            set.Add(42);

            set.Add(42);

            Assert.AreEqual(1, set.Count);
        }

        // ── Remove ────────────────────────────────────────────────────────────

        [TestMethod]
        public void Remove_ExistingElement_ReturnsTrueAndDecreasesCount()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

            bool removed = set.Remove(2);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, set.Count);
            Assert.IsFalse(set.Contains(2));
        }

        [TestMethod]
        public void Remove_AbsentElement_ReturnsFalse()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

            bool removed = set.Remove(99);

            Assert.IsFalse(removed);
            Assert.AreEqual(3, set.Count);
        }

        // ── Contains ──────────────────────────────────────────────────────────

        [TestMethod]
        public void Contains_PresentElement_ReturnsTrue()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(new[] { "a", "b" });

            Assert.IsTrue(set.Contains("a"));
        }

        [TestMethod]
        public void Contains_AbsentElement_ReturnsFalse()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(new[] { "a", "b" });

            Assert.IsFalse(set.Contains("z"));
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void Clear_RemovesAllElements()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

            set.Clear();

            Assert.AreEqual(0, set.Count);
            Assert.IsFalse(set.Contains(1));
        }

        // ── CopyTo ────────────────────────────────────────────────────────────

        [TestMethod]
        public void CopyTo_NullArray_Throws()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 2 });

            _ = Assert.ThrowsExactly<ArgumentNullException>(() => set.CopyTo(null, 0));
        }

        [TestMethod]
        public void CopyTo_NegativeIndex_Throws()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 2 });

            _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => set.CopyTo(new int[2], -1));
        }

        [TestMethod]
        public void CopyTo_CopiesElementsToArray()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
            int[] array = new int[3];

            set.CopyTo(array, 0);

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, array);
        }

        [TestMethod]
        public void CopyTo_WithOffset_CopiesAtCorrectPosition()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 7, 8 });
            int[] array = new int[4];

            set.CopyTo(array, 2);

            Assert.AreEqual(0, array[0]);
            Assert.AreEqual(0, array[1]);
            CollectionAssert.AreEquivalent(new[] { 7, 8 }, new[] { array[2], array[3] });
        }

        // ── Enumeration ───────────────────────────────────────────────────────

        [TestMethod]
        public void GetEnumerator_ReturnsAllElements()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

            List<int> result = set.ToList();

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, result);
        }

        [TestMethod]
        public void GetEnumerator_EmptySet_ReturnsEmpty()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();

            Assert.IsFalse(set.Any());
        }

        // ── IsReadOnly ────────────────────────────────────────────────────────

        [TestMethod]
        public void IsReadOnly_ReturnsFalse()
        {
            Assert.IsFalse(new ConcurrentHashSet<int>().IsReadOnly);
        }

        // ── Thread safety ─────────────────────────────────────────────────────

        [TestMethod]
        public async Task ConcurrentAdds_AllSucceedWithoutException()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();

            Task[] tasks = new Task[8];
            for (int t = 0; t < tasks.Length; t++)
            {
                int offset = t * 1000;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        _ = set.Add(offset + i);
                    }
                });
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            Assert.AreEqual(8000, set.Count);
        }

        [TestMethod]
        public async Task ConcurrentAddsAndRemoves_CountRemainsConsistent()
        {
            ConcurrentHashSet<int> set = new ConcurrentHashSet<int>(Enumerable.Range(0, 1000));

            Task adder = Task.Run(() =>
            {
                for (int i = 1000; i < 2000; i++)
                {
                    _ = set.Add(i);
                }
            });

            Task remover = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    _ = set.Remove(i);
                }
            });

            await Task.WhenAll(adder, remover).ConfigureAwait(false);

            Assert.AreEqual(1000, set.Count);
        }
    }
}
