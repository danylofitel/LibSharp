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

    private ProactiveAsyncCache<int> m_proactiveAsyncCache = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        TimeSpan refreshInterval = TimeSpan.FromSeconds(1);

        m_proactiveAsyncCache = new ProactiveAsyncCache<int>(
            s_valueFactory,
            refreshInterval,
            preFetchOffset: refreshInterval / 2,
            allowStaleReads: false);

        _ = await m_proactiveAsyncCache.GetValueAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await m_proactiveAsyncCache.DisposeAsync().ConfigureAwait(false);
    }

    [Benchmark(Baseline = true)]
    public Task<int> ProactiveAsyncCache_FreshHit()
    {
        return m_proactiveAsyncCache.GetValueAsync(CancellationToken.None);
    }
}
