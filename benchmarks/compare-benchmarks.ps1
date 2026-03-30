param(
    [Parameter(Mandatory = $true)]
    [string]$Before,

    [Parameter(Mandatory = $true)]
    [string]$After,

    [string]$ResultsRoot
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ResultsRoot))
{
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $ResultsRoot = Join-Path $repoRoot "benchmarks\results"
}

$beforePath = Join-Path $ResultsRoot $Before
$afterPath = Join-Path $ResultsRoot $After

if (-not (Test-Path $beforePath))
{
    throw "Before results folder not found: $beforePath"
}

if (-not (Test-Path $afterPath))
{
    throw "After results folder not found: $afterPath"
}

function Convert-TimeToNanoseconds
{
    param([string]$InputValue)

    if ([string]::IsNullOrWhiteSpace($InputValue) -or $InputValue -eq "-" -or $InputValue -eq "NA")
    {
        return $null
    }

    $normalized = $InputValue.Trim().Replace(",", "")
    if ($normalized -notmatch "^([0-9]*\.?[0-9]+)\s*([a-zA-Zμ]+)$")
    {
        return $null
    }

    $value = [double]::Parse($matches[1], [System.Globalization.CultureInfo]::InvariantCulture)
    $unit = $matches[2]

    switch ($unit)
    {
        "ns" { return $value }
        "us" { return $value * 1000.0 }
        "μs" { return $value * 1000.0 }
        "ms" { return $value * 1000000.0 }
        "s" { return $value * 1000000000.0 }
        default { return $null }
    }
}

function Convert-AllocatedToBytes
{
    param([string]$InputValue)

    if ([string]::IsNullOrWhiteSpace($InputValue) -or $InputValue -eq "-" -or $InputValue -eq "NA")
    {
        return $null
    }

    $normalized = $InputValue.Trim().Replace(",", "")
    if ($normalized -notmatch "^([0-9]*\.?[0-9]+)\s*([kKmMgGtT]?[bB])$")
    {
        return $null
    }

    $value = [double]::Parse($matches[1], [System.Globalization.CultureInfo]::InvariantCulture)
    $unit = $matches[2].ToUpperInvariant()

    switch ($unit)
    {
        "B" { return $value }
        "KB" { return $value * 1024.0 }
        "MB" { return $value * 1024.0 * 1024.0 }
        "GB" { return $value * 1024.0 * 1024.0 * 1024.0 }
        "TB" { return $value * 1024.0 * 1024.0 * 1024.0 * 1024.0 }
        default { return $null }
    }
}

function Get-BenchmarkResults
{
    param([string]$RunPath)

    $resultFiles = Get-ChildItem -Path (Join-Path $RunPath "results") -Filter "*-report.csv" -Recurse
    if ($resultFiles.Count -eq 0)
    {
        throw "No '*-report.csv' files found under $RunPath"
    }

    $map = @{}

    foreach ($resultFile in $resultFiles)
    {
        $rows = Import-Csv -Path $resultFile.FullName

        $fileTypeName = [System.IO.Path]::GetFileNameWithoutExtension($resultFile.Name)
        if ($fileTypeName.EndsWith("-report", [System.StringComparison]::OrdinalIgnoreCase))
        {
            $fileTypeName = $fileTypeName.Substring(0, $fileTypeName.Length - 7)
        }

        foreach ($row in $rows)
        {
            if ([string]::IsNullOrWhiteSpace($row.Method))
            {
                continue
            }

            $typeName = $null
            if ($row.PSObject.Properties.Name -contains "Type" -and -not [string]::IsNullOrWhiteSpace($row.Type))
            {
                $typeName = $row.Type
            }
            elseif ($row.PSObject.Properties.Name -contains "Namespace" -and -not [string]::IsNullOrWhiteSpace($row.Namespace))
            {
                $typeName = $row.Namespace
            }
            else
            {
                $typeName = $fileTypeName
            }

            $benchmarkName = "$typeName.$($row.Method)"
            $map[$benchmarkName] = [PSCustomObject]@{
                Benchmark = $benchmarkName
                MeanRaw = $row.Mean
                MeanNs = Convert-TimeToNanoseconds -InputValue $row.Mean
                AllocRaw = $row.Allocated
                AllocBytes = Convert-AllocatedToBytes -InputValue $row.Allocated
            }
        }
    }

    return $map
}

$beforeResults = Get-BenchmarkResults -RunPath $beforePath
$afterResults = Get-BenchmarkResults -RunPath $afterPath

$allBenchmarkNames = ($beforeResults.Keys + $afterResults.Keys | Sort-Object -Unique)

$comparison = foreach ($benchmarkName in $allBenchmarkNames)
{
    if (-not $beforeResults.ContainsKey($benchmarkName) -or -not $afterResults.ContainsKey($benchmarkName))
    {
        continue
    }

    $beforeResult = $beforeResults[$benchmarkName]
    $afterResult = $afterResults[$benchmarkName]

    $meanDeltaPercent = $null
    if ($null -ne $beforeResult.MeanNs -and $null -ne $afterResult.MeanNs -and $beforeResult.MeanNs -ne 0)
    {
        $meanDeltaPercent = (($afterResult.MeanNs - $beforeResult.MeanNs) / $beforeResult.MeanNs) * 100.0
    }

    $allocDeltaBytes = $null
    if ($null -ne $beforeResult.AllocBytes -and $null -ne $afterResult.AllocBytes)
    {
        $allocDeltaBytes = $afterResult.AllocBytes - $beforeResult.AllocBytes
    }

    [PSCustomObject]@{
        Benchmark = $benchmarkName
        BeforeMean = $beforeResult.MeanRaw
        AfterMean = $afterResult.MeanRaw
        MeanDeltaPercent = if ($null -ne $meanDeltaPercent) { [Math]::Round($meanDeltaPercent, 2) } else { $null }
        BeforeAllocated = $beforeResult.AllocRaw
        AfterAllocated = $afterResult.AllocRaw
        AllocDeltaBytes = if ($null -ne $allocDeltaBytes) { [Math]::Round($allocDeltaBytes, 2) } else { $null }
    }
}

$comparison = $comparison | Sort-Object Benchmark
if ($comparison.Count -eq 0)
{
    throw "No overlapping benchmarks between '$Before' and '$After'."
}

$outputPath = Join-Path $ResultsRoot ("comparison-{0}-vs-{1}.csv" -f $After, $Before)
$comparison | Export-Csv -Path $outputPath -NoTypeInformation -Encoding UTF8

Write-Host "Comparison written to: $outputPath"
$comparison |
    Select-Object Benchmark, BeforeMean, AfterMean, MeanDeltaPercent, BeforeAllocated, AfterAllocated, AllocDeltaBytes |
    Format-Table -AutoSize
