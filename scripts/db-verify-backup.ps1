#Requires -Version 5.1
<#
.SYNOPSIS
  Verifies an existing .bak with RESTORE VERIFYONLY WITH CHECKSUM.
#>
param(
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [Parameter(Mandatory = $true)]
    [string]$BackupFile
)

$ErrorActionPreference = "Stop"

function Test-SafeDiskPath([string]$path, [string]$label) {
    if ([string]::IsNullOrWhiteSpace($path)) { throw "$label is required." }
    if ($path -match "[;']" -or $path -match '--') {
        throw "$label contains unsafe characters for SQL DISK path."
    }
}

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Host "sqlcmd bulunamadi. SQL Server Command Line Utilities kurulu olmali veya backup islemini SSMS uzerinden yapin." -ForegroundColor Red
    exit 1
}

$full = $BackupFile
if (-not [System.IO.Path]::IsPathRooted($full)) {
    $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
    $full = Join-Path $RepoRoot $BackupFile
}

if (-not (Test-Path -LiteralPath $full)) {
    Write-Host "Backup file not found: $full" -ForegroundColor Red
    exit 1
}

$resolved = (Resolve-Path -LiteralPath $full).Path
Test-SafeDiskPath $resolved "BackupFile"
$diskSql = $resolved.Replace("'", "''")

Write-Host "==> DukkanPilot db-verify-backup" -ForegroundColor Cyan
Write-Host "Server: $ServerInstance"
Write-Host "File:   $resolved"
Write-Host ""

$sql = @"
RESTORE VERIFYONLY FROM DISK = N'$diskSql' WITH CHECKSUM;
"@

& sqlcmd -S $ServerInstance -E -Q $sql -b
if ($LASTEXITCODE -ne 0) {
    Write-Host "VERIFYONLY FAILED (exit $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

Write-Host "VERIFYONLY PASSED" -ForegroundColor Green
exit 0
