#Requires -Version 5.1
# Runs the isolated 36C integration test suite. It never uses LocalDB or migrations.
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot
$Project = Join-Path $RepoRoot "tests\DukkanPilot.IntegrationTests\DukkanPilot.IntegrationTests.csproj"

function Invoke-DotNetStep([string]$Name, [string[]]$Arguments) {
    Write-Host "==> $Name" -ForegroundColor Cyan
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] $Name (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "[OK]  $Name" -ForegroundColor Green
}

Write-Host "==> DukkanPilot integration tests (SQLite in-memory)" -ForegroundColor Cyan
Write-Host "Project: $Project"
Invoke-DotNetStep "dotnet restore" @("restore", $Project)
Invoke-DotNetStep "dotnet build -c Release --no-restore" @("build", $Project, "-c", "Release", "--no-restore")
Invoke-DotNetStep "dotnet test -c Release --no-build" @("test", $Project, "-c", "Release", "--no-build", "--no-restore")

Write-Host "check-integration-tests PASSED" -ForegroundColor Green
