# Benchmarks

This folder contains a BenchmarkDotNet project and helper scripts for repeatable performance tracking.

## Project

- Benchmark project: `benchmarks/LibSharp.Benchmarks/LibSharp.Benchmarks.csproj`
- Initial benchmark suites:
  - `AsyncLockBenchmarks`
  - `ValueCacheBenchmarks`
  - `KeyValueCacheBenchmarks`
  - `PriorityQueueBenchmarks`

## Run benchmarks

Use PowerShell from repository root:

```powershell
./benchmarks/run-benchmarks.ps1 -Label before-change
./benchmarks/run-benchmarks.ps1 -Label after-change
```

Optional parameters:

- `-Filter` benchmark filter (default `*`)
- `-NoBuild` skip build if already built in `Release`

Examples:

```powershell
./benchmarks/run-benchmarks.ps1 -Label before -Filter "*AsyncLock*"
./benchmarks/run-benchmarks.ps1 -Label after -Filter "*AsyncLock*" -NoBuild
```

## Compare runs

After collecting two labeled runs:

```powershell
./benchmarks/compare-benchmarks.ps1 -Before before-change -After after-change
```

This command:

- reads BenchmarkDotNet CSV reports from both labels,
- computes mean-time delta (%) and allocated-bytes delta,
- prints a table,
- writes a CSV summary under `benchmarks/results`.

## Output layout

Run artifacts are stored in:

- `benchmarks/results/<label>/`

Each run also includes `run-metadata.txt` with commit hash and timestamp.
