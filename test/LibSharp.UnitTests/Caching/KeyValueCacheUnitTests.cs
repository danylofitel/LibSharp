// Copyright (c) LibSharp. All rights reserved.

using System;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching
{
    [TestClass]
    public class KeyValueCacheUnitTests
    {
        [TestMethod]
        public void KeyValueCache_ValueNotExpired()
        {
            // Arrange
            Func<int, int> factory = Substitute.For<Func<int, int>>();
            _ = factory(Arg.Any<int>()).Returns(x => -((int)x[0]));

            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(factory, TimeSpan.FromHours(1));

            // Assert
            _ = factory.Received(0)(Arg.Any<int>());

            // Act
            int value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = factory.Received(1)(1);
            _ = factory.Received(0)(2);

            // Act
            value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = factory.Received(1)(1);
            _ = factory.Received(0)(2);

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = factory.Received(1)(1);
            _ = factory.Received(1)(2);

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = factory.Received(1)(1);
            _ = factory.Received(1)(2);
        }

        [TestMethod]
        public void KeyValueCache_ValueExpired()
        {
            // Arrange
            Func<int, int> factory = Substitute.For<Func<int, int>>();
            _ = factory(Arg.Any<int>()).Returns(x => -((int)x[0]));

            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(factory, TimeSpan.Zero);

            // Assert
            _ = factory.Received(0)(Arg.Any<int>());

            // Act
            int value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = factory.Received(1)(1);
            _ = factory.Received(0)(2);

            // Act
            value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = factory.Received(2)(1);
            _ = factory.Received(0)(2);

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = factory.Received(2)(1);
            _ = factory.Received(1)(2);

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = factory.Received(2)(1);
            _ = factory.Received(2)(2);
        }
    }
}
