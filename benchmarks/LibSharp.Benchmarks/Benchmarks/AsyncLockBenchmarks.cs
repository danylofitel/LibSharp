using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LibSharp.Common;

namespace LibSharp.Benchmarks.Benchmarks
{
    public class AsyncLockBenchmarks
    {
        private AsyncLock m_asyncLock = null!;
        private CancellationTokenSource m_cancellationTokenSource = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            m_asyncLock = new AsyncLock();
            m_cancellationTokenSource = new CancellationTokenSource();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            m_cancellationTokenSource.Dispose();
            m_asyncLock.Dispose();
        }

        [Benchmark(Baseline = true)]
        public async Task AcquireRelease_DefaultToken()
        {
            using (await m_asyncLock.AcquireAsync().ConfigureAwait(false))
            {
            }
        }

        [Benchmark]
        public async Task AcquireRelease_CancelableToken()
        {
            using (await m_asyncLock.AcquireAsync(m_cancellationTokenSource.Token).ConfigureAwait(false))
            {
            }
        }
    }
}
