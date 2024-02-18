// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Generic;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class ArgumentUnitTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EqualTo_Null_ExpectedNotNull_Throws()
        {
            // Act
            Argument.EqualTo(null, "not null", "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void EqualTo_NotNull_ExpectedNull_Throws()
        {
            // Act
            Argument.EqualTo("not null", null, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void EqualTo_NotEqual_Throws()
        {
            // Act
            Argument.EqualTo("value 1", "value 2", "name");
        }

        [TestMethod]
        public void EqualTo_Equal_DoesNotThrow()
        {
            // Act
            Argument.EqualTo((string)null, null, "name");
            Argument.EqualTo((int?)null, null, "name");

            Argument.EqualTo(1, 1, "name");
            Argument.EqualTo(long.MaxValue, long.MaxValue, "name");
            Argument.EqualTo(double.Epsilon, double.Epsilon, "name");
            Argument.EqualTo('c', 'c', "name");
            Argument.EqualTo("value", "value", "name");
            Argument.EqualTo(DateTime.UnixEpoch, DateTime.UnixEpoch, "name");
            Argument.EqualTo(Guid.Empty, Guid.Empty, "name");
            Argument.EqualTo(this, this, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GreaterThan_Null_Throws()
        {
            // Act
            Argument.GreaterThan(null, "1", "name");
        }

        [TestMethod]
        public void GreaterThan_GreaterThan_DoesNotThrow()
        {
            // Act
            Argument.GreaterThan(2, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GreaterThan_Equal_Throws()
        {
            // Act
            Argument.GreaterThan(1, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GreaterThan_LessThan_Throws()
        {
            // Act
            Argument.GreaterThan(0, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GreaterThanOrEqualTo_Null_Throws()
        {
            // Act
            Argument.GreaterThanOrEqualTo(null, "1", "name");
        }

        [TestMethod]
        public void GreaterThanOrEqualTo_GreaterThan_DoesNotThrow()
        {
            // Act
            Argument.GreaterThanOrEqualTo(2, 1, "name");
        }

        [TestMethod]
        public void GreaterThanOrEqualTo_Equal_DoesNotThrow()
        {
            // Act
            Argument.GreaterThanOrEqualTo(1, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GreaterThanOrEqualTo_LessThan_Throws()
        {
            // Act
            Argument.GreaterThanOrEqualTo(0, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LessThan_Null_Throws()
        {
            // Act
            Argument.LessThan(null, "1", "name");
        }

        [TestMethod]
        public void LessThan_LessThan_DoesNotThrow()
        {
            // Act
            Argument.LessThan(0, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void LessThan_Equal_Throws()
        {
            // Act
            Argument.LessThan(1, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void LessThan_GreaterThan_Throws()
        {
            // Act
            Argument.LessThan(2, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LessThanOrEqualTo_Null_Throws()
        {
            // Act
            Argument.LessThanOrEqualTo(null, "1", "name");
        }

        [TestMethod]
        public void LessThanOrEqualTo_LessThan_DoesNotThrow()
        {
            // Act
            Argument.LessThanOrEqualTo(0, 1, "name");
        }

        [TestMethod]
        public void LessThanOrEqualTo_Equal_DoesNotThrow()
        {
            // Act
            Argument.LessThanOrEqualTo(1, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void LessThanOrEqualTo_GreaterThan_Throws()
        {
            // Act
            Argument.LessThanOrEqualTo(2, 1, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NotEqualTo_BothNull_Throws()
        {
            // Act
            Argument.NotEqualTo((string)null, null, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void NotEqualTo_Same_Throws()
        {
            // Act
            Argument.NotEqualTo("value", "value", "name");
        }

        [TestMethod]
        public void NotEqualTo_Different_ArgumentNull_DoesNotThrow()
        {
            // Act
            Argument.NotEqualTo(null, "value", "name");
            Argument.NotEqualTo("value", null, "name");

            Argument.NotEqualTo(1, 2, "name");
            Argument.NotEqualTo(long.MinValue, long.MaxValue, "name");
            Argument.NotEqualTo(double.NegativeInfinity, double.PositiveInfinity, "name");
            Argument.NotEqualTo('a', 'b', "name");
            Argument.NotEqualTo(DateTime.MinValue, DateTime.MaxValue, "name");
            Argument.NotEqualTo(Guid.Empty, Guid.NewGuid(), "name");
            Argument.NotEqualTo("value", "different value", "name");
        }

        [TestMethod]
        public void NotNull_NotNull_DoesNotThrow()
        {
            // Act
            Argument.NotNull(new object(), "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NotNull_Null_Throws()
        {
            // Act
            Argument.NotNull(null, "name");
        }

        [TestMethod]
        public void NotNullOrEmpty_NotNull_DoesNotThrow()
        {
            // Act
            Argument.NotNullOrEmpty("value", "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NotNullOrEmpty_Null_Throws()
        {
            // Act
            Argument.NotNullOrEmpty(null, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void NotNullOrEmpty_Empty_Throws()
        {
            // Act
            Argument.NotNullOrEmpty(string.Empty, "name");
        }

        [TestMethod]
        public void NotNullOrEmpty_WhiteSpace_DoesNotThrow()
        {
            // Act
            Argument.NotNullOrEmpty("   ", "name");
        }

        [TestMethod]
        public void NotNullOrWhiteSpace_NotNull_DoesNotThrow()
        {
            // Act
            Argument.NotNullOrWhiteSpace("value", "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NotNullOrWhiteSpace_Null_Throws()
        {
            // Act
            Argument.NotNullOrWhiteSpace(null, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void NotNullOrWhiteSpace_Empty_Throws()
        {
            // Act
            Argument.NotNullOrWhiteSpace(string.Empty, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void NotNullOrWhiteSpace_WhiteSpace_Throws()
        {
            // Act
            Argument.NotNullOrWhiteSpace("   ", "name");
        }

        [TestMethod]
        public void OfType_SameType_DoesNotThrow()
        {
            // Act
            Argument.OfType("hello world", typeof(string), "name");
        }

        [TestMethod]
        public void OfType_ImplementsInterface_DoesNotThrow()
        {
            // Act
            Argument.OfType(new List<string>(), typeof(IEnumerable<string>), "name");
        }

        [TestMethod]
        public void OfType_ExtendsClass_DoesNotThrow()
        {
            // Act
            Argument.OfType(new ArgumentException("name"), typeof(Exception), "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OfType_Null_Throws()
        {
            // Act
            Argument.OfType(null, typeof(string), "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OfType_NullType_Throws()
        {
            // Act
            Argument.OfType("hello world", null, "name");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OfType_DifferentType_Throws()
        {
            // Act
            Argument.OfType("hello world", typeof(DateTime), "name");
        }
    }
}
