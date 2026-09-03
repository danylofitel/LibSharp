// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Generic;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common;

[TestClass]
public class XmlSerializationExtensionsUnitTests
{
    [TestMethod]
    public void XmlSerializationTest()
    {
        // Arrange
        List<string> original = new List<string> { "a", "bb", "ccc" };

        // Act
        string serialized = original.SerializeToXml();
        List<string> deserialized = serialized.DeserializeFromXml<List<string>>();

        // Assert
        CollectionAssert.AreEquivalent(original, deserialized);
    }

    [TestMethod]
    public void DeserializeFromXmlThrowsWhenXmlHasNoValue()
    {
        // Arrange
        // xsi:nil makes XmlSerializer.Deserialize return null, which the non-nullable
        // return type must not pass on to the caller.
        string nil = "<?xml version=\"1.0\"?><Payload xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\" />";

        // Act & Assert
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => nil.DeserializeFromXml<Payload>());
    }

    public class Payload
    {
        public string? Name { get; set; }
    }
}

