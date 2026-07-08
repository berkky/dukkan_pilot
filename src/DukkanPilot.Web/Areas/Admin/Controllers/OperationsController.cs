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
                new OpsDocLinkViewModel { Title = "Operational security", Path = "docs/OPERATIONAL_SECURITY_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "First release operations", Path = "docs/FIRST_RELEASE_OPERATIONS.md" },
                new OpsDocLinkViewModel { Title = "Legal readiness", Path = "docs/LEGAL_READINESS_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "Privacy & data map", Path = "docs/PRIVACY_AND_DATA_MAP.md" },
                new OpsDocLinkViewModel { Title = "Customer onboarding", Path = "docs/CUSTOMER_ONBOARDING_RUNBOOK.md" },
                new OpsDocLinkViewModel { Title = "Kickoff script", Path = "docs/KICKOFF_MEETING_SCRIPT.md" },
                new OpsDocLinkViewModel { Title = "Implementation handoff", Path = "docs/IMPLEMENTATION_HANDOFF_CHECKLIST.md" },
                new OpsDocLinkViewModel { Title = "Customer success", Path = "docs/CUSTOMER_SUCCESS_PLAYBOOK.md" }
            ],
            ScriptHints =
            [
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-release.ps1",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\publish-release.ps1",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\db-backup.ps1 -ServerInstance \"(localdb)\\MSSQLLocalDB\" -DatabaseName \"DukkanPilotDb\"",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\db-generate-migration-script.ps1",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\db-migration-status.ps1",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\run-smoke-tests.ps1 -BaseUrl http://localhost:5000"
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
                Title = "Backup & ops security docs",
                Description = "Backup/recovery + operational security checklist.",
                IsReadyHint = model.HasBackupDocs && model.HasOperationalSecurityChecklist
            }
        ];

        return View(model);
    }
}
