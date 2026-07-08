using System.Text.Json;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Services;

public sealed class NotificationCreateRequest
{
    public int? BusinessId { get; set; }
    public int? UserId { get; set; }
    public string? TargetRole { get; set; }
    public string Area { get; set; } = "Business";
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? EntityName { get; set; }
    public int? EntityId { get; set; }
    public string Severity { get; set; } = "Info";
    public DateTime? ExpiresAtUtc { get; set; }
    public object? Metadata { get; set; }
    public bool AllowDuplicate { get; set; }
}

public interface INotificationService
{
    Task CreateAsync(NotificationCreateRequest request, CancellationToken cancellationToken = default);

    Task CreateBusinessAsync(
        int businessId,
        string type,
        string title,
        string message,
        string? actionUrl = null,
        string severity = "Info",
        string? entityName = null,
        int? entityId = null,
        object? metadata = null,
        bool allowDuplicate = false,
        CancellationToken cancellationToken = default);

    Task CreateAdminAsync(
        string type,
        string title,
        string message,
        string? actionUrl = null,
        string severity = "Info",
        string? entityName = null,
        int? entityId = null,
        object? metadata = null,
        int? businessId = null,
        bool allowDuplicate = false,
        CancellationToken cancellationToken = default);

    Task MarkReadAsync(int notificationId, int? businessId, bool isAdmin, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(int? businessId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(int? businessId, bool isAdmin, CancellationToken cancellationToken = default);

    Task GenerateSmartBusinessAlertsAsync(int businessId, CancellationToken cancellationToken = default);

    Task GenerateSmartAdminAlertsAsync(CancellationToken cancellationToken = default);
}

public class NotificationService : INotificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordhash", "token", "resettoken", "trackingtoken",
        "cookie", "authorization", "connectionstring", "secret", "apikey",
        "phone", "customerphone", "whatsapp"
    };

    private readonly AppDbContext _context;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatusHelper;
    private readonly BusinessPlanLimitHelper _planLimitHelper;
    private readonly GoLiveHelper _goLiveHelper;

    public NotificationService(
        AppDbContext context,
        BusinessSubscriptionStatusHelper subscriptionStatusHelper,
        BusinessPlanLimitHelper planLimitHelper,
        GoLiveHelper goLiveHelper)
    {
        _context = context;
        _subscriptionStatusHelper = subscriptionStatusHelper;
        _planLimitHelper = planLimitHelper;
        _goLiveHelper = goLiveHelper;
    }

    public Task CreateBusinessAsync(
        int businessId,
        string type,
        string title,
        string message,
        string? actionUrl = null,
        string severity = "Info",
        string? entityName = null,
        int? entityId = null,
        object? metadata = null,
        bool allowDuplicate = false,
        CancellationToken cancellationToken = default)
        => CreateAsync(new NotificationCreateRequest
        {
            BusinessId = businessId,
            Area = "Business",
            Type = type,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Severity = severity,
            EntityName = entityName,
            EntityId = entityId,
            Metadata = metadata,
            AllowDuplicate = allowDuplicate
        }, cancellationToken);

    public Task CreateAdminAsync(
        string type,
        string title,
        string message,
        string? actionUrl = null,
        string severity = "Info",
        string? entityName = null,
        int? entityId = null,
        object? metadata = null,
        int? businessId = null,
        bool allowDuplicate = false,
        CancellationToken cancellationToken = default)
        => CreateAsync(new NotificationCreateRequest
        {
            BusinessId = businessId,
            Area = "Admin",
            TargetRole = nameof(UserRole.SuperAdmin),
            Type = type,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Severity = severity,
            EntityName = entityName,
            EntityId = entityId,
            Metadata = metadata,
            AllowDuplicate = allowDuplicate
        }, cancellationToken);

