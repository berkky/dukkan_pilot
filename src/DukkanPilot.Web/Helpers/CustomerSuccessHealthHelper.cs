using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.Success;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

/// <summary>
/// Read-only customer success and retention health snapshot from existing data.
/// No database writes, notifications, or audit activity.
/// </summary>
public class CustomerSuccessHealthHelper
{
    private readonly AppDbContext _context;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatusHelper;
    private readonly BusinessPlanLimitHelper _planLimitHelper;
    private readonly CustomerOnboardingHelper _onboardingHelper;

    public CustomerSuccessHealthHelper(
        AppDbContext context,
        BusinessSubscriptionStatusHelper subscriptionStatusHelper,
        BusinessPlanLimitHelper planLimitHelper,
        CustomerOnboardingHelper onboardingHelper)
    {
        _context = context;
        _subscriptionStatusHelper = subscriptionStatusHelper;
        _planLimitHelper = planLimitHelper;
        _onboardingHelper = onboardingHelper;
    }

    public async Task<CustomerSuccessSnapshot?> BuildAsync(
        int businessId,
        string publicMenuUrl,
        bool isBusinessOwner,
        CancellationToken cancellationToken = default)
    {
        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.Setting)
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var last7Start = now.AddDays(-7);
        var last30Start = now.AddDays(-30);

        var subscription = await _subscriptionStatusHelper.GetStatusAsync(businessId);
        var planUsage = await _planLimitHelper.GetUsageAsync(businessId);
        var onboarding = await _onboardingHelper.BuildAsync(
            businessId,
            string.IsNullOrWhiteSpace(publicMenuUrl) ? $"/m/{business.Slug}" : publicMenuUrl,
            isBusinessOwner,
            cancellationToken);

        var activeCategoryCount = await _context.Categories
            .CountAsync(c => c.BusinessId == businessId && c.IsActive, cancellationToken);

        var activeProductCount = await _context.Products
            .CountAsync(p => p.BusinessId == businessId
                && p.IsActive
                && p.Category.IsActive
                && p.Category.BusinessId == businessId, cancellationToken);

        var campaignCount = await _context.Campaigns
            .CountAsync(c => c.BusinessId == businessId && c.IsActive, cancellationToken);

        var rewardCount = await _context.Rewards
            .CountAsync(r => r.BusinessId == businessId && r.IsActive, cancellationToken);

        var staffCount = await _context.UserBusinessRoles
            .CountAsync(r => r.BusinessId == businessId
                && r.Role == BusinessRole.Staff
                && r.IsActive
                && r.AppUser.IsActive, cancellationToken);

        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        var activeCustomerCount = customers.Count(c => c.IsActive);

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        var ordersLast7 = orders.Where(o => o.CreatedAt >= last7Start).ToList();
        var ordersLast30 = orders.Where(o => o.CreatedAt >= last30Start).ToList();
        var completedOrdersLast30 = ordersLast30.Where(o => o.Status == OrderStatus.Completed).ToList();
        var cancelledOrdersLast30 = ordersLast30.Where(o => o.Status == OrderStatus.Cancelled).ToList();
        var revenueOrdersLast30 = ordersLast30.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var kitchenOrdersLast30 = ordersLast30.Where(o => o.Status is OrderStatus.Preparing or OrderStatus.Completed).ToList();
        var lastOrderAt = orders.Count > 0 ? orders.Max(o => (DateTime?)o.CreatedAt) : null;

        var revenueLast30 = revenueOrdersLast30.Sum(o => o.TotalAmount);
        var averageBasketLast30 = revenueOrdersLast30.Count > 0
            ? revenueLast30 / revenueOrdersLast30.Count
            : 0m;

        var crmStats = CustomerCrmStatsBuilder.Build(customers, orders, now);
        var repeatCustomers = crmStats.Count(s => CustomerCrmHelper.IsReturning(s));
        var newCustomersLast30 = crmStats.Count(s => CustomerCrmHelper.IsNewCustomer(s, now));
        var atRiskCustomers = crmStats.Count(s => CustomerCrmHelper.IsAtRisk(s, now));

