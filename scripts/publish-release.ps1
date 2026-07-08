#Requires -Version 5.1
<#
.SYNOPSIS
  Builds and publishes DukkanPilot.Web (Release) to artifacts/publish/DukkanPilot.Web.
.NOTES
  Does NOT apply migrations or inject production secrets.
#>
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$Project = Join-Path $RepoRoot "src\DukkanPilot.Web\DukkanPilot.Web.csproj"
$OutDir = Join-Path $RepoRoot "artifacts\publish\DukkanPilot.Web"

Write-Host "==> DukkanPilot publish-release" -ForegroundColor Cyan
Write-Host "Repo: $RepoRoot"
Write-Host "Output: $OutDir"

if (-not (Test-Path $Project)) {
    throw "Web project not found: $Project"
}

if (Test-Path $OutDir) {
    Write-Host "==> Cleaning previous publish folder..."
    Remove-Item -Recurse -Force $OutDir
}
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

Write-Host "==> dotnet restore"
dotnet restore $Project
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed (exit $LASTEXITCODE)" }

Write-Host "==> dotnet build -c Release"
dotnet build $Project -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }

Write-Host "==> dotnet publish -c Release"
dotnet publish $Project -c Release -o $OutDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

$dll = Join-Path $OutDir "DukkanPilot.Web.dll"
if (-not (Test-Path $dll)) {
    throw "Publish succeeded but DukkanPilot.Web.dll was not found in $OutDir"
}

Write-Host ""
Write-Host "Publish OK." -ForegroundColor Green
Write-Host "Publish path: $OutDir"
Write-Host "Next: copy to server, configure appsettings.Production.json, then apply migrations intentionally."
exit 0
