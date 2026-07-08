using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

public class GoLiveHelper
{
    private readonly AppDbContext _context;
    private readonly BusinessPlanLimitHelper _planLimitHelper;

    public GoLiveHelper(AppDbContext context, BusinessPlanLimitHelper planLimitHelper)
    {
        _context = context;
        _planLimitHelper = planLimitHelper;
    }

    public async Task<GoLiveViewModel> BuildAsync(int businessId, string publicMenuUrl, bool isBusinessOwner)
    {
        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            throw new InvalidOperationException("Business not found.");
        }

        var activeCategoryCount = await _context.Categories
            .CountAsync(c => c.BusinessId == businessId && c.IsActive);

        var activeProductInActiveCategoryCount = await _context.Products
            .CountAsync(p => p.BusinessId == businessId
                && p.IsActive
                && p.Category.IsActive
                && p.Category.BusinessId == businessId);

        var qrCodeCount = await _context.QrCodes.CountAsync(q => q.BusinessId == businessId);

        var activeCampaignCount = await _context.Campaigns
            .CountAsync(c => c.BusinessId == businessId && c.IsActive);

        var hasLoyaltyRule = await _context.LoyaltyRules
            .AnyAsync(r => r.BusinessId == businessId);

        var activeRewardCount = await _context.Rewards
            .CountAsync(r => r.BusinessId == businessId && r.IsActive);

        var hasAnyOrder = await _context.Orders.AnyAsync(o => o.BusinessId == businessId);
        var hasNonCancelledOrder = await _context.Orders
            .AnyAsync(o => o.BusinessId == businessId && o.Status != OrderStatus.Cancelled);

        var hasWhatsAppNumber = !string.IsNullOrWhiteSpace(business.Setting?.WhatsAppNumber);
        var hasPhone = !string.IsNullOrWhiteSpace(business.Phone);
        var hasContact = hasWhatsAppNumber || hasPhone;
        var hasName = !string.IsNullOrWhiteSpace(business.Name);
        var hasSlug = !string.IsNullOrWhiteSpace(business.Slug);
        var hasDescription = !string.IsNullOrWhiteSpace(business.Description);
        var hasLogo = !string.IsNullOrWhiteSpace(business.LogoUrl);
        var hasAddress = !string.IsNullOrWhiteSpace(business.Address);
        var profileExtrasComplete = hasDescription || hasLogo || hasAddress;

        var businessInfoComplete = hasName && hasContact;
        var whatsAppComplete = hasWhatsAppNumber;
        var categoryComplete = activeCategoryCount > 0;
        var productComplete = activeProductInActiveCategoryCount > 0;
        var publicMenuComplete = business.IsActive && categoryComplete && productComplete && hasSlug;
        var qrComplete = hasSlug;
        var campaignComplete = activeCampaignCount > 0;
        var loyaltyComplete = hasLoyaltyRule || activeRewardCount > 0;
        var testOrderComplete = hasAnyOrder;
        var reportsComplete = hasNonCancelledOrder;

