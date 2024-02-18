// Copyright (c) LibSharp. All rights reserved.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.Caching.UnitTests
{
    [TestClass]
    public class InitializerUnitTests
    {
        [TestMethod]
        public void ValueTypeTest()
        {
            // Arrange
            int value = 123;
            Func<int> factory = Substitute.For<Func<int>>();

            _ = factory().Returns(value);

            Initializer<int> initializer = new Initializer<int>();

            // Assert
            Assert.IsFalse(initializer.HasValue);

            Assert.AreEqual(value, initializer.GetValue(factory));
            Assert.IsTrue(initializer.HasValue);
            _ = factory.Received(1)();

            Assert.AreEqual(value, initializer.GetValue(factory));
            Assert.IsTrue(initializer.HasValue);
            _ = factory.Received(1)();
        }

        [TestMethod]
        public void ReferenceTypeTest()
        {
            // Arrange
            string value = "value";
            Func<string> factory = Substitute.For<Func<string>>();

            _ = factory().Returns(value);

            Initializer<string> initializer = new Initializer<string>();

            // Assert
            Assert.IsFalse(initializer.HasValue);

            Assert.AreEqual(value, initializer.GetValue(factory));
            Assert.IsTrue(initializer.HasValue);
            _ = factory.Received(1)();

            Assert.AreEqual(value, initializer.GetValue(factory));
            Assert.IsTrue(initializer.HasValue);
            _ = factory.Received(1)();
        }
    }
}
