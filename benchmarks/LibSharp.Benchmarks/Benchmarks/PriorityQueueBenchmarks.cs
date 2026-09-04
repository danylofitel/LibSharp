// Copyright (c) 2026 Danylo Fitel

using System;
using BenchmarkDotNet.Attributes;
using LibSharp.Collections;

namespace LibSharp.Benchmarks.Benchmarks;

public class PriorityQueueBenchmarks
{
    private int[] _values = null!;

    [Params(128, 1024)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        Random random = new Random(12345);
        _values = new int[ItemCount];

        for (int i = 0; i < _values.Length; i++)
        {
            _values[i] = random.Next();
        }
    }

    [Benchmark(Baseline = true)]
    public int MinPriorityQueue_EnqueueAndDrain()
    {
        MinPriorityQueue<int> queue = new MinPriorityQueue<int>(ItemCount);

        for (int i = 0; i < _values.Length; i++)
        {
            queue.Enqueue(_values[i]);
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

        for (int i = 0; i < _values.Length; i++)
        {
            queue.Enqueue(_values[i]);
        }

        int checksum = 0;
        while (queue.Count > 0)
        {
            checksum ^= queue.Dequeue();
        }

        return checksum;
    }
}
