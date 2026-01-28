# perf-trace.ps1 - Main Ultra profiling script
# Usage examples:
#   Startup: .\perf-trace.ps1 -Mode Startup -Duration 30
#   Attach:  .\perf-trace.ps1 -Mode Attach -ProcessId 1234

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Startup", "Attach")]
    [string]$Mode,

    [int]$Duration = 30,

    [int]$ProcessId,

    [string]$OutputPath,

    [int]$SamplingHz = 8190,

    [bool]$OpenBrowser = $true,

    [string]$Configuration = "Release",

    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

# Constants
$ProjectPath = Join-Path $PSScriptRoot "..\src\OpenSpartan.Workshop\OpenSpartan.Workshop.csproj"
$TracesDir = Join-Path $PSScriptRoot "..\traces"
$FirefoxProfilerUrl = "https://profiler.firefox.com/from-url/"

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-UltraTool {
    Write-Host "Checking for Ultra profiler..." -ForegroundColor Cyan

    $ultraPath = Get-Command ultra -ErrorAction SilentlyContinue
    if (-not $ultraPath) {
        Write-Host "Ultra not found. Installing via dotnet tool..." -ForegroundColor Yellow
        dotnet tool install -g ultra

        # Refresh PATH to pick up the new tool
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

        $ultraPath = Get-Command ultra -ErrorAction SilentlyContinue
        if (-not $ultraPath) {
            throw "Failed to install Ultra. Please install manually: dotnet tool install -g ultra"
        }
        Write-Host "Ultra installed successfully." -ForegroundColor Green
    } else {
        Write-Host "Ultra found at: $($ultraPath.Source)" -ForegroundColor Green
    }
}

function Get-OutputFilePath {
    param([string]$ProvidedPath)

    if ($ProvidedPath) {
        return $ProvidedPath
    }

    # Create traces directory if needed
    if (-not (Test-Path $TracesDir)) {
        New-Item -ItemType Directory -Path $TracesDir -Force | Out-Null
        Write-Host "Created traces directory: $TracesDir" -ForegroundColor Cyan
    }

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    return Join-Path $TracesDir "profile-$timestamp.json.gz"
}

function Open-FirefoxProfiler {
    param([string]$TracePath)

    if (-not $OpenBrowser) {
        return
    }

    $absolutePath = Resolve-Path $TracePath
    Write-Host "`nTo view the trace in Firefox Profiler:" -ForegroundColor Cyan
    Write-Host "  1. Open https://profiler.firefox.com/" -ForegroundColor White
    Write-Host "  2. Click 'Load a profile from file'" -ForegroundColor White
    Write-Host "  3. Select: $absolutePath" -ForegroundColor Yellow
    Write-Host "`nOr drag and drop the file onto the Firefox Profiler page." -ForegroundColor White
}

function Start-StartupProfiling {
    param(
        [string]$OutputFile,
        [int]$DurationSeconds,
        [int]$SampleRate
    )

    Write-Host "`nBuilding project in $Configuration mode..." -ForegroundColor Cyan
    dotnet build $ProjectPath -c $Configuration -p:Platform=$Platform --nologo -v q

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }

    # Find the built executable
    $outputDir = Join-Path (Split-Path $ProjectPath -Parent) "bin\$Platform\$Configuration\net10.0-windows10.0.26100.0"
    $exePath = Join-Path $outputDir "OpenSpartan.Workshop.exe"

    if (-not (Test-Path $exePath)) {
        # Try alternate path without platform folder
        $outputDir = Join-Path (Split-Path $ProjectPath -Parent) "bin\$Configuration\net10.0-windows10.0.26100.0\win-$Platform"
        $exePath = Join-Path $outputDir "OpenSpartan.Workshop.exe"
    }

    if (-not (Test-Path $exePath)) {
        throw "Could not find built executable. Expected at: $exePath"
    }

    Write-Host "Starting profiling session..." -ForegroundColor Cyan
    Write-Host "  Executable: $exePath" -ForegroundColor White
    Write-Host "  Duration: $DurationSeconds seconds" -ForegroundColor White
    Write-Host "  Sampling rate: $SampleRate Hz" -ForegroundColor White
    Write-Host "  Output: $OutputFile" -ForegroundColor White
    Write-Host "`nPress Ctrl+C to stop early.`n" -ForegroundColor Yellow

    # Run Ultra with the executable
    $ultraArgs = @(
        "profile"
        "--duration", $DurationSeconds
        "--sampling-interval-us", [math]::Round(1000000 / $SampleRate)
        "--output", $OutputFile
        "--"
        $exePath
    )

    & ultra @ultraArgs
}

function Start-AttachProfiling {
    param(
        [int]$PID,
        [string]$OutputFile,
        [int]$DurationSeconds,
        [int]$SampleRate
    )

    # Verify process exists
    $process = Get-Process -Id $PID -ErrorAction SilentlyContinue
    if (-not $process) {
        throw "Process with ID $PID not found"
    }

    Write-Host "`nAttaching to process..." -ForegroundColor Cyan
    Write-Host "  Process: $($process.ProcessName) (PID: $PID)" -ForegroundColor White
    Write-Host "  Duration: $DurationSeconds seconds" -ForegroundColor White
    Write-Host "  Sampling rate: $SampleRate Hz" -ForegroundColor White
    Write-Host "  Output: $OutputFile" -ForegroundColor White
    Write-Host "`nPress Ctrl+C to stop early.`n" -ForegroundColor Yellow

    # Run Ultra attached to the process
    $ultraArgs = @(
        "profile"
        "--pid", $PID
        "--duration", $DurationSeconds
        "--sampling-interval-us", [math]::Round(1000000 / $SampleRate)
        "--output", $OutputFile
    )

    & ultra @ultraArgs
}

# Main execution
Write-Host "=== OpenSpartan Workshop Performance Profiler ===" -ForegroundColor Magenta
Write-Host "Using Ultra profiler for full native/kernel/managed stack traces`n" -ForegroundColor White

# Check for admin rights (required for ETW)
if (-not (Test-Administrator)) {
    Write-Host "WARNING: Not running as Administrator." -ForegroundColor Yellow
    Write-Host "ETW-based profiling requires elevated privileges for full stack traces." -ForegroundColor Yellow
    Write-Host "Some features may be limited. Consider restarting PowerShell as Administrator.`n" -ForegroundColor Yellow
}

# Install Ultra if needed
Install-UltraTool

# Determine output path
$outputFile = Get-OutputFilePath -ProvidedPath $OutputPath

try {
    if ($Mode -eq "Startup") {
        Start-StartupProfiling -OutputFile $outputFile -DurationSeconds $Duration -SampleRate $SamplingHz
    }
    elseif ($Mode -eq "Attach") {
        if (-not $ProcessId) {
            throw "ProcessId is required for Attach mode. Use -ProcessId <PID>"
        }
        Start-AttachProfiling -PID $ProcessId -OutputFile $outputFile -DurationSeconds $Duration -SampleRate $SamplingHz
    }
}
catch {
    Write-Host "`nError during profiling: $_" -ForegroundColor Red
    exit 1
}

# Check if trace was created
if (Test-Path $outputFile) {
    $size = (Get-Item $outputFile).Length / 1MB
    Write-Host "`n=== Profiling Complete ===" -ForegroundColor Green
    Write-Host "Trace saved: $outputFile" -ForegroundColor Green
    Write-Host "Size: $([math]::Round($size, 2)) MB" -ForegroundColor Green

    Open-FirefoxProfiler -TracePath $outputFile
}
else {
    Write-Host "`nWarning: No trace file was created at $outputFile" -ForegroundColor Yellow
}
