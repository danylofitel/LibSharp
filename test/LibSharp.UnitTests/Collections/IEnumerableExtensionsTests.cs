// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.Collections.UnitTests
{
    [TestClass]
    public class IEnumerableExtensionsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Batch_ZeroBatchSize_Throws()
        {
            // Act
            _ = Enumerable.Empty<int>().Batch(0).ToList();
        }

        [TestMethod]
        public void Batch_EmptyListWithBatchSize1_ReturnsZeroBatches()
        {
            // Arrange
            IEnumerable<string> items = Enumerable.Empty<string>();

            // Act
            IEnumerable<IEnumerable<string>> batches = items.Batch(1);

            // Act
            Assert.IsFalse(batches.Any());
        }

        [TestMethod]
        public void Batch_EmptyListWithBatchSizeGreaterThan1_ReturnsZeroBatches()
        {
            // Arrange
            IEnumerable<string> items = Enumerable.Empty<string>();

            // Act
            IEnumerable<IEnumerable<string>> batches = items.Batch(5);

            // Act
            Assert.IsFalse(batches.Any());
        }

        [TestMethod]
        public void Batch_SingleItemWithBatchSize1_Returns1BatchWith1Item()
        {
            // Arrange
            string[] items = new[] { "unique personality" };

            // Act
            List<List<string>> batches = items
                .Batch(1)
                .Select(batch => batch.ToList())
                .ToList();

            // Act
            Assert.AreEqual(1, batches.Count);
            Assert.AreEqual(1, batches[0].Count);
            Assert.AreEqual(items[0], batches[0][0]);
        }

        [TestMethod]
        public void Batch_SingleItemWithBatchSizeGreaterThan1_Returns1BatchWith1Item()
        {
            // Arrange
            string[] items = new[] { "unique personality" };

            // Act
            List<List<string>> batches = items
                .Batch(10)
                .Select(batch => batch.ToList())
                .ToList();

            // Act
            Assert.AreEqual(1, batches.Count);
            Assert.AreEqual(1, batches[0].Count);
            Assert.AreEqual(items[0], batches[0][0]);
        }

        [TestMethod]
        public void Batch_MultipleItemsFittingIn1Batch_Returns1BatchWithAllItems()
        {
            // Arrange
            List<int> items = Enumerable.Range(0, 10).ToList();

            // Act
            List<List<int>> batches = items
                .Batch(20)
                .Select(batch => batch.ToList())
                .ToList();

            // Act
            Assert.AreEqual(1, batches.Count);
            Assert.AreEqual(items.Count, batches[0].Count);

            for (int i = 0; i < items.Count; ++i)
            {
                Assert.AreEqual(items[i], batches[0][i]);
            }
        }

        [TestMethod]
        public void Batch_MultipleItemsFilling1Batch_Returns1BatchWithAllItems()
        {
            // Arrange
            List<int> items = Enumerable.Range(0, 10).ToList();

            // Act
            List<List<int>> batches = items
                .Batch(10)
                .Select(batch => batch.ToList())
                .ToList();

            // Act
            Assert.AreEqual(1, batches.Count);
            Assert.AreEqual(items.Count, batches[0].Count);

            for (int i = 0; i < items.Count; ++i)
            {
                Assert.AreEqual(items[i], batches[0][i]);
            }
        }

        [TestMethod]
        public void Batch_MultipleItemsMakingMultipleFullBatches_ReturnsMultipleFullBatches()
        {
            // Arrange
            List<int> items = Enumerable.Range(0, 100).Select(i => i % 10).ToList();

            // Act
            List<List<int>> batches = items
                .Batch(10)
                .Select(batch => batch.ToList())
                .ToList();

            // Act
            Assert.AreEqual(10, batches.Count);

            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(10, batches[i].Count);

                for (int j = 0; j < 10; ++j)
                {
                    Assert.AreEqual(j, batches[i][j]);
                }
            }
        }

        [TestMethod]
        public void Batch_MultipleItemsMakingMultipleBatches_ReturnsMultipleBatches()
        {
            // Arrange
            List<int> items = Enumerable.Range(0, 101).Select(i => i % 10).ToList();

            // Act
            List<List<int>> batches = items
                .Batch(10)
                .Select(batch => batch.ToList())
                .ToList();

            // Act
            Assert.AreEqual(11, batches.Count);

            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(10, batches[i].Count);

                for (int j = 0; j < 10; ++j)
                {
                    Assert.AreEqual(j, batches[i][j]);
                }
            }

            Assert.AreEqual(1, batches[10].Count);
            Assert.AreEqual(0, batches[10][0]);
        }

        [TestMethod]
        public void Batch_RepeatedEnumerationOfBatchItems_Succeeds()
        {
            // Arrange
            const int Iterations = 3;
            List<int> items = Enumerable.Range(0, 100).Select(i => i % 10).ToList();

            // Act
            IEnumerable<IEnumerable<int>> batches = items.Batch(10);

            // Act
            int batchCount = 0;
            foreach (IEnumerable<int> batch in batches)
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
        [ExpectedException(typeof(ArgumentException))]
        public void Batch_SingleItemWeightMoreThanBatchWeight_Throws()
        {
            // Arrange
            List<double> items = new List<double> { 101.0 };

            // Act
            _ = items.Batch(10, 100.0, x => x).ToList();
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
            List<int> output = Enumerable.Empty<int>().Shuffle().ToList();

            // Assert
            Assert.AreEqual(0, output.Count);
        }

        [TestMethod]
        public void Shuffle_NonEmptyEnumerable()
        {
            // Arrange
            List<int> input = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            // Act
            List<int> output = input.Shuffle().ToList();

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
}
