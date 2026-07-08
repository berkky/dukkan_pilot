#Requires -Version 5.1
# Local pre-release checks: build, EF pending model changes, critical docs/files.
$ErrorActionPreference = "Continue"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$failed = $false

function Write-Ok([string]$msg) { Write-Host "[OK]  $msg" -ForegroundColor Green }
function Write-Fail([string]$msg) {
    Write-Host "[FAIL] $msg" -ForegroundColor Red
    $script:failed = $true
}
function Write-WarnMsg([string]$msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }

Write-Host "==> DukkanPilot check-release" -ForegroundColor Cyan
Write-Host "Repo: $RepoRoot"
Write-Host ""

Write-Host "==> dotnet build"
$buildOk = $false
for ($i = 1; $i -le 3; $i++) {
    $out = & dotnet build (Join-Path $RepoRoot "src\DukkanPilot.Web\DukkanPilot.Web.csproj") -c Release 2>&1
    $code = $LASTEXITCODE
    Write-Host ($out | Out-String)

    if ($code -eq 0) {
        $buildOk = $true
        break
    }

    $text = ($out | Out-String)
    if ($text -match "CS2012" -or $text -match "being used by another process" -or $text -match "VBCSCompiler" -or $text -match "Microsoft Defender") {
        Write-WarnMsg "dotnet build failed due to file lock (attempt $i/3). Retrying..."
        Start-Sleep -Seconds 2
        continue
    }

    break
}

if (-not $buildOk) {
    Write-Fail "dotnet build failed"
} else {
    Write-Ok "dotnet build"
}

Write-Host ""
Write-Host "==> EF has-pending-model-changes"
$infra = Join-Path $RepoRoot "src\DukkanPilot.Infrastructure\DukkanPilot.Infrastructure.csproj"
$web = Join-Path $RepoRoot "src\DukkanPilot.Web\DukkanPilot.Web.csproj"
# Use Release + --no-build to avoid Debug file locks when app is running.
$efOut = & dotnet ef migrations has-pending-model-changes --project $infra --startup-project $web --configuration Release --no-build 2>&1
$efCode = $LASTEXITCODE
$efText = ($efOut | Out-String)

if ($efCode -ne 0 -and ($efText -match "Could not execute because the specified command|No executable found matching command|dotnet-ef")) {
    Write-Fail "dotnet ef tool not available. Install on your machine only: dotnet tool install --global dotnet-ef (script does not install tools)."
    Write-Host $efText
} elseif ($efCode -ne 0) {
    Write-Fail "dotnet ef has-pending-model-changes failed (exit $efCode)"
    Write-Host $efText
} elseif ($efText -match "(?i)No changes have been made to the model") {
    Write-Ok "No pending EF model changes"
} elseif ($efText -match "(?i)Changes have been made|pending") {
    Write-Fail "Pending EF model changes detected - create a migration before release"
    Write-Host $efText
} else {
    Write-WarnMsg "EF check completed with exit $efCode - review output"
    Write-Host $efText
    if ($efCode -ne 0) { $failed = $true }
}

Write-Host ""
Write-Host "==> Critical files"

