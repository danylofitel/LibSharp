// Copyright (c) 2026 Danylo Fitel

using System;
using LibSharp.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibSharp.UnitTests.Caching;

[TestClass]
public class StaleReadPolicyUnitTests
{
    [TestMethod]
    public void Default_IsWait()
    {
        // The struct's default must be the safe policy, since a caller who never sets it gets this.
        StaleReadPolicy policy = default;

        Assert.IsFalse(policy.ServesStale);
        Assert.IsNull(policy.MaxStaleness);
        Assert.AreEqual(StaleReadPolicy.Wait, policy);
    }

    [TestMethod]
    public void ServeStale_ServesWithNoBound()
    {
        StaleReadPolicy policy = StaleReadPolicy.ServeStale;

        Assert.IsTrue(policy.ServesStale);
        Assert.IsNull(policy.MaxStaleness);
    }

    [TestMethod]
    public void ServeStaleUpTo_ServesWithBound()
    {
        StaleReadPolicy policy = StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromMinutes(5));

        Assert.IsTrue(policy.ServesStale);
        Assert.AreEqual(TimeSpan.FromMinutes(5), policy.MaxStaleness);
    }

    [TestMethod]
    public void ServeStaleUpTo_NegativeBound_Throws()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromSeconds(-1)));
    }

    [TestMethod]
    public void ServeStaleUpTo_ZeroBound_Throws()
    {
        // A zero bound would serve stale values for no time at all, which is Wait expressed
        // confusingly; the factory rejects it rather than accepting a policy that means nothing.
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => StaleReadPolicy.ServeStaleUpTo(TimeSpan.Zero));
    }

    [TestMethod]
    public void Equals_SamePolicy_IsEqual()
    {
        StaleReadPolicy left = StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromMinutes(1));
        StaleReadPolicy right = StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromMinutes(1));

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left.Equals((object)right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    [TestMethod]
    public void Equals_DifferentBound_IsNotEqual()
    {
        StaleReadPolicy left = StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromMinutes(1));
        StaleReadPolicy right = StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromMinutes(2));

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
        Assert.IsFalse(left == right);
    }

    [TestMethod]
    public void Equals_UnboundedVersusBounded_IsNotEqual()
    {
        // An unbounded serve-stale and a bounded one are different policies even though both serve.
        Assert.AreNotEqual(StaleReadPolicy.ServeStale, StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromMinutes(1)));
        Assert.IsTrue(StaleReadPolicy.ServeStale != StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromMinutes(1)));
    }

    [TestMethod]
    public void Equals_WaitVersusServeStale_IsNotEqual()
    {
        Assert.AreNotEqual(StaleReadPolicy.Wait, StaleReadPolicy.ServeStale);
        Assert.IsTrue(StaleReadPolicy.Wait != StaleReadPolicy.ServeStale);
    }

    [TestMethod]
    public void Equals_OtherType_IsNotEqual()
    {
        Assert.IsFalse(StaleReadPolicy.ServeStale.Equals("not a policy"));
        Assert.IsFalse(StaleReadPolicy.ServeStale.Equals(null));
    }

    [TestMethod]
    public void ToString_NamesThePolicy()
    {
        Assert.AreEqual("Wait", StaleReadPolicy.Wait.ToString());
        Assert.AreEqual("ServeStale", StaleReadPolicy.ServeStale.ToString());
        StringAssert.StartsWith(StaleReadPolicy.ServeStaleUpTo(TimeSpan.FromMinutes(5)).ToString(), "ServeStaleUpTo(");
    }
}
