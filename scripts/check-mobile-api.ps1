#Requires -Version 5.1
param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Project = Join-Path $RepoRoot "tests\DukkanPilot.IntegrationTests\DukkanPilot.IntegrationTests.csproj"
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

function Invoke-TestGroup([string]$Name, [string]$Filter) {
    Write-Host "==> $Name" -ForegroundColor Cyan
    $output = & dotnet test $Project -c Release --no-build --filter $Filter 2>&1
    $exitCode = $LASTEXITCODE
    Write-Host ($output | Out-String)
    if ($exitCode -ne 0) {
        Write-Host "[FAIL] $Name (exit $exitCode)" -ForegroundColor Red
        exit $exitCode
    }

    $text = $output | Out-String
    $match = [regex]::Match(
        $text,
        'Failed:\s*(?<failed>\d+),\s*Passed:\s*(?<passed>\d+),\s*Skipped:\s*(?<skipped>\d+),\s*Total:\s*(?<total>\d+)',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        Write-Host "[FAIL] Could not parse test totals for $Name" -ForegroundColor Red
        exit 1
    }

    return [pscustomobject]@{
        Name = $Name
        Total = [int]$match.Groups['total'].Value
        Passed = [int]$match.Groups['passed'].Value
        Failed = [int]$match.Groups['failed'].Value
        Skipped = [int]$match.Groups['skipped'].Value
    }
}

Write-Host "==> DukkanPilot Mobile API checks" -ForegroundColor Cyan
Write-Host "Project: $Project"

if (-not $NoBuild) {
    Invoke-Step "dotnet restore" { dotnet restore $Project }
    Invoke-Step "dotnet build -c Release --no-restore" { dotnet build $Project -c Release --no-restore }
}

$mobile = Invoke-TestGroup "Mobile API integration tests" "FullyQualifiedName~MobileApi"
$regression = Invoke-TestGroup "36C regression integration tests" "FullyQualifiedName!~MobileApi"

$total = $mobile.Total + $regression.Total
$passed = $mobile.Passed + $regression.Passed
$failed = $mobile.Failed + $regression.Failed
$skipped = $mobile.Skipped + $regression.Skipped

Write-Host ""
Write-Host "==> Summary" -ForegroundColor Cyan
@($mobile, $regression) | Format-Table -AutoSize Name, Total, Passed, Failed, Skipped
Write-Host "TOTAL=$total PASS=$passed FAIL=$failed SKIP=$skipped"

if ($failed -ne 0 -or $mobile.Total -eq 0 -or $regression.Total -eq 0) {
    Write-Host "check-mobile-api FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "check-mobile-api PASSED" -ForegroundColor Green
exit 0
