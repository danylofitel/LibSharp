// Copyright (c) 2026 Danylo Fitel

using System;
using System.Net;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common;

[TestClass]
public class IntExtensionsUnitTests
{
    [TestMethod]
    public void TryConvertToEnum_HttpStatusCode()
    {
        // Arrange

        // Assert
        Assert.IsFalse(int.MinValue.TryConvertToEnum<HttpStatusCode>(out _));
        Assert.IsFalse((-200).TryConvertToEnum<HttpStatusCode>(out _));
        Assert.IsFalse((-1).TryConvertToEnum<HttpStatusCode>(out _));
        Assert.IsFalse(0.TryConvertToEnum<HttpStatusCode>(out _));
        Assert.IsFalse(1.TryConvertToEnum<HttpStatusCode>(out _));
        Assert.IsFalse(99.TryConvertToEnum<HttpStatusCode>(out _));
        Assert.IsFalse(600.TryConvertToEnum<HttpStatusCode>(out _));
        Assert.IsFalse(int.MaxValue.TryConvertToEnum<HttpStatusCode>(out _));

        Assert.IsTrue(100.TryConvertToEnum<HttpStatusCode>(out HttpStatusCode result));
        Assert.AreEqual(HttpStatusCode.Continue, result);

        Assert.IsTrue(101.TryConvertToEnum<HttpStatusCode>(out result));
        Assert.AreEqual(HttpStatusCode.SwitchingProtocols, result);

        Assert.IsTrue(200.TryConvertToEnum<HttpStatusCode>(out result));
        Assert.AreEqual(HttpStatusCode.OK, result);

        Assert.IsTrue(400.TryConvertToEnum<HttpStatusCode>(out result));
        Assert.AreEqual(HttpStatusCode.BadRequest, result);

        Assert.IsTrue(401.TryConvertToEnum<HttpStatusCode>(out result));
        Assert.AreEqual(HttpStatusCode.Unauthorized, result);

        Assert.IsTrue(403.TryConvertToEnum<HttpStatusCode>(out result));
        Assert.AreEqual(HttpStatusCode.Forbidden, result);

        Assert.IsTrue(404.TryConvertToEnum<HttpStatusCode>(out result));
        Assert.AreEqual(HttpStatusCode.NotFound, result);

        Assert.IsTrue(500.TryConvertToEnum<HttpStatusCode>(out result));
        Assert.AreEqual(HttpStatusCode.InternalServerError, result);

        Assert.IsTrue(505.TryConvertToEnum<HttpStatusCode>(out result));
        Assert.AreEqual(HttpStatusCode.HttpVersionNotSupported, result);
    }

    [TestMethod]
    public void TryConvertToEnum_StringComparison()
    {
        // Arrange

        // Assert
        Assert.IsFalse(int.MinValue.TryConvertToEnum<StringComparison>(out _));
        Assert.IsFalse((-2).TryConvertToEnum<StringComparison>(out _));
        Assert.IsFalse((-1).TryConvertToEnum<StringComparison>(out _));
        Assert.IsFalse(6.TryConvertToEnum<StringComparison>(out _));
        Assert.IsFalse(7.TryConvertToEnum<StringComparison>(out _));
        Assert.IsFalse(int.MaxValue.TryConvertToEnum<StringComparison>(out _));

        Assert.IsTrue(0.TryConvertToEnum<StringComparison>(out StringComparison result));
        Assert.AreEqual(StringComparison.CurrentCulture, result);

        Assert.IsTrue(1.TryConvertToEnum<StringComparison>(out result));
        Assert.AreEqual(StringComparison.CurrentCultureIgnoreCase, result);

        Assert.IsTrue(2.TryConvertToEnum<StringComparison>(out result));
        Assert.AreEqual(StringComparison.InvariantCulture, result);

        Assert.IsTrue(3.TryConvertToEnum<StringComparison>(out result));
        Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase, result);

        Assert.IsTrue(4.TryConvertToEnum<StringComparison>(out result));
        Assert.AreEqual(StringComparison.Ordinal, result);

        Assert.IsTrue(5.TryConvertToEnum<StringComparison>(out result));
        Assert.AreEqual(StringComparison.OrdinalIgnoreCase, result);
    }

    // -- Non-int underlying types ------------------------------------------

    private enum ByteBackedEnum : byte
    {
        First = 1,
        Second = 2,
    }

    private enum LongBackedEnum : long
    {
        Big = 5_000_000_000L,
        Small = 1L,
    }

    [TestMethod]
    public void TryConvertToEnum_ByteBackedEnum_DoesNotThrow()
    {
        // Enum.IsDefined(Type, object) rejects an int for a byte-backed enum with ArgumentException,
        // which a Try method must never surface.
        Assert.IsTrue(1.TryConvertToEnum(out ByteBackedEnum defined));
        Assert.AreEqual(ByteBackedEnum.First, defined);

        Assert.IsFalse(99.TryConvertToEnum(out ByteBackedEnum _));
    }

    [TestMethod]
    public void TryConvertToEnum_ValueOutsideTheUnderlyingTypeRange_ReturnsFalse()
    {
        // Enum.ToObject would truncate 300 to 44 for a byte-backed enum, which happens to be
        // undefined here but would be a silent wrong answer for an enum that defined 44.
        Assert.IsFalse(300.TryConvertToEnum(out ByteBackedEnum _));
        Assert.IsFalse((-1).TryConvertToEnum(out ByteBackedEnum _));
    }

    [TestMethod]
    public void TryConvertToEnum_LongBackedEnum_Converts()
    {
        Assert.IsTrue(1.TryConvertToEnum(out LongBackedEnum small));
        Assert.AreEqual(LongBackedEnum.Small, small);

        // The int cannot reach the large member, so it is simply not defined for this input.
        Assert.IsFalse(42.TryConvertToEnum(out LongBackedEnum _));
    }
}
