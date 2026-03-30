param(
    [string]$Label = (Get-Date -Format "yyyyMMdd-HHmmss"),
    [string]$Filter = "*",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "benchmarks\LibSharp.Benchmarks\LibSharp.Benchmarks.csproj"
$resultsRoot = Join-Path $repoRoot "benchmarks\results"
$artifactsPath = Join-Path $resultsRoot $Label

New-Item -ItemType Directory -Force -Path $artifactsPath | Out-Null

$env:LIBSHARP_BENCHMARK_ARTIFACTS = $artifactsPath
$env:LIBSHARP_BENCHMARK_LABEL = $Label

$gitCommit = "unknown"
$gitDirty = $false
try
{
    $gitCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
    $gitStatus = (& git -C $repoRoot status --porcelain).Trim()
    $gitDirty = -not [string]::IsNullOrWhiteSpace($gitStatus)
}
catch
{
}

if ($gitDirty)
{
    Write-Warning "Working directory has uncommitted changes. Results may not be reproducible against the recorded commit."
}

$metadataPath = Join-Path $artifactsPath "run-metadata.txt"
@(
    "label=$Label"
    "createdUtc=$([DateTime]::UtcNow.ToString('o'))"
    "gitCommit=$gitCommit"
    "gitDirty=$gitDirty"
    "filter=$Filter"
) | Set-Content -Path $metadataPath -Encoding UTF8

$dotnetArgs = @("run", "--project", $projectPath, "-c", "Release")
if ($NoBuild)
{
    $dotnetArgs += "--no-build"
}

$dotnetArgs += "--"
$dotnetArgs += "--filter"
$dotnetArgs += $Filter

Write-Host "Running benchmarks with label '$Label'..."
& dotnet @dotnetArgs

if ($LASTEXITCODE -ne 0)
{
    throw "Benchmark execution failed with exit code $LASTEXITCODE."
}

Write-Host "Benchmark artifacts: $artifactsPath"
