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

    private ProactiveAsyncCache<int> m_proactiveAsyncCache = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        m_proactiveAsyncCache = new ProactiveAsyncCache<int>(
            s_valueFactory,
            s_largeRefreshInterval,
            preFetchOffset: s_largeRefreshInterval / 2,
            allowStaleReads: false);

        _ = await m_proactiveAsyncCache.GetValueAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await m_proactiveAsyncCache.DisposeAsync().ConfigureAwait(false);
    }

    [Benchmark(Baseline = true)]
    public Task<int> ProactiveAsyncCache_WarmHit()
    {
        return m_proactiveAsyncCache.GetValueAsync(CancellationToken.None);
    }
}
