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

        [TestMethod]
        public void TryMatch_MatchFound()
        {
            // Arrange
            Regex regex = new Regex("a+", RegexOptions.None, TimeSpan.FromSeconds(10));

            // Act
            Match match = regex.TryMatch("captcha");

            // Assert
            Assert.AreNotEqual(Match.Empty, match);
        }

        [TestMethod]
        public void TryMatch_MatchNotFound()
        {
            // Arrange
            Regex regex = new Regex("a+", RegexOptions.None, TimeSpan.FromSeconds(10));

            // Act
            Match match = regex.TryMatch("0xFFFFFFFF");

            // Assert
            Assert.AreEqual(Match.Empty, match);
        }

        [TestMethod]
        public void TryReplace_Replacement_MatchFound()
        {
            // Arrange
            Regex regex = new Regex("\\s*brown\\s*", RegexOptions.None, TimeSpan.FromSeconds(10));

            // Act
            string result = regex.TryReplace("the quick   \tbrown \t\t\tfox", " red ");

            // Assert
            Assert.AreEqual("the quick red fox", result);
        }

        [TestMethod]
        public void TryReplace_Replacement_MatchNotFound()
        {
            // Arrange
            Regex regex = new Regex("\\s*brown\\s*", RegexOptions.None, TimeSpan.FromSeconds(10));

            // Act
            string result = regex.TryReplace("the quick   \tbrow \t\t\tfox", " red ");

            // Assert
            Assert.AreEqual("the quick   \tbrow \t\t\tfox", result);
        }

        [TestMethod]
        public void TryReplace_Evaluator_MatchFound()
        {
            // Arrange
            Regex regex = new Regex("\\s*brown\\s*", RegexOptions.None, TimeSpan.FromSeconds(10));

            // Act
            string result = regex.TryReplace("the quick   \tbrown \t\t\tfox", match => " red ");

            // Assert
            Assert.AreEqual("the quick red fox", result);
        }

        [TestMethod]
        public void TryReplace_Evaluator_MatchNotFound()
        {
            // Arrange
            Regex regex = new Regex("\\s*brown\\s*", RegexOptions.None, TimeSpan.FromSeconds(10));

            // Act
            string result = regex.TryReplace("the quick   \tbrow \t\t\tfox", match => " red ");

            // Assert
            Assert.AreEqual("the quick   \tbrow \t\t\tfox", result);
        }
    }
}
