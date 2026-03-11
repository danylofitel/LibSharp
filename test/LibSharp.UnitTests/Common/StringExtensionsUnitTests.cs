// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Net;
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

        [TestMethod]
        public void TryConvertToEnum_HttpStatusCode()
        {
            // Arrange
            HttpStatusCode result;

            // Assert
            Assert.IsFalse(((string)null).TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse(string.Empty.TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse(" ".TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse("abc".TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse("ok".TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse("notfound".TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse("-1".TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse("0".TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse("99".TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse("600".TryConvertToEnum<HttpStatusCode>(out _));

            Assert.IsTrue("Continue".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.Continue, result);

            Assert.IsTrue("SwitchingProtocols".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.SwitchingProtocols, result);

            Assert.IsTrue("OK".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.OK, result);

            Assert.IsTrue("BadRequest".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.BadRequest, result);

            Assert.IsTrue("Unauthorized".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.Unauthorized, result);

            Assert.IsTrue("Forbidden".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.Forbidden, result);

            Assert.IsTrue("NotFound".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.NotFound, result);

            Assert.IsTrue("InternalServerError".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.InternalServerError, result);

            Assert.IsTrue("HttpVersionNotSupported".TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.HttpVersionNotSupported, result);
        }

        [TestMethod]
        public void TryConvertToEnum_StringComparison()
        {
            // Arrange
            StringComparison result;

            // Assert
            Assert.IsFalse(((string)null).TryConvertToEnum<StringComparison>(out _));
            Assert.IsFalse(string.Empty.TryConvertToEnum<StringComparison>(out _));
            Assert.IsFalse(" ".TryConvertToEnum<StringComparison>(out _));
            Assert.IsFalse("abc".TryConvertToEnum<StringComparison>(out _));
            Assert.IsFalse("ordinal".TryConvertToEnum<StringComparison>(out _));
            Assert.IsFalse("-1".TryConvertToEnum<StringComparison>(out _));
            Assert.IsFalse("6".TryConvertToEnum<StringComparison>(out _));

            Assert.IsTrue("CurrentCulture".TryConvertToEnum<StringComparison>(out result));
            Assert.AreEqual(StringComparison.CurrentCulture, result);

            Assert.IsTrue("CurrentCultureIgnoreCase".TryConvertToEnum<StringComparison>(out result));
            Assert.AreEqual(StringComparison.CurrentCultureIgnoreCase, result);

            Assert.IsTrue("InvariantCulture".TryConvertToEnum<StringComparison>(out result));
            Assert.AreEqual(StringComparison.InvariantCulture, result);

            Assert.IsTrue("InvariantCultureIgnoreCase".TryConvertToEnum<StringComparison>(out result));
            Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase, result);

            Assert.IsTrue("Ordinal".TryConvertToEnum<StringComparison>(out result));
            Assert.AreEqual(StringComparison.Ordinal, result);

            Assert.IsTrue("OrdinalIgnoreCase".TryConvertToEnum<StringComparison>(out result));
            Assert.AreEqual(StringComparison.OrdinalIgnoreCase, result);
        }
    }
}
