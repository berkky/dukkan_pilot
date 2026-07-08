#Requires -Version 5.1
<#
.SYNOPSIS
  Creates a SQL Server / LocalDB FULL backup with CHECKSUM (optional VERIFYONLY).
.NOTES
  Does not read connection strings or secrets. Does not drop or overwrite databases.
#>
param(
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$DatabaseName = "DukkanPilotDb",
    [string]$BackupDirectory = "",
    [string]$BackupName = "",
    [switch]$UseCompression,
    [bool]$VerifyAfterBackup = $true
)

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
    $BackupDirectory = Join-Path $RepoRoot "artifacts\db-backups"
}

function Test-SafeSqlIdentifier([string]$name, [string]$label) {
    if ([string]::IsNullOrWhiteSpace($name)) { throw "$label is required." }
    if ($name -notmatch '^[A-Za-z0-9_]+$') {
        throw "$label contains invalid characters. Use only letters, digits, underscore."
    }
}

function Test-SafeDiskPath([string]$path, [string]$label) {
    if ([string]::IsNullOrWhiteSpace($path)) { throw "$label is required." }
    if ($path -match "[;']" -or $path -match '--') {
        throw "$label contains unsafe characters for SQL DISK path."
    }
}

Test-SafeSqlIdentifier $DatabaseName "DatabaseName"
Test-SafeDiskPath $BackupDirectory "BackupDirectory"

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Host "sqlcmd bulunamadi. SQL Server Command Line Utilities kurulu olmali veya backup islemini SSMS uzerinden yapin." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $BackupDirectory)) {
    New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$baseName = if ([string]::IsNullOrWhiteSpace($BackupName)) { $DatabaseName } else { $BackupName }
if ($baseName -notmatch '^[A-Za-z0-9_\-]+$') {
    throw "BackupName contains invalid characters."
}

$backupFile = Join-Path $BackupDirectory ("{0}_{1}.bak" -f $baseName, $stamp)
Test-SafeDiskPath $backupFile "BackupFile"

$diskSql = $backupFile.Replace("'", "''")
$dbSql = $DatabaseName

$withParts = @("CHECKSUM", "INIT", "FORMAT")
if ($UseCompression) { $withParts += "COMPRESSION" }
$withClause = ($withParts -join ", ")

$sql = @"
BACKUP DATABASE [$dbSql]
TO DISK = N'$diskSql'
WITH $withClause, STATS = 10;
"@

Write-Host "==> DukkanPilot db-backup" -ForegroundColor Cyan
Write-Host "Server:   $ServerInstance"
Write-Host "Database: $DatabaseName"
Write-Host "File:     $backupFile"
Write-Host ""

& sqlcmd -S $ServerInstance -E -Q $sql -b
if ($LASTEXITCODE -ne 0) {
    Write-Host "BACKUP failed (exit $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $backupFile)) {
    Write-Host "BACKUP completed but file not found: $backupFile" -ForegroundColor Red
    exit 1
}

Write-Host "Backup OK: $backupFile" -ForegroundColor Green

if ($VerifyAfterBackup) {
    Write-Host "==> VERIFYONLY"
    $verifySql = @"
RESTORE VERIFYONLY FROM DISK = N'$diskSql' WITH CHECKSUM;
"@
    & sqlcmd -S $ServerInstance -E -Q $verifySql -b
    if ($LASTEXITCODE -ne 0) {
        Write-Host "VERIFYONLY failed (exit $LASTEXITCODE)" -ForegroundColor Red
        exit 1
    }
    Write-Host "Verify OK" -ForegroundColor Green
}

Write-Host ""
Write-Host "Backup path: $backupFile"
exit 0
