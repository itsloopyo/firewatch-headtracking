#!/usr/bin/env pwsh
# Validate release readiness

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

Write-Host "Validating release readiness..." -ForegroundColor Cyan
Write-Host ""

$errors = @()
$warnings = @()

# Check release DLLs exist
$buildOutputDir = Join-Path $projectDir "src\FirewatchHeadTracking\bin\Release\net472"
$requiredDlls = @("FirewatchHeadTracking.dll", "CameraUnlock.Core.dll", "CameraUnlock.Core.Unity.dll")

foreach ($dll in $requiredDlls) {
    $dllPath = Join-Path $buildOutputDir $dll
    if (Test-Path $dllPath) {
        $size = (Get-Item $dllPath).Length
        Write-Host "[OK] $dll exists ($size bytes)" -ForegroundColor Green
    } else {
        $errors += "$dll not found at: $dllPath. Run 'pixi run build' first."
    }
}

# Check manifest.json
$manifest = Join-Path $projectDir "manifest.json"
if (Test-Path $manifest) {
    try {
        $json = Get-Content $manifest | ConvertFrom-Json
        if ($json.version) {
            Write-Host "[OK] manifest.json version: $($json.version)" -ForegroundColor Green
        } else {
            $errors += "manifest.json missing version field"
        }
    } catch {
        $errors += "manifest.json is not valid JSON"
    }
} else {
    $errors += "manifest.json not found"
}

# Check CHANGELOG.md
$changelog = Join-Path $projectDir "CHANGELOG.md"
if (Test-Path $changelog) {
    Write-Host "[OK] CHANGELOG.md exists" -ForegroundColor Green
} else {
    $errors += "CHANGELOG.md not found"
}

# Check README.md
$readme = Join-Path $projectDir "README.md"
if (Test-Path $readme) {
    Write-Host "[OK] README.md exists" -ForegroundColor Green
} else {
    $warnings += "README.md not found"
}

# Check install/uninstall scripts
foreach ($script in @("install.cmd", "uninstall.cmd")) {
    $scriptPath = Join-Path $projectDir "scripts\$script"
    if (Test-Path $scriptPath) {
        Write-Host "[OK] scripts/$script exists" -ForegroundColor Green
    } else {
        $errors += "scripts/$script not found"
    }
}

Write-Host ""

# Report warnings
if ($warnings.Count -gt 0) {
    Write-Host "Warnings:" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  - $warning" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Report errors
if ($errors.Count -gt 0) {
    Write-Host "Errors:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  - $error" -ForegroundColor Red
    }
    Write-Host ""
    Write-Error "Validation failed with $($errors.Count) error(s)"
    exit 1
}

Write-Host "Validation passed!" -ForegroundColor Green
exit 0
