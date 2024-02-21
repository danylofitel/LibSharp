// Copyright (c) LibSharp. All rights reserved.

using System;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class DateTimeExtensionsUnitTests
    {
        [TestMethod]
        public void FromEpochMilliseconds()
        {
            DateTime currentDate = new DateTime(2022, 2, 2, 2, 2, 2, DateTimeKind.Utc);
            long millisecondsSinceEpoch = (long)currentDate.Subtract(DateTimeExtensions.UnixEpoch).TotalMilliseconds;
            DateTime convertedDateTime = millisecondsSinceEpoch.FromEpochMilliseconds();

            Assert.AreEqual(currentDate.Date, convertedDateTime.Date);
            Assert.AreEqual((long)currentDate.TimeOfDay.TotalMilliseconds, (long)convertedDateTime.TimeOfDay.TotalMilliseconds);
            Assert.AreEqual(DateTimeKind.Utc, convertedDateTime.Kind);
        }

        [TestMethod]
        public void FromEpochSeconds()
        {
            DateTime currentDate = new DateTime(2022, 2, 2, 2, 2, 2, DateTimeKind.Utc);
            long secondsSinceEpoch = (long)currentDate.Subtract(DateTimeExtensions.UnixEpoch).TotalSeconds;
            DateTime convertedDateTime = secondsSinceEpoch.FromEpochSeconds();

            Assert.AreEqual(currentDate.Date, convertedDateTime.Date);
            Assert.AreEqual((long)currentDate.TimeOfDay.TotalSeconds, convertedDateTime.TimeOfDay.TotalSeconds);
            Assert.AreEqual(DateTimeKind.Utc, convertedDateTime.Kind);
        }

        [TestMethod]
        public void ToEpochMilliseconds()
        {
            long fromEpoch = DateTimeExtensions.UnixEpoch.ToEpochMilliseconds();
            Assert.AreEqual(0, fromEpoch);

            long fromDayAfterEpoch = new DateTime(1970, 1, 2).ToEpochMilliseconds();
            Assert.AreEqual(24 * 60 * 60 * 1000, fromDayAfterEpoch);

            DateTime currentDate = new DateTime(2022, 2, 2, 2, 2, 2, DateTimeKind.Utc);
            currentDate = currentDate.AddMilliseconds(-currentDate.Millisecond);
            long millisecondsSinceEpoch = Convert.ToInt64(currentDate.Subtract(DateTimeExtensions.UnixEpoch).TotalMilliseconds);
            Assert.AreEqual(currentDate.ToEpochMilliseconds(), millisecondsSinceEpoch);
        }

        [TestMethod]
        public void ToEpochSeconds()
        {
            long fromEpoch = DateTimeExtensions.UnixEpoch.ToEpochSeconds();
            Assert.AreEqual(0, fromEpoch);

            long fromDayAfterEpoch = DateTimeExtensions.UnixEpoch.AddDays(1).ToEpochSeconds();
            Assert.AreEqual(24 * 60 * 60, fromDayAfterEpoch);

            DateTime currentDate = new DateTime(2022, 2, 2, 2, 2, 2, DateTimeKind.Utc);
            currentDate = currentDate.AddMilliseconds(-currentDate.Millisecond);
            long secondsSinceEpoch = Convert.ToInt64(currentDate.Subtract(DateTimeExtensions.UnixEpoch).TotalSeconds);
            Assert.AreEqual(currentDate.ToEpochSeconds(), secondsSinceEpoch);
        }
    }
}
