// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections;

[TestClass]
public class IEnumerableExtensionsTests
{
    [TestMethod]
    public void Chunk_EmptyList_ReturnsZeroChunkes()
    {
        // Arrange
        IEnumerable<string> items = Enumerable.Empty<string>();

        // Act
        IEnumerable<IEnumerable<string>> chunks = items.Chunk(1.0, _ => 0.0);

        // Act
        Assert.IsFalse(chunks.Any());
    }

    [TestMethod]
    public void Chunk_SingleItem_Returns1ChunkWith1Item()
    {
        // Arrange
        string[] items = new[] { "value" };

        // Act
        List<List<string>> chunks = items.Chunk(100, _ => 1).ToList();

        // Act
        Assert.HasCount(1, chunks);
        Assert.HasCount(1, chunks[0]);
        Assert.AreEqual(items[0], chunks[0][0]);
    }

    [TestMethod]
    public void Chunk_MultipleItemsFittingIn1Chunk_Returns1ChunkWithAllItems()
    {
        // Arrange
        List<int> items = Enumerable.Range(0, 10).ToList();

        // Act
        List<List<int>> chunks = items.Chunk(20, _ => 1).ToList();

        // Act
        Assert.HasCount(1, chunks);
        Assert.HasCount(items.Count, chunks[0]);

        for (int i = 0; i < items.Count; ++i)
        {
            Assert.AreEqual(items[i], chunks[0][i]);
        }
    }

    [TestMethod]
    public void Chunk_MultipleItemsFilling1Chunk_Returns1ChunkWithAllItems()
    {
        // Arrange
        List<int> items = Enumerable.Range(0, 10).ToList();

        // Act
        List<List<int>> chunks = items.Chunk(10, _ => 1).ToList();

        // Act
        Assert.HasCount(1, chunks);
        Assert.HasCount(items.Count, chunks[0]);

        for (int i = 0; i < items.Count; ++i)
        {
            Assert.AreEqual(items[i], chunks[0][i]);
        }
    }

    [TestMethod]
    public void Chunk_MultipleItemsMakingMultipleFullChunkes_ReturnsMultipleFullChunkes()
    {
        // Arrange
        List<int> items = Enumerable.Range(0, 100).Select(i => i % 10).ToList();

        // Act
        List<List<int>> chunks = items.Chunk(10, _ => 1).ToList();

        // Act
        Assert.HasCount(10, chunks);

        for (int i = 0; i < 10; ++i)
        {
            Assert.HasCount(10, chunks[i]);

            for (int j = 0; j < 10; ++j)
            {
                Assert.AreEqual(j, chunks[i][j]);
            }
        }
    }

    [TestMethod]
    public void Chunk_MultipleItemsMakingMultipleChunkes_ReturnsMultipleChunkes()
    {
        // Arrange
        List<int> items = Enumerable.Range(0, 101).Select(i => i % 10).ToList();

        // Act
        List<List<int>> chunks = items.Chunk(10, _ => 1).ToList();

        // Act
        Assert.HasCount(11, chunks);

        for (int i = 0; i < 10; ++i)
        {
            Assert.HasCount(10, chunks[i]);

            for (int j = 0; j < 10; ++j)
            {
                Assert.AreEqual(j, chunks[i][j]);
            }
        }

        Assert.HasCount(1, chunks[10]);
        Assert.AreEqual(0, chunks[10][0]);
    }

    [TestMethod]
    public void Chunk_RepeatedEnumerationOfChunkItems_Succeeds()
    {
        // Arrange
        const int Iterations = 3;
        List<int> items = Enumerable.Range(0, 100).Select(i => i % 10).ToList();

        // Act
        IEnumerable<IEnumerable<int>> chunks = items.Chunk(10, _ => 1);

        // Act
        int batchCount = 0;
        foreach (IEnumerable<int> batch in chunks)
        {
            ++batchCount;

            for (int iteration = 0; iteration < Iterations; ++iteration)
            {
                int currentItem = 0;
                foreach (int item in batch)
                {
                    Assert.AreEqual(currentItem, item);
                    ++currentItem;
                }

                Assert.AreEqual(10, currentItem);
            }
        }

        Assert.AreEqual(10, batchCount);
    }

