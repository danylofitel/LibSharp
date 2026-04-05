// Copyright (c) 2026 Danylo Fitel

using System;
using System.Text.RegularExpressions;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common;

[TestClass]
public class RegexExtensionsUnitTests
{
    // Catastrophically backtracking pattern: forces exponential search on a string of 'a's
    // followed by a non-matching character.
    private static readonly Regex s_backtrackingRegex =
        new Regex("(a+)+$", RegexOptions.None, TimeSpan.FromMilliseconds(1));

    private static readonly string s_backtrackingInput = new string('a', 25) + "!";

    [TestMethod]
    public void TryIsMatch_MatchFound()
    {
        // Arrange
        Regex regex = new Regex("a+", RegexOptions.None, TimeSpan.FromSeconds(10));

        // Act
        bool isMatch = regex.TryIsMatch("captcha", out bool timedOut);

        // Assert
        Assert.IsTrue(isMatch);
        Assert.IsFalse(timedOut);
    }

    [TestMethod]
    public void TryIsMatch_MatchNotFound()
    {
        // Arrange
        Regex regex = new Regex("a+", RegexOptions.None, TimeSpan.FromSeconds(10));

        // Act
        bool isMatch = regex.TryIsMatch("0xFFFFFFFF", out bool timedOut);

        // Assert
        Assert.IsFalse(isMatch);
        Assert.IsFalse(timedOut);
    }

    [TestMethod]
    public void TryIsMatch_TimedOut()
    {
        // Act
        bool isMatch = s_backtrackingRegex.TryIsMatch(s_backtrackingInput, out bool timedOut);

        // Assert
        Assert.IsFalse(isMatch);
        Assert.IsTrue(timedOut);
    }

    [TestMethod]
    public void TryMatch_MatchFound()
    {
        // Arrange
        Regex regex = new Regex("a+", RegexOptions.None, TimeSpan.FromSeconds(10));

        // Act
        Match match = regex.TryMatch("captcha", out bool timedOut);

        // Assert
        Assert.AreNotEqual(Match.Empty, match);
        Assert.IsFalse(timedOut);
    }

    [TestMethod]
    public void TryMatch_MatchNotFound()
    {
        // Arrange
        Regex regex = new Regex("a+", RegexOptions.None, TimeSpan.FromSeconds(10));

        // Act
        Match match = regex.TryMatch("0xFFFFFFFF", out bool timedOut);

        // Assert
        Assert.AreEqual(Match.Empty, match);
        Assert.IsFalse(timedOut);
    }

    [TestMethod]
    public void TryMatch_TimedOut()
    {
        // Act
        Match match = s_backtrackingRegex.TryMatch(s_backtrackingInput, out bool timedOut);

        // Assert
        Assert.AreEqual(Match.Empty, match);
        Assert.IsTrue(timedOut);
    }

    [TestMethod]
    public void TryReplace_Replacement_MatchFound()
    {
        // Arrange
        Regex regex = new Regex("\\s*brown\\s*", RegexOptions.None, TimeSpan.FromSeconds(10));

        // Act
        string result = regex.TryReplace("the quick   \tbrown \t\t\tfox", " red ", out bool timedOut);

        // Assert
        Assert.AreEqual("the quick red fox", result);
        Assert.IsFalse(timedOut);
    }

    [TestMethod]
    public void TryReplace_Replacement_MatchNotFound()
    {
        // Arrange
        Regex regex = new Regex("\\s*brown\\s*", RegexOptions.None, TimeSpan.FromSeconds(10));

        // Act
        string result = regex.TryReplace("the quick   \tbrow \t\t\tfox", " red ", out bool timedOut);

        // Assert
        Assert.AreEqual("the quick   \tbrow \t\t\tfox", result);
        Assert.IsFalse(timedOut);
    }

    [TestMethod]
    public void TryReplace_Replacement_TimedOut()
    {
        // Act
        string result = s_backtrackingRegex.TryReplace(s_backtrackingInput, "x", out bool timedOut);

        // Assert
        Assert.AreEqual(s_backtrackingInput, result);
        Assert.IsTrue(timedOut);
    }

    [TestMethod]
    public void TryReplace_Evaluator_MatchFound()
    {
        // Arrange
        Regex regex = new Regex("\\s*brown\\s*", RegexOptions.None, TimeSpan.FromSeconds(10));

        // Act
        string result = regex.TryReplace("the quick   \tbrown \t\t\tfox", match => " red ", out bool timedOut);

        // Assert
        Assert.AreEqual("the quick red fox", result);
        Assert.IsFalse(timedOut);
    }

    [TestMethod]
    public void TryReplace_Evaluator_MatchNotFound()
    {
        // Arrange
        Regex regex = new Regex("\\s*brown\\s*", RegexOptions.None, TimeSpan.FromSeconds(10));

        // Act
        string result = regex.TryReplace("the quick   \tbrow \t\t\tfox", match => " red ", out bool timedOut);

        // Assert
        Assert.AreEqual("the quick   \tbrow \t\t\tfox", result);
        Assert.IsFalse(timedOut);
    }

    [TestMethod]
    public void TryReplace_Evaluator_TimedOut()
    {
        // Act
        string result = s_backtrackingRegex.TryReplace(s_backtrackingInput, match => "x", out bool timedOut);

        // Assert
        Assert.AreEqual(s_backtrackingInput, result);
        Assert.IsTrue(timedOut);
    }
}