    public async Task CreateAsync(NotificationCreateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Type)
                || string.IsNullOrWhiteSpace(request.Title)
                || string.IsNullOrWhiteSpace(request.Message))
            {
                return;
            }

            var type = Truncate(request.Type, 80)!;
            var entityName = Truncate(request.EntityName, 80);
            var now = DateTime.UtcNow;

            if (!request.AllowDuplicate)
            {
                var existsQuery = _context.Notifications
                    .Where(n => !n.IsRead && n.Type == type);

                if (request.BusinessId.HasValue)
                {
                    existsQuery = existsQuery.Where(n => n.BusinessId == request.BusinessId.Value);
                }
                else
                {
                    existsQuery = existsQuery.Where(n => n.BusinessId == null);
                }

                if (!string.IsNullOrWhiteSpace(entityName))
                {
                    existsQuery = existsQuery.Where(n => n.EntityName == entityName);
                }

                if (request.EntityId.HasValue)
                {
                    existsQuery = existsQuery.Where(n => n.EntityId == request.EntityId.Value);
                }

                if (await existsQuery.AnyAsync(cancellationToken))
                {
                    return;
                }
            }

            var notification = new Notification
            {
                CreatedAtUtc = now,
                BusinessId = request.BusinessId,
                UserId = request.UserId,
                TargetRole = Truncate(request.TargetRole, 50),
                Area = Truncate(request.Area, 40) ?? "Business",
                Type = type,
                Title = Truncate(request.Title, 200)!,
                Message = Truncate(request.Message, 1000)!,
                ActionUrl = Truncate(request.ActionUrl, 400),
                EntityName = entityName,
                EntityId = request.EntityId,
                Severity = Truncate(string.IsNullOrWhiteSpace(request.Severity) ? "Info" : request.Severity, 20) ?? "Info",
                IsRead = false,
                ExpiresAtUtc = request.ExpiresAtUtc,
                MetadataJson = Truncate(SerializeMetadata(request.Metadata), 4000)
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Notification failures must never break the primary business action.
        }
    }

    public async Task MarkReadAsync(int notificationId, int? businessId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Notifications.Where(n => n.Id == notificationId && !n.IsRead);
            if (!isAdmin)
            {
                query = query.Where(n => n.BusinessId == businessId);
            }

            var notification = await query.FirstOrDefaultAsync(cancellationToken);
            if (notification is null)
            {
                return;
            }

            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Fail-safe
        }
    }

    public async Task MarkAllReadAsync(int? businessId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Notifications.Where(n => !n.IsRead);
            query = isAdmin
                ? query.Where(n => n.Area == "Admin")
                : query.Where(n => n.BusinessId == businessId);

            var items = await query.ToListAsync(cancellationToken);
            if (items.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var item in items)
            {
                item.IsRead = true;
                item.ReadAtUtc = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Fail-safe
        }
    }

    public async Task<int> GetUnreadCountAsync(int? businessId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Notifications.AsNoTracking().Where(n => !n.IsRead);
            query = isAdmin
                ? query.Where(n => n.Area == "Admin")
                : query.Where(n => n.BusinessId == businessId);

            return await query.CountAsync(cancellationToken);
        }
        catch
        {
            return 0;
        }
    }

    public async Task GenerateSmartBusinessAlertsAsync(int businessId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionStatusHelper.GetStatusAsync(businessId);

            if (!subscription.HasValidSubscription)
            {
                await CreateBusinessAsync(
                    businessId,
                    "SubscriptionExpired",
                    "Abonelik geçersiz",
                    subscription.Message ?? "Aktif aboneliğiniz bulunmuyor.",
                    "/Business/Billing",
                    "Critical",
                    "BusinessSubscription",
                    businessId,
                    cancellationToken: cancellationToken);
            }
            else if (subscription.DaysRemaining is >= 0 and <= 7)
            {
                await CreateBusinessAsync(
                    businessId,
                    "SubscriptionExpiring",
                    "Abonelik yakında bitiyor",
                    $"Aboneliğinizin bitmesine {subscription.DaysRemaining} gün kaldı.",
                    "/Business/Billing",
                    "Warning",
                    "BusinessSubscription",
                    businessId,
                    cancellationToken: cancellationToken);
            }

            var goLive = await _goLiveHelper.BuildDashboardCardAsync(businessId);
            if (goLive is not null && !goLive.IsReadyToGoLive)
            {
                var stepTitle = goLive.PrimaryMissingStepTitle ?? "Eksik adım";
                await CreateBusinessAsync(
                    businessId,
                    "GoLiveMissingStep",
                    "Go-Live adımları eksik",
                    $"Yayına hazırlık tamamlanmadı. Eksik: {stepTitle}",
                    "/Business/GoLive",
                    "Warning",
                    "GoLive",
                    businessId,
                    cancellationToken: cancellationToken);
            }

            var activeProducts = await _context.Products.CountAsync(
                p => p.BusinessId == businessId && p.IsActive, cancellationToken);
            if (activeProducts == 0)
            {
                await CreateBusinessAsync(
                    businessId,
                    "MenuNoActiveProducts",
                    "Aktif ürün yok",
                    "Public menüde görünecek aktif ürün bulunmuyor.",
                    "/Business/Products",
                    "Critical",
                    "Product",
                    businessId,
                    cancellationToken: cancellationToken);
            }

            var activeCategories = await _context.Categories.CountAsync(
                c => c.BusinessId == businessId && c.IsActive, cancellationToken);
            if (activeCategories == 0)
            {
                await CreateBusinessAsync(
                    businessId,
                    "MenuNoActiveCategories",
                    "Aktif kategori yok",
                    "Public menüde görünecek aktif kategori bulunmuyor.",
                    "/Business/Categories",
                    "Warning",
                    "Category",
                    businessId,
                    cancellationToken: cancellationToken);
            }

            var business = await _context.Businesses.AsNoTracking()
                .Include(b => b.Setting)
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is not null && string.IsNullOrWhiteSpace(business.Setting?.WhatsAppNumber))
            {
                await CreateBusinessAsync(
                    businessId,
                    "WhatsAppMissing",
                    "WhatsApp numarası eksik",
                    "Siparişlerin WhatsApp'a iletilmesi için numara ekleyin.",
                    "/Business/Settings",
                    "Warning",
                    "BusinessSetting",
                    businessId,
                    cancellationToken: cancellationToken);
            }

            if (subscription.HasValidSubscription)
            {
                var usage = await _planLimitHelper.GetUsageAsync(businessId);
                await TryCreatePlanLimitAlertAsync(businessId, "ProductLimitWarning", "Ürün", usage.Products, "/Business/Products", cancellationToken);
                await TryCreatePlanLimitAlertAsync(businessId, "CampaignLimitWarning", "Kampanya", usage.Campaigns, "/Business/Campaigns", cancellationToken);
                await TryCreatePlanLimitAlertAsync(businessId, "StaffLimitWarning", "Personel", usage.StaffUsers, "/Business/Staff", cancellationToken);
            }

            var now = DateTime.UtcNow;
            var expiringCampaigns = await _context.Campaigns.AsNoTracking()
                .Where(c => c.BusinessId == businessId
                    && c.IsActive
                    && c.EndDate != null
                    && c.EndDate >= now
                    && c.EndDate <= now.AddDays(7))
                .OrderBy(c => c.EndDate)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var campaign in expiringCampaigns)
            {
                var daysLeft = Math.Max(0, (int)Math.Ceiling((campaign.EndDate!.Value - now).TotalDays));
                await CreateBusinessAsync(
                    businessId,
                    "CampaignExpiring",
                    "Kampanya yakında bitiyor",
                    $"“{Truncate(campaign.Title, 80)}” kampanyasının bitmesine {daysLeft} gün kaldı.",
                    "/Business/Campaigns",
                    "Warning",
                    "Campaign",
                    campaign.Id,
                    cancellationToken: cancellationToken);
            }
        }
        catch
        {
            // Fail-safe
        }
    }

    public async Task GenerateSmartAdminAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var businesses = await _context.Businesses.AsNoTracking()
                .Include(b => b.Setting)
                .Include(b => b.Subscriptions)
                .ThenInclude(s => s.SubscriptionPlan)
                .ToListAsync(cancellationToken);

            var productCounts = await _context.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .GroupBy(p => p.BusinessId)
                .Select(g => new { BusinessId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BusinessId, x => x.Count, cancellationToken);

            var categoryCounts = await _context.Categories.AsNoTracking()
                .Where(c => c.IsActive)
                .GroupBy(c => c.BusinessId)
                .Select(g => new { BusinessId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BusinessId, x => x.Count, cancellationToken);

            var lastOrderDates = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.BusinessId)
                .Select(g => new { BusinessId = g.Key, LastOrder = g.Max(o => o.CreatedAt) })
                .ToDictionaryAsync(x => x.BusinessId, x => (DateTime?)x.LastOrder, cancellationToken);

            var expiredCount = 0;
            var expiringSoonCount = 0;
            var noProductCount = 0;
            var inactiveOrderCount = 0;

            foreach (var business in businesses.Where(b => b.IsActive))
            {
                var latestSub = AdminSaasQueryHelper.GetLatestSubscription(business.Subscriptions);

                if (latestSub is null || AdminSaasQueryHelper.IsExpiredSubscription(latestSub, now)
                    || !AdminSaasQueryHelper.IsSubscriptionValid(latestSub, now))
                {
                    if (latestSub is null || AdminSaasQueryHelper.IsExpiredSubscription(latestSub, now))
                    {
                        expiredCount++;
                    }
                }

                if (latestSub is not null && AdminSaasQueryHelper.IsExpiringSoon(latestSub, now, 7))
                {
                    expiringSoonCount++;
                }

                var products = productCounts.GetValueOrDefault(business.Id);
                if (products == 0)
                {
                    noProductCount++;
                    await CreateAdminAsync(
                        "BusinessNoActiveProducts",
                        "Aktif ürünü olmayan işletme",
                        $"“{Truncate(business.Name, 80)}” işletmesinin aktif ürünü yok.",
                        $"/Admin/Businesses/Details/{business.Id}",
                        "Warning",
                        "Business",
                        business.Id,
                        businessId: business.Id,
                        cancellationToken: cancellationToken);
                }

                var lastOrder = lastOrderDates.GetValueOrDefault(business.Id);
                if (lastOrder is null || lastOrder < now.AddDays(-30))
                {
                    inactiveOrderCount++;
                    await CreateAdminAsync(
                        "BusinessNoRecentOrders",
                        "Sipariş almayan işletme",
                        $"“{Truncate(business.Name, 80)}” son 30 günde sipariş almadı.",
                        $"/Admin/Businesses/Details/{business.Id}",
                        "Warning",
                        "Business",
                        business.Id,
                        businessId: business.Id,
                        cancellationToken: cancellationToken);
                }

                var healthInput = AdminBusinessHealthHelper.CreateInput(
                    business,
                    latestSub,
                    categoryCounts.GetValueOrDefault(business.Id),
                    products,
                    lastOrder,
                    now);
                var health = AdminBusinessHealthHelper.Evaluate(healthInput);
                if (health.Score < 50)
                {
                    await CreateAdminAsync(
                        "BusinessAtRisk",
                        "Riskli işletme",
                        $"“{Truncate(business.Name, 80)}” sağlık skoru düşük ({health.Score}).",
                        $"/Admin/Businesses/Details/{business.Id}",
                        "Critical",
                        "Business",
                        business.Id,
                        businessId: business.Id,
                        cancellationToken: cancellationToken);
                }
            }

            if (expiredCount > 0)
            {
                await CreateAdminAsync(
                    "SubscriptionExpiredSummary",
                    "Aboneliği biten işletmeler",
                    $"{expiredCount} işletmenin aboneliği geçersiz veya süresi dolmuş.",
                    "/Admin/Businesses?subscriptionFilter=expired",
                    "Critical",
                    "Platform",
                    null,
                    cancellationToken: cancellationToken);
            }

            if (expiringSoonCount > 0)
            {
                await CreateAdminAsync(
                    "SubscriptionExpiringSoonSummary",
                    "Aboneliği yakında bitecek işletmeler",
                    $"{expiringSoonCount} işletmenin aboneliği 7 gün içinde bitiyor.",
                    "/Admin/Businesses",
                    "Warning",
                    "Platform",
                    null,
                    cancellationToken: cancellationToken);
            }

            if (noProductCount > 0)
            {
                await CreateAdminAsync(
                    "BusinessesWithoutProductsSummary",
                    "Ürünsüz aktif işletmeler",
                    $"{noProductCount} aktif işletmenin aktif ürünü yok.",
                    "/Admin/Businesses",
                    "Warning",
                    "Platform",
                    null,
                    cancellationToken: cancellationToken);
            }

            if (inactiveOrderCount > 0)
            {
                await CreateAdminAsync(
                    "BusinessesWithoutRecentOrdersSummary",
                    "Son 30 günde sipariş almayan işletmeler",
                    $"{inactiveOrderCount} aktif işletme son 30 günde sipariş almadı.",
                    "/Admin/Dashboard",
                    "Info",
                    "Platform",
                    null,
                    cancellationToken: cancellationToken);
            }
        }
        catch
        {
            // Fail-safe
        }
    }

    private async Task TryCreatePlanLimitAlertAsync(
        int businessId,
        string type,
        string resourceLabel,
        Areas.Business.Models.PlanUsageMetricViewModel metric,
        string actionUrl,
        CancellationToken cancellationToken)
    {
        if (metric.IsUnlimited)
        {
            return;
        }

        if (metric.IsLimitReached)
        {
            await CreateBusinessAsync(
                businessId,
                type,
                $"{resourceLabel} limiti doldu",
                $"{resourceLabel} kullanım limiti doldu ({metric.Used}/{metric.Limit}).",
                actionUrl,
                "Critical",
                "PlanLimit",
                businessId,
                cancellationToken: cancellationToken);
            return;
        }

        if (metric.IsNearLimit)
        {
            await CreateBusinessAsync(
                businessId,
                type,
                $"{resourceLabel} limitine yaklaşılıyor",
                $"{resourceLabel} kullanımı %{metric.UsagePercent} seviyesinde ({metric.Used}/{metric.Limit}).",
                actionUrl,
                "Warning",
                "PlanLimit",
                businessId,
                cancellationToken: cancellationToken);
        }
    }

    private static string? SerializeMetadata(object? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        try
        {
            if (metadata is IDictionary<string, object?> dict)
            {
                var sanitized = dict
                    .Where(kv => !SensitiveKeys.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                return JsonSerializer.Serialize(sanitized, JsonOptions);
            }

            return JsonSerializer.Serialize(metadata, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
