// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Text;

namespace LibSharp.Common
{
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

            char[] characters = input.ToCharArray();
            Array.Reverse(characters);
            return new string(characters);
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

            return input.Substring(0, maxLength);
        }
    }
}
