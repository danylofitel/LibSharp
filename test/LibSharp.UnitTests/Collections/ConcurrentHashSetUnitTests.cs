// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections;

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
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.AreEqual(3, set.Count);
        Assert.IsTrue(set.Contains(1));
        Assert.IsTrue(set.Contains(2));
        Assert.IsTrue(set.Contains(3));
    }

    [TestMethod]
    public void Constructor_WithCollection_DuplicatesAreDeduped()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 1, 2, 2, 3 };

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

        Assert.HasCount(1, set);
    }

    [TestMethod]
    public void ICollectionAdd_DuplicateElement_CountUnchanged()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();
        set.Add(42);

        set.Add(42);

        Assert.HasCount(1, set);
    }

    // ── Remove ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Remove_ExistingElement_ReturnsTrueAndDecreasesCount()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        bool removed = set.Remove(2);

        Assert.IsTrue(removed);
        Assert.AreEqual(2, set.Count);
        Assert.IsFalse(set.Contains(2));
    }

    [TestMethod]
    public void Remove_AbsentElement_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        bool removed = set.Remove(99);

        Assert.IsFalse(removed);
        Assert.AreEqual(3, set.Count);
    }

    // ── Contains ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Contains_PresentElement_ReturnsTrue()
    {
        ConcurrentHashSet<string> set = new ConcurrentHashSet<string> { "a", "b" };

        Assert.IsTrue(set.Contains("a"));
    }

    [TestMethod]
    public void Contains_AbsentElement_ReturnsFalse()
    {
        ConcurrentHashSet<string> set = new ConcurrentHashSet<string> { "a", "b" };

        Assert.IsFalse(set.Contains("z"));
    }

    // ── Clear ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Clear_RemovesAllElements()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        set.Clear();

        Assert.AreEqual(0, set.Count);
        Assert.IsFalse(set.Contains(1));
    }

    // ── CopyTo ────────────────────────────────────────────────────────────

    [TestMethod]
    public void CopyTo_NullArray_Throws()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2 };

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => set.CopyTo(null, 0));
    }

    [TestMethod]
    public void CopyTo_NegativeIndex_Throws()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2 };

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => set.CopyTo(new int[2], -1));
    }

    [TestMethod]
    public void CopyTo_CopiesElementsToArray()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };
        int[] array = new int[3];

        set.CopyTo(array, 0);

        CollectionAssert.AreEquivalent(s_expected, array);
    }

    [TestMethod]
    public void CopyTo_WithOffset_CopiesAtCorrectPosition()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 7, 8 };
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
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        List<int> result = new List<int>(set);

        CollectionAssert.AreEquivalent(s_expected, result);
    }

    [TestMethod]
    public void GetEnumerator_EmptySet_ReturnsEmpty()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();

        Assert.IsFalse(set.Count != 0);
    }

    // ── IsReadOnly ────────────────────────────────────────────────────────

    [TestMethod]
    public void IsReadOnly_ReturnsFalse()
    {
        Assert.IsFalse(new ConcurrentHashSet<int>().IsReadOnly);
    }

    // ── ISet<T> / IReadOnlySet<T> ─────────────────────────────────────────

    [TestMethod]
    public void UnionWith_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().UnionWith(null));
    }

    [TestMethod]
    public void UnionWith_AddsNewElements()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2 };

        set.UnionWith(new[] { 2, 3, 4 });

        Assert.AreEqual(4, set.Count);
        Assert.IsTrue(set.Contains(1));
        Assert.IsTrue(set.Contains(2));
        Assert.IsTrue(set.Contains(3));
        Assert.IsTrue(set.Contains(4));
    }

    [TestMethod]
    public void UnionWith_EmptyOther_Unchanged()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2 };

        set.UnionWith(Array.Empty<int>());

        Assert.AreEqual(2, set.Count);
    }

    [TestMethod]
    public void IntersectWith_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().IntersectWith(null));
    }

    [TestMethod]
    public void IntersectWith_KeepsOnlyCommonElements()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3, 4 };

        set.IntersectWith(new[] { 2, 4, 6 });

        Assert.AreEqual(2, set.Count);
        Assert.IsTrue(set.Contains(2));
        Assert.IsTrue(set.Contains(4));
        Assert.IsFalse(set.Contains(1));
        Assert.IsFalse(set.Contains(3));
    }

    [TestMethod]
    public void IntersectWith_EmptyOther_ClearsSet()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        set.IntersectWith(Array.Empty<int>());

        Assert.AreEqual(0, set.Count);
    }

    [TestMethod]
    public void ExceptWith_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().ExceptWith(null));
    }

    [TestMethod]
    public void ExceptWith_RemovesCommonElements()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3, 4 };

        set.ExceptWith(new[] { 2, 4, 6 });

        Assert.AreEqual(2, set.Count);
        Assert.IsTrue(set.Contains(1));
        Assert.IsTrue(set.Contains(3));
        Assert.IsFalse(set.Contains(2));
        Assert.IsFalse(set.Contains(4));
    }

    [TestMethod]
    public void ExceptWith_EmptyOther_Unchanged()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        set.ExceptWith(Array.Empty<int>());

        Assert.AreEqual(3, set.Count);
    }

    [TestMethod]
    public void SymmetricExceptWith_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().SymmetricExceptWith(null));
    }

    [TestMethod]
    public void SymmetricExceptWith_TogglesElements()
    {
        // {1, 2, 3} △ {2, 3, 4} = {1, 4}
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        set.SymmetricExceptWith(new[] { 2, 3, 4 });

        Assert.AreEqual(2, set.Count);
        Assert.IsTrue(set.Contains(1));
        Assert.IsTrue(set.Contains(4));
        Assert.IsFalse(set.Contains(2));
        Assert.IsFalse(set.Contains(3));
    }

    [TestMethod]
    public void SymmetricExceptWith_DuplicatesInOther_Ignored()
    {
        // {}.SymmetricExceptWith([1, 1]) — 1 appears in other (deduplicated), not in set → add 1 once.
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int>();

        set.SymmetricExceptWith(new[] { 1, 1 });

        Assert.AreEqual(1, set.Count);
        Assert.IsTrue(set.Contains(1));
    }

    [TestMethod]
    public void IsSubsetOf_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().IsSubsetOf(null));
    }

    [TestMethod]
    public void IsSubsetOf_EmptySet_IsSubsetOfAnything()
    {
        Assert.IsTrue(new ConcurrentHashSet<int>().IsSubsetOf(Array.Empty<int>()));
        Assert.IsTrue(new ConcurrentHashSet<int>().IsSubsetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsSubsetOf_Subset_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2 };

        Assert.IsTrue(set.IsSubsetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsSubsetOf_EqualSets_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsTrue(set.IsSubsetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsSubsetOf_NotSubset_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 4 };

        Assert.IsFalse(set.IsSubsetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsSupersetOf_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().IsSupersetOf(null));
    }

    [TestMethod]
    public void IsSupersetOf_Superset_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsTrue(set.IsSupersetOf(new[] { 1, 2 }));
    }

    [TestMethod]
    public void IsSupersetOf_EqualSets_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsTrue(set.IsSupersetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsSupersetOf_NotSuperset_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2 };

        Assert.IsFalse(set.IsSupersetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsProperSubsetOf_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().IsProperSubsetOf(null));
    }

    [TestMethod]
    public void IsProperSubsetOf_StrictSubset_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2 };

        Assert.IsTrue(set.IsProperSubsetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsProperSubsetOf_EqualSets_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsFalse(set.IsProperSubsetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsProperSubsetOf_NotSubset_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 4 };

        Assert.IsFalse(set.IsProperSubsetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsProperSupersetOf_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().IsProperSupersetOf(null));
    }

    [TestMethod]
    public void IsProperSupersetOf_StrictSuperset_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsTrue(set.IsProperSupersetOf(new[] { 1, 2 }));
    }

    [TestMethod]
    public void IsProperSupersetOf_EqualSets_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsFalse(set.IsProperSupersetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void IsProperSupersetOf_NotSuperset_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2 };

        Assert.IsFalse(set.IsProperSupersetOf(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void Overlaps_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().Overlaps(null));
    }

    [TestMethod]
    public void Overlaps_CommonElement_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsTrue(set.Overlaps(new[] { 3, 4, 5 }));
    }

    [TestMethod]
    public void Overlaps_NoCommonElements_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsFalse(set.Overlaps(new[] { 4, 5, 6 }));
    }

    [TestMethod]
    public void Overlaps_EmptyOther_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsFalse(set.Overlaps(Array.Empty<int>()));
    }

    [TestMethod]
    public void SetEquals_NullOther_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>().SetEquals(null));
    }

    [TestMethod]
    public void SetEquals_EqualSets_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsTrue(set.SetEquals(new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void SetEquals_EqualSets_DuplicatesInOther_ReturnsTrue()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsTrue(set.SetEquals(new[] { 1, 1, 2, 3 }));
    }

    [TestMethod]
    public void SetEquals_DifferentSets_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsFalse(set.SetEquals(new[] { 1, 2, 4 }));
    }

    [TestMethod]
    public void SetEquals_DifferentSizes_ReturnsFalse()
    {
        ConcurrentHashSet<int> set = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsFalse(set.SetEquals(new[] { 1, 2 }));
    }

    [TestMethod]
    public void ISet_UsesComparer()
    {
        // All set operations must respect the custom comparer (case-insensitive here).
        ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
        _ = set.Add("Hello");
        _ = set.Add("World");

        Assert.IsTrue(set.IsSubsetOf(new[] { "hello", "world", "extra" }));
        Assert.IsTrue(set.IsSupersetOf(new[] { "HELLO" }));
        Assert.IsTrue(set.SetEquals(new[] { "HELLO", "WORLD" }));
        Assert.IsTrue(set.Overlaps(new[] { "hello" }));
        Assert.IsFalse(set.Overlaps(new[] { "other" }));
    }

    [TestMethod]
    public void ISet_CanBeUsedViaInterface()
    {
        // Verify the class satisfies ISet<T> and IReadOnlySet<T> at the type level.
        ISet<int> iset = new ConcurrentHashSet<int> { 1, 2, 3 };
        IReadOnlySet<int> iReadOnlySet = new ConcurrentHashSet<int> { 1, 2, 3 };

        Assert.IsTrue(iset.IsSubsetOf(new[] { 1, 2, 3, 4 }));
        Assert.IsTrue(iReadOnlySet.IsSupersetOf(new[] { 1, 2 }));
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
            }, TestContext.CancellationToken);
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
        }, TestContext.CancellationToken);

        Task remover = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                _ = set.Remove(i);
            }
        }, TestContext.CancellationToken);

        await Task.WhenAll(adder, remover).ConfigureAwait(false);

        Assert.AreEqual(1000, set.Count);
    }

    public TestContext TestContext { get; set; }

    private static readonly int[] s_expected = new[] { 1, 2, 3 };
}

