# perf-startup.ps1 - Convenience wrapper for startup profiling
# Builds the project and profiles application startup
#
# Usage:
#   .\perf-startup.ps1                      # Profile for 30 seconds (default)
#   .\perf-startup.ps1 -Duration 10         # Profile for 10 seconds
#   .\perf-startup.ps1 -Configuration Debug # Use Debug build

param(
    [int]$Duration = 30,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x86", "x64", "ARM64")]
    [string]$Platform = "x64",

    [string]$OutputPath,

    [int]$SamplingHz = 8190,

    [bool]$OpenBrowser = $true
)

$scriptPath = Join-Path $PSScriptRoot "perf-trace.ps1"

if (-not (Test-Path $scriptPath)) {
    Write-Host "Error: perf-trace.ps1 not found at $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "=== Startup Profiling ===" -ForegroundColor Magenta
Write-Host "This will build and launch the application with profiling enabled.`n" -ForegroundColor White

$args = @{
    Mode = "Startup"
    Duration = $Duration
    Configuration = $Configuration
    Platform = $Platform
    SamplingHz = $SamplingHz
    OpenBrowser = $OpenBrowser
}

if ($OutputPath) {
    $args.OutputPath = $OutputPath
}

& $scriptPath @args
