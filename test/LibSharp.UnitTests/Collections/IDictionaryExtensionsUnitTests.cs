// Copyright (c) 2026 Danylo Fitel

using System.Collections.Generic;
using LibSharp.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Collections;

[TestClass]
public class IDictionaryExtensionsUnitTests
{
    [TestMethod]
    public void AddOrUpdate_FromValue_ValueDoesNotExist_AddsValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>();

        // Act
        string value = dictionary.AddOrUpdate(
            "key",
            "addedValue",
            (key, existingValue) => "updatedValue");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("addedValue", value);
        Assert.AreEqual("addedValue", dictionary["key"]);
    }

    [TestMethod]
    public void AddOrUpdate_FromValue_ValueExists_UpdatesValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>
        {
            ["key"] = "existingValue",
        };

        // Act
        string value = dictionary.AddOrUpdate(
            "key",
            "addedValue",
            (key, existingValue) => "updatedValue");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("updatedValue", value);
        Assert.AreEqual("updatedValue", dictionary["key"]);
    }

    [TestMethod]
    public void AddOrUpdate_FromValueFactory_ValueDoesNotExist_AddsValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>();

        // Act
        string value = dictionary.AddOrUpdate(
            "key",
            key => "addedValue",
            (key, existingValue) => "updatedValue");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("addedValue", value);
        Assert.AreEqual("addedValue", dictionary["key"]);
    }

    [TestMethod]
    public void AddOrUpdate_FromValueFactory_ValueExists_UpdatesValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>
        {
            ["key"] = "existingValue",
        };

        // Act
        string value = dictionary.AddOrUpdate(
            "key",
            key => "addedValue",
            (key, existingValue) => "updatedValue");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("updatedValue", value);
        Assert.AreEqual("updatedValue", dictionary["key"]);
    }

    [TestMethod]
    public void AddOrUpdate_FromValueFactoryWithArgument_ValueDoesNotExist_AddsValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>();

        // Act
        string value = dictionary.AddOrUpdate(
            "key",
            (key, argument) => "addedValue" + argument,
            (key, existingValue, argument) => "updatedValue" + argument,
            "Argument");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("addedValueArgument", value);
        Assert.AreEqual("addedValueArgument", dictionary["key"]);
    }

    [TestMethod]
    public void AddOrUpdate_FromValueFactoryWithArgument_ValueExists_UpdatesValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>
        {
            ["key"] = "existingValue",
        };

        // Act
        string value = dictionary.AddOrUpdate(
            "key",
            (key, argument) => "addedValue" + argument,
            (key, existingValue, argument) => "updatedValue" + argument,
            "Argument");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("updatedValueArgument", value);
        Assert.AreEqual("updatedValueArgument", dictionary["key"]);
    }

    [TestMethod]
    public void GetOrAdd_FromValue_ValueDoesNotExist_AddsValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>();

        // Act
        string value = dictionary.GetOrAdd(
            "key",
            "addedValue");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("addedValue", value);
        Assert.AreEqual("addedValue", dictionary["key"]);
    }

    [TestMethod]
    public void GetOrAdd_FromValue_ValueExists_Returns()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>
        {
            ["key"] = "existingValue",
        };

        // Act
        string value = dictionary.GetOrAdd(
            "key",
            "addedValue");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("existingValue", value);
        Assert.AreEqual("existingValue", dictionary["key"]);
    }

    [TestMethod]
    public void GetOrAdd_FromValueFactory_ValueDoesNotExist_AddsValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>();

        // Act
        string value = dictionary.GetOrAdd(
            "key",
            keyValue => "addedValue");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("addedValue", value);
        Assert.AreEqual("addedValue", dictionary["key"]);
    }

    [TestMethod]
    public void GetOrAdd_FromValueFactory_ValueExists_Returns()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>
        {
            ["key"] = "existingValue",
        };

        // Act
        string value = dictionary.GetOrAdd(
            "key",
            keyValue => "addedValue");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("existingValue", value);
        Assert.AreEqual("existingValue", dictionary["key"]);
    }

    [TestMethod]
    public void GetOrAdd_FromValueFactoryWithArgument_ValueDoesNotExist_AddsValue()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>();

        // Act
        string value = dictionary.GetOrAdd(
            "key",
            (keyValue, argument) => "addedValue" + argument,
            "Argument");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("addedValueArgument", value);
        Assert.AreEqual("addedValueArgument", dictionary["key"]);
    }

    [TestMethod]
    public void GetOrAdd_FromValueFactoryWithArgument_ValueExists_Returns()
    {
        // Arrange
        IDictionary<string, string> dictionary = new Dictionary<string, string>
        {
            ["key"] = "existingValue",
        };

        // Act
        string value = dictionary.GetOrAdd(
            "key",
            (keyValue, argument) => "addedValue" + argument,
            "Argument");

        // Assert
        Assert.HasCount(1, dictionary);
        Assert.AreEqual("existingValue", value);
        Assert.AreEqual("existingValue", dictionary["key"]);
    }

    [TestMethod]
    public void Copy_EmptyDictionary_ReturnsEmptyDictionary()
    {
        // Arrange
        IDictionary<string, string> original = new Dictionary<string, string>();

        // Act
        IDictionary<string, string> copy = original.Copy();

        // Assert
        Assert.IsEmpty(original);
        Assert.IsEmpty(copy);
        Assert.AreNotEqual(original, copy);
    }

    [TestMethod]
    public void Copy_NonEmptyDictionary_ReturnsCopiedDictionary()
    {
        // Arrange
        IDictionary<string, string> original = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
        };

        // Act
        IDictionary<string, string> copy = original.Copy();

        // Assert
        Assert.HasCount(2, original);
        Assert.HasCount(2, copy);

        foreach (KeyValuePair<string, string> pair in original)
        {
            Assert.AreEqual(pair.Value, copy[pair.Key]);
        }
    }

    [TestMethod]
    public void CopyTo_FromEmptyDictionary_ReturnsDestinationDictionary()
    {
        // Arrange
        IDictionary<string, string> source = new Dictionary<string, string>();
        IDictionary<string, string> destination = new Dictionary<string, string>
        {
            ["key1"] = "value1",
        };

        // Act
        IDictionary<string, string> result = source.CopyTo(destination);

        // Assert
        Assert.AreEqual(destination, result);
        Assert.IsEmpty(source);
        Assert.HasCount(1, result);
        Assert.AreEqual("value1", result["key1"]);
    }

    [TestMethod]
    public void CopyTo_FromNonEmptyDictionary_KeysDoNotOverlap_CopiesAllEntries()
    {
        // Arrange
        IDictionary<string, string> source = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
        };

        IDictionary<string, string> destination = new Dictionary<string, string>
        {
            ["key3"] = "value3",
            ["key4"] = "value4",
        };

        // Act
        IDictionary<string, string> result = source.CopyTo(destination);

        // Assert
        Assert.AreEqual(destination, result);
        Assert.HasCount(2, source);
        Assert.HasCount(4, result);

        foreach (KeyValuePair<string, string> pair in source)
        {
            Assert.AreEqual(pair.Value, destination[pair.Key]);
        }
    }

    [TestMethod]
    public void CopyTo_FromNonEmptyDictionary_KeysOverlap_ReplacesEntries()
    {
        // Arrange
        IDictionary<string, string> source = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
        };

        IDictionary<string, string> destination = new Dictionary<string, string>
        {
            ["key2"] = "originalValue2",
            ["key3"] = "value3",
        };

        // Act
        IDictionary<string, string> result = source.CopyTo(destination);

        // Assert
        Assert.AreEqual(destination, result);
        Assert.HasCount(2, source);
        Assert.HasCount(3, result);

        foreach (KeyValuePair<string, string> pair in source)
        {
            Assert.AreEqual(pair.Value, destination[pair.Key]);
        }
    }
}
