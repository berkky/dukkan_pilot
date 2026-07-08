#Requires -Version 5.1
<#
.SYNOPSIS
  Restores a .bak into a separate TEST database (never auto-targets production DB).
.NOTES
  Does NOT DROP databases. Does NOT restore onto DukkanPilotDb unless -Force (with loud warning).
#>
param(
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,
    [string]$RestoreDatabaseName = "",
    [string]$DataDirectory = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

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

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Host "sqlcmd bulunamadi. SQL Server Command Line Utilities kurulu olmali veya backup islemini SSMS uzerinden yapin." -ForegroundColor Red
    exit 1
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

# Safety first: refuse primary DB name before touching files or SQL.
if ([string]::IsNullOrWhiteSpace($RestoreDatabaseName)) {
    $RestoreDatabaseName = "DukkanPilot_RestoreTest_" + (Get-Date -Format "yyyyMMddHHmmss")
}
Test-SafeSqlIdentifier $RestoreDatabaseName "RestoreDatabaseName"

$isProtected = ($RestoreDatabaseName -ieq "DukkanPilotDb")

if ($isProtected -and -not $Force) {
    Write-Host "REFUSED: RestoreDatabaseName='DukkanPilotDb' is the primary application database." -ForegroundColor Red
    Write-Host "This script restores into a TEST database by default." -ForegroundColor Yellow
    Write-Host "Use DukkanPilot_RestoreTest_... or pass -Force only if you fully accept overwrite risk." -ForegroundColor Yellow
    exit 1
}

if ($isProtected -and $Force) {
    Write-Host "DANGER: -Force restore onto DukkanPilotDb will OVERWRITE that database." -ForegroundColor Red
    Write-Host "Abort now if this is a live/production instance." -ForegroundColor Red
    Start-Sleep -Seconds 3
}

$fullBak = $BackupFile
if (-not [System.IO.Path]::IsPathRooted($fullBak)) {
    $fullBak = Join-Path $RepoRoot $BackupFile
}
if (-not (Test-Path -LiteralPath $fullBak)) {
    Write-Host "Backup file not found: $fullBak" -ForegroundColor Red
    exit 1
}
$resolvedBak = (Resolve-Path -LiteralPath $fullBak).Path
Test-SafeDiskPath $resolvedBak "BackupFile"

$diskSql = $resolvedBak.Replace("'", "''")

Write-Host "==> DukkanPilot db-restore-test" -ForegroundColor Cyan
Write-Host "Server:  $ServerInstance"
Write-Host "Backup:  $resolvedBak"
Write-Host "Target:  $RestoreDatabaseName"
Write-Host ""

# Capture logical names via temp table (avoids brittle text parsing of FILELISTONLY headers)
$probe = @"
SET NOCOUNT ON;
CREATE TABLE #fl (
  LogicalName nvarchar(128),
  PhysicalName nvarchar(260),
  Type char(1),
  FileGroupName nvarchar(128),
  Size numeric(20,0),
  MaxSize numeric(20,0),
  FileId bigint,
  CreateLSN uniqueidentifier,
  DropLSN uniqueidentifier,
  UniqueId uniqueidentifier,
  ReadOnlyLSN uniqueidentifier,
  ReadWriteLSN uniqueidentifier,
  BackupSizeInBytes bigint,
  SourceBlockSize int,
  FileGroupId int,
  LogGroupGUID uniqueidentifier,
  DifferentialBaseLSN uniqueidentifier,
  DifferentialBaseGUID uniqueidentifier,
  IsReadOnly bit,
  IsPresent bit,
  TDEThumbprint varbinary(32)
);
BEGIN TRY
  ALTER TABLE #fl ADD SnapshotUrl nvarchar(360) NULL;
END TRY BEGIN CATCH END CATCH;
INSERT INTO #fl EXEC('RESTORE FILELISTONLY FROM DISK = N''$diskSql''');
SELECT LogicalName + N'|' + Type FROM #fl WHERE Type IN (N'D', N'L');
DROP TABLE #fl;
"@

$probeOut = & sqlcmd -S $ServerInstance -E -Q $probe -h -1 -W 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "RESTORE FILELISTONLY failed:" -ForegroundColor Red
    Write-Host ($probeOut | Out-String)
    exit 1
}

$logicalData = $null
$logicalLog = $null
foreach ($line in ($probeOut | Out-String) -split "`r?`n") {
    $trim = $line.Trim()
    if ($trim -match '^(.+)\|D$') { if (-not $logicalData) { $logicalData = $Matches[1].Trim() } }
    elseif ($trim -match '^(.+)\|L$') { if (-not $logicalLog) { $logicalLog = $Matches[1].Trim() } }
}

if (-not $logicalData -or -not $logicalLog) {
    Write-Host "Could not determine logical data/log names from backup." -ForegroundColor Red
    Write-Host ($probeOut | Out-String)
    exit 1
}

# Soften validation: logical names from SQL Server may include more than [A-Za-z0-9_]
if ($logicalData -match "[;']" -or $logicalLog -match "[;']") {
    throw "Logical file name contains unsafe characters."
}

if ([string]::IsNullOrWhiteSpace($DataDirectory)) {
    $DataDirectory = Join-Path $RepoRoot "artifacts\db-restore-data"
}
if (-not (Test-Path $DataDirectory)) {
    New-Item -ItemType Directory -Path $DataDirectory -Force | Out-Null
}
Test-SafeDiskPath $DataDirectory "DataDirectory"

$mdf = Join-Path $DataDirectory ($RestoreDatabaseName + ".mdf")
$ldf = Join-Path $DataDirectory ($RestoreDatabaseName + "_log.ldf")
Test-SafeDiskPath $mdf "MdfPath"
Test-SafeDiskPath $ldf "LdfPath"
$mdfSql = $mdf.Replace("'", "''")
$ldfSql = $ldf.Replace("'", "''")
$ld = $logicalData.Replace("'", "''")
$ll = $logicalLog.Replace("'", "''")

$restoreSql = @"
RESTORE DATABASE [$RestoreDatabaseName]
FROM DISK = N'$diskSql'
WITH MOVE N'$ld' TO N'$mdfSql',
     MOVE N'$ll' TO N'$ldfSql',
     REPLACE,
     CHECKSUM,
     STATS = 10;
"@

Write-Host "Logical data: $logicalData"
Write-Host "Logical log:  $logicalLog"
Write-Host "MDF: $mdf"
Write-Host "LDF: $ldf"
Write-Host ""

& sqlcmd -S $ServerInstance -E -Q $restoreSql -b
if ($LASTEXITCODE -ne 0) {
    Write-Host "RESTORE failed (exit $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

Write-Host "==> DBCC CHECKDB"
& sqlcmd -S $ServerInstance -E -Q "DBCC CHECKDB (N'$RestoreDatabaseName') WITH NO_INFOMSGS;" -b
if ($LASTEXITCODE -ne 0) {
    Write-Host "DBCC CHECKDB reported failure (exit $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Restore-test OK" -ForegroundColor Green
Write-Host "Restored test database: $RestoreDatabaseName"
Write-Host "This script does NOT drop the test database."
Write-Host "Manual cleanup (optional):"
Write-Host ("  sqlcmd -S `"{0}`" -E -Q `"ALTER DATABASE [{1}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{1}];`"" -f $ServerInstance, $RestoreDatabaseName)
exit 0
