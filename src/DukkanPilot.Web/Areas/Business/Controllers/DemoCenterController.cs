using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Areas.Business.Controllers;

[Route("Business/DemoCenter")]
public class DemoCenterController : BusinessBaseController
{
    private readonly AppDbContext _context;

    public DemoCenterController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["ActiveMenu"] = "demo-center";

        var forbidResult = GetCurrentBusinessIdOrForbid(out var businessId);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        var business = await _context.Businesses.AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return NotFound();
        }

        var hasWhatsApp = !string.IsNullOrWhiteSpace(business.Setting?.WhatsAppNumber)
            || !string.IsNullOrWhiteSpace(business.Phone);
        var activeCategories = await _context.Categories.CountAsync(c => c.BusinessId == businessId && c.IsActive);
        var activeProducts = await _context.Products.CountAsync(p => p.BusinessId == businessId && p.IsActive);
        var campaigns = await _context.Campaigns.CountAsync(c => c.BusinessId == businessId && c.IsActive);
        var hasOrder = await _context.Orders.AnyAsync(o => o.BusinessId == businessId);
        var hasNotification = await _context.Notifications.AnyAsync(n => n.BusinessId == businessId);
        var hasAudit = await _context.AuditLogs.AnyAsync(a => a.BusinessId == businessId);
        var hasSlug = !string.IsNullOrWhiteSpace(business.Slug);
        var publicMenuUrl = $"{Request.Scheme}://{Request.Host}/m/{business.Slug}";

        var checks = new List<DemoCenterCheckViewModel>
        {
            new() { Key = "active", Title = "İşletme aktif", IsReady = business.IsActive },
            new() { Key = "contact", Title = "WhatsApp / telefon var", IsReady = hasWhatsApp },
            new() { Key = "category", Title = "Aktif kategori var", IsReady = activeCategories > 0 },
            new() { Key = "product", Title = "Aktif ürün var", IsReady = activeProducts > 0 },
            new() { Key = "campaign", Title = "Aktif kampanya var", IsReady = campaigns > 0 },
            new() { Key = "menu", Title = "Public menü linki hazır", IsReady = hasSlug && business.IsActive },
            new() { Key = "order", Title = "En az bir sipariş var", IsReady = hasOrder },
            new() { Key = "activity", Title = "Bildirim veya audit kaydı var", IsReady = hasNotification || hasAudit }
        };

        var readyCount = checks.Count(c => c.IsReady);
        var score = (int)Math.Round(readyCount * 100.0 / checks.Count);
        var (label, badge) = score switch
        {
            >= 85 => ("Demo hazır", "bg-success"),
            >= 60 => ("Neredeyse hazır", "bg-primary"),
            >= 40 => ("Eksikler var", "bg-warning text-dark"),
            _ => ("Demo için erken", "bg-secondary")
        };

        var menuReady = business.IsActive && activeCategories > 0 && activeProducts > 0 && hasSlug;

        var model = new DemoCenterViewModel
        {
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            PublicMenuUrl = publicMenuUrl,
            ReadinessScore = score,
            ReadinessLabel = label,
            ReadinessBadgeClass = badge,
            Checks = checks,
            Steps =
            [
                new DemoCenterStepViewModel
                {
                    Order = 1,
                    Title = "Public QR menüyü aç",
                    Description = "Müşteriye önce menü ve ürünleri gösterin.",
                    ActionText = "Public Menüyü Aç",
                    ActionUrl = $"/m/{business.Slug}",
                    OpenInNewTab = true,
                    IsReady = menuReady
                },
                new DemoCenterStepViewModel
                {
                    Order = 2,
                    Title = "Sepete ürün ekle",
                    Description = "Public menüde sepete ekleyip özet kartını gösterin.",
                    ActionText = "Menü Stüdyosu",
                    ActionUrl = "/Business/MenuStudio",
                    IsReady = activeProducts > 0
                },
                new DemoCenterStepViewModel
                {
                    Order = 3,
                    Title = "Kampanyayı göster",
                    Description = "Auto apply kampanya ile sepet indirimini anlatın.",
                    ActionText = "Kampanyalar",
                    ActionUrl = "/Business/Campaigns",
                    IsReady = campaigns > 0
                },
                new DemoCenterStepViewModel
                {
                    Order = 4,
                    Title = "Sipariş oluştur",
                    Description = "Public menüden test siparişi verin; confirmation ve tracking ekranına gidin.",
                    ActionText = "Test için Menüyü Aç",
                    ActionUrl = $"/m/{business.Slug}",
                    OpenInNewTab = true,
                    IsReady = menuReady && hasWhatsApp
                },
                new DemoCenterStepViewModel
                {
                    Order = 5,
                    Title = "Kitchen’da siparişi ilerlet",
                    Description = "Hazırlanıyor / Tamamlandı akışını operasyon ekranında gösterin.",
                    ActionText = "Mutfak Modu",
                    ActionUrl = "/Business/Orders/Kitchen",
                    IsReady = hasOrder
                },
                new DemoCenterStepViewModel
                {
                    Order = 6,
                    Title = "Müşteri CRM’i göster",
                    Description = "Sipariş sonrası müşteri listesini ve içgörüleri açın.",
                    ActionText = "Müşteriler",
                    ActionUrl = "/Business/Customers",
                    IsReady = hasOrder
                },
                new DemoCenterStepViewModel
                {
                    Order = 7,
                    Title = "Raporlarda ciro/kampanya etkisi",
                    Description = "Ciro, ürün ve kampanya performans kartlarını gösterin.",
                    ActionText = "Raporlar",
                    ActionUrl = "/Business/Reports",
                    IsReady = hasOrder
                },
                new DemoCenterStepViewModel
                {
                    Order = 8,
                    Title = "Audit Log geçmişi",
                    Description = "Kritik işlemlerin izlenebildiğini gösterin.",
                    ActionText = "Aktivite Geçmişi",
                    ActionUrl = "/Business/AuditLogs",
                    IsReady = hasAudit
                },
                new DemoCenterStepViewModel
                {
                    Order = 9,
                    Title = "Bildirim Merkezi",
                    Description = "Sipariş, limit ve Go-Live uyarılarını gösterin.",
                    ActionText = "Bildirimler",
                    ActionUrl = "/Business/Notifications",
                    IsReady = hasNotification
                }
            ]
        };

        return View(model);
    }
}
