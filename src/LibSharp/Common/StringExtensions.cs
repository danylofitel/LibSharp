// Copyright (c) LibSharp. All rights reserved.

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
    public static string Base64Decode(this string input, Encoding encoding = null)
    {
        Argument.NotNull(input, nameof(input));

        byte[] bytes = Convert.FromBase64String(input);
        return (encoding ?? Encoding.UTF8).GetString(bytes);
    }

    /// <summary>
    /// Performs a base 64 encoding of a string.
    /// </summary>
    /// <param name="input">Input string.</param>
    /// <param name="encoding">String encoding.</param>
    /// <returns>Base 64 encoded string.</returns>
    public static string Base64Encode(this string input, Encoding encoding = null)
    {
        Argument.NotNull(input, nameof(input));

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
        Argument.NotNull(input, nameof(input));

        int[] characterIndexes = StringInfo.ParseCombiningCharacters(input);

        Array.Reverse(characterIndexes);

        IEnumerable<string> elements = characterIndexes.Select(i => StringInfo.GetNextTextElement(input, i));

        return string.Concat(elements);
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
    /// Truncates the string to the specified maximum length.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The string truncated to the maximum length.</returns>
    public static string Truncate(this string input, int maxLength)
    {
        Argument.NotNull(input, nameof(input));
        Argument.GreaterThanOrEqualTo(maxLength, 0, nameof(maxLength));

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
        Argument.NotNull(input, nameof(input));
        Argument.GreaterThanOrEqualTo(maxTextElements, 0, nameof(maxTextElements));

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
