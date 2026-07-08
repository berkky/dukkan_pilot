#Requires -Version 5.1
<#
.SYNOPSIS
  Validate public demo menu readiness without placing an order.
.PARAMETER BaseUrl
  Default http://localhost:5000
.PARAMETER DemoSlug
  Default demo-kafe
.NOTES
  Does not POST orders. Read-only GET checks.
#>
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$DemoSlug = "demo-kafe"
)

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue
$BaseUrl = $BaseUrl.TrimEnd("/")
$DemoSlug = $DemoSlug.Trim()
if ([string]::IsNullOrWhiteSpace($DemoSlug)) { $DemoSlug = "demo-kafe" }

$failed = $false

function Write-Ok([string]$msg) { Write-Host "[OK]  $msg" -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host "[FAIL] $msg" -ForegroundColor Red; $script:failed = $true }

function Get-Body([string]$Url) {
    try {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AllowAutoRedirect = $true
        $client = New-Object System.Net.Http.HttpClient($handler)
        $client.Timeout = [TimeSpan]::FromSeconds(30)
        $resp = $client.GetAsync($Url).GetAwaiter().GetResult()
        $code = [int]$resp.StatusCode
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $client.Dispose(); $handler.Dispose()
        return @{ Status = $code; Body = $body }
    } catch {
        return @{ Status = $null; Body = ""; Error = $_.Exception.Message }
    }
}

Write-Host "==> DukkanPilot public demo readiness" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host "DemoSlug: $DemoSlug"
Write-Host ""

$path = "/m/$DemoSlug"
$resp = Get-Body "$BaseUrl$path"
if ($null -eq $resp.Status) {
    Write-Fail "Request failed: $($resp.Error)"
} elseif ($resp.Status -ne 200) {
    Write-Fail "$path expected 200 got $($resp.Status)"
} else {
    Write-Ok "$path 200"
}

if ($resp.Status -eq 200) {
    # Light-weight content checks to reduce false positives.
    if ($resp.Body -notmatch "(?i)demo\s*kafe|dukkanpilot|sepete ekle|₺|kampanya|kategori") {
        Write-Fail "Demo page content does not look like a menu (no expected keywords found)."
    } else {
        Write-Ok "Demo page contains expected menu keywords"
    }
}

Write-Host ""
if ($failed) {
    Write-Host "public-demo-readiness FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "public-demo-readiness PASSED" -ForegroundColor Green
exit 0

