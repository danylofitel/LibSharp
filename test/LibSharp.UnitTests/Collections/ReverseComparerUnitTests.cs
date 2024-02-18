// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.Collections.UnitTests
{
    [TestClass]
    public class ReverseComparerUnitTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullComparer_Throws()
        {
            // Act
            _ = new ReverseComparer<int>(null);
        }

        [TestMethod]
        public void Compare_ReturnsReversedResult()
        {
            // Arrange
            IComparer<int> normal = Comparer<int>.Default;
            IComparer<int> reverse = new ReverseComparer<int>(Comparer<int>.Default);

            // Assert
            Assert.AreEqual(normal.Compare(1, 1), reverse.Compare(1, 1));
            Assert.AreEqual(normal.Compare(1, 2), reverse.Compare(2, 1));
            Assert.AreEqual(normal.Compare(2, 1), reverse.Compare(1, 2));
            Assert.AreEqual(normal.Compare(-2, -1), reverse.Compare(-1, -2));
            Assert.AreEqual(normal.Compare(-2, 1), reverse.Compare(1, -2));

            Assert.AreEqual(-normal.Compare(1, 1), reverse.Compare(1, 1));
            Assert.AreEqual(-normal.Compare(2, 1), reverse.Compare(2, 1));
            Assert.AreEqual(-normal.Compare(1, 2), reverse.Compare(1, 2));
            Assert.AreEqual(-normal.Compare(-1, -2), reverse.Compare(-1, -2));
            Assert.AreEqual(-normal.Compare(1, -2), reverse.Compare(1, -2));
        }
    }
}