        var steps = new List<GoLiveStepViewModel>
        {
            CreateStep(
                key: "business-info",
                title: "İşletme bilgileri tamamlandı",
                description: "İşletme adı ve iletişim bilgileri yayına hazır profil için gereklidir.",
                isCompleted: businessInfoComplete,
                isRequired: true,
                actionText: "Ayarları Düzenle",
                actionUrl: "/Business/Settings",
                ownerOnly: true,
                warningText: !isBusinessOwner && !businessInfoComplete
                    ? "Bu adımı tamamlamak için Owner yetkisi gerekir."
                    : null),

            CreateStep(
                key: "whatsapp",
                title: "WhatsApp sipariş numarası ayarlandı",
                description: "Müşteriler siparişi WhatsApp üzerinden iletebilsin.",
                isCompleted: whatsAppComplete,
                isRequired: true,
                actionText: "WhatsApp Numarası Ekle",
                actionUrl: "/Business/Settings",
                ownerOnly: true,
                warningText: BuildWhatsAppWarning(isBusinessOwner, hasWhatsAppNumber, hasPhone)),

            CreateStep(
                key: "category",
                title: "Menü kategorisi oluşturuldu",
                description: "Public menüde görünecek en az bir aktif kategori ekleyin.",
                isCompleted: categoryComplete,
                isRequired: true,
                actionText: "Kategori Ekle",
                actionUrl: "/Business/Categories/Create",
                secondaryActionText: "Kategoriler",
                secondaryActionUrl: "/Business/Categories"),

            CreateStep(
                key: "product",
                title: "Menü ürünü eklendi",
                description: "Aktif bir kategori altında en az bir aktif ürün olmalıdır.",
                isCompleted: productComplete,
                isRequired: true,
                actionText: "Ürün Ekle",
                actionUrl: "/Business/Products/Create",
                secondaryActionText: "CSV İçe Aktar",
                secondaryActionUrl: "/Business/Products/ImportCsv"),

            CreateStep(
                key: "public-menu",
                title: "Public menü yayına hazır",
                description: "İşletme aktif, slug mevcut ve menüde görünür ürün/kategori vardır.",
                isCompleted: publicMenuComplete,
                isRequired: true,
                actionText: "Public Menüyü Aç",
                actionUrl: $"/m/{business.Slug}",
                secondaryActionText: "Menü Stüdyosu",
                secondaryActionUrl: "/Business/MenuStudio"),

            CreateStep(
                key: "qr-menu",
                title: "QR menü hazır",
                description: qrCodeCount > 0
                    ? $"Public menü linki ve QR kayıtları hazır ({qrCodeCount} QR)."
                    : "Public menü linki hazır; QR kod oluşturabilir veya afiş yazdırabilirsiniz.",
                isCompleted: qrComplete,
                isRequired: true,
                actionText: "QR Menü",
                actionUrl: "/Business/QrMenu",
                secondaryActionText: "QR Afiş Yazdır",
                secondaryActionUrl: "/Business/QrMenu/Print"),

            CreateStep(
                key: "campaign",
                title: "Kampanya sistemi hazır",
                description: "İlk kampanyanızı oluşturarak sepet indirimini aktif edebilirsiniz.",
                isCompleted: campaignComplete,
                isRequired: false,
                actionText: "Kampanya Oluştur",
                actionUrl: "/Business/Campaigns/Create",
                secondaryActionText: "Kampanya Raporu",
                secondaryActionUrl: "/Business/Reports/Campaigns"),

            CreateStep(
                key: "loyalty",
                title: "Sadakat / ödül sistemi hazır",
                description: "Sadakat kuralı veya aktif ödül tanımlayarak müşteri bağlılığını artırın.",
                isCompleted: loyaltyComplete,
                isRequired: false,
                actionText: "Sadakat",
                actionUrl: "/Business/Loyalty",
                secondaryActionText: "Ödüller",
                secondaryActionUrl: "/Business/Rewards"),

            CreateStep(
                key: "test-order",
                title: "Test siparişi verildi",
                description: "Public menüden bir test siparişi vererek uçtan uca akışı doğrulayın.",
                isCompleted: testOrderComplete,
                isRequired: true,
                actionText: "Test Siparişi Ver",
                actionUrl: $"/m/{business.Slug}",
                secondaryActionText: "Siparişler",
                secondaryActionUrl: "/Business/Orders"),

            CreateStep(
                key: "kitchen",
                title: "Operasyon ekranı hazır",
                description: "Mutfak modu ve sipariş yönetimi ile günlük operasyonu yürütebilirsiniz.",
                isCompleted: true,
                isRequired: false,
                actionText: "Mutfak Modu",
                actionUrl: "/Business/Orders/Kitchen",
                secondaryActionText: "Siparişler",
                secondaryActionUrl: "/Business/Orders"),

            CreateStep(
                key: "reports",
                title: "Raporlama takip edilebilir",
                description: "İptal edilmemiş siparişlerle satış ve kampanya raporlarını izleyebilirsiniz.",
                isCompleted: reportsComplete,
                isRequired: false,
                actionText: "Raporlar",
                actionUrl: "/Business/Reports")
        };

        var requiredSteps = steps.Where(s => s.IsRequired).ToList();
        var requiredCompleted = requiredSteps.Count(s => s.IsCompleted);
        var completedCount = steps.Count(s => s.IsCompleted);
        var isReady = requiredSteps.All(s => s.IsCompleted);

        var healthScore = CalculateHealthScore(
            business.IsActive,
            hasContact,
            profileExtrasComplete,
            categoryComplete,
            productComplete,
            publicMenuComplete,
            qrComplete,
            testOrderComplete,
            campaignComplete,
            loyaltyComplete);

        var (label, badgeClass) = ResolveHealthLabel(healthScore, isReady);

        var primaryMissing = steps.FirstOrDefault(s => s.IsRequired && !s.IsCompleted)
            ?? steps.FirstOrDefault(s => !s.IsCompleted);

        var planUsage = await _planLimitHelper.GetUsageAsync(businessId);

