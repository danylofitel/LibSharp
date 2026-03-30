using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using LibSharp.Caching;

namespace LibSharp.Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    public class KeyValueCacheBenchmarks
    {
        private KeyValueCache<int, int> m_keyValueCache = null!;

        private int m_keyCounter;

        [GlobalSetup]
        public void GlobalSetup()
        {
            m_keyValueCache = new KeyValueCache<int, int>(key => key + 1, TimeSpan.FromMinutes(10));
            _ = m_keyValueCache.GetValue(0);
        }

        [Benchmark(Baseline = true)]
        public int GetValue_HotKey()
        {
            return m_keyValueCache.GetValue(0);
        }

        [Benchmark]
        public int GetValue_RotatingBoundedKeys()
        {
            int key = Interlocked.Increment(ref m_keyCounter) & 1023;
            return m_keyValueCache.GetValue(key);
        }
    }
}