$required = @(
    "src\DukkanPilot.Web\appsettings.Production.example.json",
    "docs\DEPLOYMENT_CHECKLIST.md",
    "docs\RELEASE_CHECKLIST.md",
    "docs\SMOKE_TEST_CHECKLIST.md",
    "docs\PRODUCTION_CONFIGURATION.md",
    "docs\IIS_DEPLOYMENT_GUIDE.md",
    "docs\Kestrel_SERVICE_GUIDE.md",
    "docs\DATABASE_BACKUP_AND_RECOVERY.md",
    "docs\MIGRATION_RUNBOOK.md",
    "docs\INCIDENT_RESPONSE_RUNBOOK.md",
    "docs\OPERATIONAL_SECURITY_CHECKLIST.md",
    "docs\FIRST_RELEASE_OPERATIONS.md",
    "docs\LEGAL_READINESS_CHECKLIST.md",
    "docs\PRIVACY_AND_DATA_MAP.md",
    "docs\COOKIE_AND_TRACKING_NOTES.md",
    "docs\TERMS_TEMPLATE_NOTES.md",
    "docs\SALES_PIPELINE_RUNBOOK.md",
    "docs\SALES_REQUEST_DATA_MAP.md",
    "docs\CUSTOMER_ONBOARDING_RUNBOOK.md",
    "docs\KICKOFF_MEETING_SCRIPT.md",
    "docs\IMPLEMENTATION_HANDOFF_CHECKLIST.md",
    "docs\CUSTOMER_SUCCESS_PLAYBOOK.md",
    "docs\CUSTOMER_SUCCESS_HEALTH_SCORE.md",
    "docs\RETENTION_PLAYBOOK.md",
    "docs\UPGRADE_OPPORTUNITY_PLAYBOOK.md",
    "docs\CHURN_RISK_RUNBOOK.md",
    "src\DukkanPilot.Web\Helpers\CustomerOnboardingHelper.cs",
    "src\DukkanPilot.Web\Helpers\CustomerSuccessHealthHelper.cs",
    "src\DukkanPilot.Web\Areas\Business\Controllers\OnboardingController.cs",
    "src\DukkanPilot.Web\Areas\Admin\Controllers\OnboardingController.cs",
    "src\DukkanPilot.Web\Areas\Business\Controllers\SuccessController.cs",
    "src\DukkanPilot.Web\Areas\Admin\Controllers\CustomerSuccessController.cs",
    "scripts\publish-release.ps1",
    "scripts\run-smoke-tests.ps1",
    "scripts\check-security-headers.ps1",
    "scripts\check-seo-endpoints.ps1",
    "scripts\check-public-demo-readiness.ps1",
    "scripts\release-quality-gate.ps1",
    "scripts\db-backup.ps1",
    "scripts\db-verify-backup.ps1",
    "scripts\db-restore-test.ps1",
    "scripts\db-generate-migration-script.ps1",
    "scripts\db-migration-status.ps1",
    "docs\QA_TEST_PLAN.md",
    "docs\REGRESSION_TEST_MATRIX.md",
    "docs\UAT_SCRIPT_FIRST_CUSTOMER.md",
    "docs\BUG_REPORT_TEMPLATE.md",
    "docs\RELEASE_QUALITY_GATE.md",
    "src\DukkanPilot.Web\Areas\Admin\Controllers\QualityController.cs",
    "src\DukkanPilot.Web\Areas\Admin\Models\AdminQualityViewModel.cs",
    "src\DukkanPilot.Web\Areas\Admin\Views\Quality\Index.cshtml",
    "src\DukkanPilot.Web\Controllers\LegalController.cs",
    "src\DukkanPilot.Web\Views\Legal\Privacy.cshtml",
    "src\DukkanPilot.Web\Views\Legal\Trust.cshtml",
    "src\DukkanPilot.Web\Views\Shared\_CookieNotice.cshtml",
    "src\DukkanPilot.Web\wwwroot\js\cookie-notice.js",
    "src\DukkanPilot.Core\Entities\SalesRequest.cs",
    "src\DukkanPilot.Web\Controllers\SalesRequestController.cs",
    "src\DukkanPilot.Web\Services\SalesRequestService.cs",
    "src\DukkanPilot.Web\Areas\Admin\Controllers\SalesRequestsController.cs"
)

foreach ($rel in $required) {
    $path = Join-Path $RepoRoot $rel
    if (Test-Path $path) {
        Write-Ok $rel
    } else {
        Write-Fail "Missing: $rel"
    }
}

$prodExample = Join-Path $RepoRoot "src\DukkanPilot.Web\appsettings.Production.example.json"
if (Test-Path $prodExample) {
    try {
        $null = Get-Content $prodExample -Raw -Encoding UTF8 | ConvertFrom-Json
        Write-Ok "appsettings.Production.example.json is valid JSON"
    } catch {
        Write-Fail ("appsettings.Production.example.json is not valid JSON: " + $_.Exception.Message)
    }

    $raw = Get-Content $prodExample -Raw -Encoding UTF8
    if ($raw -match "Password=" -and $raw -notmatch "YOUR_PASSWORD" -and $raw -notmatch "CHANGE_ME") {
        Write-WarnMsg "Production example may contain a non-placeholder password - review before commit"
    }
}

Write-Host ""
if ($failed) {
    Write-Host "check-release FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "check-release PASSED" -ForegroundColor Green
exit 0
