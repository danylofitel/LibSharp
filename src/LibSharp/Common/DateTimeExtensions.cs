// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Common
{
    /// <summary>
    /// Extension methods for DateTime.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// The value of this constant is equivalent to 00:00:00.0000000 UTC, January 1, 1970, in the Gregorian calendar.
        /// UnixEpoch defines the point in time when Unix time is equal to 0.
        /// </summary>
        public static DateTime UnixEpoch { get; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Creates a DateTime from Epoch milliseconds.
        /// </summary>
        /// <param name="epochMilliseconds">Epoch milliseconds.</param>
        /// <returns>A DateTime value.</returns>
        public static DateTime FromEpochMilliseconds(this long epochMilliseconds)
        {
            return UnixEpoch.AddMilliseconds(epochMilliseconds);
        }

        /// <summary>
        /// Creates a DateTime from Epoch seconds.
        /// </summary>
        /// <param name="epochSeconds">Epoch seconds.</param>
        /// <returns>A DateTime value.</returns>
        public static DateTime FromEpochSeconds(this long epochSeconds)
        {
            return UnixEpoch.AddSeconds(epochSeconds);
        }

        /// <summary>
        /// Converts a DateTime to Epoch milliseconds.
        /// </summary>
        /// <param name="dateTime">DateTime value.</param>
        /// <returns>Epoch milliseconds.</returns>
        public static long ToEpochMilliseconds(this DateTime dateTime)
        {
            TimeSpan epochTimeSpan = dateTime.Subtract(UnixEpoch);
            return Convert.ToInt64(epochTimeSpan.TotalMilliseconds);
        }

        /// <summary>
        /// Converts a DateTime to Epoch seconds.
        /// </summary>
        /// <param name="dateTime">DateTime value.</param>
        /// <returns>Epoch seconds.</returns>
        public static long ToEpochSeconds(this DateTime dateTime)
        {
            TimeSpan epochTimeSpan = dateTime.Subtract(UnixEpoch);
            return Convert.ToInt64(epochTimeSpan.TotalSeconds);
        }
    }
}
