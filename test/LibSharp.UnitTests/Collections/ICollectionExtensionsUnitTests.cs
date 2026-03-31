// Copyright (c) LibSharp. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections;

[TestClass]
public class ICollectionExtensionsUnitTests
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
}

