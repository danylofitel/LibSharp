// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using LibSharp.Caching;

namespace LibSharp.Benchmarks.Benchmarks;

public class ValueCacheBenchmarks
{
    private ValueCache<int> _cachedValueCache = null!;
    private ValueCache<int> _expiredValueCache = null!;

    private int _counter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _cachedValueCache = new ValueCache<int>(() => 42, TimeSpan.FromMinutes(10));
        _ = _cachedValueCache.GetValue();

        _expiredValueCache = new ValueCache<int>(() => Interlocked.Increment(ref _counter), TimeSpan.Zero);
    }

    [Benchmark(Baseline = true)]
    public int GetValue_CachedHit()
    {
        return _cachedValueCache.GetValue();
    }

    [Benchmark]
    public int GetValue_ExpiredRefresh()
    {
        return _expiredValueCache.GetValue();
    }
}
