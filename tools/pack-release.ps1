#Requires -Version 5.1
<#
.SYNOPSIS
  Publish the unpackaged WinUI Hub and zip it for a GitHub Release.
  Does not include QEMU or the Guest ISO.
#>
param(
    [string]$Version = "0.0.0-dev",
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$publishDir = Join-Path $repoRoot "publish\win-x64"
$zipName = "AndroidTvHub-win-x64-$Version.zip"
$zipPath = Join-Path $repoRoot (Join-Path $OutputDir $zipName)

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishDir, (Join-Path $repoRoot $OutputDir) | Out-Null

dotnet publish (Join-Path $repoRoot "src\AndroidTvHub\AndroidTvHub.csproj") `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:Platform=x64 `
    -p:Version=$Version `
    -p:InformationalVersion=$Version `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

$exe = Join-Path $publishDir "AndroidTvHub.exe"
if (-not (Test-Path $exe)) {
    throw "Publish succeeded but AndroidTvHub.exe was not found at $exe"
}

Copy-Item (Join-Path $repoRoot "LICENSE") $publishDir -Force
Copy-Item (Join-Path $repoRoot "THIRD-PARTY-NOTICES.md") $publishDir -Force

$install = @"
Android TV Hub $Version
Unpackaged Windows x64 App Player for Android TV OS.

This zip does not include QEMU or a Guest ISO. The Hub downloads the pinned
Lineage TV image at runtime. Install QEMU separately:

  winget install SoftwareFreedomConservancy.QEMU

Requirements: Windows 10 2004+ or Windows 11, x64, virtualization + WHPX.
See README.md in the source repository.
"@
Set-Content -Path (Join-Path $publishDir "INSTALL.txt") -Value $install -Encoding UTF8

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
Write-Host "Wrote $zipPath ($((Get-Item $zipPath).Length) bytes)"
