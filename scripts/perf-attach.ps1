# perf-attach.ps1 - Convenience wrapper for attach profiling
# Attaches to a running OpenSpartan.Workshop process
#
# Usage:
#   .\perf-attach.ps1                       # Lists processes and prompts for selection
#   .\perf-attach.ps1 -ProcessId 1234       # Attach to specific PID
#   .\perf-attach.ps1 -Duration 10          # Profile for 10 seconds

param(
    [int]$ProcessId,

    [int]$Duration = 30,

    [string]$OutputPath,

    [int]$SamplingHz = 8190,

    [bool]$OpenBrowser = $true
)

$scriptPath = Join-Path $PSScriptRoot "perf-trace.ps1"

if (-not (Test-Path $scriptPath)) {
    Write-Host "Error: perf-trace.ps1 not found at $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "=== Attach Profiling ===" -ForegroundColor Magenta

# If no ProcessId provided, list available processes
if (-not $ProcessId) {
    Write-Host "Searching for OpenSpartan.Workshop processes...`n" -ForegroundColor Cyan

    $processes = Get-Process -Name "OpenSpartan.Workshop" -ErrorAction SilentlyContinue

    if (-not $processes -or $processes.Count -eq 0) {
        Write-Host "No OpenSpartan.Workshop processes found." -ForegroundColor Yellow
        Write-Host "Start the application first, then run this script again." -ForegroundColor White
        Write-Host "`nAlternatively, use -ProcessId to specify a different process." -ForegroundColor White
        exit 1
    }

    Write-Host "Found OpenSpartan.Workshop processes:" -ForegroundColor Green
    Write-Host ""
    Write-Host "  PID`t`tStart Time`t`t`tMemory (MB)" -ForegroundColor White
    Write-Host "  ---`t`t----------`t`t`t----------" -ForegroundColor White

    foreach ($proc in $processes) {
        $memMB = [math]::Round($proc.WorkingSet64 / 1MB, 1)
        $startTime = $proc.StartTime.ToString("yyyy-MM-dd HH:mm:ss")
        Write-Host "  $($proc.Id)`t`t$startTime`t`t$memMB" -ForegroundColor Cyan
    }

    Write-Host ""

    if ($processes.Count -eq 1) {
        $ProcessId = $processes[0].Id
        Write-Host "Auto-selecting the only available process (PID: $ProcessId)`n" -ForegroundColor Green
    }
    else {
        $inputPid = Read-Host "Enter the PID to profile"
        if (-not $inputPid -or -not ($inputPid -match '^\d+$')) {
            Write-Host "Invalid PID entered." -ForegroundColor Red
            exit 1
        }
        $ProcessId = [int]$inputPid
    }
}

Write-Host "Attaching to process with PID: $ProcessId`n" -ForegroundColor White

$args = @{
    Mode = "Attach"
    ProcessId = $ProcessId
    Duration = $Duration
    SamplingHz = $SamplingHz
    OpenBrowser = $OpenBrowser
}

if ($OutputPath) {
    $args.OutputPath = $OutputPath
}

& $scriptPath @args
