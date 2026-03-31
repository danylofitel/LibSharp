// Copyright (c) LibSharp. All rights reserved.

using System;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common;

[TestClass]
public class OptionalUnitTests
{
    [TestMethod]
    public void ValueType_Default_HasValue_ReturnsFalse()
    {
        // Arrange
        Optional<DateTime> optional = default;

        // Assert
        Assert.IsFalse(optional.HasValue);
    }

    [TestMethod]
    public void ValueType_Default_Value_Throws()
    {
        // Arrange
        Optional<DateTime> optional = default;

        // Act
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = optional.Value);
    }

    [TestMethod]
    public void ValueType_Default_WhenCopied_RetainsState()
    {
        // Arrange
        Optional<DateTime> optional = default;

        // Act
        Optional<DateTime> copy = CopyOptional(optional);

        // Assert
        Assert.IsFalse(copy.HasValue);
    }

    [TestMethod]
    public void ValueType_FromValue()
    {
        // Arrange
        Optional<DateTime> optional = new Optional<DateTime>(DateTime.UnixEpoch);

        // Assert
        Assert.IsTrue(optional.HasValue);
        Assert.AreEqual(DateTime.UnixEpoch, optional.Value);
    }

    [TestMethod]
    public void ValueType_FromValue_WhenCopied_RetainsState()
    {
        // Arrange
        Optional<DateTime> optional = new Optional<DateTime>(DateTime.UnixEpoch);

        // Act
        Optional<DateTime> copy = CopyOptional(optional);

        // Assert
        Assert.IsTrue(copy.HasValue);
        Assert.AreEqual(optional.Value, copy.Value);
    }

    [TestMethod]
    public void ReferenceType_Default_HasValue_ReturnsFalse()
    {
        // Arrange
        Optional<string> optional = default;

        // Assert
        Assert.IsFalse(optional.HasValue);
    }

    [TestMethod]
    public void ReferenceType_Default_Value_Throws()
    {
        // Arrange
        Optional<string> optional = default;

        // Act
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = optional.Value);
    }

    [TestMethod]
    public void ReferenceType_FromNullValue_HasValue_ReturnsTrue()
    {
        // Arrange
        Optional<string> optional = new Optional<string>(null);

        // Assert
        Assert.IsTrue(optional.HasValue);
        Assert.IsNull(optional.Value);
    }

    [TestMethod]
    public void ReferenceType_Default_WhenCopied_RetainsState()
    {
        // Arrange
        Optional<string> optional = default;

        // Act
        Optional<string> copy = CopyOptional(optional);

        // Assert
        Assert.IsFalse(copy.HasValue);
    }

    [TestMethod]
    public void ReferenceType_FromValue()
    {
        // Arrange
        Optional<string> optional = new Optional<string>("boxed");

        // Assert
        Assert.IsTrue(optional.HasValue);
        Assert.AreEqual("boxed", optional.Value);
    }

    [TestMethod]
    public void ReferenceType_FromValue_WhenCopied_RetainsState()
    {
        // Arrange
        Optional<string> optional = new Optional<string>("boxed");

        // Act
        Optional<string> copy = CopyOptional(optional);

        // Assert
        Assert.IsTrue(copy.HasValue);
        Assert.AreEqual(optional.Value, copy.Value);
    }

    [TestMethod]
    public void GetValueOrDefault_NoValue_ReturnsDefault()
    {
        // Assert
        Assert.IsNull(default(Optional<string>).GetValueOrDefault());
        Assert.AreEqual(0, default(Optional<int>).GetValueOrDefault());
    }

    [TestMethod]
    public void GetValueOrDefault_HasValue_ReturnsValue()
    {
        // Assert
        Assert.AreEqual("hello", new Optional<string>("hello").GetValueOrDefault());
        Assert.AreEqual(42, new Optional<int>(42).GetValueOrDefault());
    }

    [TestMethod]
    public void GetValueOrDefault_NullValue_ReturnsNull()
    {
        // Assert
        Assert.IsNull(new Optional<string>(null).GetValueOrDefault());
    }

    [TestMethod]
    public void GetValueOrDefault_WithFallback_NoValue_ReturnsFallback()
    {
        // Assert
        Assert.AreEqual("fallback", default(Optional<string>).GetValueOrDefault("fallback"));
        Assert.AreEqual(-1, default(Optional<int>).GetValueOrDefault(-1));
    }

    [TestMethod]
    public void GetValueOrDefault_WithFallback_HasValue_ReturnsValue()
    {
        // Assert
        Assert.AreEqual("hello", new Optional<string>("hello").GetValueOrDefault("fallback"));
        Assert.AreEqual(42, new Optional<int>(42).GetValueOrDefault(-1));
    }

    [TestMethod]
    public void GetValueOrDefault_WithFallback_NullValue_ReturnsNull()
    {
        // A present null value is returned as-is; the fallback is only used when there is no value.
        Assert.IsNull(new Optional<string>(null).GetValueOrDefault("fallback"));
    }

    [TestMethod]
    public void TryGetValue_NoValue_ReturnsFalse()
    {
        // Assert
        Assert.IsFalse(default(Optional<string>).TryGetValue(out _));
        Assert.IsFalse(default(Optional<int>).TryGetValue(out _));
    }

    [TestMethod]
    public void TryGetValue_HasValue_ReturnsTrueAndSetsValue()
    {
        // Act
        bool result = new Optional<string>("hello").TryGetValue(out string value);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual("hello", value);
    }

    [TestMethod]
    public void TryGetValue_NullValue_ReturnsTrueAndSetsNull()
    {
        // Act
        bool result = new Optional<string>(null).TryGetValue(out string value);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNull(value);
    }

    [TestMethod]
    public void ToString_NoValue_ReturnsEmpty()
    {
        // Assert
        Assert.AreEqual(string.Empty, default(Optional<string>).ToString());
        Assert.AreEqual(string.Empty, default(Optional<int>).ToString());
    }

    [TestMethod]
    public void ToString_HasValue_ReturnsValueToString()
    {
        // Assert
        Assert.AreEqual("hello", new Optional<string>("hello").ToString());
        Assert.AreEqual("42", new Optional<int>(42).ToString());
    }

    [TestMethod]
    public void ToString_NullValue_ReturnsEmpty()
    {
        // Assert
        Assert.AreEqual(string.Empty, new Optional<string>(null).ToString());
    }

    [TestMethod]
    public void Equals_ValueType_NoValue()
    {
        // Arrange
        Optional<string> optionalValue = default;

        // Assert
        Assert.IsFalse(optionalValue.Equals(null));
        Assert.IsFalse(optionalValue.Equals("not boxed"));
        Assert.IsFalse(optionalValue.Equals("boxed"));
    }

    [TestMethod]
    public void Equals_ValueType_HasValue()
    {
        // Arrange
        Optional<string> optionalValue = new Optional<string>("boxed");

        // Assert
        Assert.IsFalse(optionalValue.Equals(null));
        Assert.IsFalse(optionalValue.Equals("not boxed"));

        Assert.IsTrue(optionalValue.Equals("boxed"));
    }

    [TestMethod]
    public void Equals_ValueType_NullValue()
    {
        // Arrange
        Optional<string> optionalValue = new Optional<string>(null);

        // Assert
        Assert.IsTrue(optionalValue.Equals(null));
        Assert.IsFalse(optionalValue.Equals("boxed"));
    }

    [TestMethod]
    public void Equals_OptionalType_NoValue()
    {
        // Arrange
        Optional<string> optional = default;

        // Assert
        Assert.IsFalse(optional.Equals(new Optional<string>(string.Empty)));
        Assert.IsFalse(optional.Equals(new Optional<string>("not boxed")));
        Assert.IsFalse(optional.Equals(new Optional<string>(null)));

        Assert.IsTrue(optional.Equals(optional));
        Assert.IsTrue(optional.Equals(default(Optional<string>)));
    }

    [TestMethod]
    public void Equals_OptionalType_HasValue()
    {
        // Arrange
        Optional<string> optional = new Optional<string>("boxed");

        // Assert
        Assert.IsFalse(optional.Equals(new Optional<string>(string.Empty)));
        Assert.IsFalse(optional.Equals(new Optional<string>("not boxed")));
        Assert.IsFalse(optional.Equals(new Optional<string>(null)));

        Assert.IsTrue(optional.Equals(optional));
        Assert.IsTrue(optional.Equals(new Optional<string>("boxed")));
    }

    [TestMethod]
    public void Equals_OptionalType_NullValue()
    {
        // Arrange
        Optional<string> optional = new Optional<string>(null);

        // Assert
        Assert.IsFalse(optional.Equals(default(Optional<string>)));
        Assert.IsFalse(optional.Equals(new Optional<string>("boxed")));

        Assert.IsTrue(optional.Equals(optional));
        Assert.IsTrue(optional.Equals(new Optional<string>(null)));
    }

    [TestMethod]
    public void Equals_Object_NoValue()
    {
        // Arrange
        Optional<string> optional = default;

        // Assert
        Assert.IsFalse(optional.Equals((object)null));
        Assert.IsFalse(optional.Equals(1));
        Assert.IsFalse(optional.Equals(DateTime.UnixEpoch));
        Assert.IsFalse(optional.Equals(new Optional<object>(new object())));
        Assert.IsFalse(optional.Equals(new Optional<object>("boxed")));
        Assert.IsFalse(optional.Equals((object)"boxed"));
        Assert.IsFalse(optional.Equals((object)new Optional<string>("boxed")));

        Assert.IsTrue(optional.Equals((object)optional));
        Assert.IsTrue(optional.Equals((object)default(Optional<string>)));
    }

    [TestMethod]
    public void Equals_Object_HasValue()
    {
        // Arrange
        Optional<string> optional = new Optional<string>("boxed");

        // Assert
        Assert.IsFalse(optional.Equals((object)null));
        Assert.IsFalse(optional.Equals(1));
        Assert.IsFalse(optional.Equals(DateTime.UnixEpoch));
        Assert.IsFalse(optional.Equals(new Optional<object>(new object())));
        Assert.IsFalse(optional.Equals(new Optional<object>("boxed")));

        Assert.IsTrue(optional.Equals((object)optional));
        Assert.IsTrue(optional.Equals((object)"boxed"));
        Assert.IsTrue(optional.Equals((object)new Optional<string>("boxed")));
    }

    [TestMethod]
    public void Equals_Object_NullValue()
    {
        // Arrange
        Optional<string> optional = new Optional<string>(null);

        // Assert
        Assert.IsTrue(optional.Equals((object)null));
        Assert.IsTrue(optional.Equals((object)new Optional<string>(null)));

        Assert.IsFalse(optional.Equals((object)default(Optional<string>)));
        Assert.IsFalse(optional.Equals((object)"boxed"));
        Assert.IsFalse(optional.Equals((object)new Optional<string>("boxed")));
    }

    [TestMethod]
    public void GetHashCode_NoValue_ReturnsZero()
    {
        // Assert
        Assert.AreEqual(0, default(Optional<int>).GetHashCode());
        Assert.AreEqual(0, default(Optional<int?>).GetHashCode());
        Assert.AreEqual(0, default(Optional<string>).GetHashCode());
        Assert.AreEqual(0, default(Optional<Optional<string>>).GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_NullValue_ReturnsZero()
    {
        // Assert
        Assert.AreEqual(0, new Optional<string>(null).GetHashCode());
        Assert.AreEqual(0, new Optional<object>(null).GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_HasValue_ReturnsValueHashCode()
    {
        // Assert
        Assert.AreEqual(0.GetHashCode(), new Optional<int>(0).GetHashCode());
        Assert.AreEqual(1.GetHashCode(), new Optional<int>(1).GetHashCode());
        Assert.AreEqual(12321.GetHashCode(), new Optional<int>(12321).GetHashCode());
    }

    [TestMethod]
    public void OperatorEquals()
    {
        // Arrange
        object testObject = new object();

        // Assert
        Assert.IsTrue(default(Optional<object>) == default);
        Assert.IsTrue(default(Optional<int>) == default);
        Assert.IsTrue(default(Optional<string>) == default);

        Assert.IsFalse(new Optional<object>(new object()) == default);
        Assert.IsFalse(new Optional<int>(5) == default);
        Assert.IsFalse(new Optional<string>("value") == default);
        Assert.IsFalse(new Optional<string>(null) == default);

        Assert.IsTrue(new Optional<object>(testObject) == new Optional<object>(testObject));
        Assert.IsTrue(new Optional<int>(5) == new Optional<int>(5));
        Assert.IsTrue(new Optional<string>("value") == new Optional<string>("value"));
        Assert.IsTrue(new Optional<string>(null) == new Optional<string>(null));

        Assert.IsFalse(new Optional<object>(new object()) == new Optional<object>(new object()));
        Assert.IsFalse(new Optional<int>(5) == new Optional<int>(7));
        Assert.IsFalse(new Optional<string>("value") == new Optional<string>("other"));
        Assert.IsFalse(new Optional<string>(null) == new Optional<string>("value"));
    }

    [TestMethod]
    public void OperatorNotEquals()
    {
        // Arrange
        object testObject = new object();

        // Assert
        Assert.IsFalse(default(Optional<object>) != default);
        Assert.IsFalse(default(Optional<int>) != default);
        Assert.IsFalse(default(Optional<string>) != default);

        Assert.IsTrue(new Optional<object>(new object()) != default);
        Assert.IsTrue(new Optional<int>(5) != default);
        Assert.IsTrue(new Optional<string>("value") != default);
        Assert.IsTrue(new Optional<string>(null) != default);

        Assert.IsFalse(new Optional<object>(testObject) != new Optional<object>(testObject));
        Assert.IsFalse(new Optional<int>(5) != new Optional<int>(5));
        Assert.IsFalse(new Optional<string>("value") != new Optional<string>("value"));
        Assert.IsFalse(new Optional<string>(null) != new Optional<string>(null));

        Assert.IsTrue(new Optional<object>(new object()) != new Optional<object>(new object()));
        Assert.IsTrue(new Optional<int>(5) != new Optional<int>(7));
        Assert.IsTrue(new Optional<string>("value") != new Optional<string>("other"));
        Assert.IsTrue(new Optional<string>(null) != new Optional<string>("value"));
    }

    private static Optional<T> CopyOptional<T>(Optional<T> original)
    {
        return original;
    }
}
