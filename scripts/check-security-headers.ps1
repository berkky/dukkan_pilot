#Requires -Version 5.1
<#
.SYNOPSIS
  Validate SecurityHeadersMiddleware headers on a path.
.PARAMETER BaseUrl
  Default http://localhost:5000
.PARAMETER Path
  Default /
.NOTES
  Assumes app is running.
#>
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Path = "/"
)

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue
$BaseUrl = $BaseUrl.TrimEnd("/")
if ([string]::IsNullOrWhiteSpace($Path)) { $Path = "/" }
if (-not $Path.StartsWith("/")) { $Path = "/" + $Path }

$failed = $false
$warnings = @()

function Write-Ok([string]$msg) { Write-Host "[OK]  $msg" -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host "[FAIL] $msg" -ForegroundColor Red; $script:failed = $true }
function Write-WarnMsg([string]$msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow; $script:warnings += $msg }

function Get-Headers {
    param([string]$Url)
    try {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AllowAutoRedirect = $false
        $client = New-Object System.Net.Http.HttpClient($handler)
        $client.Timeout = [TimeSpan]::FromSeconds(30)
        $response = $client.GetAsync($Url).GetAwaiter().GetResult()
        $headers = @{}
        foreach ($h in $response.Headers) { $headers[$h.Key] = ($h.Value -join ", ") }
        foreach ($h in $response.Content.Headers) { $headers[$h.Key] = ($h.Value -join ", ") }
        $code = [int]$response.StatusCode
        $client.Dispose(); $handler.Dispose()
        return @{ Status = $code; Headers = $headers }
    } catch {
        return @{ Status = $null; Headers = @{}; Error = $_.Exception.Message }
    }
}

Write-Host "==> DukkanPilot security headers check" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host "Path: $Path"
Write-Host ""

$url = "$BaseUrl$Path"
$resp = Get-Headers -Url $url
if ($null -eq $resp.Status) {
    Write-Fail "Request failed: $($resp.Error)"
    exit 1
}

if ($resp.Status -lt 200 -or $resp.Status -ge 500) {
    Write-WarnMsg "Non-200 response status: $($resp.Status) (still checking headers)"
}

$expected = @{
    "X-Content-Type-Options" = "nosniff"
    "X-Frame-Options"        = "SAMEORIGIN"
    "Referrer-Policy"        = "strict-origin-when-cross-origin"
    "Permissions-Policy"     = "camera=(), microphone=(), geolocation=()"
}

foreach ($key in $expected.Keys) {
    if (-not $resp.Headers.ContainsKey($key)) {
        Write-Fail "Missing header: $key"
        continue
    }

    $actual = $resp.Headers[$key]
    $want = $expected[$key]
    if ($actual -ne $want) {
        Write-WarnMsg "Header value differs: $key expected '$want' got '$actual'"
    } else {
        Write-Ok "$key = $actual"
    }
}

Write-Host ""
if ($failed) {
    Write-Host "security-headers FAILED" -ForegroundColor Red
    exit 1
}

if ($warnings.Count -gt 0) {
    Write-Host "security-headers PASSED with WARNINGS" -ForegroundColor Yellow
    exit 0
}

Write-Host "security-headers PASSED" -ForegroundColor Green
exit 0
