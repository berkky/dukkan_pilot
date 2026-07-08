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
$warned = $false

function Write-Ok([string]$msg) { Write-Host "[OK]  $msg" -ForegroundColor Green }
function Write-Fail([string]$msg) { Write-Host "[FAIL] $msg" -ForegroundColor Red; $script:failed = $true }
function Write-WarnMsg([string]$msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow; $script:warned = $true }

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
    # Critical: no raw exceptions / stack traces
    $badPatterns = @("System.NullReferenceException", "StackTrace", "InvalidOperationException", "Unhandled", "Developer Exception Page", "at Microsoft.")
    foreach ($p in $badPatterns) {
        if ($resp.Body -match [regex]::Escape($p)) {
            Write-Fail "Response contains server error trace/pattern: $p"
        }
    }

    # Critical: basic UI anchors exist
    if ($resp.Body -notmatch 'id="public-menu-root"') { Write-Fail "Missing public menu root element" }
    if ($resp.Body -notmatch 'class="category-nav') { Write-Fail "Missing category navigation" }
    if ($resp.Body -notmatch 'class="product-card"') { Write-Fail "Missing product cards" }
    if ($resp.Body -notmatch 'id="cartOffcanvas"') { Write-Fail "Missing cart drawer/offcanvas" }
    if ($resp.Body -notmatch 'Sepete\s*Ekle') { Write-Fail "Missing 'Sepete Ekle' CTA" }
    if ($resp.Body -notmatch 'id="customer-name"') { Write-Fail "Missing order form fields" }

    # Non-critical: campaigns / rewards can be empty; warn if missing on demo
    if ($resp.Body -notmatch 'Aktif Fırsatlar|public-campaign-card') {
        Write-WarnMsg "No campaign showcase detected on demo page (ok, but demo is stronger with campaigns)."
    } else {
        Write-Ok "Campaign showcase detected"
    }

    if ($resp.Body -notmatch 'Sadakat Ödülleri|public-reward-card') {
        Write-WarnMsg "No rewards showcase detected on demo page (ok, optional)."
    } else {
        Write-Ok "Rewards showcase detected"
    }

    # Safety: no private/admin links leaked
    $private = @("/Admin/", "/Business/", "/Account/")
    foreach ($bad in $private) {
        if ($resp.Body -match [regex]::Escape($bad)) {
            Write-WarnMsg ("Found private path in HTML: " + $bad + " (verify it is not a clickable link)")
        }
    }
}

Write-Host ""
if ($failed) {
    Write-Host "public-demo-readiness FAILED" -ForegroundColor Red
    exit 1
}

if ($warned) {
    Write-Host "public-demo-readiness PASSED with WARNINGS" -ForegroundColor Yellow
    exit 0
}

Write-Host "public-demo-readiness PASSED" -ForegroundColor Green
exit 0

