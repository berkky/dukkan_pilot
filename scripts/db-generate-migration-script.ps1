#Requires -Version 5.1
<#
.SYNOPSIS
  Generates an EF Core migration SQL script (idempotent by default). Does NOT apply to any database.
#>
param(
    [string]$OutputDirectory = "",
    [string]$FromMigration = "",
    [string]$ToMigration = "",
    [bool]$Idempotent = $true
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepoRoot "artifacts\sql"
}

$infra = Join-Path $RepoRoot "src\DukkanPilot.Infrastructure"
$web = Join-Path $RepoRoot "src\DukkanPilot.Web"

if (-not (Test-Path $infra)) { throw "Infrastructure project not found: $infra" }
if (-not (Test-Path $web)) { throw "Web project not found: $web" }

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$suffix = if ($Idempotent) { "idempotent" } else { "plain" }
$outFile = Join-Path $OutputDirectory ("DukkanPilot_migrations_{0}_{1}.sql" -f $suffix, $stamp)

Write-Host "==> DukkanPilot db-generate-migration-script" -ForegroundColor Cyan
Write-Host "Output: $outFile"
Write-Host "Idempotent: $Idempotent"
Write-Host ""

$args = @(
    "ef", "migrations", "script",
    "--project", $infra,
    "--startup-project", $web,
    "--output", $outFile
)

if ($Idempotent) {
    $args += "--idempotent"
}

if (-not [string]::IsNullOrWhiteSpace($FromMigration) -and -not [string]::IsNullOrWhiteSpace($ToMigration)) {
    $args += @($FromMigration, $ToMigration)
} elseif (-not [string]::IsNullOrWhiteSpace($FromMigration) -or -not [string]::IsNullOrWhiteSpace($ToMigration)) {
    throw "Provide both -FromMigration and -ToMigration, or neither."
}

& dotnet @args
if ($LASTEXITCODE -ne 0) {
    Write-Host "dotnet ef migrations script failed (exit $LASTEXITCODE)" -ForegroundColor Red
    if ($LASTEXITCODE -ne 0) {
        Write-Host "If tool missing: install locally only via 'dotnet tool install --global dotnet-ef' (this script does not install tools)." -ForegroundColor Yellow
    }
    exit 1
}

if (-not (Test-Path $outFile)) {
    Write-Host "Script command succeeded but output file missing: $outFile" -ForegroundColor Red
    exit 1
}

Write-Host "Migration SQL generated:" -ForegroundColor Green
Write-Host $outFile
Write-Host "Note: SQL was NOT applied to any database."
exit 0
