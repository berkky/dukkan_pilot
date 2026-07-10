#Requires -Version 5.1
<#
.SYNOPSIS
  Simple HTTP response-time smoke checks (not a benchmark).
.PARAMETER BaseUrl
  Default http://localhost:5000
.PARAMETER WarningMs
  Warn when response exceeds this threshold (default 1500).
.PARAMETER FailMs
  Fail when response exceeds this threshold (default 4000).
.PARAMETER Repeat
  Number of requests per path (default 1). First request may be slower (cold start).
.PARAMETER Paths
  Optional comma-separated paths. When omitted, default public routes are used.
.PARAMETER SkipAuthRedirects
  Reserved for future use; default paths are public-only.
.NOTES
  Exit 1 only on FAIL (non-200 or duration > FailMs). WARN does not fail the script.
#>
param(
    [string]$BaseUrl = "http://localhost:5000",
    [int]$WarningMs = 1500,
    [int]$FailMs = 4000,
    [int]$Repeat = 1,
    [string]$Paths = "",
    [switch]$SkipAuthRedirects
)

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue

$BaseUrl = $BaseUrl.TrimEnd("/")
if ($Repeat -lt 1) { $Repeat = 1 }

$defaultPaths = @(
    "/",
    "/Pricing",
    "/DemoPacks",
    "/RoiCalculator",
    "/Help",
    "/m/demo-kafe",
    "/m/demo-kafe?table=TBL-KAFE-1",
    "/m/demo-tatlici",
    "/m/demo-burgerci",
    "/m/demo-restoran",
    "/m/demo-nargile",
    "/health"
)

$pathList = @()
if (-not [string]::IsNullOrWhiteSpace($Paths)) {
    $pathList = $Paths.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
} else {
    $pathList = $defaultPaths
}

$failed = $false
$warned = $false
$results = @()

function Measure-Get {
    param([string]$Url)

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AllowAutoRedirect = $true
        $client = New-Object System.Net.Http.HttpClient($handler)
        $client.Timeout = [TimeSpan]::FromSeconds(60)
        $response = $client.GetAsync($Url).GetAwaiter().GetResult()
        $code = [int]$response.StatusCode
        $client.Dispose()
        $handler.Dispose()
        $sw.Stop()
        return @{ Status = $code; Ms = [int]$sw.ElapsedMilliseconds; Error = "" }
    } catch {
        $sw.Stop()
        return @{ Status = $null; Ms = [int]$sw.ElapsedMilliseconds; Error = $_.Exception.Message }
    }
}

Write-Host "==> DukkanPilot performance smoke" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host "WarningMs: $WarningMs | FailMs: $FailMs | Repeat: $Repeat"
Write-Host "Note: First request after app start may be slower (cold start). This is a smoke check, not a benchmark."
Write-Host ""

foreach ($path in $pathList) {
    if (-not $path.StartsWith("/")) { $path = "/$path" }
    $url = "$BaseUrl$path"

    $lastMs = 0
    $lastStatus = $null
    $lastError = ""

    for ($i = 1; $i -le $Repeat; $i++) {
        $hit = Measure-Get -Url $url
        $lastMs = $hit.Ms
        $lastStatus = $hit.Status
        $lastError = $hit.Error
    }

    $statusLabel = "PASS"
    $note = ""

    if ($null -eq $lastStatus -or $lastStatus -lt 200 -or $lastStatus -ge 400) {
        $statusLabel = "FAIL"
        $script:failed = $true
        if ($lastError) { $note = $lastError } else { $note = "HTTP $lastStatus" }
    } elseif ($lastMs -gt $FailMs) {
        $statusLabel = "FAIL"
        $script:failed = $true
        $note = "Slow: ${lastMs}ms > FailMs ${FailMs}ms"
    } elseif ($lastMs -gt $WarningMs) {
        $statusLabel = "WARN"
        $script:warned = $true
        $note = "Slow: ${lastMs}ms > WarningMs ${WarningMs}ms"
    }

    $results += [pscustomobject]@{
        Path = $path
        StatusCode = $(if ($null -eq $lastStatus) { "-" } else { $lastStatus })
        Ms = $lastMs
        Result = $statusLabel
        Note = $note
    }
}

$results | Format-Table -AutoSize Path, StatusCode, Ms, Result, Note

Write-Host ""
$passCount = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$warnCount = ($results | Where-Object { $_.Result -eq "WARN" }).Count
$failCount = ($results | Where-Object { $_.Result -eq "FAIL" }).Count
Write-Host "Summary: PASS $passCount | WARN $warnCount | FAIL $failCount"

if ($failed) {
    Write-Host "performance-smoke FAILED" -ForegroundColor Red
    exit 1
}

if ($warned) {
    Write-Host "performance-smoke PASSED with WARNINGS" -ForegroundColor Yellow
    exit 0
}

Write-Host "performance-smoke PASSED" -ForegroundColor Green
exit 0
