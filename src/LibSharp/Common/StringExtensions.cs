// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public static string Reverse(this string input)
    {
        Argument.NotNull(input);

        int[] characterIndexes = StringInfo.ParseCombiningCharacters(input);

        StringBuilder builder = new StringBuilder(input.Length);
        for (int i = characterIndexes.Length - 1; i >= 0; --i)
        {
            int start = characterIndexes[i];
            int end = i + 1 < characterIndexes.Length ? characterIndexes[i + 1] : input.Length;
            _ = builder.Append(input, start, end - start);
        }

        return builder.ToString();
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
    public static string TruncateTextElements(this string input, int maxTextElements)
    {
        Argument.NotNull(input);
        Argument.GreaterThanOrEqualTo(maxTextElements, 0);

        if (maxTextElements == 0)
        {
            return string.Empty;
        }

        int[] characterIndexes = StringInfo.ParseCombiningCharacters(input);
        if (characterIndexes.Length <= maxTextElements)
        {
            return input;
        }

        return input[..characterIndexes[maxTextElements]];
    }
}
