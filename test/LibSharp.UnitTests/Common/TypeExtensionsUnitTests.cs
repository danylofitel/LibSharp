// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class TypeExtensionsUnitTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GetDefaultComparer_NonComparableType_Throws()
        {
            // Act
            _ = TypeExtensions.GetDefaultComparer<object>();
        }

        [TestMethod]
        public void GetDefaultComparer_ComparableType_ReturnsDefaultComparer()
        {
            // Act
            IComparer<int> comparer = TypeExtensions.GetDefaultComparer<int>();

            // Assert
            Assert.AreEqual(Comparer<int>.Default, comparer);
        }
    }
}
