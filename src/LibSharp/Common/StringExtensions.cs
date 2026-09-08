// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibSharp.Common;

/// <summary>
/// Extension methods for string.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Performs a base 64 decoding of a string.
    /// </summary>
    /// <param name="input">Base 64 encoded string.</param>
    /// <param name="encoding">String encoding.</param>
    /// <returns>Original string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
    public static string Base64Decode(this string input, Encoding? encoding = null)
    {
        Argument.NotNull(input);

        byte[] bytes = Convert.FromBase64String(input);
        return (encoding ?? Encoding.UTF8).GetString(bytes);
    }

    /// <summary>
    /// Performs a base 64 encoding of a string.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="encoding">String encoding.</param>
    /// <returns>Base 64 encoded string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
    public static string Base64Encode(this string input, Encoding? encoding = null)
    {
        Argument.NotNull(input);

        byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(input);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Reverses the string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The reversed string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
    public static string Reverse(this string input)
    {
        Argument.NotNull(input);

        if (input.Length <= 1)
        {
            return input;
        }

        // Reversing text elements preserves the total number of chars, so the result length is
        // known up front and each element can be copied straight into its final position.
        return string.Create(input.Length, input, static (destination, source) =>
        {
            ReadOnlySpan<char> remaining = source;
            int written = 0;

            while (!remaining.IsEmpty)
            {
                int length = StringInfo.GetNextTextElementLength(remaining);
                remaining[..length].CopyTo(destination[(destination.Length - written - length)..]);
                written += length;
                remaining = remaining[length..];
            }
        });
    }

    /// <summary>
    /// Converts a string to an enum value if possible.
    /// </summary>
    /// <typeparam name="T">Enum type.</typeparam>
    /// <param name="value">String value.</param>
    /// <param name="result">Enum value.</param>
    /// <returns>True if the value is defined and was successfully converted, false otherwise.</returns>
    public static bool TryConvertToEnum<T>(this string value, out T result)
        where T : struct, Enum
    {
        if (Enum.TryParse(value, out result) && Enum.IsDefined(result))
        {
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Truncates the string to the specified maximum number of UTF-16 code units.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The string truncated to the maximum length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is outside the permitted range.</exception>
    /// <remarks>
    /// Cuts at a code unit, so it can split a surrogate pair or separate a combining mark from the
    /// character it modifies, leaving text that no longer renders correctly. Use
    /// <see cref="TruncateTextElements"/> to cut on grapheme boundaries instead.
    /// </remarks>
    public static string Truncate(this string input, int maxLength)
    {
        Argument.NotNull(input);
        Argument.GreaterThanOrEqualTo(maxLength, 0);

        if (input.Length <= maxLength)
        {
            return input;
        }

        return input[..maxLength];
    }

    /// <summary>
    /// Truncates the string to the specified maximum number of text elements.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="maxTextElements">The maximum number of text elements.</param>
    /// <returns>The string truncated to the maximum number of text elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxTextElements"/> is outside the permitted range.</exception>
    public static string TruncateTextElements(this string input, int maxTextElements)
    {
        Argument.NotNull(input);
        Argument.GreaterThanOrEqualTo(maxTextElements, 0);

        if (maxTextElements == 0)
        {
            return string.Empty;
        }

        // Walk only as far as the limit rather than indexing every element: a string within the
        // limit is recognised without scanning the remainder.
        ReadOnlySpan<char> remaining = input;
        int offset = 0;

        for (int i = 0; i < maxTextElements; ++i)
        {
            if (remaining.IsEmpty)
            {
                return input;
            }

            int length = StringInfo.GetNextTextElementLength(remaining);
            offset += length;
            remaining = remaining[length..];
        }

        return remaining.IsEmpty ? input : input[..offset];
    }
}
