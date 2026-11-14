<#
.SYNOPSIS
    Сборка установщика AudioSwitch: публикация приложения + компиляция Inno Setup.

.DESCRIPTION
    1. dotnet publish (по умолчанию self-contained win-x64, чтобы пользователю
       не требовался установленный .NET Desktop Runtime).
    2. ISCC.exe компилирует installer\AudioSwitch.iss, передавая версию из csproj.
    Результат: dist\AudioSwitch-<версия>-x64-setup.exe.

    Требуется Inno Setup 6: winget install --id JRSoftware.InnoSetup

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -FrameworkDependent   # легче, но нужен .NET 9 Desktop Runtime
#>
[CmdletBinding()]
param (
    # Собрать фреймворк-зависимую версию (требуется .NET 9 Desktop Runtime у пользователя).
    [switch]$FrameworkDependent,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Architecture = 'win-x64',

    # Переопределить версию (по умолчанию берётся из <Version> в csproj).
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$project = Join-Path $root 'AudioSwitch\AudioSwitch.csproj'
$iss = Join-Path $root 'installer\AudioSwitch.iss'
$publishDir = Join-Path $root ('publish\AudioSwitch')
$distDir = Join-Path $root 'dist'

function Find-Iscc {
    $candidates = @()
    foreach ($regKey in @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
                          'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1')) {
        $loc = (Get-ItemProperty $regKey -ErrorAction SilentlyContinue).InstallLocation
        if ($loc) { $candidates += (Join-Path $loc 'ISCC.exe') }
    }
    $candidates += "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    $candidates += "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    $candidates += "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"

    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c)) { return $c }
    }
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

# --- Версия из csproj -----------------------------------------------------
if (-not $Version) {
    [xml]$csproj = Get-Content $project -Raw
    $Version = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
}
if (-not $Version) { throw 'Не удалось определить версию (элемент <Version> в csproj).' }
Write-Host "== AudioSwitch $Version ($Architecture, $Configuration) ==" -ForegroundColor Cyan

# --- Публикация ------------------------------------------------------------
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

$publishArgs = @(
    'publish', $project,
    '-c', $Configuration,
    '-r', $Architecture,
    '--self-contained', ([string](-not $FrameworkDependent)),
    '-o', $publishDir,
    '-p:DebugType=none',
    # Локализованные ресурсы фреймворка: только нейтральные (en) + русские
    # (';' экранирован как %3B — иначе MSBuild считает это разделителем свойств).
    '-p:SatelliteResourceLanguages=en%3Bru',
    '--nologo', '-v', 'quiet'
)
Write-Host '> dotnet' ($publishArgs -join ' ') -ForegroundColor DarkGray
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish завершился с кодом $LASTEXITCODE." }

$published = Get-ChildItem $publishDir -Filter 'AudioSwitch.exe' -File
if (-not $published) { throw "AudioSwitch.exe не найден в $publishDir." }

# --- Компиляция установщика ------------------------------------------------
$iscc = Find-Iscc
if (-not $iscc) {
    throw @"
ISCC.exe (Inno Setup 6) не найден.
Установите: winget install --id JRSoftware.InnoSetup
"@
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$archTag = if ($Architecture -like '*arm64*') { 'arm64' } else { 'x64' }
$issArgs = @(
    "/DMyAppVersion=$Version"
    "/DSetupArch=$archTag"
    "/DSourceDir=$($publishDir.TrimEnd('\'))"
    "/DOutputDir=$($distDir.TrimEnd('\'))"
    $iss
)
Write-Host "> $iscc" ($issArgs -join ' ') -ForegroundColor DarkGray
& $iscc @issArgs | Out-Host
if ($LASTEXITCODE -ne 0) { throw "ISCC завершился с кодом $LASTEXITCODE." }

$setup = Get-Item (Join-Path $distDir "AudioSwitch-$Version-$archTag-setup.exe")
$sizeMb = [math]::Round($setup.Length / 1MB, 1)
Write-Host ''
Write-Host "Готово: $($setup.FullName) ($sizeMb МБ)" -ForegroundColor Green