    [TestMethod]
    public void Chunk_SingleItemWeightMoreThanChunkWeight_Throws()
    {
        // Arrange
        List<double> items = new List<double> { 101.0 };

        // Act
        _ = Assert.ThrowsExactly<ArgumentException>(() => items.Chunk(100.0, x => x).ToList());
    }

    [TestMethod]
    public void FirstIndexOf_NoDuplicates()
    {
        // Arrange
        IReadOnlyList<int> list = new[] { 0, 1, 2 };

        // Assert
        Assert.AreEqual(-1, list.FirstIndexOf(item => item < 0));
        Assert.AreEqual(-1, list.FirstIndexOf(item => item == -1));
        Assert.AreEqual(-1, list.FirstIndexOf(item => item == 3));
        Assert.AreEqual(-1, list.FirstIndexOf(item => item > 2));

        Assert.AreEqual(0, list.FirstIndexOf(item => item == 0));
        Assert.AreEqual(1, list.FirstIndexOf(item => item == 1));
        Assert.AreEqual(2, list.FirstIndexOf(item => item == 2));
    }

    [TestMethod]
    public void FirstIndexOf_Duplicates()
    {
        // Arrange
        IReadOnlyList<int> list = new[] { 0, 1, 2, 1, 3, 2 };

        // Assert
        Assert.AreEqual(-1, list.FirstIndexOf(item => item < 0));
        Assert.AreEqual(-1, list.FirstIndexOf(item => item == -1));
        Assert.AreEqual(-1, list.FirstIndexOf(item => item == 4));
        Assert.AreEqual(-1, list.FirstIndexOf(item => item > 3));

        Assert.AreEqual(0, list.FirstIndexOf(item => item == 0));
        Assert.AreEqual(1, list.FirstIndexOf(item => item == 1));
        Assert.AreEqual(2, list.FirstIndexOf(item => item == 2));
        Assert.AreEqual(4, list.FirstIndexOf(item => item == 3));
    }

    [TestMethod]
    public void LastIndexOf_NoDuplicates()
    {
        // Arrange
        IReadOnlyList<int> list = new[] { 0, 1, 2 };

        // Assert
        Assert.AreEqual(-1, list.LastIndexOf(item => item < 0));
        Assert.AreEqual(-1, list.LastIndexOf(item => item == -1));
        Assert.AreEqual(-1, list.LastIndexOf(item => item == 3));
        Assert.AreEqual(-1, list.LastIndexOf(item => item > 2));

        Assert.AreEqual(0, list.LastIndexOf(item => item == 0));
        Assert.AreEqual(1, list.LastIndexOf(item => item == 1));
        Assert.AreEqual(2, list.LastIndexOf(item => item == 2));
    }

    [TestMethod]
    public void LastIndexOf_Duplicates()
    {
        // Arrange
        IReadOnlyList<int> list = new[] { 0, 1, 2, 1, 3, 2 };

        // Assert
        Assert.AreEqual(-1, list.LastIndexOf(item => item < 0));
        Assert.AreEqual(-1, list.LastIndexOf(item => item == -1));
        Assert.AreEqual(-1, list.LastIndexOf(item => item == 4));
        Assert.AreEqual(-1, list.LastIndexOf(item => item > 3));

        Assert.AreEqual(0, list.LastIndexOf(item => item == 0));
        Assert.AreEqual(3, list.LastIndexOf(item => item == 1));
        Assert.AreEqual(5, list.LastIndexOf(item => item == 2));
        Assert.AreEqual(4, list.LastIndexOf(item => item == 3));
    }

    [TestMethod]
    public void Shuffle_EmptyEnumerable()
    {
        // Act
        int[] output = IEnumerableExtensions.Shuffle(Enumerable.Empty<int>());

        // Assert
        Assert.IsEmpty(output);
    }

    [TestMethod]
    public void Shuffle_NonEmptyEnumerable()
    {
        // Arrange
        List<int> input = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // Act
        int[] output = IEnumerableExtensions.Shuffle(input);

        // Assert
        CollectionAssert.AreEquivalent(input, output);
    }

    private class Wrapper
    {
        public Wrapper(int sortNumber)
        {
            SortNumber = sortNumber;
        }

        public int SortNumber { get; }
    }
}

