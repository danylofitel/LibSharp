// Copyright (c) 2026 Danylo Fitel

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LibSharp.Threading;

namespace LibSharp.Benchmarks.Benchmarks;

public class AsyncLockBenchmarks
{
    private AsyncLock _asyncLock = null!;
    private CancellationTokenSource _cancellationTokenSource = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _asyncLock = new AsyncLock();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _cancellationTokenSource.Dispose();
        _asyncLock.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task AcquireRelease_DefaultToken()
    {
        using (await _asyncLock.AcquireAsync().ConfigureAwait(false))
        {
        }
    }

    [Benchmark]
    public async Task AcquireRelease_CancelableToken()
    {
        using (await _asyncLock.AcquireAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
        {
        }
    }
}
