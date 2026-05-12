#!/usr/bin/env pwsh
#Requires -Version 5.1
# Bump vendored MelonLoader to the latest upstream within the pinned range and
# rewrite vendor/melonloader/{LICENSE,README.md}. Manual: dev runs this when
# they want a fresh upstream bump, then commits the result. CI never refreshes.
# See ~/.claude/CLAUDE.md "Vendoring Third-Party Dependencies".
#
# Firewatch pins MelonLoader to v0.5.x (NOT v0.6.x). v0.6.x regressed the
# Pastel library (RegexOptions) in a way that crashes on Unity 2017 Mono.
# Bumping past v0.5 is an explicit per-mod retest, not an automatic refresh.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

$module = Join-Path $projectDir 'cameraunlock-core/powershell/ModLoaderSetup.psm1'
if (-not (Test-Path $module)) {
    throw "ModLoaderSetup.psm1 not found at $module. Run 'pixi run sync' to update the cameraunlock-core submodule."
}
Import-Module $module -Force

$out = Join-Path $projectDir 'vendor/melonloader'
Refresh-VendoredLoader `
    -Name 'melonloader' `
    -OutputDir $out `
    -OutputFileName 'MelonLoader.x64.zip' `
    -Owner 'LavaGang' -Repo 'MelonLoader' `
    -VersionPrefix 'v0.5.' `
    -AssetPattern '^MelonLoader\.x64\.zip$' | Out-Null

Write-Host ""
Write-Host "vendor/melonloader refreshed. Review and commit." -ForegroundColor Green