        var hasNotification = await _context.Notifications
            .AnyAsync(n => n.BusinessId == businessId, cancellationToken);
        var hasAudit = await _context.AuditLogs
            .AnyAsync(a => a.BusinessId == businessId, cancellationToken);
        var lastNotificationAt = await _context.Notifications
            .Where(n => n.BusinessId == businessId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => (DateTime?)n.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var lastAuditAt = await _context.AuditLogs
            .Where(a => a.BusinessId == businessId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => (DateTime?)a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var criticalNotificationCount = await _context.Notifications
            .CountAsync(n => n.BusinessId == businessId && !n.IsRead && n.Severity == "Critical", cancellationToken);

        var billingInvoices = await _context.BillingInvoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId && i.Status != "Cancelled")
            .Select(i => new { i.Status, i.PaymentStatus, i.TotalAmount })
            .ToListAsync(cancellationToken);

        var overdueInvoiceCount = billingInvoices.Count(i => i.Status == "Overdue");
        var openInvoiceAmount = billingInvoices
            .Where(i => i.PaymentStatus != "Paid" && i.PaymentStatus != "Cancelled")
            .Sum(i => i.TotalAmount);

        DateTime? lastActivityAt = lastOrderAt;
        if (lastAuditAt.HasValue && (!lastActivityAt.HasValue || lastAuditAt > lastActivityAt))
        {
            lastActivityAt = lastAuditAt;
        }
        if (lastNotificationAt.HasValue && (!lastActivityAt.HasValue || lastNotificationAt > lastActivityAt))
        {
            lastActivityAt = lastNotificationAt;
        }

        var hasWhatsApp = !string.IsNullOrWhiteSpace(business.Setting?.WhatsAppNumber);
        var hasPhone = !string.IsNullOrWhiteSpace(business.Phone);
        var publicMenuReady = business.IsActive
            && !string.IsNullOrWhiteSpace(business.Slug)
            && activeCategoryCount > 0
            && activeProductCount > 0;

        var hasOrders7 = ordersLast7.Count > 0;
        var hasOrders30 = ordersLast30.Count > 0;
        var hasCompleted30 = completedOrdersLast30.Count > 0;
        var hasKitchen30 = kitchenOrdersLast30.Count > 0;
        var enoughProducts = activeProductCount >= 5;
        var engagementRecent = (lastAuditAt.HasValue && lastAuditAt >= last30Start)
            || (lastNotificationAt.HasValue && lastNotificationAt >= last30Start)
            || hasOrders30;
        var subscriptionHealthy = subscription.HasValidSubscription
            && (!subscription.DaysRemaining.HasValue || subscription.DaysRemaining.Value > 7);
        var trialSubscription = subscription.IsTrial;
        var usageNearLimit = planUsage.HasValidSubscription
            && planUsage.AllMetrics.Any(m => !m.IsUnlimited && m.UsagePercent >= 80);

        var growthSignals = new List<CustomerSuccessSignal>();
        var riskSignals = new List<CustomerSuccessSignal>();
        var positiveSignals = new List<CustomerSuccessSignal>();
        var recommendations = new List<CustomerSuccessRecommendation>();
        var breakdown = new List<CustomerSuccessBreakdownItem>();

        var usageScore = 0;
        if (hasOrders7) usageScore += 10;
        if (hasOrders30) usageScore += 7;
        if (hasCompleted30) usageScore += 5;
        if (hasKitchen30) usageScore += 3;
        usageScore = Math.Min(25, usageScore);

        var menuScore = 0;
        if (publicMenuReady) menuScore += 8;
        if (activeCategoryCount > 0) menuScore += 3;
        if (activeProductCount > 0) menuScore += 4;
        menuScore = Math.Min(15, menuScore);

        var engagementScore = 0;
        if (engagementRecent) engagementScore += 8;
        if (hasAudit) engagementScore += 4;
        if (hasNotification) engagementScore += 3;
        engagementScore = Math.Min(15, engagementScore);

        var revenueScore = 0;
        if (revenueLast30 > 0) revenueScore += 7;
        if (averageBasketLast30 > 0) revenueScore += 3;
        if (repeatCustomers > 0) revenueScore += 3;
        if (activeCustomerCount > 0) revenueScore += 2;
        revenueScore = Math.Min(15, revenueScore);

        var subscriptionScore = 0;
        if (subscription.HasValidSubscription) subscriptionScore += 8;
        if (!subscription.DaysRemaining.HasValue || subscription.DaysRemaining > 14) subscriptionScore += 4;
        if (!trialSubscription) subscriptionScore += 3;
        subscriptionScore = Math.Min(15, subscriptionScore);

        var adoptionScore = 0;
        if (campaignCount > 0) adoptionScore += 3;
        if (rewardCount > 0) adoptionScore += 2;
        if (repeatCustomers > 0) adoptionScore += 2;
        if (staffCount > 0) adoptionScore += 1;
        if (enoughProducts) adoptionScore += 2;
        adoptionScore = Math.Min(10, adoptionScore);

        var growthBonus = 0;
        if (usageNearLimit) growthBonus += 2;
        if (ordersLast30.Count >= 10) growthBonus += 1;
        if (campaignCount > 0 && repeatCustomers > 0) growthBonus += 1;
        if (staffCount == 0 && ordersLast30.Count >= 15) growthBonus += 1;

        var riskPenalty = 0;
        if (business.IsActive && activeProductCount == 0) riskPenalty += 12;
        if (business.IsActive && !hasOrders30) riskPenalty += 12;
        if (!hasWhatsApp) riskPenalty += 8;
        if (!subscription.HasValidSubscription) riskPenalty += 12;
        else if (subscription.DaysRemaining.HasValue && subscription.DaysRemaining.Value <= 7) riskPenalty += 8;
        if (onboarding is not null && onboarding.Score < 60) riskPenalty += 6;
        if (activeCustomerCount == 0) riskPenalty += 4;
        if (criticalNotificationCount > 0) riskPenalty += 5;
        if (!hasAudit) riskPenalty += 3;
        if (!publicMenuReady) riskPenalty += 8;
        if (overdueInvoiceCount > 0) riskPenalty += 10;
        else if (openInvoiceAmount > 0) riskPenalty += 4;

        var rawScore = usageScore + menuScore + engagementScore + revenueScore + subscriptionScore + adoptionScore + growthBonus - riskPenalty;
        var score = Math.Clamp(rawScore, 0, 100);

        if (hasOrders7)
        {
            positiveSignals.Add(CreateSignal("orders-7", "Son 7 gün sipariş", ordersLast7.Count.ToString(), true, "success", "Son 7 günde aktif sipariş akışı var."));
        }
        if (repeatCustomers > 0)
        {
            positiveSignals.Add(CreateSignal("repeat-customers", "Tekrar eden müşteri", repeatCustomers.ToString(), true, "success", "Müşteri bağlılığı sinyali."));
        }
        if (campaignCount > 0 || rewardCount > 0)
        {
            positiveSignals.Add(CreateSignal("adoption-depth", "Kampanya / ödül kullanımı", $"{campaignCount} kampanya · {rewardCount} ödül", true, "info", "Adoption derinliği artıyor."));
        }

        if (!hasWhatsApp)
        {
            riskSignals.Add(CreateSignal("missing-whatsapp", "WhatsApp eksik", "Yok", false, "warning", "Sipariş yönlendirmesi için WhatsApp numarası eksik."));
            recommendations.Add(CreateRecommendation("WhatsApp numaranı tamamla", "Sipariş akışının çalışması ve churn riskinin düşmesi için Settings ekranından WhatsApp numarasını ekleyin.", "warning", "/Business/Settings", "Ayarlar", "profile", true));
        }
        if (activeProductCount == 0)
        {
            riskSignals.Add(CreateSignal("no-products", "Aktif ürün", "0", false, "critical", "İşletme aktif ama müşteriye sunulan ürün yok."));
            recommendations.Add(CreateRecommendation("Aktif ürün ekle", "Public menü canlı olsa bile ürün yoksa kullanım başlayamaz.", "danger", "/Business/Products/Create", "Ürün Ekle", "menu", true));
        }
        if (!hasOrders30)
        {
            riskSignals.Add(CreateSignal("no-orders-30", "Son 30 gün sipariş", "0", false, "critical", "Son 30 günde sipariş alınmamış."));
            recommendations.Add(CreateRecommendation("Test veya gerçek sipariş akışını yeniden başlat", "Public menü, QR ve WhatsApp akışını yeniden test edin; sipariş yoksa churn riski artar.", "danger", $"/m/{business.Slug}", "Test Siparişi", "usage", true));
        }
        if (!subscription.HasValidSubscription)
        {
            riskSignals.Add(CreateSignal("subscription-invalid", "Abonelik", subscription.StatusText, false, "critical", "Abonelik geçerli değil."));
            recommendations.Add(CreateRecommendation("Abonelik durumunu çöz", "Plan veya abonelik sorunu kullanım kesintisine yol açabilir.", "danger", "/Business/Billing", "Abonelik", "subscription", true));
        }
        else if (subscription.DaysRemaining.HasValue && subscription.DaysRemaining.Value <= 7)
        {
            riskSignals.Add(CreateSignal("subscription-expiring", "Kalan gün", $"{subscription.DaysRemaining} gün", false, "warning", "Abonelik 7 gün içinde bitebilir."));
            recommendations.Add(CreateRecommendation("Abonelik yenilemeyi planla", "Kesinti riskini önlemek için planı gözden geçirin.", "warning", "/Business/Billing", "Billing", "subscription", true));
        }
        if (onboarding is not null && onboarding.Score < 60)
        {
            riskSignals.Add(CreateSignal("onboarding-low", "Onboarding skoru", onboarding.Score.ToString(), false, "warning", "Kurulum eksikleri kullanım sağlığını aşağı çekiyor."));
            recommendations.Add(CreateRecommendation("Önce kurulum eksiklerini kapat", "Onboarding tamamlanmadan retention metrikleri sağlıklı olmayabilir.", "warning", "/Business/Onboarding", "Kurulum Sihirbazı", "onboarding", false));
        }
        if (!publicMenuReady)
        {
            riskSignals.Add(CreateSignal("menu-not-ready", "Public menü", "Eksik", false, "warning", "Slug, kategori veya ürün eksik."));
        }
        if (overdueInvoiceCount > 0)
        {
            riskSignals.Add(CreateSignal("billing-overdue", "Tahsilat riski", $"{overdueInvoiceCount} gecikmiş", false, "critical", "Gecikmiş tahsilat kaydı var. Ödeme durumunu kontrol edin."));
            recommendations.Add(CreateRecommendation("Ödeme durumunu kontrol edin", "Gecikmiş tahsilat kaydı bulunuyor. Ödeme ve abonelik durumunu netleştirin.", "danger", "/Business/Billing/Invoices", "Tahsilat Kayıtları", "billing", true));
        }
        else if (openInvoiceAmount > 0)
        {
            riskSignals.Add(CreateSignal("billing-open", "Açık tahsilat", openInvoiceAmount.ToString("N2"), false, "warning", "Ödenmemiş/kısmi tahsilat kaydı mevcut."));
            recommendations.Add(CreateRecommendation("Tahsilat kayıtlarını kontrol edin", "Ödenmemiş veya kısmi tahsilat kayıtlarınız var. Vade ve ödeme durumunu kontrol edin.", "warning", "/Business/Billing/Invoices", "Tahsilat Kayıtları", "billing", false));
        }
        if (!engagementRecent)
        {
            riskSignals.Add(CreateSignal("low-activity", "Son panel aktivitesi", lastActivityAt?.ToLocalTime().ToString("g") ?? "Yok", false, "warning", "Son 30 günde belirgin panel aktivitesi görünmüyor."));
        }
        if (criticalNotificationCount > 0)
        {
            riskSignals.Add(CreateSignal("critical-notifications", "Kritik bildirim", criticalNotificationCount.ToString(), false, "critical", "Okunmamış kritik bildirimler mevcut."));
        }

        if (campaignCount == 0)
        {
            recommendations.Add(CreateRecommendation("İlk kampanyanı başlat", "Kampanya kullanımı sipariş ve retention açısından iyi bir sonraki adımdır.", "info", "/Business/Campaigns/Create", "Kampanya Oluştur", "growth", false));
        }
        if (rewardCount == 0)
        {
            recommendations.Add(CreateRecommendation("Sadakat ödülü ekle", "Tekrar eden müşteri oranını artırmak için ödül veya sadakat kullanın.", "info", "/Business/Rewards", "Ödüller", "growth", false));
        }
        if (ordersLast30.Count > 0)
        {
            recommendations.Add(CreateRecommendation("Raporları incele", "Son 30 gün ciro ve sipariş trendlerini izleyin.", "info", "/Business/Reports", "Raporlar", "analytics", false));
        }
        if (usageNearLimit)
        {
            growthSignals.Add(CreateSignal("near-limit", "Plan limitine yakın", "80%+", true, "info", "Yüksek kullanım büyüme fırsatı olabilir."));
            recommendations.Add(CreateRecommendation("Plan limitini gözden geçir", "Limitlere yaklaşmak upgrade için iyi bir sinyal olabilir.", "info", "/Business/Billing", "Planı Gör", "expansion", false));
        }
        if (await HasOpenUpgradeRequestAsync(businessId, cancellationToken))
        {
            growthSignals.Add(CreateSignal("upgrade-request", "Upgrade talebi", "Açık talep var", true, "success", "İşletme zaten yükseltme ilgisi göstermiş."));
        }
        if (ordersLast30.Count >= 10 && planUsage.HasValidSubscription)
        {
            growthSignals.Add(CreateSignal("high-usage", "Sipariş hacmi", ordersLast30.Count.ToString(), true, "success", "Son 30 gündeki kullanım büyüme potansiyeli gösteriyor."));
        }
        if (activeCustomerCount > 0 && repeatCustomers == 0)
        {
            recommendations.Add(CreateRecommendation("CRM ve tekrar eden müşteri takibini güçlendir", "Sadakat veya kampanya ile müşteriyi geri çağırın.", "info", "/Business/Customers/Insights", "CRM İçgörüleri", "crm", false));
        }

        var status = ResolveStatus(score);
        var (statusLabel, statusBadge, cardVariant) = DescribeStatus(status);
        var churnRisk = ResolveChurnRisk(score, riskSignals, subscription, hasOrders30, publicMenuReady);
        var (churnLabel, churnBadge) = DescribeChurnRisk(churnRisk);
        var expansion = ResolveExpansionPotential(score, growthSignals.Count, usageNearLimit, ordersLast30.Count, campaignCount, repeatCustomers);
        var (expansionLabel, expansionBadge) = DescribeExpansionPotential(expansion);

        breakdown.Add(new CustomerSuccessBreakdownItem { Key = "usage", Label = "Kullanım", Score = usageScore, MaxScore = 25, Description = "Sipariş ve operasyon kullanımı" });
        breakdown.Add(new CustomerSuccessBreakdownItem { Key = "menu", Label = "Menü", Score = menuScore, MaxScore = 15, Description = "Kategori, ürün ve public menü hazır mı?" });
        breakdown.Add(new CustomerSuccessBreakdownItem { Key = "engagement", Label = "Operasyon", Score = engagementScore, MaxScore = 15, Description = "Audit/notification/panel aktivitesi" });
        breakdown.Add(new CustomerSuccessBreakdownItem { Key = "revenue", Label = "Ticari aktivite", Score = revenueScore, MaxScore = 15, Description = "Ciro, sepet, tekrar eden müşteri" });
        breakdown.Add(new CustomerSuccessBreakdownItem { Key = "subscription", Label = "Abonelik", Score = subscriptionScore, MaxScore = 15, Description = "Plan geçerliliği ve kalan süre" });
        breakdown.Add(new CustomerSuccessBreakdownItem { Key = "adoption", Label = "Adoption", Score = adoptionScore, MaxScore = 10, Description = "Kampanya, ödül, CRM, staff" });
        breakdown.Add(new CustomerSuccessBreakdownItem { Key = "growth", Label = "Büyüme etkisi", Score = growthBonus - riskPenalty, MaxScore = 5, Description = "Risk penalty ve growth sinyalleri" });

        var orderedRecommendations = recommendations
            .OrderByDescending(r => r.IsCritical)
            .ThenByDescending(r => SeverityRank(r.Severity))
            .Take(8)
            .ToList();

        var topRisk = riskSignals.OrderByDescending(s => SeverityRank(s.Severity)).FirstOrDefault()?.Label;
        var nextRecommendation = orderedRecommendations.FirstOrDefault();

        return new CustomerSuccessSnapshot
        {
            BusinessId = businessId,
            BusinessName = business.Name,
            BusinessSlug = business.Slug,
            PublicMenuUrl = string.IsNullOrWhiteSpace(publicMenuUrl) ? $"/m/{business.Slug}" : publicMenuUrl,
            Score = score,
            Status = status,
            StatusLabel = statusLabel,
            StatusBadgeClass = statusBadge,
            CardVariantClass = cardVariant,
            ChurnRisk = churnRisk,
            ChurnRiskLabel = churnLabel,
            ChurnRiskBadgeClass = churnBadge,
            ExpansionPotential = expansion,
            ExpansionPotentialLabel = expansionLabel,
            ExpansionPotentialBadgeClass = expansionBadge,
            IsAtRisk = status is CustomerSuccessHealthStatus.Critical or CustomerSuccessHealthStatus.AtRisk,
            IsHealthyOrBetter = status is CustomerSuccessHealthStatus.Healthy or CustomerSuccessHealthStatus.GrowthReady,
            TopRiskLabel = topRisk,
            NextRecommendedActionTitle = nextRecommendation?.Title,
            NextRecommendedActionUrl = nextRecommendation?.ActionUrl,
            NextRecommendedActionText = nextRecommendation?.ActionText,
            LastOrderAtUtc = lastOrderAt,
            LastActivityAtUtc = lastActivityAt,
            Kpis = new CustomerSuccessKpiSnapshot
            {
                OrdersLast7Days = ordersLast7.Count,
                OrdersLast30Days = ordersLast30.Count,
                CompletedOrdersLast30Days = completedOrdersLast30.Count,
                CancelledOrdersLast30Days = cancelledOrdersLast30.Count,
                RevenueLast30Days = revenueLast30,
                AverageBasketLast30Days = averageBasketLast30,
                NewCustomersLast30Days = newCustomersLast30,
                RepeatCustomers = repeatCustomers,
                ActiveCustomerCount = activeCustomerCount,
                ActiveCategoryCount = activeCategoryCount,
                ActiveProductCount = activeProductCount,
                CampaignCount = campaignCount,
                RewardCount = rewardCount,
                StaffCount = staffCount,
                CriticalNotificationCount = criticalNotificationCount
            },
            Subscription = subscription,
            PlanUsage = planUsage,
            Onboarding = onboarding,
            RiskSignals = riskSignals,
            GrowthSignals = growthSignals,
            PositiveSignals = positiveSignals,
            Recommendations = orderedRecommendations,
            Breakdown = breakdown,
            QuickLinks = BuildQuickLinks(isBusinessOwner, business.Slug)
        };
    }

