#Requires -Version 5.1
<#
.SYNOPSIS
  HTTP smoke tests against a running DukkanPilot instance.
.PARAMETER BaseUrl
  Default http://localhost:5000
.NOTES
  Does not start the app. Run Development on :5000 first.
#>
param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Continue"
$BaseUrl = $BaseUrl.TrimEnd("/")

$failed = $false
$results = @()

function Get-HttpStatus {
    param([string]$Url)

    # Prefer HttpClient so redirects are measurable without following them.
    try {
        $handler = New-Object System.Net.Http.HttpClientHandler
        $handler.AllowAutoRedirect = $false
        $client = New-Object System.Net.Http.HttpClient($handler)
        $client.Timeout = [TimeSpan]::FromSeconds(30)
        $response = $client.GetAsync($Url).GetAwaiter().GetResult()
        $code = [int]$response.StatusCode
        $client.Dispose()
        $handler.Dispose()
        return @{ Status = $code; Detail = "" }
    } catch {
        # Fallback: WebRequest
    }

    try {
        $req = [System.Net.HttpWebRequest]::Create($Url)
        $req.AllowAutoRedirect = $false
        $req.Method = "GET"
        $req.Timeout = 30000
        try {
            $resp = $req.GetResponse()
            $code = [int]$resp.StatusCode
            $resp.Close()
            return @{ Status = $code; Detail = "" }
        } catch [System.Net.WebException] {
            $webResp = $_.Exception.Response
            if ($webResp) {
                $code = [int]$webResp.StatusCode
                $webResp.Close()
                return @{ Status = $code; Detail = "" }
            }
            return @{ Status = $null; Detail = $_.Exception.Message }
        }
    } catch {
        return @{ Status = $null; Detail = $_.Exception.Message }
    }
}

function Test-Url {
    param(
        [string]$Path,
        [int[]]$ExpectedStatuses,
        [string]$Note = ""
    )

    $url = "$BaseUrl$Path"
    $hit = Get-HttpStatus -Url $url
    $status = $hit.Status
    $detail = $hit.Detail
    $ok = ($null -ne $status -and $ExpectedStatuses -contains $status)

    if (-not $ok) {
        $script:failed = $true
    }

    $script:results += [pscustomobject]@{
        Path     = $Path
        Status   = $(if ($null -eq $status) { "-" } else { $status })
        Expected = ($ExpectedStatuses -join "/")
        Result   = $(if ($ok) { "PASS" } else { "FAIL" })
        Note     = $(if ($detail) { $detail } else { $Note })
    }
}

Write-Host "==> DukkanPilot smoke tests" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host ""

Test-Url "/" @(200) "Landing"
Test-Url "/Pricing" @(200)
Test-Url "/Features" @(200)
Test-Url "/Demo" @(200)
Test-Url "/health" @(200) "JSON health"
Test-Url "/robots.txt" @(200)
Test-Url "/sitemap.xml" @(200)
Test-Url "/m/demo-kafe" @(200) "Public menu"
Test-Url "/Account/Login" @(200)
Test-Url "/Account/Register" @(200)

Test-Url "/Business/Dashboard" @(302, 301) "Auth redirect"
Test-Url "/Admin/Dashboard" @(302, 301) "Auth redirect"
Test-Url "/Business/DemoCenter" @(302, 301) "Auth redirect"
Test-Url "/Admin/SalesCenter" @(302, 301) "Auth redirect"
Test-Url "/Admin/Operations" @(302, 301) "Auth redirect"

$results | Format-Table -AutoSize Path, Status, Expected, Result, Note

Write-Host ""
$pass = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$total = $results.Count
Write-Host "Passed $pass / $total"

if ($failed) {
    Write-Host "smoke-tests FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "smoke-tests PASSED" -ForegroundColor Green
exit 0
