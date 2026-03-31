// Copyright (c) LibSharp. All rights reserved.

using System.Text.RegularExpressions;

namespace LibSharp.Common
{
    /// <summary>
    /// Extension methods for Regex.
    /// </summary>
    public static class RegexExtensions
    {
        /// <summary>
        /// Indicates whether the regex finds a match in a specified input string.
        /// Does not throw if the regex times out.
        /// </summary>
        /// <param name="regex">The regex to run.</param>
        /// <param name="value">The value to match against.</param>
        /// <param name="timedOut">Set to <c>true</c> if the regex timed out, <c>false</c> otherwise.</param>
        /// <returns>True if a match was found, false if not or if the regex timed out.</returns>
        public static bool TryIsMatch(this Regex regex, string value, out bool timedOut)
        {
            Argument.NotNull(regex, nameof(regex));
            Argument.NotNull(value, nameof(value));

            try
            {
                timedOut = false;
                return regex.IsMatch(value);
            }
            catch (RegexMatchTimeoutException)
            {
                timedOut = true;
                return false;
            }
        }

        /// <summary>
        /// Searches the specified input string for the first occurrence of the regex.
        /// Does not throw if the regex times out.
        /// </summary>
        /// <param name="regex">The regex to run.</param>
        /// <param name="value">The value to match against.</param>
        /// <param name="timedOut">Set to <c>true</c> if the regex timed out, <c>false</c> otherwise.</param>
        /// <returns>A match if it was found, an empty match if not or if the regex timed out.</returns>
        public static Match TryMatch(this Regex regex, string value, out bool timedOut)
        {
            Argument.NotNull(regex, nameof(regex));
            Argument.NotNull(value, nameof(value));

            try
            {
                timedOut = false;
                return regex.Match(value);
            }
            catch (RegexMatchTimeoutException)
            {
                timedOut = true;
                return Match.Empty;
            }
        }

        /// <summary>
        /// In a specified input string, replaces all strings that match a regular expression pattern
        /// with a specified replacement string.
        /// Does not throw if the regex times out.
        /// </summary>
        /// <param name="regex">The regex.</param>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="replacement">The replacement string.</param>
        /// <param name="timedOut">Set to <c>true</c> if the regex timed out, <c>false</c> otherwise.</param>
        /// <returns>A string with all occurrences replaced. Returns the original string if the regex times out.</returns>
        public static string TryReplace(this Regex regex, string input, string replacement, out bool timedOut)
        {
            Argument.NotNull(regex, nameof(regex));
            Argument.NotNull(input, nameof(input));
            Argument.NotNull(replacement, nameof(replacement));

            try
            {
                timedOut = false;
                return regex.Replace(input, replacement);
            }
            catch (RegexMatchTimeoutException)
            {
                timedOut = true;
                return input;
            }
        }

        /// <summary>
        /// In a specified input string, replaces all strings that match a regular expression pattern
        /// with a string returned by a match evaluator delegate.
        /// Does not throw if the regex times out.
        /// </summary>
        /// <param name="regex">The regex.</param>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="evaluator">A custom method that examines each match and returns either the original matched string or a replacement string.</param>
        /// <param name="timedOut">Set to <c>true</c> if the regex timed out, <c>false</c> otherwise.</param>
        /// <returns>A string with all matched substrings replaced. Returns the original string if the regex times out.</returns>
        public static string TryReplace(this Regex regex, string input, MatchEvaluator evaluator, out bool timedOut)
        {
            Argument.NotNull(regex, nameof(regex));
            Argument.NotNull(input, nameof(input));
            Argument.NotNull(evaluator, nameof(evaluator));

            try
            {
                timedOut = false;
                return regex.Replace(input, evaluator);
            }
            catch (RegexMatchTimeoutException)
            {
                timedOut = true;
                return input;
            }
        }
    }
}
