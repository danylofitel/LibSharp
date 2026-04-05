// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common;

[TestClass]
public class TypeExtensionsUnitTests
{
    private sealed class NonGenericComparable : IComparable
    {
        public int Value { get; init; }

        public int CompareTo(object obj)
        {
            return Value.CompareTo(((NonGenericComparable)obj).Value);
        }
    }

    [TestMethod]
    public void GetDefaultComparer_NonComparableType_Throws()
    {
        // Act
        _ = Assert.ThrowsExactly<ArgumentException>(() => TypeExtensions.GetDefaultComparer<object>());
    }

    [TestMethod]
    public void GetDefaultComparer_ComparableType_ReturnsDefaultComparer()
    {
        // Act
        IComparer<int> comparer = TypeExtensions.GetDefaultComparer<int>();

        // Assert
        Assert.AreEqual(Comparer<int>.Default, comparer);
    }

    [TestMethod]
    public void GetDefaultComparer_NonGenericComparableType_ReturnsDefaultComparer()
    {
        // Act
        IComparer<NonGenericComparable> comparer = TypeExtensions.GetDefaultComparer<NonGenericComparable>();

        // Assert
        Assert.AreEqual(Comparer<NonGenericComparable>.Default, comparer);
        Assert.IsTrue(comparer.Compare(new NonGenericComparable { Value = 1 }, new NonGenericComparable { Value = 2 }) < 0);
    }
}
