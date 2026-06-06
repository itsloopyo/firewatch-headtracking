#!/usr/bin/env pwsh
#Requires -Version 5.1
[CmdletBinding()]
param([switch]$AllowDirty)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

Import-Module (Join-Path $ProjectRoot 'cameraunlock-core\powershell\NightlyRelease.psm1') -Force

$csprojPath = Join-Path $ProjectRoot 'src\FirewatchHeadTracking\FirewatchHeadTracking.csproj'
$match = Select-String -Path $csprojPath -Pattern '<Version>([^<]+)</Version>' | Select-Object -First 1
if (-not $match) {
    throw "Could not extract <Version> from $csprojPath"
}
$version = $match.Matches[0].Groups[1].Value

Publish-NightlyBuild `
    -ModId 'firewatch' `
    -ModName 'FirewatchHeadTracking' `
    -Version $version `
    -ProjectRoot $ProjectRoot `
    -BuildCommand 'pixi run build' `
    -AllowDirty:$AllowDirty
