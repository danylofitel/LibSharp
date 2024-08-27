// Copyright (c) LibSharp. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections
{
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
            Assert.AreEqual(values.Count, collection.Count);
            foreach (int value in values)
            {
                Assert.IsTrue(collection.Contains(value));
            }
        }

        [TestMethod]
        public void AddRange_NonEmptyInitialCollection()
        {
            // Arrange
            ICollection<int> collection = new List<int>(Enumerable.Range(0, 5));
            IReadOnlyList<int> values = Enumerable.Range(5, 5).ToList();

            // Act
            collection.AddRange(values);

            // Assert
            Assert.AreEqual(10, collection.Count);
            foreach (int value in Enumerable.Range(0, 10))
            {
                Assert.IsTrue(collection.Contains(value));
            }
        }
    }
}
