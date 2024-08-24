// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Text.RegularExpressions;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests
{
    [TestClass]
    public class RegexExtensionsUnitTests
    {
        [TestMethod]
        public void TryIsMatch_MatchFound()
        {
            // Arrange
            Regex regex = new Regex("a+", RegexOptions.None, TimeSpan.FromSeconds(10));

            // Act
            bool isMatch = regex.TryIsMatch("captcha");

            // Assert
            Assert.IsTrue(isMatch);
        }

        [TestMethod]
        public void TryIsMatch_MatchNotFound()
        {
            // Arrange
            Regex regex = new Regex("a+", RegexOptions.None, TimeSpan.FromSeconds(10));

            // Act
            bool isMatch = regex.TryIsMatch("0xFFFFFFFF");

            // Assert
            Assert.IsFalse(isMatch);
        }
    }
}
