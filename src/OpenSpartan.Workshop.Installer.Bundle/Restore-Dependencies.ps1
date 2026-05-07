<#
.SYNOPSIS
    Downloads runtime installers required by the WiX bundle.

.DESCRIPTION
    Populates the Dependencies/ folder with the .NET Desktop Runtime and the
    Windows App Runtime installers that get embedded into the bundle.
    Run this before building OpenSpartan.Workshop.Installer.Bundle locally
    or in CI; the binaries themselves are intentionally not committed.
#>
$ErrorActionPreference = 'Stop'

$dependenciesDir = Join-Path $PSScriptRoot 'Dependencies'
New-Item -ItemType Directory -Force -Path $dependenciesDir | Out-Null

$downloads = @(
    @{
        Name = 'Windows App Runtime 1.8'
        Url  = 'https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe'
        Out  = Join-Path $dependenciesDir 'WindowsAppRuntimeInstall-x64.exe'
    },
    @{
        Name = '.NET 10 Desktop Runtime (10.0.0)'
        Url  = 'https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.0/windowsdesktop-runtime-10.0.0-win-x64.exe'
        Out  = Join-Path $dependenciesDir 'windowsdesktop-runtime-10.0.0-win-x64.exe'
    }
)

foreach ($d in $downloads) {
    Write-Host "Downloading $($d.Name) -> $($d.Out)"
    Invoke-WebRequest -Uri $d.Url -OutFile $d.Out -UseBasicParsing
}

Write-Host "All bundle dependencies restored."
