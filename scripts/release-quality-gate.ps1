#Requires -Version 5.1
<#
.SYNOPSIS
  Release quality gate: build + release checks + migration status + optional web checks.
.PARAMETER BaseUrl
  Default http://localhost:5000
.PARAMETER SkipWebChecks
  Skip HTTP checks even if app is running.
.PARAMETER SkipMigrationScript
  Skip db-generate-migration-script.ps1 (can be slow / optional).
#>
param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$SkipWebChecks,
    [switch]$SkipMigrationScript,
    [switch]$SkipPerformanceSmoke,
    [int]$PerformanceWarningMs = 1500,
    [int]$PerformanceFailMs = 4000
)

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$BaseUrl = $BaseUrl.TrimEnd("/")
$failed = $false
$steps = @()

function Add-Step([string]$name, [string]$status, [string]$note = "") {
    $script:steps += [pscustomobject]@{ Step = $name; Status = $status; Note = $note }
}

function Run-Step([string]$name, [string]$command, [string]$workDir = $RepoRoot) {
    Write-Host ""
    Write-Host "==> $name" -ForegroundColor Cyan
    Push-Location $workDir
    try {
        & powershell -ExecutionPolicy Bypass -Command $command
        $code = $LASTEXITCODE
        if ($code -ne 0) {
            Add-Step $name "FAIL" "exit $code"
            $script:failed = $true
        } else {
            Add-Step $name "PASS" ""
        }
    } catch {
        Add-Step $name "FAIL" $_.Exception.Message
        $script:failed = $true
    } finally {
        Pop-Location
    }
}

function Url-Ok([string]$url) {
    try {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AllowAutoRedirect = $false
        $client = New-Object System.Net.Http.HttpClient($handler)
        $client.Timeout = [TimeSpan]::FromSeconds(5)
        $resp = $client.GetAsync($url).GetAwaiter().GetResult()
        $code = [int]$resp.StatusCode
        $client.Dispose(); $handler.Dispose()
        return ($code -ge 200 -and $code -lt 500)
    } catch {
        return $false
    }
}

Write-Host "==> DukkanPilot release quality gate" -ForegroundColor Cyan
Write-Host "Repo: $RepoRoot"
Write-Host "BaseUrl: $BaseUrl"

Write-Host ""
Write-Host "==> dotnet build" -ForegroundColor Cyan
dotnet build (Join-Path $RepoRoot "src\\DukkanPilot.Web\\DukkanPilot.Web.csproj") -c Release
if ($LASTEXITCODE -ne 0) { Add-Step "dotnet build" "FAIL" "exit $LASTEXITCODE"; $failed = $true } else { Add-Step "dotnet build" "PASS" "" }

Run-Step "check-release.ps1" ".\\scripts\\check-release.ps1"
Run-Step "db-migration-status.ps1" ".\\scripts\\db-migration-status.ps1"
Run-Step "check-integration-tests.ps1" ".\\scripts\\check-integration-tests.ps1"
Run-Step "check-mobile-api.ps1" ".\scripts\check-mobile-api.ps1 -NoBuild"

if (-not $SkipMigrationScript) {
    Run-Step "db-generate-migration-script.ps1" ".\\scripts\\db-generate-migration-script.ps1"
} else {
    Add-Step "db-generate-migration-script.ps1" "SKIP" "SkipMigrationScript"
}

$webAvailable = Url-Ok "$BaseUrl/health"
if ($SkipWebChecks) {
    Add-Step "Web checks" "SKIP" "SkipWebChecks"
} elseif (-not $webAvailable) {
    Add-Step "Web checks" "SKIP" "App not reachable at $BaseUrl (start app to run HTTP checks)"
} else {
    Run-Step "run-smoke-tests.ps1" ".\\scripts\\run-smoke-tests.ps1 -BaseUrl $BaseUrl"
    Run-Step "check-security-headers.ps1" ".\\scripts\\check-security-headers.ps1 -BaseUrl $BaseUrl -Path /"
    Run-Step "check-seo-endpoints.ps1" ".\\scripts\\check-seo-endpoints.ps1 -BaseUrl $BaseUrl"
    $demoSlugList = 'demo-kafe,demo-tatlici,demo-burgerci,demo-restoran,demo-nargile'
    Run-Step "check-public-demo-readiness.ps1" "& { .\scripts\check-public-demo-readiness.ps1 -BaseUrl '$BaseUrl' -DemoSlugs '$demoSlugList' }"
    if (-not $SkipPerformanceSmoke) {
        Run-Step "check-performance-smoke.ps1" "& { .\scripts\check-performance-smoke.ps1 -BaseUrl '$BaseUrl' -WarningMs $PerformanceWarningMs -FailMs $PerformanceFailMs }"
    } else {
        Add-Step "check-performance-smoke.ps1" "SKIP" "SkipPerformanceSmoke"
    }
}

Write-Host ""
Write-Host "==> Summary" -ForegroundColor Cyan
$steps | Format-Table -AutoSize Step, Status, Note

Write-Host ""
if ($failed) {
    Write-Host "release-quality-gate FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "release-quality-gate PASSED" -ForegroundColor Green
exit 0

