// Copyright (c) 2026 Danylo Fitel

using System;

namespace LibSharp.Common;

/// <summary>
/// Int extensions.
/// </summary>
public static class IntExtensions
{
    /// <summary>
    /// Converts an integer to an enum value if possible.
    /// </summary>
    /// <typeparam name="T">Enum type.</typeparam>
    /// <param name="value">Integer value.</param>
    /// <param name="result">Enum value.</param>
    /// <returns>True if the value is defined and was successfully converted, false otherwise.</returns>
    public static bool TryConvertToEnum<T>(this int value, out T result)
        where T : struct, Enum
    {
        // Enum.IsDefined(Type, object) requires the boxed value to carry the enum's exact underlying
        // type. Handing it an int for a byte- or long-backed enum throws ArgumentException instead of
        // reporting "not defined". Convert first, with arange check, because Enum.ToObject would silently
        // truncate: 300 becomes 44 for a byte.
        object? underlying = ToUnderlyingType<T>(value);

        if (underlying is not null && Enum.IsDefined(typeof(T), underlying))
        {
            // The CLR permits unboxing a value of the underlying type straight to the enum type.
            result = (T)underlying;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Boxes the value as the enum's underlying type, or returns <c>null</c> when it does not fit.
    /// </summary>
    private static object? ToUnderlyingType<T>(int value)
        where T : struct, Enum
    {
        return Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) switch
        {
            TypeCode.SByte when value is >= sbyte.MinValue and <= sbyte.MaxValue => (sbyte)value,
            TypeCode.Byte when value is >= byte.MinValue and <= byte.MaxValue => (byte)value,
            TypeCode.Int16 when value is >= short.MinValue and <= short.MaxValue => (short)value,
            TypeCode.UInt16 when value is >= ushort.MinValue and <= ushort.MaxValue => (ushort)value,
            TypeCode.Int32 => value,
            TypeCode.UInt32 when value >= 0 => (uint)value,
            TypeCode.Int64 => (long)value,
            TypeCode.UInt64 when value >= 0 => (ulong)value,
            _ => null,
        };
    }
}
