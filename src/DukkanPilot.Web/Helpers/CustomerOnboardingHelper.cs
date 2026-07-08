using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

/// <summary>
/// Read-only customer onboarding score/checklist from existing tenant data.
/// Does not write to the database.
/// </summary>
public class CustomerOnboardingHelper
{
    private readonly AppDbContext _context;

    public CustomerOnboardingHelper(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerOnboardingSnapshot?> BuildAsync(
        int businessId,
        string publicMenuUrl,
        bool isBusinessOwner,
        CancellationToken cancellationToken = default)
    {
        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .Include(b => b.Subscriptions)
                .ThenInclude(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business is null)
        {
            return null;
        }

        var activeCategoryCount = await _context.Categories.AsNoTracking()
            .CountAsync(c => c.BusinessId == businessId && c.IsActive, cancellationToken);

        var activeProductCount = await _context.Products.AsNoTracking()
            .CountAsync(p => p.BusinessId == businessId
                && p.IsActive
                && p.Category.IsActive
                && p.Category.BusinessId == businessId, cancellationToken);

        var orderCount = await _context.Orders.AsNoTracking()
            .CountAsync(o => o.BusinessId == businessId, cancellationToken);

        var hasKitchenFlowOrder = await _context.Orders.AsNoTracking()
            .AnyAsync(o => o.BusinessId == businessId
                && (o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Completed), cancellationToken);

        var hasNonCancelledOrder = await _context.Orders.AsNoTracking()
            .AnyAsync(o => o.BusinessId == businessId && o.Status != OrderStatus.Cancelled, cancellationToken);

        var campaignCount = await _context.Campaigns.AsNoTracking()
            .CountAsync(c => c.BusinessId == businessId && c.IsActive, cancellationToken);

        var hasLoyaltyRule = await _context.LoyaltyRules.AsNoTracking()
            .AnyAsync(r => r.BusinessId == businessId, cancellationToken);

        var rewardCount = await _context.Rewards.AsNoTracking()
            .CountAsync(r => r.BusinessId == businessId && r.IsActive, cancellationToken);

        var staffCount = await _context.UserBusinessRoles.AsNoTracking()
            .CountAsync(r => r.BusinessId == businessId
                && r.IsActive
                && r.Role == BusinessRole.Staff
                && r.AppUser.IsActive, cancellationToken);

        var hasOwner = await _context.UserBusinessRoles.AsNoTracking()
            .AnyAsync(r => r.BusinessId == businessId
                && r.IsActive
                && r.Role == BusinessRole.Owner
                && r.AppUser.IsActive, cancellationToken);

        var customerCount = await _context.Customers.AsNoTracking()
            .CountAsync(c => c.BusinessId == businessId && c.IsActive, cancellationToken);

        var hasNotification = await _context.Notifications.AsNoTracking()
            .AnyAsync(n => n.BusinessId == businessId, cancellationToken);

        var hasAudit = await _context.AuditLogs.AsNoTracking()
            .AnyAsync(a => a.BusinessId == businessId, cancellationToken);

        var lastOrderAt = await _context.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (DateTime?)o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lastAuditAt = await _context.AuditLogs.AsNoTracking()
            .Where(a => a.BusinessId == businessId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => (DateTime?)a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var lastNotificationAt = await _context.Notifications.AsNoTracking()
            .Where(n => n.BusinessId == businessId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => (DateTime?)n.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        DateTime? lastActivity = lastOrderAt;
        if (lastAuditAt.HasValue && (!lastActivity.HasValue || lastAuditAt > lastActivity))
        {
            lastActivity = lastAuditAt;
        }

        if (lastNotificationAt.HasValue && (!lastActivity.HasValue || lastNotificationAt > lastActivity))
        {
            lastActivity = lastNotificationAt;
        }

        var hasName = !string.IsNullOrWhiteSpace(business.Name);
        var hasSlug = !string.IsNullOrWhiteSpace(business.Slug);
        var hasWhatsApp = !string.IsNullOrWhiteSpace(business.Setting?.WhatsAppNumber);
        var hasPhone = !string.IsNullOrWhiteSpace(business.Phone);
        var hasProfileExtras = !string.IsNullOrWhiteSpace(business.Description)
            || !string.IsNullOrWhiteSpace(business.LogoUrl)
            || !string.IsNullOrWhiteSpace(business.Address);

        var businessActive = business.IsActive;
        var nameSlugOk = hasName && hasSlug;
        var whatsAppOk = hasWhatsApp;
        var categoryOk = activeCategoryCount > 0;
        var productOk = activeProductCount > 0;
        var publicMenuOk = businessActive && categoryOk && productOk && hasSlug;
        var qrOk = hasSlug;
        var testOrderOk = orderCount > 0;
        var kitchenOk = hasKitchenFlowOrder;
        var campaignOrRewardOk = campaignCount > 0 || hasLoyaltyRule || rewardCount > 0;
        var staffOrOwnerOk = hasOwner || staffCount > 0;
        var crmOk = customerCount > 0 || hasNonCancelledOrder;
        var reportsOk = hasNonCancelledOrder;
        var activityOk = hasNotification || hasAudit;
        var legalAwarenessOk = hasAudit || hasNotification; // soft signal: panel activity implies awareness path
        var profileOk = nameSlugOk && (hasPhone || hasWhatsApp) && hasProfileExtras;

        var steps = new List<CustomerOnboardingStep>
        {
            CreateStep(
                "business-profile",
                "İşletme bilgilerini tamamla",
                "İşletme adı, slug ve profil iletişim bilgileri hazır olsun.",
                profileOk || (nameSlugOk && (hasPhone || hasWhatsApp)),
                required: true,
                weight: 8,
                "Ayarları Düzenle",
                "/Business/Settings",
                "Adres, logo veya açıklama eklemek profili güçlendirir.",
                ownerOnly: true,
                warning: !isBusinessOwner && !(profileOk || nameSlugOk)
                    ? "Bu adım için Owner yetkisi gerekir."
                    : null),

            CreateStep(
                "whatsapp",
                "WhatsApp numarasını ayarla",
                "Müşteri siparişleri WhatsApp üzerinden iletilebilsin.",
                whatsAppOk,
                required: true,
                weight: 12,
                "WhatsApp Ayarla",
                "/Business/Settings",
                hasPhone && !hasWhatsApp
                    ? "Telefon var; sipariş için WhatsApp numarası önerilir."
                    : "Settings → WhatsApp numarası alanını doldurun.",
                ownerOnly: true,
                warning: !isBusinessOwner && !whatsAppOk
                    ? "Bu adım için Owner yetkisi gerekir."
                    : null),

            CreateStep(
                "category",
                "İlk kategorini oluştur",
                "Public menüde görünecek en az bir aktif kategori.",
                categoryOk,
                required: true,
                weight: 10,
                "Kategori Ekle",
                "/Business/Categories/Create",
                "Örn. Kahveler, Tatlılar."),

            CreateStep(
                "product",
                "İlk ürünlerini ekle",
                "Aktif kategori altında en az bir aktif ürün.",
                productOk,
                required: true,
                weight: 15,
                "Ürün Ekle",
                "/Business/Products/Create",
                "CSV ile toplu içe aktarma da kullanılabilir."),

            CreateStep(
                "public-menu",
                "Public QR menüyü kontrol et",
                "İşletme aktif, slug ve görünür menü içeriği hazır olsun.",
                publicMenuOk,
                required: true,
                weight: 10,
                "Public Menüyü Aç",
                string.IsNullOrWhiteSpace(publicMenuUrl) ? $"/m/{business.Slug}" : publicMenuUrl,
                "Menü Stüdyosu ile düzenlemeyi hızlandırın."),

            CreateStep(
                "qr-poster",
                "QR posterini hazırla",
                "Müşteriye verilecek QR afiş / yazdırılabilir poster.",
                qrOk,
                required: true,
                weight: 7,
                "QR Afiş",
                "/Business/QrMenu/Print",
                "Masalara ve kasa yakınına yerleştirin."),

            CreateStep(
                "test-order",
                "Test siparişi oluştur",
                "Public menüden uçtan uca bir sipariş deneyin.",
                testOrderOk,
                required: true,
                weight: 10,
                "Test Siparişi",
                $"/m/{business.Slug}",
                "Sipariş sonrası Business → Siparişler ekranında görünür."),

            CreateStep(
                "kitchen",
                "Kitchen ekranında sipariş akışını dene",
                "Sipariş durumunu Preparing veya Completed yaparak operasyonu doğrulayın.",
                kitchenOk,
                required: false,
                weight: 8,
                "Mutfak Modu",
                "/Business/Orders/Kitchen",
                "Personeline kitchen ekranını gösterin."),

            CreateStep(
                "campaign-reward",
                "Kampanya veya sadakat ödülü ekle",
                "İlk kampanya, sadakat kuralı veya ödül tanımı.",
                campaignOrRewardOk,
                required: false,
                weight: 5,
                "Kampanyalar",
                "/Business/Campaigns",
                "Ödüller veya sadakat ekranından da ilerleyebilirsiniz."),

            CreateStep(
                "staff",
                "Personel ekle",
                "Owner hazır olsun; mümkünse Staff kullanıcı ekleyin.",
                staffOrOwnerOk,
                required: false,
                weight: 4,
                "Personel",
                "/Business/Staff",
                "Staff, mutfak ve sipariş operasyonunu paylaşır.",
                ownerOnly: true,
                warning: !isBusinessOwner
                    ? "Personel ekleme Owner yetkisi gerektirir."
                    : null),

            CreateStep(
                "notifications-audit",
                "Bildirimleri ve audit log’u kontrol et",
                "Panel aktivitesi ve uyarı merkezi çalışıyor mu bakın.",
                activityOk,
                required: false,
                weight: 3,
                "Bildirimler",
                "/Business/Notifications",
                "Aktivite Geçmişi ile kritik değişiklikleri izleyin."),

            CreateStep(
                "crm-reports",
                "Raporlar ve CRM’i incele",
                "Siparişlerden gelen müşteri ve satış özetlerini görün.",
                reportsOk || crmOk,
                required: false,
                weight: 3,
                "Raporlar",
                "/Business/Reports",
                "CRM İçgörüleri ile tekrar eden müşterileri görün."),

            CreateStep(
                "legal-awareness",
                "Yasal sayfalar hakkında bilgi sahibi ol",
                "Gizlilik, KVKK ve Trust sayfalarını inceleyin (işaretleme kaydı tutulmaz).",
                legalAwarenessOk || publicMenuOk,
                required: false,
                weight: 2,
                "Trust Center",
                "/Trust",
                "Kalıcı onay kaydı bu ekranda yoktur; Sales form onayları ayrıdır."),

            CreateStep(
                "business-active",
                "İşletme hesabı aktif",
                "İşletme kaydı aktif ve slug geçerli.",
                businessActive && nameSlugOk,
                required: true,
                weight: 8,
                "Ayarlar",
                "/Business/Settings",
                ownerOnly: true)
        };

        // Ensure unique keys and intentional order for display checklist
        var displaySteps = new List<CustomerOnboardingStep>
        {
            steps.First(s => s.Key == "business-profile"),
            steps.First(s => s.Key == "whatsapp"),
            steps.First(s => s.Key == "category"),
            steps.First(s => s.Key == "product"),
            steps.First(s => s.Key == "public-menu"),
            steps.First(s => s.Key == "qr-poster"),
            steps.First(s => s.Key == "test-order"),
            steps.First(s => s.Key == "kitchen"),
            steps.First(s => s.Key == "campaign-reward"),
            steps.First(s => s.Key == "staff"),
            steps.First(s => s.Key == "notifications-audit"),
            steps.First(s => s.Key == "crm-reports")
        };

        var score = CalculateScore(
            businessActive,
            nameSlugOk,
            whatsAppOk,
            categoryOk,
            productOk,
            publicMenuOk,
            qrOk,
            testOrderOk,
            kitchenOk,
            campaignOrRewardOk,
            staffOrOwnerOk,
            activityOk);

        var required = displaySteps.Where(s => s.IsRequired).ToList();
        var requiredCompleted = required.Count(s => s.IsCompleted);
        var missingRequired = required.Count - requiredCompleted;

        var publicReady = publicMenuOk;
        var isLive = score >= 100
            || (testOrderOk && publicReady && productOk && whatsAppOk && businessActive);
        var isReady = missingRequired == 0 && publicReady && productOk;

        var status = ResolveStatus(score, isLive, isReady);
        var (statusLabel, badgeClass, cardVariant) = DescribeStatus(status);

        var next = displaySteps.FirstOrDefault(s => s.IsRequired && !s.IsCompleted)
            ?? displaySteps.FirstOrDefault(s => !s.IsCompleted);

        var latestSub = AdminSaasQueryHelper.GetLatestSubscription(business.Subscriptions);

        var isAtRisk = !businessActive
            || !productOk
            || !whatsAppOk
            || orderCount == 0
            || score < 40;

        return new CustomerOnboardingSnapshot
        {
            BusinessId = businessId,
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            BusinessIsActive = business.IsActive,
            PublicMenuUrl = string.IsNullOrWhiteSpace(publicMenuUrl)
                ? $"/m/{business.Slug}"
                : publicMenuUrl,
            Score = score,
            Status = status,
            StatusLabel = statusLabel,
            StatusBadgeClass = badgeClass,
            CardVariantClass = cardVariant,
            CompletedStepCount = displaySteps.Count(s => s.IsCompleted),
            TotalStepCount = displaySteps.Count,
            RequiredCompletedCount = requiredCompleted,
            RequiredTotalCount = required.Count,
            MissingRequiredCount = missingRequired,
            IsLive = isLive,
            IsReadyToLaunch = isReady || status is OnboardingStatus.ReadyToLaunch or OnboardingStatus.Live,
            IsAtRisk = isAtRisk,
            NextBestActionTitle = next?.Title,
            NextBestActionUrl = next?.ActionUrl,
            NextBestActionText = next?.ActionText,
            NextBestActionOwnerOnly = next?.OwnerOnly == true,
            ActiveCategoryCount = activeCategoryCount,
            ActiveProductCount = activeProductCount,
            OrderCount = orderCount,
            CampaignCount = campaignCount,
            RewardCount = rewardCount,
            StaffCount = staffCount,
            CustomerCount = customerCount,
            LastActivityAtUtc = lastActivity,
            PlanName = latestSub?.SubscriptionPlan?.Name,
            SubscriptionStatusLabel = latestSub is null
                ? "Abonelik yok"
                : AdminSaasQueryHelper.GetStatusLabel(latestSub.Status),
            Steps = displaySteps,
            LaunchChecklist =
            [
                "Müşteriye QR afişi ver",
                "WhatsApp sipariş yönlendirmesini test et",
                "Personeline kitchen ekranını göster",
                "İlk kampanyanı aktif et"
            ],
            QuickLinks = BuildQuickLinks(business.Slug, isBusinessOwner)
        };
    }

    public async Task<CustomerOnboardingDashboardCard?> BuildDashboardCardAsync(
        int businessId,
        CancellationToken cancellationToken = default)
    {
        var snap = await BuildAsync(businessId, string.Empty, isBusinessOwner: true, cancellationToken);
        if (snap is null)
        {
            return null;
        }

        return new CustomerOnboardingDashboardCard
        {
            Score = snap.Score,
            StatusLabel = snap.StatusLabel,
            StatusBadgeClass = snap.StatusBadgeClass,
            CardVariantClass = snap.CardVariantClass,
            NextBestActionTitle = snap.NextBestActionTitle,
            IsLowScore = snap.Score < 60,
            IsReadyOrLive = snap.Status is OnboardingStatus.ReadyToLaunch or OnboardingStatus.Live
        };
    }

    public async Task<List<CustomerOnboardingSnapshot>> BuildForBusinessesAsync(
        IEnumerable<int> businessIds,
        CancellationToken cancellationToken = default)
    {
        var list = new List<CustomerOnboardingSnapshot>();
        foreach (var id in businessIds.Distinct())
        {
            var snap = await BuildAsync(id, string.Empty, isBusinessOwner: true, cancellationToken);
            if (snap is not null)
            {
                list.Add(snap);
            }
        }

        return list;
    }

    private static int CalculateScore(
        bool businessActive,
        bool nameSlugOk,
        bool whatsAppOk,
        bool categoryOk,
        bool productOk,
        bool publicMenuOk,
        bool qrOk,
        bool testOrderOk,
        bool kitchenOk,
        bool campaignOrRewardOk,
        bool staffOrOwnerOk,
        bool activityOk)
    {
        // Weights sum to 100 when all true.
        var score = 0;
        if (businessActive) score += 8;
        if (nameSlugOk) score += 8;
        if (whatsAppOk) score += 12;
        if (categoryOk) score += 10;
        if (productOk) score += 15;
        if (publicMenuOk) score += 10;
        if (qrOk) score += 7;
        if (testOrderOk) score += 10;
        if (kitchenOk) score += 8;
        if (campaignOrRewardOk) score += 5;
        if (staffOrOwnerOk) score += 4;
        if (activityOk) score += 3;
        return Math.Min(100, score);
    }

    private static OnboardingStatus ResolveStatus(int score, bool isLive, bool isReady)
    {
        if (isLive || score >= 100)
        {
            return OnboardingStatus.Live;
        }

        if (isReady || score >= 85)
        {
            return OnboardingStatus.ReadyToLaunch;
        }

        if (score >= 60)
        {
            return OnboardingStatus.AlmostReady;
        }

        if (score >= 21)
        {
            return OnboardingStatus.SetupInProgress;
        }

        return OnboardingStatus.NotStarted;
    }

    private static (string Label, string BadgeClass, string CardVariant) DescribeStatus(OnboardingStatus status)
    {
        return status switch
        {
            OnboardingStatus.Live => ("Canlı", "bg-success", "border-success"),
            OnboardingStatus.ReadyToLaunch => ("Yayına Hazır", "bg-success", "border-success"),
            OnboardingStatus.AlmostReady => ("Yayına Yakın", "bg-primary", "border-primary"),
            OnboardingStatus.SetupInProgress => ("Kurulum Devam Ediyor", "bg-warning text-dark", "border-warning"),
            _ => ("Başlamadı", "bg-secondary", "border-secondary")
        };
    }

    private static CustomerOnboardingStep CreateStep(
        string key,
        string title,
        string description,
        bool isCompleted,
        bool required,
        int weight,
        string? actionText,
        string? actionUrl,
        string? help = null,
        bool ownerOnly = false,
        string? warning = null)
    {
        string badgeText;
        string badgeClass;
        string severity;

        if (isCompleted)
        {
            badgeText = "Tamamlandı";
            badgeClass = "bg-success";
            severity = "success";
        }
        else if (required)
        {
            badgeText = "Eksik";
            badgeClass = "bg-warning text-dark";
            severity = "warning";
        }
        else
        {
            badgeText = "Önerilir";
            badgeClass = "bg-info text-dark";
            severity = "info";
        }

        return new CustomerOnboardingStep
        {
            Key = key,
            Title = title,
            Description = description,
            IsCompleted = isCompleted,
            IsRequired = required,
            ScoreWeight = weight,
            ActionText = actionText,
            ActionUrl = actionUrl,
            HelpText = help,
            BadgeText = badgeText,
            BadgeClass = badgeClass,
            Severity = severity,
            OwnerOnly = ownerOnly,
            WarningText = warning
        };
    }

    private static List<CustomerOnboardingQuickLink> BuildQuickLinks(string slug, bool isBusinessOwner)
    {
        var links = new List<CustomerOnboardingQuickLink>
        {
            new() { Title = "Go-Live", Url = "/Business/GoLive" },
            new() { Title = "Demo Merkezi", Url = "/Business/DemoCenter" },
            new() { Title = "Menü Stüdyosu", Url = "/Business/MenuStudio" },
            new() { Title = "Ürünler", Url = "/Business/Products" },
            new() { Title = "Mutfak", Url = "/Business/Orders/Kitchen" },
            new() { Title = "Kampanyalar", Url = "/Business/Campaigns" },
            new() { Title = "Ödüller", Url = "/Business/Rewards" },
            new() { Title = "Müşteriler", Url = "/Business/Customers" },
            new() { Title = "Raporlar", Url = "/Business/Reports" },
            new() { Title = "Bildirimler", Url = "/Business/Notifications" },
            new() { Title = "Aktivite", Url = "/Business/AuditLogs" },
            new() { Title = "Public Menü", Url = $"/m/{slug}" },
            new() { Title = "Ayarlar", Url = "/Business/Settings", OwnerOnly = true },
            new() { Title = "Personel", Url = "/Business/Staff", OwnerOnly = true }
        };

        if (!isBusinessOwner)
        {
            links.RemoveAll(l => l.OwnerOnly);
        }

        return links;
    }
}
