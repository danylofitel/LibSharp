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
        public void KeyValueCache_TimeToLive_ValueNotExpired()
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
        public void KeyValueCache_TimeToLive_ValueExpired()
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

        [TestMethod]
        public void KeyValueCache_ExpirationFunction_ValueNotExpired()
        {
            // Arrange
            Func<int, int> factory = Substitute.For<Func<int, int>>();
            _ = factory(Arg.Any<int>()).Returns(x => -((int)x[0]));

            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(factory, (_, _) => DateTime.UtcNow.AddHours(1));

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
        public void KeyValueCache_ExpirationFunction_ValueExpired()
        {
            // Arrange
            Func<int, int> factory = Substitute.For<Func<int, int>>();
            _ = factory(Arg.Any<int>()).Returns(x => -((int)x[0]));

            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(factory, (_, _) => DateTime.UtcNow);

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

        [TestMethod]
        public void KeyValueCache_UpdateFactory_TimeToLive_ValueNotExpired()
        {
            // Arrange
            Func<int, int> createFactory = Substitute.For<Func<int, int>>();
            _ = createFactory(Arg.Any<int>()).Returns(x => -((int)x[0]));

            Func<int, int, int> updateFactory = Substitute.For<Func<int, int, int>>();
            _ = updateFactory(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ((int)x[1]) * 10);

            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(createFactory, TimeSpan.FromHours(1));

            // Assert
            _ = createFactory.Received(0)(Arg.Any<int>());
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            int value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(0)(2);
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(0)(2);
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(1)(2);
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(1)(2);
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());
        }

        [TestMethod]
        public void KeyValueCache_UpdateFactory_TimeToLive_ValueExpired()
        {
            // Arrange
            Func<int, int> createFactory = Substitute.For<Func<int, int>>();
            _ = createFactory(Arg.Any<int>()).Returns(x => -((int)x[0]));

            Func<int, int, int> updateFactory = Substitute.For<Func<int, int, int>>();
            _ = updateFactory(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ((int)x[1]) * 10);

            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(createFactory, updateFactory, TimeSpan.Zero);

            // Assert
            _ = createFactory.Received(0)(Arg.Any<int>());
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            int value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(0)(2);
            _ = updateFactory.Received(0)(1, Arg.Any<int>());
            _ = updateFactory.Received(0)(1, Arg.Any<int>());

            // Act
            value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-10, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(0)(2);
            _ = updateFactory.Received(1)(1, Arg.Any<int>());
            _ = updateFactory.Received(0)(2, Arg.Any<int>());

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(1)(2);
            _ = updateFactory.Received(1)(1, Arg.Any<int>());
            _ = updateFactory.Received(0)(2, Arg.Any<int>());

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-20, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(1)(2);
            _ = updateFactory.Received(1)(1, Arg.Any<int>());
            _ = updateFactory.Received(1)(2, Arg.Any<int>());
        }

        [TestMethod]
        public void KeyValueCache_UpdateFactory_ExpirationFunction_ValueNotExpired()
        {
            // Arrange
            Func<int, int> createFactory = Substitute.For<Func<int, int>>();
            _ = createFactory(Arg.Any<int>()).Returns(x => -((int)x[0]));

            Func<int, int, int> updateFactory = Substitute.For<Func<int, int, int>>();
            _ = updateFactory(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ((int)x[1]) * 10);

            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(createFactory, updateFactory, (_, _) => DateTime.UtcNow.AddHours(1));

            // Assert
            _ = createFactory.Received(0)(Arg.Any<int>());
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            int value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(0)(2);
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(0)(2);
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(1)(2);
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(1)(2);
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());
        }

        [TestMethod]
        public void KeyValueCache_UpdateFactory_ExpirationFunction_ValueExpired()
        {
            // Arrange
            Func<int, int> createFactory = Substitute.For<Func<int, int>>();
            _ = createFactory(Arg.Any<int>()).Returns(x => -((int)x[0]));

            Func<int, int, int> updateFactory = Substitute.For<Func<int, int, int>>();
            _ = updateFactory(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ((int)x[1]) * 10);

            KeyValueCache<int, int> cache = new KeyValueCache<int, int>(createFactory, updateFactory, (_, _) => DateTime.UtcNow);

            // Assert
            _ = createFactory.Received(0)(Arg.Any<int>());
            _ = updateFactory.Received(0)(Arg.Any<int>(), Arg.Any<int>());

            // Act
            int value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-1, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(0)(2);
            _ = updateFactory.Received(0)(1, Arg.Any<int>());
            _ = updateFactory.Received(0)(1, Arg.Any<int>());

            // Act
            value = cache.GetValue(1);

            // Assert
            Assert.AreEqual(-10, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(0)(2);
            _ = updateFactory.Received(1)(1, Arg.Any<int>());
            _ = updateFactory.Received(0)(2, Arg.Any<int>());

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-2, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(1)(2);
            _ = updateFactory.Received(1)(1, Arg.Any<int>());
            _ = updateFactory.Received(0)(2, Arg.Any<int>());

            // Act
            value = cache.GetValue(2);

            // Assert
            Assert.AreEqual(-20, value);
            _ = createFactory.Received(1)(1);
            _ = createFactory.Received(1)(2);
            _ = updateFactory.Received(1)(1, Arg.Any<int>());
            _ = updateFactory.Received(1)(2, Arg.Any<int>());
        }
    }
}
