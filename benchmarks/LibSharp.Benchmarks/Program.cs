// Copyright (c) 2026 Danylo Fitel

using BenchmarkDotNet.Running;

namespace LibSharp.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        _ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args, LibSharpBenchmarkConfig.Create());
    }
}
