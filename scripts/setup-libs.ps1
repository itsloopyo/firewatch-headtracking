#!/usr/bin/env pwsh
#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libsDir = Join-Path $projectRoot "src\FirewatchHeadTracking\libs"

# Import shared modules
$modulePath = Join-Path $projectRoot "cameraunlock-core\powershell\GamePathDetection.psm1"
Import-Module $modulePath -Force
$modLoaderPath = Join-Path $projectRoot "cameraunlock-core\powershell\ModLoaderSetup.psm1"
Import-Module $modLoaderPath -Force

$gameId = 'Firewatch'
$config = Get-GameConfig -GameId $gameId

# Find game installation
$gamePath = Find-GamePath -GameId $gameId

if (-not $gamePath) {
    Write-GameNotFoundError -GameName 'Firewatch' -EnvVar $config.EnvVar -SteamFolder $config.SteamFolder
    exit 1
}

Write-Host "Found game installation at: $gamePath" -ForegroundColor Green

# Find the Managed folder (contains game DLLs)
$managedPath = Get-ManagedPath -GamePath $gamePath -DataFolder $config.DataFolder

if (-not (Test-Path $managedPath)) {
    Write-Host "ERROR: Managed folder not found at: $managedPath" -ForegroundColor Red
    Write-Host "The game installation may be corrupted. Try verifying game files."
    exit 1
}

Write-Host "Found Managed folder at: $managedPath" -ForegroundColor Green

# Install MelonLoader if missing (0.5.7 x64 — 0.6.x crashes on Unity 2017 Mono)
if (-not (Test-MelonLoaderInstalled -GamePath $gamePath)) {
    Install-MelonLoader -GamePath $gamePath -Architecture x64 -Version '0.5.7'
} else {
    Write-Host "Found MelonLoader at: $(Join-Path $gamePath 'MelonLoader')" -ForegroundColor Green
}

# MelonLoader 0.5.7 flat layout: DLLs directly in MelonLoader/
$melonLoaderLibPath = Join-Path $gamePath "MelonLoader"

# Required DLLs from Managed folder
$managedDlls = @(
    "Assembly-CSharp.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.PhysicsModule.dll",
    "UnityEngine.TextRenderingModule.dll",
    "UnityEngine.UIModule.dll",
    "UnityEngine.UI.dll"
)

# Required DLLs from MelonLoader folder
$melonDlls = @(
    "MelonLoader.dll",
    "0Harmony.dll"
)

# Check if all libs already exist and are up-to-date
$stale = @($managedDlls | Where-Object {
    $dest = Join-Path $libsDir $_
    $src = Join-Path $managedPath $_
    -not (Test-Path $dest) -or (Get-Item $src).LastWriteTime -gt (Get-Item $dest).LastWriteTime
})
$staleMelon = @($melonDlls | Where-Object {
    $dest = Join-Path $libsDir $_
    $src = Join-Path $melonLoaderLibPath $_
    -not (Test-Path $dest) -or (Get-Item $src).LastWriteTime -gt (Get-Item $dest).LastWriteTime
})

if ((Test-Path $libsDir) -and $stale.Count -eq 0 -and $staleMelon.Count -eq 0) {
    Write-Host "All libs are up-to-date, skipping copy." -ForegroundColor Green
    exit 0
}

# Create libs directory if it doesn't exist
if (-not (Test-Path $libsDir)) {
    New-Item -ItemType Directory -Path $libsDir -Force | Out-Null
    Write-Host "Created libs directory: $libsDir" -ForegroundColor Green
}

# Copy managed DLLs
$copyCount = 0
foreach ($dll in $managedDlls) {
    $sourcePath = Join-Path $managedPath $dll
    $destPath = Join-Path $libsDir $dll

    if (-not (Test-Path $sourcePath)) {
        Write-Host "ERROR: Required DLL not found: $sourcePath" -ForegroundColor Red
        exit 1
    }

    Copy-Item -Path $sourcePath -Destination $destPath -Force
    Write-Host "Copied: $dll (Managed)" -ForegroundColor Cyan
    $copyCount++
}

# Copy MelonLoader DLLs
foreach ($dll in $melonDlls) {
    $sourcePath = Join-Path $melonLoaderLibPath $dll
    $destPath = Join-Path $libsDir $dll

    if (-not (Test-Path $sourcePath)) {
        Write-Host "ERROR: Required DLL not found: $sourcePath" -ForegroundColor Red
        exit 1
    }

    Copy-Item -Path $sourcePath -Destination $destPath -Force
    Write-Host "Copied: $dll (MelonLoader)" -ForegroundColor Cyan
    $copyCount++
}

Write-Host ""
Write-Host "SUCCESS: Copied $copyCount DLLs to libs/" -ForegroundColor Green
Write-Host "You can now build the project with: pixi run build"
