#Requires -Version 5.1

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Solution = Join-Path $RepoRoot "DukkanPilot.Mobile.slnx"
$CoreProject = Join-Path $RepoRoot "src/DukkanPilot.Mobile.Core/DukkanPilot.Mobile.Core.csproj"
$MobileProject = Join-Path $RepoRoot "src/DukkanPilot.Mobile/DukkanPilot.Mobile.csproj"
$TestProject = Join-Path $RepoRoot "tests/DukkanPilot.Mobile.Tests/DukkanPilot.Mobile.Tests.csproj"
Set-Location $RepoRoot

function Invoke-Step([string]$Name, [scriptblock]$Action) {
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] $Name (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "[OK]  $Name" -ForegroundColor Green
}

function Invoke-MobileTests {
    Write-Host "==> Mobile unit tests" -ForegroundColor Cyan
    $output = & dotnet test $TestProject -c Release --no-restore 2>&1
    $exitCode = $LASTEXITCODE
    Write-Host ($output | Out-String)
    if ($exitCode -ne 0) {
        Write-Host "[FAIL] Mobile unit tests (exit $exitCode)" -ForegroundColor Red
        exit $exitCode
    }

    $text = $output | Out-String
    $match = [regex]::Match(
        $text,
        'Failed:[ ]*(?<failed>[0-9]+),[ ]*Passed:[ ]*(?<passed>[0-9]+),[ ]*Skipped:[ ]*(?<skipped>[0-9]+),[ ]*Total:[ ]*(?<total>[0-9]+)',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        Write-Host "[FAIL] Could not parse mobile test totals." -ForegroundColor Red
        exit 1
    }

    return [pscustomobject]@{
        Total = [int]$match.Groups['total'].Value
        Passed = [int]$match.Groups['passed'].Value
        Failed = [int]$match.Groups['failed'].Value
        Skipped = [int]$match.Groups['skipped'].Value
    }
}

Write-Host "==> DukkanPilot Mobile application checks" -ForegroundColor Cyan
Write-Host "Solution: $Solution"

$workloads = (& dotnet workload list 2>&1 | Out-String)
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAIL] dotnet workload list" -ForegroundColor Red
    exit $LASTEXITCODE
}

$hasMaui = $workloads -match '(?m)^ *maui +'
if ($hasMaui) {
    Invoke-Step "dotnet restore DukkanPilot.Mobile.slnx" { dotnet restore $Solution }
}
else {
    Write-Host "[SKIP] MAUI workload is not installed; restoring Core and Tests only." -ForegroundColor Yellow
    Invoke-Step "dotnet restore Mobile.Core" { dotnet restore $CoreProject }
    Invoke-Step "dotnet restore Mobile.Tests" { dotnet restore $TestProject }
}

Invoke-Step "Mobile.Core Release build" { dotnet build $CoreProject -c Release --no-restore }
$tests = Invoke-MobileTests

$androidResult = "SKIP (MAUI workload missing)"
$windowsResult = "SKIP (not Windows or MAUI workload missing)"
if ($hasMaui) {
    Invoke-Step "Android Release build" {
        dotnet build $MobileProject -f net10.0-android -c Release --no-restore
    }
    $androidResult = "PASS"

    if ($env:OS -eq "Windows_NT") {
        Invoke-Step "Windows Release build" {
            dotnet build $MobileProject -f net10.0-windows10.0.19041.0 -c Release --no-restore
        }
        $windowsResult = "PASS"
    }
}

Write-Host ""
Write-Host "==> Summary" -ForegroundColor Cyan
Write-Host "TOTAL=$($tests.Total) PASS=$($tests.Passed) FAIL=$($tests.Failed) SKIP=$($tests.Skipped)"
Write-Host "ANDROID=$androidResult"
Write-Host "WINDOWS=$windowsResult"

if ($tests.Total -eq 0 -or $tests.Failed -ne 0) {
    Write-Host "check-mobile-app FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "check-mobile-app PASSED" -ForegroundColor Green
exit 0