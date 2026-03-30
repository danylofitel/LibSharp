using System;
using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;

namespace LibSharp.Benchmarks
{
    internal static class LibSharpBenchmarkConfig
    {
        public static IConfig Create()
        {
            ManualConfig config = ManualConfig.Create(DefaultConfig.Instance);

            _ = config.AddDiagnoser(MemoryDiagnoser.Default);
            _ = config.AddExporter(JsonExporter.FullCompressed);
            _ = config.AddJob(Job.Default.WithId("Default"));

            config.Options |= ConfigOptions.JoinSummary;
            config.Options |= ConfigOptions.DisableOptimizationsValidator;
            config.ArtifactsPath = ResolveArtifactsPath();

            return config;
        }

        private static string ResolveArtifactsPath()
        {
            string? artifactsFromEnvironment = Environment.GetEnvironmentVariable("LIBSHARP_BENCHMARK_ARTIFACTS");
            if (!string.IsNullOrWhiteSpace(artifactsFromEnvironment))
            {
                return Path.GetFullPath(artifactsFromEnvironment);
            }

            string? label = Environment.GetEnvironmentVariable("LIBSHARP_BENCHMARK_LABEL");
            if (string.IsNullOrWhiteSpace(label))
            {
                label = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            }

            string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            return Path.Combine(repositoryRoot, "benchmarks", "results", label);
        }
    }
}
