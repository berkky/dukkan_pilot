#Requires -Version 5.1
<#
.SYNOPSIS
  Validate robots.txt and sitemap.xml contents for SEO and privacy safety.
.PARAMETER BaseUrl
  Default http://localhost:5000
.NOTES
  Assumes app is running.
#>
param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Continue"
Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue
$BaseUrl = $BaseUrl.TrimEnd("/")
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

Write-Host "==> DukkanPilot SEO endpoints check" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host ""

$robots = Get-Body "$BaseUrl/robots.txt"
if ($null -eq $robots.Status) { Write-Fail "robots.txt request failed: $($robots.Error)" }
elseif ($robots.Status -ne 200) { Write-Fail "robots.txt expected 200 got $($robots.Status)" }
else { Write-Ok "/robots.txt 200" }

$sitemap = Get-Body "$BaseUrl/sitemap.xml"
if ($null -eq $sitemap.Status) { Write-Fail "sitemap.xml request failed: $($sitemap.Error)" }
elseif ($sitemap.Status -ne 200) { Write-Fail "sitemap.xml expected 200 got $($sitemap.Status)" }
else { Write-Ok "/sitemap.xml 200" }

if ($robots.Status -eq 200) {
    $mustDisallow = @("/Admin", "/Business", "/Account")
    foreach ($p in $mustDisallow) {
        if ($robots.Body -notmatch [regex]::Escape("Disallow: $p")) {
            Write-Fail "robots.txt missing disallow: $p"
        }
    }
}

if ($sitemap.Status -eq 200) {
    $mustHave = @(
        "/",
        "/Pricing",
        "/Features",
        "/Demo",
        "/Trust",
        "/Privacy",
        "/Kvkk",
        "/Sales/RequestDemo",
        "/Sales/RequestPlan"
    )
    foreach ($path in $mustHave) {
        if ($sitemap.Body -notmatch [regex]::Escape(">$BaseUrl$path<") -and $sitemap.Body -notmatch [regex]::Escape(">$path<")) {
            # sitemap uses full baseUrl; allow either just in case
            Write-Fail "sitemap missing url: $path"
        }
    }

    $mustNotContain = @("/Admin/", "/Business/", "/Account/", "utm_", "tracking", "token")
    foreach ($bad in $mustNotContain) {
        if ($sitemap.Body -match [regex]::Escape($bad)) {
            Write-Fail "sitemap contains private/sensitive pattern: $bad"
        }
    }
}

Write-Host ""
if ($failed) {
    Write-Host "seo-endpoints FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "seo-endpoints PASSED" -ForegroundColor Green
exit 0
