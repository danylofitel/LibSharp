// Copyright (c) LibSharp. All rights reserved.

using System.Collections.Generic;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class XmlSerializationExtensionsUnitTests
    {
        [TestMethod]
        public void XmlSerializationTest()
        {
            // Arrange
            List<string> original = ["a", "bb", "ccc"];

            // Act
            string serialized = original.SerializeToXml();
            List<string> deserialized = serialized.DeserializeFromXml<List<string>>();

            // Assert
            CollectionAssert.AreEquivalent(original, deserialized);
        }
    }
}