    public async Task<CustomerSuccessDashboardCard?> BuildDashboardCardAsync(
        int businessId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildAsync(businessId, string.Empty, isBusinessOwner: true, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        return new CustomerSuccessDashboardCard
        {
            Score = snapshot.Score,
            StatusLabel = snapshot.StatusLabel,
            StatusBadgeClass = snapshot.StatusBadgeClass,
            CardVariantClass = snapshot.CardVariantClass,
            ChurnRiskLabel = snapshot.ChurnRiskLabel,
            ChurnRiskBadgeClass = snapshot.ChurnRiskBadgeClass,
            TopRiskLabel = snapshot.TopRiskLabel,
            NextActionTitle = snapshot.NextRecommendedActionTitle,
            IsCriticalOrAtRisk = snapshot.IsAtRisk,
            IsHealthyOrBetter = snapshot.IsHealthyOrBetter
        };
    }

    public async Task<List<CustomerSuccessSnapshot>> BuildForBusinessesAsync(
        IEnumerable<int> businessIds,
        CancellationToken cancellationToken = default)
    {
        var snapshots = new List<CustomerSuccessSnapshot>();
        foreach (var businessId in businessIds.Distinct())
        {
            var snapshot = await BuildAsync(businessId, string.Empty, isBusinessOwner: true, cancellationToken);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }

    private async Task<bool> HasOpenUpgradeRequestAsync(int businessId, CancellationToken cancellationToken)
    {
        return await _context.SalesRequests
            .AnyAsync(r => r.BusinessId == businessId
                && r.RequestType == "UpgradeRequest"
                && (r.Status == "New" || r.Status == "Contacted" || r.Status == "Qualified" || r.Status == "WaitingCustomer"),
                cancellationToken);
    }

    private static CustomerSuccessSignal CreateSignal(
        string key,
        string label,
        string value,
        bool isPositive,
        string severity,
        string description) => new()
    {
        Key = key,
        Label = label,
        Value = value,
        IsPositive = isPositive,
        Severity = severity,
        Description = description
    };

    private static CustomerSuccessRecommendation CreateRecommendation(
        string title,
        string description,
        string severity,
        string? actionUrl,
        string? actionText,
        string category,
        bool isCritical) => new()
    {
        Title = title,
        Description = description,
        Severity = severity,
        ActionUrl = actionUrl,
        ActionText = actionText,
        Category = category,
        IsCritical = isCritical
    };

    private static CustomerSuccessHealthStatus ResolveStatus(int score) => score switch
    {
        <= 34 => CustomerSuccessHealthStatus.Critical,
        <= 59 => CustomerSuccessHealthStatus.AtRisk,
        <= 79 => CustomerSuccessHealthStatus.Stable,
        <= 89 => CustomerSuccessHealthStatus.Healthy,
        _ => CustomerSuccessHealthStatus.GrowthReady
    };

    private static (string Label, string BadgeClass, string CardVariant) DescribeStatus(CustomerSuccessHealthStatus status) => status switch
    {
        CustomerSuccessHealthStatus.Critical => ("Kritik", "bg-danger", "border-danger"),
        CustomerSuccessHealthStatus.AtRisk => ("Riskli", "bg-warning text-dark", "border-warning"),
        CustomerSuccessHealthStatus.Stable => ("Stabil", "bg-primary", "border-primary"),
        CustomerSuccessHealthStatus.Healthy => ("Sağlıklı", "bg-success", "border-success"),
        _ => ("Büyümeye Hazır", "bg-success", "border-success")
    };

    private static CustomerSuccessChurnRisk ResolveChurnRisk(
        int score,
        List<CustomerSuccessSignal> riskSignals,
        BusinessSubscriptionStatusViewModel subscription,
        bool hasOrders30,
        bool publicMenuReady)
    {
        if (!subscription.HasValidSubscription || !hasOrders30 || !publicMenuReady || score <= 34)
        {
            return CustomerSuccessChurnRisk.Critical;
        }

        if (riskSignals.Count >= 3 || score <= 59)
        {
            return CustomerSuccessChurnRisk.High;
        }

        if (riskSignals.Count >= 1 || score <= 79)
        {
            return CustomerSuccessChurnRisk.Medium;
        }

        return CustomerSuccessChurnRisk.Low;
    }

    private static (string Label, string BadgeClass) DescribeChurnRisk(CustomerSuccessChurnRisk risk) => risk switch
    {
        CustomerSuccessChurnRisk.Critical => ("Critical", "bg-danger"),
        CustomerSuccessChurnRisk.High => ("High", "bg-warning text-dark"),
        CustomerSuccessChurnRisk.Medium => ("Medium", "bg-primary"),
        _ => ("Low", "bg-success")
    };

    private static CustomerSuccessExpansionPotential ResolveExpansionPotential(
        int score,
        int growthSignalCount,
        bool usageNearLimit,
        int ordersLast30,
        int campaignCount,
        int repeatCustomers)
    {
        if (score >= 90 && (usageNearLimit || ordersLast30 >= 15) && (campaignCount > 0 || repeatCustomers > 0))
        {
            return CustomerSuccessExpansionPotential.StrongFit;
        }

        if (score >= 80 && (usageNearLimit || growthSignalCount >= 2))
        {
            return CustomerSuccessExpansionPotential.GoodFit;
        }

        if (score >= 60 && (growthSignalCount > 0 || ordersLast30 >= 5))
        {
            return CustomerSuccessExpansionPotential.Watch;
        }

        return CustomerSuccessExpansionPotential.None;
    }

    private static (string Label, string BadgeClass) DescribeExpansionPotential(CustomerSuccessExpansionPotential potential) => potential switch
    {
        CustomerSuccessExpansionPotential.StrongFit => ("StrongFit", "bg-success"),
        CustomerSuccessExpansionPotential.GoodFit => ("GoodFit", "bg-primary"),
        CustomerSuccessExpansionPotential.Watch => ("Watch", "bg-info text-dark"),
        _ => ("None", "bg-secondary")
    };

    private static int SeverityRank(string severity) => severity.ToLowerInvariant() switch
    {
        "danger" or "critical" => 4,
        "warning" => 3,
        "success" => 2,
        _ => 1
    };

    private static List<(string Title, string Url, bool OwnerOnly)> BuildQuickLinks(bool isBusinessOwner, string slug)
    {
        var links = new List<(string Title, string Url, bool OwnerOnly)>
        {
            ("Kurulum Sihirbazı", "/Business/Onboarding", false),
            ("Go-Live", "/Business/GoLive", false),
            ("Ürünler", "/Business/Products", false),
            ("Mutfak", "/Business/Orders/Kitchen", false),
            ("Kampanyalar", "/Business/Campaigns", false),
            ("Müşteriler", "/Business/Customers", false),
            ("Raporlar", "/Business/Reports", false),
            ("Bildirimler", "/Business/Notifications", false),
            ("Public Menü", $"/m/{slug}", false),
            ("Abonelik", "/Business/Billing", true)
        };

        return isBusinessOwner ? links : links.Where(x => !x.OwnerOnly).ToList();
    }
}
