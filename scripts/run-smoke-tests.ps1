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

function Add-Result {
    param(
        [string]$Group,
        [string]$Path,
        [string]$Method,
        [string]$Expected,
        [string]$Actual,
        [string]$Status,
        [string]$Note
    )

    $script:results += [pscustomobject]@{
        Group    = $Group
        Method   = $Method
        Path     = $Path
        Expected = $Expected
        Actual   = $Actual
        Status   = $Status
        Note     = $Note
    }
}

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
        $location = $response.Headers.Location
        $body = ""
        try { $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() } catch { }
        $client.Dispose()
        $handler.Dispose()
        return @{ Status = $code; Detail = ""; Location = $(if ($location) { $location.ToString() } else { "" }); Body = $body }
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
            $location = $resp.Headers["Location"]
            $resp.Close()
            return @{ Status = $code; Detail = ""; Location = $location; Body = "" }
        } catch [System.Net.WebException] {
            $webResp = $_.Exception.Response
            if ($webResp) {
                $code = [int]$webResp.StatusCode
                $location = $webResp.Headers["Location"]
                $webResp.Close()
                return @{ Status = $code; Detail = ""; Location = $location; Body = "" }
            }
            return @{ Status = $null; Detail = $_.Exception.Message; Location = ""; Body = "" }
        }
    } catch {
        return @{ Status = $null; Detail = $_.Exception.Message; Location = ""; Body = "" }
    }
}

function Test-Url {
    param(
        [string]$Group,
        [string]$Path,
        [int[]]$ExpectedStatuses,
        [string]$Note = "",
        [string]$Method = "GET",
        [string]$MustContain = ""
    )

    $url = "$BaseUrl$Path"
    $hit = Get-HttpStatus -Url $url
    $status = $hit.Status
    $detail = $hit.Detail
    $location = $hit.Location

    $okStatus = ($null -ne $status -and $ExpectedStatuses -contains $status)
    $okContent = $true
    if ($okStatus -and -not [string]::IsNullOrWhiteSpace($MustContain)) {
        if ([string]::IsNullOrWhiteSpace($hit.Body)) {
            $okContent = $true
        } else {
            $okContent = ($hit.Body -match [regex]::Escape($MustContain))
        }
    }

    $ok = $okStatus -and $okContent

    if (-not $ok) {
        $script:failed = $true
    }

    $expectedText = ($ExpectedStatuses -join "/")
    $actualText = $(if ($null -eq $status) { "-" } else { $status })
    if (-not [string]::IsNullOrWhiteSpace($location)) { $actualText = "$actualText → $location" }
    if (-not $okStatus -and $detail) { $Note = $detail }
    if ($okStatus -and -not $okContent) { $Note = "Response did not contain: $MustContain" }
    if ($okStatus -and -not [string]::IsNullOrWhiteSpace($MustContain) -and [string]::IsNullOrWhiteSpace($hit.Body)) { $Note = "Body empty - skipped content check" }

    Add-Result -Group $Group -Method $Method -Path $Path -Expected $expectedText -Actual $actualText -Status $(if ($ok) { "PASS" } else { "FAIL" }) -Note $Note
}

Write-Host "==> DukkanPilot smoke tests" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host ""

Test-Url "PublicMarketing" "/" @(200) "Landing"
Test-Url "PublicMarketing" "/Pricing" @(200)
Test-Url "PublicMarketing" "/Features" @(200)
Test-Url "PublicMarketing" "/Demo" @(200)
Test-Url "PublicMarketing" "/Trust" @(200) "Trust Center"

Test-Url "PublicMarketing" "/Help" @(200) "Help Center"
Test-Url "PublicMarketing" "/Help/nedir" @(200) "Help article"

Test-Url "Legal" "/Privacy" @(200) "Legal"
Test-Url "Legal" "/Terms" @(200) "Legal"
Test-Url "Legal" "/Kvkk" @(200) "Legal"
Test-Url "Legal" "/Cookies" @(200) "Legal"
Test-Url "Legal" "/DataProcessing" @(200) "Legal"

Test-Url "Sales" "/Sales/RequestDemo" @(200) "Sales form"
Test-Url "Sales" "/Sales/RequestPlan" @(200) "Sales form"

Test-Url "PublicMenu" "/m/demo-kafe" @(200) "Public menu"

Test-Url "System" "/health" @(200) "JSON health" -MustContain '"status"'
Test-Url "System" "/robots.txt" @(200)
Test-Url "System" "/sitemap.xml" @(200)

Test-Url "Account" "/Account/Login" @(200)
Test-Url "Account" "/Account/Register" @(200)

Test-Url "AuthRedirects" "/Business/Dashboard" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Business/Onboarding" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Business/Success" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Business/Billing/Requests" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Business/Billing/Invoices" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Business/Billing/Payments" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Business/HelpCenter" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Admin/Dashboard" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Admin/SalesRequests" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Admin/Onboarding" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Admin/CustomerSuccess" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Admin/Operations" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Admin/Billing" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Admin/Billing/Payments" @(302, 301) "Auth redirect"
Test-Url "AuthRedirects" "/Admin/HelpCenter" @(302, 301) "Auth redirect"

$results | Sort-Object Group, Path | Format-Table -AutoSize Group, Path, Expected, Actual, Status, Note

Write-Host ""
$pass = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$total = $results.Count
Write-Host "Passed $pass / $total"

if ($failed) {
    Write-Host ""
    Write-Host "Failing checks:" -ForegroundColor Yellow
    $results | Where-Object { $_.Status -eq "FAIL" } | Format-Table -AutoSize Group, Path, Expected, Actual, Status, Note
}

if ($failed) {
    Write-Host "smoke-tests FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "smoke-tests PASSED" -ForegroundColor Green
exit 0
