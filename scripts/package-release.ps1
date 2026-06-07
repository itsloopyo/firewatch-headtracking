#!/usr/bin/env pwsh
#Requires -Version 5.1
# Package release ZIPs for Firewatch Head Tracking (MelonLoader mod).
#
# Produces:
#   release/FirewatchHeadTracking-v<version>-installer.zip   (GitHub release: install.cmd + plugins/ + vendor/ + docs)
#   release/FirewatchHeadTracking-v<version>-nexus.zip       (Nexus: extract-to-game-folder layout)
#
# Version is read from the .csproj <Version> element (canonical source).
# See ~/.claude/CLAUDE.md "Build & Release" / "Vendoring Third-Party Dependencies".

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ReleaseWorkflow.psm1") -Force

$csprojPath = Join-Path $projectDir "src\FirewatchHeadTracking\FirewatchHeadTracking.csproj"
$version = Get-CsprojVersion $csprojPath

Write-Host "=== Firewatch Head Tracking - Package Release ===" -ForegroundColor Magenta
Write-Host ""
Write-Host "Version: $version" -ForegroundColor Cyan
Write-Host ""

# Per the vendor-as-install-source doctrine: package consumes whatever is
# committed under vendor/. Bumping is a deliberate `pixi run update-deps`
# step the dev runs before tagging. CI never touches the network here.
$vendorMlDir = Join-Path $projectDir "vendor\melonloader"
$vendorMlZip = Join-Path $vendorMlDir "MelonLoader.x64.zip"
if (-not (Test-Path $vendorMlZip)) {
    throw "Bundled MelonLoader missing: $vendorMlZip. Run 'pixi run update-deps' to populate it."
}

$releaseDir = Join-Path $projectDir "release"
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

$buildOutputDir = Join-Path $projectDir "src\FirewatchHeadTracking\bin\Release\net35"
$modDll = "FirewatchHeadTracking.dll"
$libDlls = @("CameraUnlock.Core.dll", "CameraUnlock.Core.Unity.dll")
$allDlls = @($modDll) + $libDlls

foreach ($dll in $allDlls) {
    $dllPath = Join-Path $buildOutputDir $dll
    if (-not (Test-Path $dllPath)) {
        throw "$dll not found at: $dllPath. Run 'pixi run build' first."
    }
}

$scriptsDir = Join-Path $projectDir "scripts"
foreach ($script in @("install.cmd", "uninstall.cmd")) {
    if (-not (Test-Path (Join-Path $scriptsDir $script))) {
        throw "Required script not found: scripts/$script"
    }
}

# --- Installer ZIP (GitHub release) ---

Write-Host ""
Write-Host "--- Installer ZIP ---" -ForegroundColor Yellow

$ghStagingDir = Join-Path $releaseDir "staging-installer"
if (Test-Path $ghStagingDir) { Remove-Item -Recurse -Force $ghStagingDir }
New-Item -ItemType Directory -Path $ghStagingDir -Force | Out-Null

# install.cmd / uninstall.cmd
foreach ($script in @("install.cmd", "uninstall.cmd")) {
    Copy-Item (Join-Path $scriptsDir $script) -Destination $ghStagingDir -Force
    Write-Host "  $script" -ForegroundColor Green
}

# launcher-manifest.json at ZIP root (the file lopari ingests). Stamp
# mod_info.version with the build's version so it can never drift from the DLLs
# this ZIP ships. "schema_version" has a numeric value, so the regex matches
# only mod_info.version.
$manifestSrc = Join-Path $projectDir "launcher-manifest.json"
if (-not (Test-Path $manifestSrc)) {
    throw "launcher-manifest.json not found at repo root."
}
$manifestRaw = Get-Content $manifestSrc -Raw
$manifestRaw = [regex]::Replace($manifestRaw, '("version"\s*:\s*")[^"]+(")', "`${1}$version`${2}")
Set-Content -Path (Join-Path $ghStagingDir "launcher-manifest.json") -Value $manifestRaw -NoNewline
Write-Host "  launcher-manifest.json (v$version)" -ForegroundColor Green

# plugins/ (mod DLL + core DLLs - install.cmd splits into Mods/ + UserLibs/)
$pluginsDir = Join-Path $ghStagingDir "plugins"
New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null
foreach ($dll in $allDlls) {
    Copy-Item (Join-Path $buildOutputDir $dll) -Destination $pluginsDir -Force
    Write-Host "  plugins/$dll" -ForegroundColor Green
}

