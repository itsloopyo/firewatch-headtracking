#!/usr/bin/env pwsh
# Stages MelonLoader's DLLs into src/FirewatchHeadTracking/libs/ from the committed
# vendor zip. The Unity reference stubs are a separate step: `pixi run setup` runs
# this first, then compiles them from the shared sources in
# cameraunlock-core/csharp/stubs. No Firewatch installation is needed for either.

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libsPath    = Join-Path $projectRoot 'src\FirewatchHeadTracking\libs'
$vendorZip   = Join-Path $projectRoot 'vendor\melonloader\MelonLoader.x64.zip'

if (-not (Test-Path $vendorZip)) { throw "Vendored MelonLoader not found at $vendorZip" }

New-Item -ItemType Directory -Path $libsPath -Force | Out-Null

Write-Host "Staging MelonLoader references..." -ForegroundColor Cyan

# Extracted to a scratch dir first so a half-written libs/ never becomes the input to a
# build: an empty libs/ resolves no references and buries the cause under ~390 CS0246s.
$stageDir = Join-Path $env:TEMP ("fw-libs-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

try {
    # MelonLoader 0.5.7 flat layout: DLLs sit directly in MelonLoader/ inside the zip.
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($vendorZip, $stageDir)
    foreach ($dll in @('MelonLoader.dll', '0Harmony.dll')) {
        $src = Join-Path $stageDir "MelonLoader\$dll"
        if (-not (Test-Path $src)) { throw "$dll not found in vendor zip at MelonLoader\" }
        Copy-Item $src (Join-Path $libsPath $dll) -Force
        Write-Host "  MelonLoader: $dll" -ForegroundColor Gray
    }
} finally {
    Remove-Item $stageDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "MelonLoader references staged." -ForegroundColor Green
