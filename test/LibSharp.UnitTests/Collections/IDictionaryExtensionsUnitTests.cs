// Copyright (c) LibSharp. All rights reserved.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.Collections.UnitTests
{
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(1, dictionary.Count);
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
            Assert.AreEqual(0, original.Count);
            Assert.AreEqual(0, copy.Count);
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
            Assert.AreEqual(2, original.Count);
            Assert.AreEqual(2, copy.Count);

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
            Assert.AreEqual(0, source.Count);
            Assert.AreEqual(1, result.Count);
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
            Assert.AreEqual(2, source.Count);
            Assert.AreEqual(4, result.Count);

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
            Assert.AreEqual(2, source.Count);
            Assert.AreEqual(3, result.Count);

            foreach (KeyValuePair<string, string> pair in source)
            {
                Assert.AreEqual(pair.Value, destination[pair.Key]);
            }
        }
    }
}
