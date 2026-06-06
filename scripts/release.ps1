#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
    Canonical release workflow for Firewatch Head Tracking.

.DESCRIPTION
    Eight-step release per the CameraUnlock spec:
    1. Parse version, validate semver.
    2. Verify on main, clean tree, tag does not yet exist.
    3. Update version in csproj (canonical) + manifest.json (mirror).
    4. pixi run build (Release).
    5. Generate CHANGELOG.md from commits since the last tag.
    6. Commit "Release v<version>" with the version bump + changelog.
    7. Create annotated tag v<version>.
    8. Push commits + tag (this triggers .github/workflows/release.yml,
       which builds and uploads the installer + nexus ZIPs).

    Headless: no Read-Host, no pause. The script's existence is the user's
    confirmation; safety comes from failing fast on dirty tree / existing tag.

.PARAMETER Version
    Semantic version to release (e.g. "1.0.0").

.EXAMPLE
    pixi run release 1.0.0
#>
param(
    [Parameter(Position=0)]
    [string]$Version = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

$csprojPath = Join-Path $projectDir "src\FirewatchHeadTracking\FirewatchHeadTracking.csproj"
$manifestPath = Join-Path $projectDir "manifest.json"
$changelogPath = Join-Path $projectDir "CHANGELOG.md"
$modCsPath = Join-Path $projectDir "src\FirewatchHeadTracking\Core\FirewatchHeadTrackingMod.cs"
$installCmdPath = Join-Path $projectDir "scripts\install.cmd"

Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ReleaseWorkflow.psm1") -Force

Write-Host "=== Firewatch Head Tracking Release ===" -ForegroundColor Cyan
Write-Host ""

$currentVersion = Get-CsprojVersion $csprojPath

if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Current version: $currentVersion" -ForegroundColor Yellow
    Write-Host "Usage: pixi run release <major|minor|patch|nightly|X.Y.Z>" -ForegroundColor Yellow
    exit 0
}

if ($Version -eq 'nightly') {
    & (Join-Path $scriptDir 'release-nightly.ps1')
    exit $LASTEXITCODE
}

# --- Step 1: resolve major/minor/patch into a concrete version (or accept literal X.Y.Z) ---
try {
    $Version = Resolve-ReleaseVersion -Argument $Version -CurrentVersion $currentVersion
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$tagName = "v$Version"

# --- Step 2: git state ---
$currentBranch = git rev-parse --abbrev-ref HEAD
if ($currentBranch -ne "main") {
    Write-Host "ERROR: Must be on 'main' branch (currently on '$currentBranch')." -ForegroundColor Red
    exit 1
}

if (-not (Test-CleanGitStatus)) {
    Write-Host "ERROR: Working tree has uncommitted changes. Commit or stash before releasing." -ForegroundColor Red
    exit 1
}

if (Test-GitTagExists $tagName) {
    Write-Host "ERROR: Tag '$tagName' already exists. Pick a new version." -ForegroundColor Red
    exit 1
}

Write-Host "Current version: $currentVersion" -ForegroundColor Gray
Write-Host "Releasing:       $Version" -ForegroundColor Green
Write-Host ""

# --- Step 3: update version in canonical source + mirror ---
Write-Host "[3/8] Updating version in csproj + manifest.json..." -ForegroundColor Cyan
Set-CsprojVersion -CsprojPath $csprojPath -Version $Version
if (Test-Path $manifestPath) {
    Update-ManifestVersion -ManifestPath $manifestPath -Version $Version | Out-Null
}

# Keep MelonInfo + ModVersion (shown in MelonLoader log) and install.cmd's
# MOD_VERSION (written to .headtracking-state.json) in lockstep with csproj.
$modCs = Get-Content $modCsPath -Raw
$modCs = [regex]::Replace($modCs, '(MelonInfo\([^)]*?,\s*")[^"]+(",\s*"itsloopyo")', "`${1}$Version`${2}")
$modCs = [regex]::Replace($modCs, '(ModVersion\s*=\s*")[^"]+(")', "`${1}$Version`${2}")
Set-Content -Path $modCsPath -Value $modCs -NoNewline

$installCmd = Get-Content $installCmdPath -Raw
$installCmd = [regex]::Replace($installCmd, '(set "MOD_VERSION=)[^"]+(")', "`${1}$Version`${2}")
Set-Content -Path $installCmdPath -Value $installCmd -NoNewline

# --- Step 4: build ---
Write-Host "[4/8] Building Release via pixi..." -ForegroundColor Cyan
pixi run build
if ($LASTEXITCODE -ne 0) {
    throw "pixi run build failed"
}

# --- Step 5: changelog ---
Write-Host "[5/8] Generating CHANGELOG entry..." -ForegroundColor Cyan
$hasExistingTags = git tag -l 2>$null
if (-not $hasExistingTags) {
    $date = Get-Date -Format 'yyyy-MM-dd'
    $firstEntry = "# Changelog`n`n## [$Version] - $date`n`nFirst release.`n"
    Set-Content $changelogPath $firstEntry
    Write-Host "  First release - wrote initial CHANGELOG entry" -ForegroundColor Gray
} else {
    New-ChangelogFromCommits -ChangelogPath $changelogPath -Version $Version -ArtifactPaths @(
        "src/",
        "cameraunlock-core/",
        "scripts/install.cmd",
        "scripts/uninstall.cmd"
    ) | Out-Null
}

# --- Step 6: commit ---
Write-Host "[6/8] Committing version bump + changelog..." -ForegroundColor Cyan
git add $csprojPath $changelogPath $modCsPath $installCmdPath
if (Test-Path $manifestPath) { git add $manifestPath }
git commit -m "Release v$Version"
if ($LASTEXITCODE -ne 0) {
    throw "git commit failed"
}

# --- Step 7: tag ---
Write-Host "[7/8] Creating annotated tag $tagName..." -ForegroundColor Cyan
git tag -a $tagName -m "Release v$Version"
if ($LASTEXITCODE -ne 0) {
    throw "git tag failed"
}

# --- Step 8: push (triggers release.yml) ---
Write-Host "[8/8] Pushing commits + tag to origin/main..." -ForegroundColor Cyan
git push origin main
if ($LASTEXITCODE -ne 0) {
    throw "git push origin main failed. Tag created locally; push manually: git push origin main --tags"
}
git push origin $tagName
if ($LASTEXITCODE -ne 0) {
    throw "git push origin $tagName failed. Push manually: git push origin $tagName"
}

Write-Host ""
Write-Host "Release $tagName pushed. CI (.github/workflows/release.yml) will build and publish the GitHub release." -ForegroundColor Green
