// Copyright (c) 2026 Danylo Fitel

using System;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace LibSharp.UnitTests.Caching;

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

    [TestMethod]
    public void GetValue_FactoryReadsInitializer_ThrowsInsteadOfRecursing()
    {
        // Arrange
        Initializer<int> initializer = new Initializer<int>();
        int Factory()
        {
            return initializer.GetValue(Factory);
        }

        // Act & Assert — without the guard this recurses until the stack overflows, which is
        // process-fatal and cannot be caught.
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => initializer.GetValue(Factory));
    }

    [TestMethod]
    public void GetValue_FactoryThrows_InitializerStaysUsable()
    {
        // Arrange
        Initializer<int> initializer = new Initializer<int>();

        // Act — a failed attempt must not leave the re-entrancy guard latched.
        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => initializer.GetValue(static () => throw new InvalidOperationException("boom")));

        // Assert
        Assert.AreEqual(7, initializer.GetValue(static () => 7));
        Assert.IsTrue(initializer.HasValue);
    }
}
