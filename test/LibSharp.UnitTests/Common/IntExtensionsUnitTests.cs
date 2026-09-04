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

    [TestMethod]
    public void TryConvertToEnum_EveryUnderlyingType_ConvertsDefinedValues()
    {
        // The conversion must work for every underlying type an enum may declare, not just int.
        Assert.IsTrue((-5).TryConvertToEnum<SByteEnum>(out SByteEnum sb));
        Assert.AreEqual(SByteEnum.MinusFive, sb);

        Assert.IsTrue(200.TryConvertToEnum<ByteEnum>(out ByteEnum b));
        Assert.AreEqual(ByteEnum.TwoHundred, b);

        Assert.IsTrue((-30000).TryConvertToEnum<Int16Enum>(out Int16Enum s16));
        Assert.AreEqual(Int16Enum.MinusThirtyThousand, s16);

        Assert.IsTrue(60000.TryConvertToEnum<UInt16Enum>(out UInt16Enum u16));
        Assert.AreEqual(UInt16Enum.SixtyThousand, u16);

        Assert.IsTrue(7.TryConvertToEnum<Int32Enum>(out Int32Enum s32));
        Assert.AreEqual(Int32Enum.Seven, s32);

        Assert.IsTrue(9.TryConvertToEnum<UInt32Enum>(out UInt32Enum u32));
        Assert.AreEqual(UInt32Enum.Nine, u32);

        Assert.IsTrue(11.TryConvertToEnum<Int64Enum>(out Int64Enum s64));
        Assert.AreEqual(Int64Enum.Eleven, s64);

        Assert.IsTrue(13.TryConvertToEnum<UInt64Enum>(out UInt64Enum u64));
        Assert.AreEqual(UInt64Enum.Thirteen, u64);
    }

    [TestMethod]
    public void TryConvertToEnum_OutOfRangeForUnderlyingType_ReturnsFalseWithoutTruncating()
    {
        // Each value below is undefined for its enum but would land on a DEFINED member if the
        // narrowing conversion were allowed to wrap, which is the failure this guards against.
        // 300 truncates to 44 in a byte, 65536 to 0 in a ushort, and negatives wrap huge unsigned.
        Assert.IsFalse(300.TryConvertToEnum<ByteEnum>(out ByteEnum b));
        Assert.AreEqual(default, b);

        Assert.IsFalse(384.TryConvertToEnum<SByteEnum>(out SByteEnum sb));   // 384 -> 128 -> -128
        Assert.AreEqual(default, sb);

        Assert.IsFalse(65536.TryConvertToEnum<UInt16Enum>(out UInt16Enum u16));
        Assert.AreEqual(default, u16);

        Assert.IsFalse((-1).TryConvertToEnum<UInt32Enum>(out UInt32Enum u32));
        Assert.AreEqual(default, u32);

        Assert.IsFalse((-1).TryConvertToEnum<UInt64Enum>(out UInt64Enum u64));
        Assert.AreEqual(default, u64);

        Assert.IsFalse(int.MaxValue.TryConvertToEnum<Int16Enum>(out Int16Enum s16));
        Assert.AreEqual(default, s16);
    }

    [TestMethod]
    public void TryConvertToEnum_UndefinedValueInRange_ReturnsFalse()
    {
        // In range for the underlying type, but not a declared member.
        Assert.IsFalse(42.TryConvertToEnum<ByteEnum>(out ByteEnum b));
        Assert.AreEqual(default, b);

        Assert.IsFalse(12.TryConvertToEnum<Int64Enum>(out Int64Enum s64));
        Assert.AreEqual(default, s64);
    }

    private enum SByteEnum : sbyte { Zero = 0, MinusFive = -5, MinusOneTwentyEight = -128 }

    private enum ByteEnum : byte { Zero = 0, FortyFour = 44, TwoHundred = 200 }

    private enum Int16Enum : short { Zero = 0, MinusThirtyThousand = -30000 }

    private enum UInt16Enum : ushort { Zero = 0, SixtyThousand = 60000 }

    private enum Int32Enum { Zero = 0, Seven = 7 }

    private enum UInt32Enum : uint { Zero = 0, Nine = 9 }

    private enum Int64Enum : long { Zero = 0, Eleven = 11 }

    private enum UInt64Enum : ulong { Zero = 0, Thirteen = 13 }
}
