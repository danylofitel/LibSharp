// Copyright (c) 2026 Danylo Fitel

using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LibSharp.Caching;

namespace LibSharp.Benchmarks.Benchmarks;

public class ProactiveAsyncCacheWarmHitBenchmarks
{
    private static readonly Func<CancellationToken, Task<int>> s_valueFactory = _ => Task.FromResult(42);
    private static readonly TimeSpan s_largeRefreshInterval = TimeSpan.FromMinutes(10);

    private ProactiveAsyncCache<int> _proactiveAsyncCache = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _proactiveAsyncCache = new ProactiveAsyncCache<int>(
            s_valueFactory,
            new ProactiveAsyncCacheOptions
            {
                RefreshInterval = s_largeRefreshInterval,
                PreFetchOffset = s_largeRefreshInterval / 2,
            });

        _ = await _proactiveAsyncCache.GetValueAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _proactiveAsyncCache.DisposeAsync().ConfigureAwait(false);
    }

    [Benchmark(Baseline = true)]
    public ValueTask<int> ProactiveAsyncCache_WarmHit()
    {
        return _proactiveAsyncCache.GetValueAsync(CancellationToken.None);
    }
}
