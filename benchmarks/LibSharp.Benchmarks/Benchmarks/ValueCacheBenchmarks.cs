// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using LibSharp.Caching;

namespace LibSharp.Benchmarks.Benchmarks;

public class ValueCacheBenchmarks
{
    private ValueCache<int> m_cachedValueCache = null!;
    private ValueCache<int> m_expiredValueCache = null!;

    private int m_counter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        m_cachedValueCache = new ValueCache<int>(() => 42, TimeSpan.FromMinutes(10));
        _ = m_cachedValueCache.GetValue();

        m_expiredValueCache = new ValueCache<int>(() => Interlocked.Increment(ref m_counter), TimeSpan.Zero);
    }

    [Benchmark(Baseline = true)]
    public int GetValue_CachedHit()
    {
        return m_cachedValueCache.GetValue();
    }

    [Benchmark]
    public int GetValue_ExpiredRefresh()
    {
        return m_expiredValueCache.GetValue();
    }
}
