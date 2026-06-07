#!/usr/bin/env pwsh
# Populates src/FirewatchHeadTracking/libs/ for a game-free build.
# MelonLoader DLLs come from the committed vendor zip. Unity reference stubs are
# compiled from the checked-in UnityStubs.cs. No Firewatch installation needed.

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$scriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot  = Split-Path -Parent $scriptDir
$libsPath     = Join-Path $projectRoot 'src\FirewatchHeadTracking\libs'
$vendorZip    = Join-Path $projectRoot 'vendor\melonloader\MelonLoader.x64.zip'
$stubSource   = Join-Path $libsPath 'UnityStubs.cs'

if (-not (Test-Path $vendorZip)) { throw "Vendored MelonLoader not found at $vendorZip" }
if (-not (Test-Path $stubSource)) { throw "UnityStubs.cs not found at $libsPath" }

New-Item -ItemType Directory -Path $libsPath -Force | Out-Null

Write-Host "Bootstrapping build dependencies (no game install required)..." -ForegroundColor Cyan

# Wipe libs/ except the tracked stub source so stale game DLLs can't mask CI parity.
Get-ChildItem -Path $libsPath -Force |
    Where-Object { $_.Name -ne 'UnityStubs.cs' } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# MelonLoader from vendor zip (0.5.7 flat layout: DLLs directly in MelonLoader/)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$tempDir = Join-Path $env:TEMP ("fw-ml-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
try {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($vendorZip, $tempDir)
    foreach ($dll in @('MelonLoader.dll', '0Harmony.dll')) {
        $src = Join-Path $tempDir "MelonLoader\$dll"
        if (-not (Test-Path $src)) { throw "$dll not found in vendor zip at MelonLoader\" }
        Copy-Item $src (Join-Path $libsPath $dll) -Force
        Write-Host "  MelonLoader: $dll" -ForegroundColor Gray
    }
} finally {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}

# Unity reference stubs compiled from UnityStubs.cs (net35 to match Firewatch's Unity version)
function Build-Stub([string]$assemblyName, [string]$compileItem) {
    $proj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net35</TargetFramework>
    <LangVersion>7.3</LangVersion>
    <AssemblyName>$assemblyName</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NoWarn>CS0169;CS0649;CS0067;CS0660;CS0661</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net35" Version="1.0.3" PrivateAssets="all" />
    <Compile Include="$compileItem" />
  </ItemGroup>
</Project>
"@
    $projPath = Join-Path $libsPath "Stub_$assemblyName.csproj"
    $proj | Out-File -FilePath $projPath -Encoding utf8
    dotnet build $projPath -c Release -o $libsPath --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Failed to build stub $assemblyName" }
    Remove-Item $projPath -ErrorAction SilentlyContinue
    Write-Host "  Stub: $assemblyName.dll" -ForegroundColor Gray
}

Build-Stub 'UnityEngine' 'UnityStubs.cs'

$emptySource = Join-Path $libsPath 'EmptyStub.cs'
'// Empty stub assembly' | Out-File -FilePath $emptySource -Encoding utf8
foreach ($m in @(
    'UnityEngine.CoreModule', 'UnityEngine.IMGUIModule', 'UnityEngine.PhysicsModule',
    'UnityEngine.UIModule', 'UnityEngine.TextRenderingModule', 'UnityEngine.UI',
    'Assembly-CSharp'
)) { Build-Stub $m 'EmptyStub.cs' }

Remove-Item $emptySource -ErrorAction SilentlyContinue
Remove-Item (Join-Path $libsPath '*.deps.json') -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $libsPath '*.pdb')        -Force -ErrorAction SilentlyContinue

Write-Host "Build dependencies ready." -ForegroundColor Green
