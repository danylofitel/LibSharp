// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using LibSharp.Caching;

namespace LibSharp.Benchmarks.Benchmarks;

public class KeyValueCacheBenchmarks
{
    private KeyValueCache<int, int> _keyValueCache = null!;

    private int _keyCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keyValueCache = new KeyValueCache<int, int>(key => key + 1, TimeSpan.FromMinutes(10));
        _ = _keyValueCache.GetValue(0);
    }

    [Benchmark(Baseline = true)]
    public int GetValue_HotKey()
    {
        return _keyValueCache.GetValue(0);
    }

    [Benchmark]
    public int GetValue_RotatingBoundedKeys()
    {
        int key = Interlocked.Increment(ref _keyCounter) & 1023;
        return _keyValueCache.GetValue(key);
    }
}
