// Copyright (c) LibSharp. All rights reserved.

using System;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching
{
    [TestClass]
    public class KeyValueCacheUnitTests
    {
        [TestMethod]
        public void KeyValueCache_ValueNotExpired()
        {
            // Arrange
            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(MockFactory, TimeSpan.FromHours(1));

            // Act
            int value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);

            // Act
            value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
        }

        [TestMethod]
        public void KeyValueCache_ValueExpired()
        {
            // Arrange
            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(MockFactory, TimeSpan.Zero);

            // Act
            int value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);

            // Act
            value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
        }

        private static int MockFactory(int key)
        {
            return -key;
        }
    }
}
