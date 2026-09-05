// Copyright (c) 2026 Danylo Fitel

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections;

[TestClass]
public class CollectionExtensionsUnitTests
{
    [TestMethod]
    public void AddRange_EmptyInitialCollection()
    {
        // Arrange
        ICollection<int> collection = new List<int>();
        IReadOnlyList<int> values = Enumerable.Range(0, 10).ToList();

        // Act
        collection.AddRange(values);

        // Assert
        Assert.HasCount(values.Count, collection);
        foreach (int value in values)
        {
            Assert.Contains(value, collection);
        }
    }

    [TestMethod]
    public void AddRange_NonEmptyInitialCollection()
    {
        // Arrange
        ICollection<int> collection = Enumerable.Range(0, 5).ToList();
        IReadOnlyList<int> values = Enumerable.Range(5, 5).ToList();

        // Act
        collection.AddRange(values);

        // Assert
        Assert.HasCount(10, collection);
        foreach (int value in Enumerable.Range(0, 10))
        {
            Assert.Contains(value, collection);
        }
    }

    [TestMethod]
    public void AddRange_NonListCollection_UsesFallbackPath()
    {
        // A List<T> target takes the List.AddRange fast path, so this is what covers the general
        // ICollection<T> loop that every other target uses.
        ICollection<int> collection = new HashSet<int> { 1 };

        collection.AddRange(new[] { 2, 3, 2 });

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, collection.ToList());
    }

    [TestMethod]
    public void AddRange_ListTarget_MatchesFallbackBehaviour()
    {
        // The fast path must be a pure optimisation: same elements, same order.
        List<int> viaFastPath = new List<int> { 1 };
        ICollection<int> viaLoop = new Collection<int> { 1 };

        viaFastPath.AddRange(new[] { 2, 3 });
        viaLoop.AddRange(new[] { 2, 3 });

        CollectionAssert.AreEqual(viaFastPath, viaLoop.ToList());
    }
}

