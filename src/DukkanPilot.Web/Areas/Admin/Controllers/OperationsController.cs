using System.Reflection;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Operations")]
public class OperationsController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public OperationsController(
        AppDbContext context,
        IHostEnvironment environment,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _environment = environment;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "operations";

        var contentRoot = _webHostEnvironment.ContentRootPath;
        // Web ContentRoot is src/DukkanPilot.Web — repo root is two levels up
        var repoRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", ".."));

        bool FileExists(params string[] relativeParts)
            => System.IO.File.Exists(Path.Combine(new[] { repoRoot }.Concat(relativeParts).ToArray()));

        var model = new AdminOperationsViewModel
        {
            EnvironmentName = _environment.EnvironmentName,
            MachineName = Environment.MachineName,
            AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "n/a",
            UtcNow = DateTime.UtcNow,
            HasProductionExample = FileExists("src", "DukkanPilot.Web", "appsettings.Production.example.json"),
            HasDeploymentChecklist = FileExists("docs", "DEPLOYMENT_CHECKLIST.md"),
            HasReleaseChecklist = FileExists("docs", "RELEASE_CHECKLIST.md"),
            HasSmokeChecklist = FileExists("docs", "SMOKE_TEST_CHECKLIST.md"),
            HasBackupDocs = FileExists("docs", "DATABASE_BACKUP_AND_RECOVERY.md"),
            HasMigrationRunbook = FileExists("docs", "MIGRATION_RUNBOOK.md"),
            HasIncidentRunbook = FileExists("docs", "INCIDENT_RESPONSE_RUNBOOK.md"),
            HasReliabilityRunbook = FileExists("docs", "RELIABILITY_RUNBOOK.md"),
            HasPerformanceHardeningDocs = FileExists("docs", "PERFORMANCE_HARDENING_GUIDE.md"),
            HasPerformanceSmokeDocs = FileExists("docs", "PERFORMANCE_SMOKE_TESTS.md"),
            HasPerformanceSmokeScript = FileExists("scripts", "check-performance-smoke.ps1"),
            HasOperationalSecurityChecklist = FileExists("docs", "OPERATIONAL_SECURITY_CHECKLIST.md"),
            HasFirstReleaseOpsDocs = FileExists("docs", "FIRST_RELEASE_OPERATIONS.md"),
            HasLegalReadinessDocs = FileExists("docs", "LEGAL_READINESS_CHECKLIST.md"),
            HasPrivacyDataMapDocs = FileExists("docs", "PRIVACY_AND_DATA_MAP.md"),
            HasCookieDocs = FileExists("docs", "COOKIE_AND_TRACKING_NOTES.md"),
            HasTermsNotesDocs = FileExists("docs", "TERMS_TEMPLATE_NOTES.md"),
            HasLegalPrivacyView = FileExists("src", "DukkanPilot.Web", "Views", "Legal", "Privacy.cshtml"),
            HasTrustView = FileExists("src", "DukkanPilot.Web", "Views", "Legal", "Trust.cshtml"),
            HasCookieNoticeAssets = FileExists("src", "DukkanPilot.Web", "wwwroot", "js", "cookie-notice.js")
                && FileExists("src", "DukkanPilot.Web", "Views", "Shared", "_CookieNotice.cshtml"),
            HasSupportEmailPlaceholder = FileExists("src", "DukkanPilot.Web", "appsettings.Production.example.json"),
            DocLinks =
            [
                new OpsDocLinkViewModel { Title = "Deployment checklist", Path = "docs/DEPLOYMENT_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "Release checklist", Path = "docs/RELEASE_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "Smoke test checklist", Path = "docs/SMOKE_TEST_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "Backup & recovery", Path = "docs/DATABASE_BACKUP_AND_RECOVERY.md" },
                new OpsDocLinkViewModel { Title = "Migration runbook", Path = "docs/MIGRATION_RUNBOOK.md" },
                new OpsDocLinkViewModel { Title = "Incident response", Path = "docs/INCIDENT_RESPONSE_RUNBOOK.md" },
                new OpsDocLinkViewModel { Title = "Reliability runbook", Path = "docs/RELIABILITY_RUNBOOK.md" },
                new OpsDocLinkViewModel { Title = "Performance hardening", Path = "docs/PERFORMANCE_HARDENING_GUIDE.md" },
                new OpsDocLinkViewModel { Title = "Performance smoke tests", Path = "docs/PERFORMANCE_SMOKE_TESTS.md" },
                new OpsDocLinkViewModel { Title = "Operational security", Path = "docs/OPERATIONAL_SECURITY_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "First release operations", Path = "docs/FIRST_RELEASE_OPERATIONS.md" },
                new OpsDocLinkViewModel { Title = "Legal readiness", Path = "docs/LEGAL_READINESS_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "Privacy & data map", Path = "docs/PRIVACY_AND_DATA_MAP.md" },
                new OpsDocLinkViewModel { Title = "Customer onboarding", Path = "docs/CUSTOMER_ONBOARDING_RUNBOOK.md" },
                new OpsDocLinkViewModel { Title = "Kickoff script", Path = "docs/KICKOFF_MEETING_SCRIPT.md" },
                new OpsDocLinkViewModel { Title = "Implementation handoff", Path = "docs/IMPLEMENTATION_HANDOFF_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "Customer success", Path = "docs/CUSTOMER_SUCCESS_PLAYBOOK.md" },
                new OpsDocLinkViewModel { Title = "Health score", Path = "docs/CUSTOMER_SUCCESS_HEALTH_SCORE.md" },
                new OpsDocLinkViewModel { Title = "Retention playbook", Path = "docs/RETENTION_PLAYBOOK.md" },
                new OpsDocLinkViewModel { Title = "Upgrade opportunity", Path = "docs/UPGRADE_OPPORTUNITY_PLAYBOOK.md" },
                new OpsDocLinkViewModel { Title = "Churn risk runbook", Path = "docs/CHURN_RISK_RUNBOOK.md" },
                new OpsDocLinkViewModel { Title = "Support center runbook", Path = "docs/SUPPORT_CENTER_RUNBOOK.md" },
                new OpsDocLinkViewModel { Title = "Support UAT", Path = "docs/SUPPORT_UAT_SCRIPT.md" }
            ],
            ScriptHints =
            [
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-release.ps1",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\release-quality-gate.ps1 -BaseUrl http://localhost:5000",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\publish-release.ps1",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\db-backup.ps1 -ServerInstance \"(localdb)\\MSSQLLocalDB\" -DatabaseName \"DukkanPilotDb\"",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\db-generate-migration-script.ps1",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\db-migration-status.ps1",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\run-smoke-tests.ps1 -BaseUrl http://localhost:5000",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-security-headers.ps1 -BaseUrl http://localhost:5000 -Path /",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-seo-endpoints.ps1 -BaseUrl http://localhost:5000",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-public-demo-readiness.ps1 -BaseUrl http://localhost:5000 -DemoSlugs \"demo-kafe,demo-tatlici,demo-burgerci,demo-restoran,demo-nargile\"",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-performance-smoke.ps1 -BaseUrl http://localhost:5000"
            ]
        };

        try
        {
            model.DatabaseCanConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (model.DatabaseCanConnect)
            {
                var applied = (await _context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
                var pending = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                model.AppliedMigrationCount = applied.Count;
                model.PendingMigrationCount = pending.Count;
                model.LastAppliedMigration = applied.LastOrDefault();
                model.PendingMigrationNames = pending;
                model.DatabaseStatusMessage = pending.Count == 0
                    ? "Baglanti OK; bekleyen migration yok."
                    : "Baglanti OK; bekleyen migration var.";
            }
            else
            {
                model.DatabaseStatusMessage = "Veritabanina baglanilamadi.";
            }
        }
        catch (Exception)
        {
            model.DatabaseCanConnect = false;
            model.DatabaseStatusMessage = "Veritabani durum sorgusu basarisiz (detay gosterilmiyor).";
        }

        model.OperationalChecklist =
        [
            new OpsChecklistItemViewModel
            {
                Title = "Backup alindi mi?",
                Description = "Release / migration oncesi scripts/db-backup.ps1 veya SSMS ile FULL backup.",
                IsReadyHint = model.HasBackupDocs
            },
            new OpsChecklistItemViewModel
            {
                Title = "Migration script uretildi mi?",
                Description = "scripts/db-generate-migration-script.ps1 ile idempotent SQL review.",
                IsReadyHint = model.HasMigrationRunbook
            },
            new OpsChecklistItemViewModel
            {
                Title = "Smoke test calistirildi mi?",
                Description = "scripts/run-smoke-tests.ps1 + docs/SMOKE_TEST_CHECKLIST.md.",
                IsReadyHint = model.HasSmokeChecklist
            },
            new OpsChecklistItemViewModel
            {
                Title = "/health kontrol edildi mi?",
                Description = "Canli veya staging /health JSON: status ok, database ok.",
                IsReadyHint = true
            },
            new OpsChecklistItemViewModel
            {
                Title = "Rollback plani hazir mi?",
                Description = "Onceki publish yedegi + DB backup; EF down migration riskli.",
                IsReadyHint = model.HasIncidentRunbook && model.HasBackupDocs
            },
            new OpsChecklistItemViewModel
            {
                Title = "Onboarding board available",
                Description = "/Admin/Onboarding + docs/CUSTOMER_ONBOARDING_RUNBOOK.md",
                IsReadyHint = FileExists("docs", "CUSTOMER_ONBOARDING_RUNBOOK.md")
                    && FileExists("src", "DukkanPilot.Web", "Areas", "Admin", "Controllers", "OnboardingController.cs")
            },
            new OpsChecklistItemViewModel
            {
                Title = "Customer Success board available",
                Description = "/Admin/CustomerSuccess + docs/CUSTOMER_SUCCESS_HEALTH_SCORE.md",
                IsReadyHint = FileExists("docs", "CUSTOMER_SUCCESS_HEALTH_SCORE.md")
                    && FileExists("src", "DukkanPilot.Web", "Areas", "Admin", "Controllers", "CustomerSuccessController.cs")
            },
            new OpsChecklistItemViewModel
            {
                Title = "Quality Center available",
                Description = "/Admin/Quality + release-quality-gate scripts",
                IsReadyHint = FileExists("src", "DukkanPilot.Web", "Areas", "Admin", "Controllers", "QualityController.cs")
                    && FileExists("scripts", "release-quality-gate.ps1")
            }
            ,
            new OpsChecklistItemViewModel
            {
                Title = "Manual billing operations available",
                Description = "/Admin/Billing (invoice/payment/cancel) + Business ledger (read-only). Resmi e-Belge değildir.",
                IsReadyHint = FileExists("src", "DukkanPilot.Web", "Areas", "Admin", "Controllers", "BillingController.cs")
                    && FileExists("src", "DukkanPilot.Web", "Areas", "Admin", "Views", "Billing", "Index.cshtml")
            }
        ];

        model.PerformanceReliabilityChecklist =
        [
            new OpsChecklistItemViewModel
            {
                Title = "Release quality gate çalıştırıldı",
                Description = "scripts/release-quality-gate.ps1 — build, migration, smoke, SEO, demo, performance.",
                IsReadyHint = FileExists("scripts", "release-quality-gate.ps1")
            },
            new OpsChecklistItemViewModel
            {
                Title = "Performance smoke çalıştırıldı",
                Description = "scripts/check-performance-smoke.ps1 — public route response süreleri (smoke, benchmark değil).",
                IsReadyHint = model.HasPerformanceSmokeScript && model.HasPerformanceSmokeDocs
            },
            new OpsChecklistItemViewModel
            {
                Title = "Public demo slugs kontrol edildi",
                Description = "check-public-demo-readiness.ps1 — 5 vertical demo menü.",
                IsReadyHint = FileExists("scripts", "check-public-demo-readiness.ps1")
            },
            new OpsChecklistItemViewModel
            {
                Title = "Migration status: no pending",
                Description = "db-migration-status.ps1 veya dotnet ef has-pending-model-changes.",
                IsReadyHint = model.HasMigrationRunbook
            },
            new OpsChecklistItemViewModel
            {
                Title = "Backup script hazır",
                Description = "db-backup.ps1 + verify; release öncesi FULL backup.",
                IsReadyHint = model.HasBackupDocs && FileExists("scripts", "db-backup.ps1")
            },
            new OpsChecklistItemViewModel
            {
                Title = "Açık acil destek talepleri",
                Description = "/Admin/Support — Urgent/High öncelikli açık ticket kontrolü.",
                IsReadyHint = FileExists("src", "DukkanPilot.Web", "Areas", "Admin", "Controllers", "SupportController.cs")
            },
            new OpsChecklistItemViewModel
            {
                Title = "DLL lock / port conflict biliniyor",
                Description = "Çalışan dotnet run kapatılmadan build/gate fail olabilir; docs/RELIABILITY_RUNBOOK.md.",
                IsReadyHint = model.HasReliabilityRunbook
            }
        ];

        model.LegalReadinessChecklist =
        [
            new OpsChecklistItemViewModel
            {
                Title = "Legal pages available",
                Description = "/Privacy /Kvkk /Terms /Cookies /DataProcessing /Trust",
                IsReadyHint = model.HasLegalPrivacyView && model.HasTrustView
            },
            new OpsChecklistItemViewModel
            {
                Title = "Production SupportEmail placeholder",
                Description = "appsettings.Production.example.json App.SupportEmail mevcut (secret yok).",
                IsReadyHint = model.HasSupportEmailPlaceholder
            },
            new OpsChecklistItemViewModel
            {
                Title = "Legal docs",
                Description = "LEGAL_READINESS / PRIVACY_AND_DATA_MAP / COOKIE / TERMS notes.",
                IsReadyHint = model.HasLegalReadinessDocs && model.HasPrivacyDataMapDocs
                    && model.HasCookieDocs && model.HasTermsNotesDocs
            },
            new OpsChecklistItemViewModel
            {
                Title = "Cookie notice assets",
                Description = "_CookieNotice + cookie-notice.js (localStorage, tracking yok).",
                IsReadyHint = model.HasCookieNoticeAssets
            },
            new OpsChecklistItemViewModel
            {
                Title = "No public demo passwords",
                Description = "Landing /Demo sifre gostermemeli; Login'deki demo hesaplar ayrik.",
                IsReadyHint = true
            },
            new OpsChecklistItemViewModel
            {
                Title = "Support center ready",
                Description = "SUPPORT_CENTER_RUNBOOK + /Admin/Support + business ticket akışı.",
                IsReadyHint = FileExists("docs", "SUPPORT_CENTER_RUNBOOK.md")
                    && FileExists("src", "DukkanPilot.Web", "Areas", "Admin", "Controllers", "SupportController.cs")
            },
            new OpsChecklistItemViewModel
            {
                Title = "Backup & ops security docs",
                Description = "Backup/recovery + operational security checklist.",
                IsReadyHint = model.HasBackupDocs && model.HasOperationalSecurityChecklist
            }
        ];

        return View(model);
    }
}
