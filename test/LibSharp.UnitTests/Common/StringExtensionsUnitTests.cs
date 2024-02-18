// Copyright (c) LibSharp. All rights reserved.

using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class StringExtensionsUnitTests
    {
        [TestMethod]
        public void Base64Encode_Succeeds()
        {
            // Arrange
            const string PlainText = "Plain text";
            const string Base64Text = "UGxhaW4gdGV4dA==";

            // Act
            string result = PlainText.Base64Encode();

            // Assert
            Assert.AreEqual(Base64Text, result);
        }

        [TestMethod]
        public void Base64Decode_Succeeds()
        {
            // Arrange
            const string PlainText = "Not a secret";
            const string Base64Text = "Tm90IGEgc2VjcmV0";

            // Act
            string result = Base64Text.Base64Decode();

            // Assert
            Assert.AreEqual(PlainText, result);
        }

        [TestMethod]
        public void Base64EncodingAndDecoding_AreReverse()
        {
            // Arrange
            const string PlainText = "Secure password";
            const string Base64Text = "U2VjdXJlIHBhc3N3b3Jk";

            // Act
            string plainTextTransformation = PlainText.Base64Encode().Base64Decode();
            string base64EncodedTransformation = Base64Text.Base64Decode().Base64Encode();

            // Assert
            Assert.AreEqual(PlainText, plainTextTransformation);
            Assert.AreEqual(Base64Text, base64EncodedTransformation);
        }

        [TestMethod]
        public void Base64Encoding_MultipleIterations_IsReversible()
        {
            // Arrange
            const string PlainText = "*O_^$BgoI*Ws64BKLDygdbks H(Y*PWRS";

            // Act
            string plainTextTransformation = PlainText
                .Base64Encode()
                .Base64Encode()
                .Base64Encode()
                .Base64Decode()
                .Base64Decode()
                .Base64Decode();

            // Assert
            Assert.AreEqual(PlainText, plainTextTransformation);
        }

        [TestMethod]
        public void Reverse()
        {
            // Assert
            Assert.AreEqual(string.Empty, string.Empty.Reverse());
            Assert.AreEqual(" ", " ".Reverse());
            Assert.AreEqual("a", "a".Reverse());
            Assert.AreEqual("aBa", "aBa".Reverse());
            Assert.AreEqual("Ba", "aB".Reverse());
            Assert.AreEqual("9876543210", "0123456789".Reverse());
        }

        [TestMethod]
        public void Truncate()
        {
            // Assert
            Assert.AreEqual(string.Empty, string.Empty.Truncate(0));
            Assert.AreEqual(string.Empty, string.Empty.Truncate(10));
            Assert.AreEqual(string.Empty, "a".Truncate(0));
            Assert.AreEqual("a", "a".Truncate(1));
            Assert.AreEqual("a", "a".Truncate(2));
            Assert.AreEqual("a", "aBc".Truncate(1));
            Assert.AreEqual("aB", "aBc".Truncate(2));
            Assert.AreEqual("aBc", "aBc".Truncate(3));
            Assert.AreEqual("aBc", "aBc".Truncate(int.MaxValue));
        }
    }
}