# shared/ (body scripts + find-game.ps1 + GamePathDetection.psm1 + games.json)
Copy-SharedBundle -StagingDir $ghStagingDir -NoRefresh

# vendor/melonloader/ (zip + LICENSE + README.md)
$vendorStaging = Join-Path $ghStagingDir "vendor\melonloader"
New-Item -ItemType Directory -Path $vendorStaging -Force | Out-Null
foreach ($file in @("MelonLoader.x64.zip", "LICENSE", "README.md")) {
    $src = Join-Path $vendorMlDir $file
    if (-not (Test-Path $src)) {
        throw "Vendor file missing: vendor/melonloader/$file. Run 'pixi run update-deps'."
    }
    Copy-Item $src -Destination $vendorStaging -Force
    Write-Host "  vendor/melonloader/$file" -ForegroundColor Green
}

# Top-level docs
foreach ($doc in @("README.md", "CHANGELOG.md", "THIRD-PARTY-NOTICES.md")) {
    $docPath = Join-Path $projectDir $doc
    if (Test-Path $docPath) {
        Copy-Item $docPath -Destination $ghStagingDir -Force
        Write-Host "  $doc" -ForegroundColor Green
    }
}

$ghZipPath = Join-Path $releaseDir "FirewatchHeadTracking-v$version-installer.zip"
if (Test-Path $ghZipPath) { Remove-Item $ghZipPath -Force }

Write-Host ""
Write-Host "Creating installer ZIP..." -ForegroundColor Cyan
Push-Location $ghStagingDir
try {
    Compress-Archive -Path ".\*" -DestinationPath $ghZipPath -Force
} finally {
    Pop-Location
}
Remove-Item -Recurse -Force $ghStagingDir

$ghZipSize = (Get-Item $ghZipPath).Length / 1KB
Write-Host ("  $ghZipPath ({0:N1} KB)" -f $ghZipSize) -ForegroundColor Green

# --- Nexus ZIP (extract-to-game-folder; deploy subtree only) ---

Write-Host ""
Write-Host "--- Nexus ZIP ---" -ForegroundColor Yellow

$nexusStagingDir = Join-Path $releaseDir "staging-nexus"
if (Test-Path $nexusStagingDir) { Remove-Item -Recurse -Force $nexusStagingDir }

# MelonLoader deploy subtree: Mods/<ModName>.dll + UserLibs/<core>.dll
$nexusModsDir = Join-Path $nexusStagingDir "Mods"
$nexusUserLibsDir = Join-Path $nexusStagingDir "UserLibs"
New-Item -ItemType Directory -Path $nexusModsDir -Force | Out-Null
New-Item -ItemType Directory -Path $nexusUserLibsDir -Force | Out-Null

Copy-Item (Join-Path $buildOutputDir $modDll) -Destination $nexusModsDir -Force
Write-Host "  Mods/$modDll" -ForegroundColor Green

foreach ($libDll in $libDlls) {
    Copy-Item (Join-Path $buildOutputDir $libDll) -Destination $nexusUserLibsDir -Force
    Write-Host "  UserLibs/$libDll" -ForegroundColor Green
}

$nexusZipPath = Join-Path $releaseDir "FirewatchHeadTracking-v$version-nexus.zip"
if (Test-Path $nexusZipPath) { Remove-Item $nexusZipPath -Force }

Write-Host ""
Write-Host "Creating nexus ZIP..." -ForegroundColor Cyan
Push-Location $nexusStagingDir
try {
    Compress-Archive -Path ".\*" -DestinationPath $nexusZipPath -Force
} finally {
    Pop-Location
}
Remove-Item -Recurse -Force $nexusStagingDir

$nexusZipSize = (Get-Item $nexusZipPath).Length / 1KB
Write-Host ("  $nexusZipPath ({0:N1} KB)" -f $nexusZipSize) -ForegroundColor Green

Write-Host ""
Write-Host "=== Package Complete ===" -ForegroundColor Magenta
Write-Host ""
Write-Host ("Installer: $ghZipPath ({0:N1} KB)" -f $ghZipSize) -ForegroundColor Green
Write-Host ("Nexus:     $nexusZipPath ({0:N1} KB)" -f $nexusZipSize) -ForegroundColor Green

# Output both zip paths for capture (one per line)
Write-Output $ghZipPath
Write-Output $nexusZipPath
