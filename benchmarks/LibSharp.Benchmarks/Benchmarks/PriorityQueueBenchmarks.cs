// Copyright (c) 2026 Danylo Fitel

using System;
using BenchmarkDotNet.Attributes;
using LibSharp.Collections;

namespace LibSharp.Benchmarks.Benchmarks;

public class PriorityQueueBenchmarks
{
    private int[] m_values = null!;

    [Params(128, 1024)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        Random random = new Random(12345);
        m_values = new int[ItemCount];

        for (int i = 0; i < m_values.Length; i++)
        {
            m_values[i] = random.Next();
        }
    }

    [Benchmark(Baseline = true)]
    public int MinPriorityQueue_EnqueueAndDrain()
    {
        MinPriorityQueue<int> queue = new MinPriorityQueue<int>(ItemCount);

        for (int i = 0; i < m_values.Length; i++)
        {
            queue.Enqueue(m_values[i]);
        }

        int checksum = 0;
        while (queue.Count > 0)
        {
            checksum ^= queue.Dequeue();
        }

        return checksum;
    }

    [Benchmark]
    public int MaxPriorityQueue_EnqueueAndDrain()
    {
        MaxPriorityQueue<int> queue = new MaxPriorityQueue<int>(ItemCount);

        for (int i = 0; i < m_values.Length; i++)
        {
            queue.Enqueue(m_values[i]);
        }

        int checksum = 0;
        while (queue.Count > 0)
        {
            checksum ^= queue.Dequeue();
        }

        return checksum;
    }
}
