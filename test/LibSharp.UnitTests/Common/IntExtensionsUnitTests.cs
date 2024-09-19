// Copyright (c) LibSharp. All rights reserved.

using System.Net;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common
{
    [TestClass]
    public class IntExtensionsUnitTests
    {
        [TestMethod]
        public void TryConvertToEnum_HttpStatusCode()
        {
            // Arrange
            HttpStatusCode result;

            // Assert
            Assert.IsFalse(int.MinValue.TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse((-1).TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse(0.TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse(1.TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse(99.TryConvertToEnum<HttpStatusCode>(out _));
            Assert.IsFalse(int.MaxValue.TryConvertToEnum<HttpStatusCode>(out _));

            Assert.IsTrue(100.TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.Continue, result);

            Assert.IsTrue(200.TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.OK, result);

            Assert.IsTrue(400.TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.BadRequest, result);

            Assert.IsTrue(401.TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.Unauthorized, result);

            Assert.IsTrue(403.TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.Forbidden, result);

            Assert.IsTrue(404.TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.NotFound, result);

            Assert.IsTrue(500.TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.InternalServerError, result);

            Assert.IsTrue(505.TryConvertToEnum<HttpStatusCode>(out result));
            Assert.AreEqual(HttpStatusCode.HttpVersionNotSupported, result);
        }
    }
}
