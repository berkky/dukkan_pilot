using System.Reflection;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Admin.Controllers;

[Route("Admin/Quality")]
public class QualityController : AdminBaseController
{
    private readonly AppDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public QualityController(AppDbContext context, IHostEnvironment environment, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _environment = environment;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["ActiveMenu"] = "quality";

        var contentRoot = _webHostEnvironment.ContentRootPath;
        var repoRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", ".."));

        bool FileExists(params string[] relativeParts)
            => System.IO.File.Exists(Path.Combine(new[] { repoRoot }.Concat(relativeParts).ToArray()));

        var model = new AdminQualityViewModel
        {
            EnvironmentName = _environment.EnvironmentName,
            MachineName = Environment.MachineName,
            AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "n/a",
            UtcNow = DateTime.UtcNow,
            ScriptHints =
            [
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\run-smoke-tests.ps1 -BaseUrl http://localhost:5000",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-security-headers.ps1 -BaseUrl http://localhost:5000 -Path /",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-seo-endpoints.ps1 -BaseUrl http://localhost:5000",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\check-public-demo-readiness.ps1 -BaseUrl http://localhost:5000 -DemoSlug demo-kafe",
                "powershell -ExecutionPolicy Bypass -File .\\scripts\\release-quality-gate.ps1 -BaseUrl http://localhost:5000"
            ],
            QuickLinks =
            [
                new QualityQuickLinkViewModel { Title = "Health", Url = "/health", ButtonClass = "btn-outline-success" },
                new QualityQuickLinkViewModel { Title = "Operations", Url = "/Admin/Operations", ButtonClass = "btn-outline-dark" },
                new QualityQuickLinkViewModel { Title = "CustomerSuccess", Url = "/Admin/CustomerSuccess", ButtonClass = "btn-outline-dark" },
                new QualityQuickLinkViewModel { Title = "Onboarding", Url = "/Admin/Onboarding", ButtonClass = "btn-outline-primary" },
                new QualityQuickLinkViewModel { Title = "SalesCenter", Url = "/Admin/SalesCenter", ButtonClass = "btn-outline-primary" },
                new QualityQuickLinkViewModel { Title = "SalesRequests", Url = "/Admin/SalesRequests", ButtonClass = "btn-outline-secondary" },
                new QualityQuickLinkViewModel { Title = "Billing", Url = "/Admin/Billing", ButtonClass = "btn-outline-warning" },
                new QualityQuickLinkViewModel { Title = "Landing Demo", Url = "/Demo", ButtonClass = "btn-outline-primary" },
                new QualityQuickLinkViewModel { Title = "Public Menu", Url = "/m/demo-kafe", ButtonClass = "btn-outline-success" }
            ]
        };

        model.ReadinessCards =
        [
            new QualityReadinessCardViewModel { Title = "Build script", Description = "dotnet build", IsReady = true },
            new QualityReadinessCardViewModel { Title = "Release check", Description = "scripts/check-release.ps1", IsReady = FileExists("scripts", "check-release.ps1") },
            new QualityReadinessCardViewModel { Title = "Smoke tests", Description = "scripts/run-smoke-tests.ps1", IsReady = FileExists("scripts", "run-smoke-tests.ps1") },
            new QualityReadinessCardViewModel { Title = "Security headers", Description = "scripts/check-security-headers.ps1", IsReady = FileExists("scripts", "check-security-headers.ps1") },
            new QualityReadinessCardViewModel { Title = "SEO endpoints", Description = "scripts/check-seo-endpoints.ps1", IsReady = FileExists("scripts", "check-seo-endpoints.ps1") },
            new QualityReadinessCardViewModel { Title = "Demo readiness", Description = "scripts/check-public-demo-readiness.ps1", IsReady = FileExists("scripts", "check-public-demo-readiness.ps1") },
            new QualityReadinessCardViewModel { Title = "Release gate", Description = "scripts/release-quality-gate.ps1", IsReady = FileExists("scripts", "release-quality-gate.ps1") },
            new QualityReadinessCardViewModel { Title = "Backup scripts", Description = "scripts/db-backup.ps1 + verify/restore", IsReady = FileExists("scripts", "db-backup.ps1") && FileExists("scripts", "db-verify-backup.ps1") && FileExists("scripts", "db-restore-test.ps1") },
            new QualityReadinessCardViewModel { Title = "Legal docs", Description = "docs/LEGAL_READINESS_CHECKLIST.md", IsReady = FileExists("docs", "LEGAL_READINESS_CHECKLIST.md") },
            new QualityReadinessCardViewModel { Title = "Onboarding docs", Description = "docs/CUSTOMER_ONBOARDING_RUNBOOK.md", IsReady = FileExists("docs", "CUSTOMER_ONBOARDING_RUNBOOK.md") },
            new QualityReadinessCardViewModel { Title = "Customer success docs", Description = "docs/CUSTOMER_SUCCESS_HEALTH_SCORE.md", IsReady = FileExists("docs", "CUSTOMER_SUCCESS_HEALTH_SCORE.md") }
        ];

        model.QaChecklist =
        [
            new QualityChecklistItemViewModel { Title = "Public smoke geçti mi?", Description = "Landing / legal / sales / public menu / system endpoints" },
            new QualityChecklistItemViewModel { Title = "Auth redirect geçti mi?", Description = "Business/Admin protected routes 302 dönmeli" },
            new QualityChecklistItemViewModel { Title = "Security headers kontrol edildi mi?", Description = "nosniff / frame / referrer / permissions policy" },
            new QualityChecklistItemViewModel { Title = "SEO endpoints kontrol edildi mi?", Description = "robots + sitemap; private URL yok" },
            new QualityChecklistItemViewModel { Title = "Public menu mobile polish gözden geçirildi mi?", Description = "/m/demo-kafe: hero, kategori scroll, sepet bar, form, confirmation, tracking" },
            new QualityChecklistItemViewModel { Title = "Sales form manuel test edildi mi?", Description = "RequestDemo/RequestPlan + Privacy/KVKK" },
            new QualityChecklistItemViewModel { Title = "Public order manuel test edildi mi?", Description = "Public menu → WhatsApp order → confirmation/tracking" },
            new QualityChecklistItemViewModel { Title = "Admin dashboard kontrol edildi mi?", Description = "SalesCenter/Onboarding/CustomerSuccess/Operations" },
            new QualityChecklistItemViewModel { Title = "Business dashboard kontrol edildi mi?", Description = "Onboarding/Success/GoLive/DemoCenter" },
            new QualityChecklistItemViewModel { Title = "Backup verify yapıldı mı?", Description = "db-backup + db-verify-backup + restore test" },
            new QualityChecklistItemViewModel { Title = "Billing invoice create test edildi mi?", Description = "/Admin/Billing/CreateInvoice → kayıt oluşturma + audit + notification" },
            new QualityChecklistItemViewModel { Title = "Billing payment record test edildi mi?", Description = "/Admin/Billing/RecordPayment → Partial/Paid status güncelleme" },
            new QualityChecklistItemViewModel { Title = "Business billing ledger erişimi test edildi mi?", Description = "Owner: /Business/Billing/Invoices; Staff erişmemeli" },
            new QualityChecklistItemViewModel { Title = "Release quality gate çalıştırıldı mı?", Description = "scripts/release-quality-gate.ps1 PASS" }
        ];

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
        catch
        {
            model.DatabaseCanConnect = false;
            model.DatabaseStatusMessage = "Veritabani durum sorgusu basarisiz (detay gosterilmiyor).";
        }

        return View(model);
    }
}

