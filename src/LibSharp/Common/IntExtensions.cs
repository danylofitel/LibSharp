// Copyright (c) 2026 Danylo Fitel

using System;
using System.Runtime.CompilerServices;

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
        T converted = default;

        // Each case writes exactly sizeof(T) bytes into the enum's own storage, so the value is
        // reinterpreted rather than boxed. The range checks come first because a narrowing
        // conversion wraps silently: 300 would become 44 for a byte-backed enum.
        switch (EnumInfo<T>.s_underlyingTypeCode)
        {
            case TypeCode.SByte when value is >= sbyte.MinValue and <= sbyte.MaxValue:
                Unsafe.As<T, sbyte>(ref converted) = (sbyte)value;
                break;
            case TypeCode.Byte when value is >= byte.MinValue and <= byte.MaxValue:
                Unsafe.As<T, byte>(ref converted) = (byte)value;
                break;
            case TypeCode.Int16 when value is >= short.MinValue and <= short.MaxValue:
                Unsafe.As<T, short>(ref converted) = (short)value;
                break;
            case TypeCode.UInt16 when value is >= ushort.MinValue and <= ushort.MaxValue:
                Unsafe.As<T, ushort>(ref converted) = (ushort)value;
                break;
            case TypeCode.Int32:
                Unsafe.As<T, int>(ref converted) = value;
                break;
            case TypeCode.UInt32 when value >= 0:
                Unsafe.As<T, uint>(ref converted) = (uint)value;
                break;
            case TypeCode.Int64:
                Unsafe.As<T, long>(ref converted) = value;
                break;
            case TypeCode.UInt64 when value >= 0:
                Unsafe.As<T, ulong>(ref converted) = (ulong)value;
                break;
            default:
                result = default;
                return false;
        }

        if (Enum.IsDefined(converted))
        {
            result = converted;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Per-enum data resolved once, on first use of each closed generic type.
    /// </summary>
    private static class EnumInfo<T>
        where T : struct, Enum
    {
        public static readonly TypeCode s_underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T)));
    }
}