        return new GoLiveViewModel
        {
            BusinessId = businessId,
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            PublicMenuUrl = publicMenuUrl,
            HealthScore = healthScore,
            HealthLabel = label,
            HealthBadgeClass = badgeClass,
            CompletedStepCount = completedCount,
            TotalStepCount = steps.Count,
            RequiredCompletedCount = requiredCompleted,
            RequiredTotalCount = requiredSteps.Count,
            ProgressPercent = requiredSteps.Count == 0
                ? 100
                : (int)Math.Round(requiredCompleted * 100.0 / requiredSteps.Count),
            IsReadyToGoLive = isReady,
            IsBusinessOwner = isBusinessOwner,
            PrimaryMissingStepTitle = primaryMissing?.Title,
            PrimaryMissingStepActionUrl = primaryMissing?.ActionUrl,
            PrimaryMissingStepActionText = primaryMissing?.ActionText,
            PrimaryMissingStepOwnerOnly = primaryMissing?.OwnerOnly == true,
            SetupSteps = steps,
            QuickActions = BuildQuickActions(business.Slug, isBusinessOwner),
            PublicMenuPreview = new GoLivePreviewViewModel
            {
                BusinessName = business.Name,
                Description = business.Description,
                LogoUrl = business.LogoUrl,
                ThemeColor = ResolveThemeColor(business.Setting?.ThemeColor),
                Currency = string.IsNullOrWhiteSpace(business.Setting?.Currency)
                    ? "TRY"
                    : business.Setting!.Currency,
                CategoryCount = activeCategoryCount,
                ActiveProductCount = activeProductInActiveCategoryCount,
                CampaignCount = activeCampaignCount,
                RewardCount = activeRewardCount
            },
            TestChecklist = BuildTestChecklist(business.Slug, testOrderComplete),
            PlanUsage = planUsage,
            LaunchTips =
            [
                "Menü fotoğraflarını güncelleyin; ürün görselleri dönüşümü artırır.",
                "En çok satan ürünleri listenin üstünde tutun.",
                "WhatsApp numaranızı gerçek bir siparişle test edin.",
                "QR afişini masalara ve kasa yakınına yerleştirin.",
                "İlk kampanyanızı başlatarak sepet dönüşümünü destekleyin.",
                "Sadakat ödülü ekleyerek tekrar siparişi teşvik edin."
            ]
        };
    }

    public async Task<GoLiveDashboardCardViewModel?> BuildDashboardCardAsync(int businessId)
    {
        var model = await BuildAsync(businessId, publicMenuUrl: string.Empty, isBusinessOwner: true);
        return new GoLiveDashboardCardViewModel
        {
            HealthScore = model.HealthScore,
            HealthLabel = model.HealthLabel,
            HealthBadgeClass = model.HealthBadgeClass,
            ProgressPercent = model.ProgressPercent,
            RequiredCompletedCount = model.RequiredCompletedCount,
            RequiredTotalCount = model.RequiredTotalCount,
            IsReadyToGoLive = model.IsReadyToGoLive,
            PrimaryMissingStepTitle = model.PrimaryMissingStepTitle
        };
    }

    private static GoLiveStepViewModel CreateStep(
        string key,
        string title,
        string description,
        bool isCompleted,
        bool isRequired,
        string? actionText = null,
        string? actionUrl = null,
        string? secondaryActionText = null,
        string? secondaryActionUrl = null,
        bool ownerOnly = false,
        string? warningText = null)
    {
        string statusText;
        string badgeClass;

        if (isCompleted)
        {
            statusText = "Tamamlandı";
            badgeClass = "bg-success";
        }
        else if (isRequired)
        {
            statusText = "Eksik";
            badgeClass = "bg-warning text-dark";
        }
        else
        {
            statusText = "Önerilir";
            badgeClass = "bg-info text-dark";
        }

        return new GoLiveStepViewModel
        {
            Key = key,
            Title = title,
            Description = description,
            IsCompleted = isCompleted,
            IsRequired = isRequired,
            StatusText = statusText,
            BadgeClass = badgeClass,
            ActionText = actionText,
            ActionUrl = actionUrl,
            SecondaryActionText = secondaryActionText,
            SecondaryActionUrl = secondaryActionUrl,
            OwnerOnly = ownerOnly,
            WarningText = warningText
        };
    }

    private static string? BuildWhatsAppWarning(bool isBusinessOwner, bool hasWhatsApp, bool hasPhone)
    {
        if (hasWhatsApp)
        {
            return null;
        }

        if (hasPhone)
        {
            var tip = "Telefon var, WhatsApp numarası önerilir.";
            return isBusinessOwner
                ? tip
                : $"{tip} Bu adımı tamamlamak için Owner yetkisi gerekir.";
        }

        return isBusinessOwner
            ? null
            : "Bu adımı tamamlamak için Owner yetkisi gerekir.";
    }

    private static int CalculateHealthScore(
        bool businessActive,
        bool hasContact,
        bool profileExtrasComplete,
        bool hasCategory,
        bool hasProduct,
        bool publicReady,
        bool qrReady,
        bool hasTestOrder,
        bool hasCampaign,
        bool hasLoyalty)
    {
        var score = 0;
        if (businessActive) score += 10;
        if (hasContact) score += 15;
        if (profileExtrasComplete) score += 10;
        if (hasCategory) score += 15;
        if (hasProduct) score += 20;
        if (publicReady) score += 10;
        if (qrReady) score += 10;
        if (hasTestOrder) score += 10;

        var bonus = 0;
        if (hasCampaign) bonus += 5;
        if (hasLoyalty) bonus += 5;

        // Base max 100; optional bonus normalized into remaining headroom.
        var normalized = Math.Min(100, score + bonus);
        return normalized;
    }

    private static (string Label, string BadgeClass) ResolveHealthLabel(int score, bool isReady)
    {
        if (!isReady && score >= 90)
        {
            return ("Neredeyse Hazır", "bg-warning text-dark");
        }

        return score switch
        {
            >= 90 when isReady => ("Yayına Hazır", "bg-success"),
            >= 70 => ("Neredeyse Hazır", "bg-warning text-dark"),
            >= 40 => ("Kurulum Devam Ediyor", "bg-info text-dark"),
            _ => ("Başlangıç Aşaması", "bg-secondary")
        };
    }

    private static List<GoLiveQuickActionViewModel> BuildQuickActions(string slug, bool isBusinessOwner)
    {
        var actions = new List<GoLiveQuickActionViewModel>
        {
            new()
            {
                Title = "Public Menüyü Aç",
                Description = "Müşteri menü deneyimini önizleyin.",
                Url = $"/m/{slug}",
                ButtonClass = "btn-primary",
                IsExternal = true
            },
            new()
            {
                Title = "QR Afişini Yazdır",
                Description = "Masalar için yazdırılabilir afiş.",
                Url = "/Business/QrMenu/Print",
                ButtonClass = "btn-outline-primary",
                IsExternal = true
            },
            new()
            {
                Title = "Menü Stüdyosu",
                Description = "Menü sağlığı ve hızlı düzenleme.",
                Url = "/Business/MenuStudio",
                ButtonClass = "btn-outline-primary"
            },
            new()
            {
                Title = "Mutfak Modu",
                Description = "Operasyon ekranını açın.",
                Url = "/Business/Orders/Kitchen",
                ButtonClass = "btn-outline-secondary"
            },
            new()
            {
                Title = "Abonelik / Plan",
                Description = "Plan kullanımını ve yükseltmeyi yönetin.",
                Url = "/Business/Billing",
                ButtonClass = "btn-outline-secondary",
                OwnerOnly = true
            }
        };

        if (!isBusinessOwner)
        {
            actions.RemoveAll(a => a.OwnerOnly);
        }

        return actions;
    }

    private static List<GoLiveTestItemViewModel> BuildTestChecklist(string slug, bool hasOrder)
    {
        var menuUrl = $"/m/{slug}";
        return
        [
            new GoLiveTestItemViewModel
            {
                Title = "Public menüyü aç",
                Description = "Menü sayfası login istemeden açılmalı.",
                IsCompleted = true,
                ActionUrl = menuUrl,
                ActionText = "Menüyü Aç"
            },
            new GoLiveTestItemViewModel
            {
                Title = "Sepete ürün ekle",
                Description = "Ürün kartından sepete ekleyip özeti kontrol edin.",
                IsCompleted = false,
                ActionUrl = menuUrl,
                ActionText = "Menüye Git"
            },
            new GoLiveTestItemViewModel
            {
                Title = "WhatsApp sipariş mesajını kontrol et",
                Description = "Sipariş sonrası wa.me mesajındaki ürün ve tutarları doğrulayın.",
                IsCompleted = false,
                ActionUrl = menuUrl,
                ActionText = "Test Et"
            },
            new GoLiveTestItemViewModel
            {
                Title = "Business Orders ekranında siparişi gör",
                Description = "Panelde yeni siparişin listelendiğini kontrol edin.",
                IsCompleted = hasOrder,
                ActionUrl = "/Business/Orders",
                ActionText = "Siparişler"
            },
            new GoLiveTestItemViewModel
            {
                Title = "Kitchen ekranında durumu güncelle",
                Description = "Bekleyen → Hazırlanıyor → Tamamlandı akışını deneyin.",
                IsCompleted = false,
                ActionUrl = "/Business/Orders/Kitchen",
                ActionText = "Mutfak"
            },
            new GoLiveTestItemViewModel
            {
                Title = "Tracking ekranında durum değişimini gör",
                Description = "Confirmation/tracking sayfasında durumun güncellendiğini doğrulayın.",
                IsCompleted = hasOrder,
                ActionUrl = "/Business/Orders",
                ActionText = "Sipariş Detayı"
            }
        ];
    }

    private static string ResolveThemeColor(string? themeColor)
    {
        if (string.IsNullOrWhiteSpace(themeColor))
        {
            return "#2563eb";
        }

        var trimmed = themeColor.Trim();
        return trimmed.StartsWith('#') && trimmed.Length is 4 or 7 ? trimmed : "#2563eb";
    }
}
