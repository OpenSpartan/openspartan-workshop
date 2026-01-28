# trace-startup.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,
    
    [string]$OutputPath = "trace.nettrace",
    [int]$BufferSizeMB = 256,
    [string]$Providers = "Microsoft-DotNETCore-SampleProfiler,Microsoft-Windows-DotNETRuntime:0x1F000000001:5"
)

$env:DOTNET_EnableEventPipe = "1"
$env:DOTNET_EventPipeOutputPath = $OutputPath
$env:DOTNET_EventPipeCircularMB = $BufferSizeMB
$env:DOTNET_EventPipeProviders = $Providers

Write-Host "Starting app with tracing enabled..." -ForegroundColor Cyan
Write-Host "Trace will be saved to: $OutputPath" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the app and finalize the trace.`n" -ForegroundColor Yellow

try {
    dotnet run --project $ProjectPath
}
finally {
    # Clean up environment
    Remove-Item Env:DOTNET_EnableEventPipe -ErrorAction SilentlyContinue
    Remove-Item Env:DOTNET_EventPipeOutputPath -ErrorAction SilentlyContinue
    Remove-Item Env:DOTNET_EventPipeCircularMB -ErrorAction SilentlyContinue
    Remove-Item Env:DOTNET_EventPipeProviders -ErrorAction SilentlyContinue
    
    if (Test-Path $OutputPath) {
        $size = (Get-Item $OutputPath).Length / 1MB
        Write-Host "`nTrace saved: $OutputPath ($([math]::Round($size, 2)) MB)" -ForegroundColor Green
    }
}