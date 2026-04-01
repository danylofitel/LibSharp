// Copyright (c) LibSharp. All rights reserved.

using System;

namespace LibSharp.Common;

/// <summary>
/// Extension methods for DateTime.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Creates a UTC DateTime from Epoch milliseconds.
    /// </summary>
    /// <param name="epochMilliseconds">Epoch milliseconds.</param>
    /// <returns>A UTC DateTime value.</returns>
    public static DateTime FromEpochMilliseconds(this long epochMilliseconds)
    {
        return DateTime.UnixEpoch.AddTicks(checked(epochMilliseconds * TimeSpan.TicksPerMillisecond));
    }

    /// <summary>
    /// Creates a UTC DateTime from Epoch seconds.
    /// </summary>
    /// <param name="epochSeconds">Epoch seconds.</param>
    /// <returns>A UTC DateTime value.</returns>
    public static DateTime FromEpochSeconds(this long epochSeconds)
    {
        return DateTime.UnixEpoch.AddTicks(checked(epochSeconds * TimeSpan.TicksPerSecond));
    }

    /// <summary>
    /// Converts a UTC DateTime to Epoch milliseconds.
    /// </summary>
    /// <param name="dateTime">DateTime value. Must be UTC.</param>
    /// <returns>Epoch milliseconds.</returns>
    /// <exception cref="ArgumentException">Thrown when dateTime.Kind is not UTC.</exception>
    public static long ToEpochMilliseconds(this DateTime dateTime)
    {
        Argument.EqualTo(dateTime.Kind, DateTimeKind.Utc, nameof(dateTime));

        long epochTicks = dateTime.Ticks - DateTime.UnixEpoch.Ticks;
        return DivideRoundDown(epochTicks, TimeSpan.TicksPerMillisecond);
    }

    /// <summary>
    /// Converts a UTC DateTime to Epoch seconds.
    /// </summary>
    /// <param name="dateTime">DateTime value. Must be UTC.</param>
    /// <returns>Epoch seconds.</returns>
    /// <exception cref="ArgumentException">Thrown when dateTime.Kind is not UTC.</exception>
    public static long ToEpochSeconds(this DateTime dateTime)
    {
        Argument.EqualTo(dateTime.Kind, DateTimeKind.Utc, nameof(dateTime));

        long epochTicks = dateTime.Ticks - DateTime.UnixEpoch.Ticks;
        return DivideRoundDown(epochTicks, TimeSpan.TicksPerSecond);
    }

    private static long DivideRoundDown(long dividend, long divisor)
    {
        long quotient = dividend / divisor;
        long remainder = dividend % divisor;

        if (remainder < 0)
        {
            --quotient;
        }

        return quotient;
    }
}
