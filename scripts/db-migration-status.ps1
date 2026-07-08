#Requires -Version 5.1
<#
.SYNOPSIS
  Shows EF migration list and pending model-change status. Does NOT update the database.
#>
$ErrorActionPreference = "Continue"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$infra = Join-Path $RepoRoot "src\DukkanPilot.Infrastructure\DukkanPilot.Infrastructure.csproj"
$web = Join-Path $RepoRoot "src\DukkanPilot.Web\DukkanPilot.Web.csproj"
$failed = $false

Write-Host "==> DukkanPilot db-migration-status" -ForegroundColor Cyan
Write-Host "Repo: $RepoRoot"
Write-Host ""

Write-Host "==> migrations list"
$listOut = & dotnet ef migrations list --project $infra --startup-project $web --configuration Release --no-build 2>&1
$listCode = $LASTEXITCODE
Write-Host ($listOut | Out-String)

if ($listCode -ne 0) {
    $text = ($listOut | Out-String)
    if ($text -match "Could not execute because the specified command|No executable found matching command|dotnet-ef") {
        Write-Host "dotnet ef tool not available. Install on your machine only: dotnet tool install --global dotnet-ef (script does not install tools)." -ForegroundColor Red
    } else {
        Write-Host "migrations list failed (exit $listCode)" -ForegroundColor Red
    }
    $failed = $true
}

Write-Host ""
Write-Host "==> has-pending-model-changes"
$pendingOut = & dotnet ef migrations has-pending-model-changes --project $infra --startup-project $web --configuration Release --no-build 2>&1
$pendingCode = $LASTEXITCODE
$pendingText = ($pendingOut | Out-String)
Write-Host $pendingText

if ($pendingCode -ne 0 -and ($pendingText -match "Could not execute because the specified command|No executable found matching command|dotnet-ef")) {
    Write-Host "dotnet ef tool not available." -ForegroundColor Red
    $failed = $true
} elseif ($pendingCode -ne 0) {
    Write-Host "has-pending-model-changes failed (exit $pendingCode)" -ForegroundColor Red
    $failed = $true
} elseif ($pendingText -match "(?i)No changes have been made to the model") {
    Write-Host "[OK] No pending EF model changes" -ForegroundColor Green
} elseif ($pendingText -match "(?i)Changes have been made|pending") {
    Write-Host "[FAIL] Pending EF model changes detected" -ForegroundColor Red
    $failed = $true
}

Write-Host ""
Write-Host "Note: This script does not apply migrations / does not update the database."

if ($failed) {
    Write-Host "db-migration-status FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "db-migration-status PASSED" -ForegroundColor Green
exit 0
