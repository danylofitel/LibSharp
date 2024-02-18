// Copyright (c) LibSharp. All rights reserved.

using System;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class BoxUnitTests
    {
        [TestMethod]
        public void ValueType_Default_HasValue_ReturnsFalse()
        {
            // Arrange
            Box<DateTime> box = default;

            // Assert
            Assert.IsFalse(box.HasValue);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ValueType_Default_Value_Throws()
        {
            // Arrange
            Box<DateTime> box = default;

            // Act
            _ = box.Value;
        }

        [TestMethod]
        public void ValueType_FromValue()
        {
            // Arrange
            Box<DateTime> box = new Box<DateTime>(DateTime.UnixEpoch);

            // Assert
            Assert.IsTrue(box.HasValue);
            Assert.AreEqual(DateTime.UnixEpoch, box.Value);
        }

        [TestMethod]
        public void ReferenceType_Default_HasValue_ReturnsFalse()
        {
            // Arrange
            Box<string> box = default;

            // Assert
            Assert.IsFalse(box.HasValue);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReferenceType_Default_Value_Throws()
        {
            // Arrange
            Box<string> box = default;

            // Act
            _ = box.Value;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ReferenceType_FromNullValue_Throws()
        {
            // Act
            _ = new Box<string>(null);
        }

        [TestMethod]
        public void ReferenceType_FromValue()
        {
            // Arrange
            Box<string> box = new Box<string>("boxed");

            // Assert
            Assert.IsTrue(box.HasValue);
            Assert.AreEqual("boxed", box.Value);
        }

        [TestMethod]
        public void Equals_ValueType_NoValue()
        {
            // Arrange
            Box<string> boxedValue = default;

            // Assert
            Assert.IsTrue(boxedValue.Equals(null));

            Assert.IsFalse(boxedValue.Equals("not boxed"));
            Assert.IsFalse(boxedValue.Equals("boxed"));
        }

        [TestMethod]
        public void Equals_ValueType_HasValue()
        {
            // Arrange
            Box<string> boxedValue = new Box<string>("boxed");

            // Assert
            Assert.IsFalse(boxedValue.Equals(null));
            Assert.IsFalse(boxedValue.Equals("not boxed"));

            Assert.IsTrue(boxedValue.Equals("boxed"));
        }

        [TestMethod]
        public void Equals_BoxType_NoValue()
        {
            // Arrange
            Box<string> box = default;

            // Assert
            Assert.IsFalse(box.Equals(new Box<string>(string.Empty)));
            Assert.IsFalse(box.Equals(new Box<string>("not boxed")));

            Assert.IsTrue(box.Equals(box));
            Assert.IsTrue(box.Equals(default(Box<string>)));
        }

        [TestMethod]
        public void Equals_BoxType_HasValue()
        {
            // Arrange
            Box<string> box = new Box<string>("boxed");

            // Assert
            Assert.IsFalse(box.Equals(new Box<string>(string.Empty)));
            Assert.IsFalse(box.Equals(new Box<string>("not boxed")));

            Assert.IsTrue(box.Equals(box));
            Assert.IsTrue(box.Equals(new Box<string>("boxed")));
        }

        [TestMethod]
        public void Equals_Object_NoValue()
        {
            // Arrange
            Box<string> box = default;

            // Assert
            Assert.IsFalse(box.Equals((object)null));
            Assert.IsFalse(box.Equals(1));
            Assert.IsFalse(box.Equals(DateTime.UnixEpoch));
            Assert.IsFalse(box.Equals(new Box<object>(new object())));
            Assert.IsFalse(box.Equals(new Box<object>("boxed")));
            Assert.IsFalse(box.Equals((object)"boxed"));
            Assert.IsFalse(box.Equals((object)new Box<string>("boxed")));

            Assert.IsTrue(box.Equals((object)box));
            Assert.IsTrue(box.Equals((object)default(Box<string>)));
        }

        [TestMethod]
        public void Equals_Object_HasValue()
        {
            // Arrange
            Box<string> box = new Box<string>("boxed");

            // Assert
            Assert.IsFalse(box.Equals((object)null));
            Assert.IsFalse(box.Equals(1));
            Assert.IsFalse(box.Equals(DateTime.UnixEpoch));
            Assert.IsFalse(box.Equals(new Box<object>(new object())));
            Assert.IsFalse(box.Equals(new Box<object>("boxed")));

            Assert.IsTrue(box.Equals((object)box));
            Assert.IsTrue(box.Equals((object)"boxed"));
            Assert.IsTrue(box.Equals((object)new Box<string>("boxed")));
        }

        [TestMethod]
        public void GetHashCode_NoValue_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0, default(Box<int>).GetHashCode());
            Assert.AreEqual(0, default(Box<int?>).GetHashCode());
            Assert.AreEqual(0, default(Box<string>).GetHashCode());
            Assert.AreEqual(0, default(Box<Box<string>>).GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_HasValue_ReturnsValueHashCode()
        {
            // Assert
            Assert.AreEqual(0.GetHashCode(), new Box<int>(0).GetHashCode());
            Assert.AreEqual(1.GetHashCode(), new Box<int>(1).GetHashCode());
            Assert.AreEqual(12321.GetHashCode(), new Box<int>(12321).GetHashCode());
        }

        [TestMethod]
        public void OperatorEquals()
        {
            // Arrange
            object testObject = new object();

            // Assert
            Assert.IsTrue(default(Box<object>) == default);
            Assert.IsTrue(default(Box<int>) == default);
            Assert.IsTrue(default(Box<string>) == default);

            Assert.IsFalse(new Box<object>(new object()) == default);
            Assert.IsFalse(new Box<int>(5) == default);
            Assert.IsFalse(new Box<string>("value") == default);

            Assert.IsTrue(new Box<object>(testObject) == new Box<object>(testObject));
            Assert.IsTrue(new Box<int>(5) == new Box<int>(5));
            Assert.IsTrue(new Box<string>("value") == new Box<string>("value"));

            Assert.IsFalse(new Box<object>(new object()) == new Box<object>(new object()));
            Assert.IsFalse(new Box<int>(5) == new Box<int>(7));
            Assert.IsFalse(new Box<string>("value") == new Box<string>("other"));
        }

        [TestMethod]
        public void OperatorNotEquals()
        {
            // Arrange
            object testObject = new object();

            // Assert
            Assert.IsFalse(default(Box<object>) != default);
            Assert.IsFalse(default(Box<int>) != default);
            Assert.IsFalse(default(Box<string>) != default);

            Assert.IsTrue(new Box<object>(new object()) != default);
            Assert.IsTrue(new Box<int>(5) != default);
            Assert.IsTrue(new Box<string>("value") != default);

            Assert.IsFalse(new Box<object>(testObject) != new Box<object>(testObject));
            Assert.IsFalse(new Box<int>(5) != new Box<int>(5));
            Assert.IsFalse(new Box<string>("value") != new Box<string>("value"));

            Assert.IsTrue(new Box<object>(new object()) != new Box<object>(new object()));
            Assert.IsTrue(new Box<int>(5) != new Box<int>(7));
            Assert.IsTrue(new Box<string>("value") != new Box<string>("other"));
        }
    }
}
