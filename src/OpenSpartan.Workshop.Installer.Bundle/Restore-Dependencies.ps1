<#
.SYNOPSIS
    Downloads runtime installers required by the WiX bundle for a specific
    target architecture.

.PARAMETER Arch
    Target architecture: x64 (default), ARM64, or x86.

.DESCRIPTION
    Populates the Dependencies/ folder with the .NET Desktop Runtime and the
    Windows App Runtime installers for the requested architecture. Run this
    before building OpenSpartan.Workshop.Installer.Bundle locally or in CI;
    the binaries themselves are intentionally not committed.
#>
param(
    [ValidateSet('x64', 'ARM64', 'x86')]
    [string]$Arch = 'x64'
)

$ErrorActionPreference = 'Stop'

$archLower = $Arch.ToLowerInvariant()
$dependenciesDir = Join-Path $PSScriptRoot 'Dependencies'
New-Item -ItemType Directory -Force -Path $dependenciesDir | Out-Null

$downloads = @(
    @{
        Name = "Windows App Runtime 1.8 ($Arch)"
        Url  = "https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-$archLower.exe"
        Out  = Join-Path $dependenciesDir "WindowsAppRuntimeInstall-$archLower.exe"
    },
    @{
        Name = ".NET 10 Desktop Runtime 10.0.0 ($Arch)"
        Url  = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.0/windowsdesktop-runtime-10.0.0-win-$archLower.exe"
        Out  = Join-Path $dependenciesDir "windowsdesktop-runtime-10.0.0-win-$archLower.exe"
    }
)

foreach ($d in $downloads) {
    Write-Host "Downloading $($d.Name) -> $($d.Out)"
    Invoke-WebRequest -Uri $d.Url -OutFile $d.Out -UseBasicParsing
}

Write-Host "Bundle dependencies for $Arch restored."
