// Copyright (c) 2026 Danylo Fitel

using System;
using LibSharp.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Common;

[TestClass]
public class ResultUnitTests
{
    // ── default ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Default_IsError()
    {
        Result<int, string> result = default;

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsError);
    }

    [TestMethod]
    public void Default_Value_Throws()
    {
        Result<int, string> result = default;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = result.Value);
    }

    [TestMethod]
    public void Default_Error_ReturnsDefaultTError()
    {
        Result<int, string> result = default;

        Assert.IsNull(result.Error);
    }

    // ── Ok ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Ok_ValueType_IsSuccess()
    {
        Result<int, string> result = Result<int, string>.Ok(42);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(result.IsError);
        Assert.AreEqual(42, result.Value);
    }

    [TestMethod]
    public void Ok_ReferenceType_IsSuccess()
    {
        Result<string, int> result = Result<string, int>.Ok("hello");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("hello", result.Value);
    }

    [TestMethod]
    public void Ok_NullValue_IsSuccess()
    {
        Result<string, int> result = Result<string, int>.Ok(null);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value);
    }

    [TestMethod]
    public void Ok_Error_Throws()
    {
        Result<int, string> result = Result<int, string>.Ok(42);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = result.Error);
    }

    // ── Error ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Error_ValueType_IsError()
    {
        Result<string, int> result = Result<string, int>.Fail(404);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(404, result.Error);
    }

    [TestMethod]
    public void Error_ReferenceType_IsError()
    {
        Result<int, string> result = Result<int, string>.Fail("not found");

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("not found", result.Error);
    }

    [TestMethod]
    public void Error_NullError_IsError()
    {
        Result<int, string> result = Result<int, string>.Fail(null);

        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void Error_Value_Throws()
    {
        Result<int, string> result = Result<int, string>.Fail("bad");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = result.Value);
    }

    // ── GetValueOrDefault ─────────────────────────────────────────────────

    [TestMethod]
    public void GetValueOrDefault_Success_ReturnsValue()
    {
        Assert.AreEqual(42, Result<int, string>.Ok(42).GetValueOrDefault());
        Assert.AreEqual("hello", Result<string, int>.Ok("hello").GetValueOrDefault());
    }

    [TestMethod]
    public void GetValueOrDefault_Error_ReturnsDefault()
    {
        Assert.AreEqual(0, Result<int, string>.Fail("bad").GetValueOrDefault());
        Assert.IsNull(Result<string, int>.Fail(1).GetValueOrDefault());
    }

    [TestMethod]
    public void GetValueOrDefault_WithFallback_Success_ReturnsValue()
    {
        Assert.AreEqual(42, Result<int, string>.Ok(42).GetValueOrDefault(-1));
    }

    [TestMethod]
    public void GetValueOrDefault_WithFallback_Error_ReturnsFallback()
    {
        Assert.AreEqual(-1, Result<int, string>.Fail("bad").GetValueOrDefault(-1));
    }

    // ── GetErrorOrDefault ─────────────────────────────────────────────────

    [TestMethod]
    public void GetErrorOrDefault_Error_ReturnsError()
    {
        Assert.AreEqual("bad", Result<int, string>.Fail("bad").GetErrorOrDefault());
        Assert.AreEqual(404, Result<string, int>.Fail(404).GetErrorOrDefault());
    }

    [TestMethod]
    public void GetErrorOrDefault_Success_ReturnsDefault()
    {
        Assert.IsNull(Result<int, string>.Ok(42).GetErrorOrDefault());
        Assert.AreEqual(0, Result<string, int>.Ok("hello").GetErrorOrDefault());
    }

    [TestMethod]
    public void GetErrorOrDefault_WithFallback_Error_ReturnsError()
    {
        Assert.AreEqual("bad", Result<int, string>.Fail("bad").GetErrorOrDefault("fallback"));
    }

    [TestMethod]
    public void GetErrorOrDefault_WithFallback_Success_ReturnsFallback()
    {
        Assert.AreEqual("fallback", Result<int, string>.Ok(42).GetErrorOrDefault("fallback"));
    }

    // ── TryGetValue ───────────────────────────────────────────────────────

    [TestMethod]
    public void TryGetValue_Success_ReturnsTrueAndSetsValue()
    {
        bool success = Result<int, string>.Ok(7).TryGetValue(out int value);

        Assert.IsTrue(success);
        Assert.AreEqual(7, value);
    }

    [TestMethod]
    public void TryGetValue_Error_ReturnsFalse()
    {
        bool success = Result<int, string>.Fail("bad").TryGetValue(out int value);

        Assert.IsFalse(success);
        Assert.AreEqual(0, value);
    }

    // ── TryGetError ───────────────────────────────────────────────────────

    [TestMethod]
    public void TryGetError_Error_ReturnsTrueAndSetsError()
    {
        bool isError = Result<int, string>.Fail("bad").TryGetError(out string error);

        Assert.IsTrue(isError);
        Assert.AreEqual("bad", error);
    }

    [TestMethod]
    public void TryGetError_Success_ReturnsFalse()
    {
        bool isError = Result<int, string>.Ok(42).TryGetError(out string error);

        Assert.IsFalse(isError);
        Assert.IsNull(error);
    }

    // ── ToString ──────────────────────────────────────────────────────────

    [TestMethod]
    public void ToString_Success_ReturnsValueToString()
    {
        Assert.AreEqual("42", Result<int, string>.Ok(42).ToString());
        Assert.AreEqual("hello", Result<string, int>.Ok("hello").ToString());
    }

    [TestMethod]
    public void ToString_Success_NullValue_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Result<string, int>.Ok(null).ToString());
    }

    [TestMethod]
    public void ToString_Error_ReturnsErrorToString()
    {
        Assert.AreEqual("bad", Result<int, string>.Fail("bad").ToString());
        Assert.AreEqual("404", Result<string, int>.Fail(404).ToString());
    }

    [TestMethod]
    public void ToString_Error_NullError_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Result<int, string>.Fail(null).ToString());
    }

    // ── Equals ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Equals_TwoSuccesses_SameValue_AreEqual()
    {
        Assert.IsTrue(Result<int, string>.Ok(1).Equals(Result<int, string>.Ok(1)));
        Assert.IsTrue(Result<string, int>.Ok("a").Equals(Result<string, int>.Ok("a")));
    }

    [TestMethod]
    public void Equals_TwoSuccesses_DifferentValues_AreNotEqual()
    {
        Assert.IsFalse(Result<int, string>.Ok(1).Equals(Result<int, string>.Ok(2)));
    }

    [TestMethod]
    public void Equals_TwoErrors_SameError_AreEqual()
    {
        Assert.IsTrue(Result<int, string>.Fail("bad").Equals(Result<int, string>.Fail("bad")));
    }

    [TestMethod]
    public void Equals_TwoErrors_DifferentErrors_AreNotEqual()
    {
        Assert.IsFalse(Result<int, string>.Fail("bad").Equals(Result<int, string>.Fail("worse")));
    }

    [TestMethod]
    public void Equals_SuccessAndError_AreNotEqual()
    {
        Assert.IsFalse(Result<int, string>.Ok(0).Equals(Result<int, string>.Fail(null)));
    }

    [TestMethod]
    public void Equals_Object_SameResult_AreEqual()
    {
        Result<int, string> result = Result<int, string>.Ok(5);
        Assert.IsTrue(result.Equals((object)result));
        Assert.IsTrue(result.Equals((object)Result<int, string>.Ok(5)));
    }

    [TestMethod]
    public void Equals_Object_DifferentType_ReturnsFalse()
    {
        Assert.IsFalse(Result<int, string>.Ok(5).Equals((object)5));
        Assert.IsFalse(Result<int, string>.Ok(5).Equals(null));
    }

    // ── GetHashCode ───────────────────────────────────────────────────────

    [TestMethod]
    public void GetHashCode_EqualResults_SameHashCode()
    {
        Assert.AreEqual(Result<int, string>.Ok(1).GetHashCode(), Result<int, string>.Ok(1).GetHashCode());
        Assert.AreEqual(Result<int, string>.Fail("x").GetHashCode(), Result<int, string>.Fail("x").GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_SuccessAndError_DifferentHashCodes()
    {
        // A success with value 0 and an error with null should have different hash codes
        // because IsSuccess is mixed into the hash.
        Assert.AreNotEqual(
            Result<int, string>.Ok(0).GetHashCode(),
            Result<int, string>.Fail(null).GetHashCode());
    }

    // ── Operators ─────────────────────────────────────────────────────────

    [TestMethod]
    public void OperatorEquals()
    {
        Assert.IsTrue(Result<int, string>.Ok(1) == Result<int, string>.Ok(1));
        Assert.IsTrue(Result<int, string>.Fail("x") == Result<int, string>.Fail("x"));
        Assert.IsFalse(Result<int, string>.Ok(1) == Result<int, string>.Ok(2));
        Assert.IsFalse(Result<int, string>.Ok(0) == Result<int, string>.Fail(null));
    }

    [TestMethod]
    public void OperatorNotEquals()
    {
        Assert.IsFalse(Result<int, string>.Ok(1) != Result<int, string>.Ok(1));
        Assert.IsTrue(Result<int, string>.Ok(1) != Result<int, string>.Ok(2));
        Assert.IsTrue(Result<int, string>.Ok(0) != Result<int, string>.Fail(null));
    }
}
