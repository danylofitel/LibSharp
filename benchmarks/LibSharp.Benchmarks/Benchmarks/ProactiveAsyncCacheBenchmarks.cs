// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LibSharp.Caching;

namespace LibSharp.Benchmarks.Benchmarks;

public class ProactiveAsyncCacheBenchmarks
{
    private static readonly Func<CancellationToken, Task<int>> s_valueFactory = _ => Task.FromResult(42);

    private ProactiveAsyncCache<int> _proactiveAsyncCache = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        TimeSpan refreshInterval = TimeSpan.FromSeconds(1);

        _proactiveAsyncCache = new ProactiveAsyncCache<int>(
            s_valueFactory,
            new ProactiveAsyncCacheOptions
            {
                RefreshInterval = refreshInterval,
                PreFetchOffset = refreshInterval / 2,
            });

        _ = await _proactiveAsyncCache.GetValueAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _proactiveAsyncCache.DisposeAsync().ConfigureAwait(false);
    }

    [Benchmark(Baseline = true)]
    public ValueTask<int> ProactiveAsyncCache_FreshHit()
    {
        return _proactiveAsyncCache.GetValueAsync(CancellationToken.None);
    }
}
