<#
.SYNOPSIS
    One-click build script: dotnet publish → compile Inno Setup → AdminToolbox-Setup.exe

.DESCRIPTION
    1. Runs `dotnet publish` (Release, win-x64, self-contained, single-file)
    2. Locates or auto-installs Inno Setup 6 via winget
    3. Compiles installer\AdminToolbox.iss → bin\Installer\AdminToolbox-Setup.exe

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -SkipPublish   # use existing bin\Publish output
#>

[CmdletBinding()]
param(
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root     = $PSScriptRoot
$issFile  = Join-Path $root "installer\AdminToolbox.iss"
$outDir   = Join-Path $root "bin\Installer"

# -------------------------------------------------------
# 1. dotnet publish
# -------------------------------------------------------
if (-not $SkipPublish) {
    Write-Host "`n==> dotnet publish (Release, win-x64, self-contained)..." -ForegroundColor Cyan
    $publishArgs = @(
        'publish', "$root\AdminToolbox.csproj",
        '--configuration', 'Release',
        '--runtime', 'win-x64',
        '--self-contained', 'true',
        '-p:PublishSingleFile=true',
        '-p:PublishReadyToRun=true',
        '-o', "$root\bin\Publish"
    )
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }
    Write-Host "    publish OK" -ForegroundColor Green
} else {
    Write-Host "`n==> Skipping dotnet publish (-SkipPublish)" -ForegroundColor Yellow
}

# -------------------------------------------------------
# 2. Locate Inno Setup ISCC.exe (install via winget if missing)
# -------------------------------------------------------
function Find-ISCC {
    $candidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    return $null
}

$iscc = Find-ISCC

if (-not $iscc) {
    Write-Host "`n==> Inno Setup 6 not found - installing via winget..." -ForegroundColor Cyan
    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw "winget is not available. Please install Inno Setup 6 manually from https://jrsoftware.org/isinfo.php and re-run."
    }
    & winget install --id JRSoftware.InnoSetup --exact --accept-source-agreements --accept-package-agreements --silent
    if ($LASTEXITCODE -ne 0) { throw "winget install InnoSetup failed (exit $LASTEXITCODE)" }

    $iscc = Find-ISCC
    if (-not $iscc) { throw "ISCC.exe not found even after winget install. Check PATH." }
    Write-Host "    Inno Setup installed: $iscc" -ForegroundColor Green
} else {
    Write-Host "`n==> Inno Setup found: $iscc" -ForegroundColor Green
}

# -------------------------------------------------------
# 3. Compile the installer
# -------------------------------------------------------
Write-Host "`n==> Compiling installer..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

& $iscc $issFile
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed (exit $LASTEXITCODE)" }

$setupExe = Join-Path $outDir "AdminToolbox-Setup.exe"
if (-not (Test-Path $setupExe)) { throw "Expected output not found: $setupExe" }

$sizeMB = [math]::Round((Get-Item $setupExe).Length / 1MB, 1)
$sizeStr = "$sizeMB MB"
Write-Host ""
Write-Host "==> SUCCESS: $setupExe ($sizeStr)" -ForegroundColor Green
