using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

public class BusinessPlanLimitHelper
{
    private const int UnlimitedLimit = -1;

    private readonly AppDbContext _context;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatusHelper;

    public BusinessPlanLimitHelper(
        AppDbContext context,
        BusinessSubscriptionStatusHelper subscriptionStatusHelper)
    {
        _context = context;
        _subscriptionStatusHelper = subscriptionStatusHelper;
    }

    public async Task<BusinessPlanUsageViewModel> GetUsageAsync(int businessId)
    {
        var subscriptionStatus = await _subscriptionStatusHelper.GetStatusAsync(businessId);
        if (!subscriptionStatus.HasValidSubscription)
        {
            return CreateEmptyUsage(subscriptionStatus.PlanName, false);
        }

        var plan = await GetActivePlanAsync(businessId);
        var limits = ResolveLimits(plan);

        var productsUsed = await _context.Products.CountAsync(p => p.BusinessId == businessId);
        var categoriesUsed = await _context.Categories.CountAsync(c => c.BusinessId == businessId);
        var staffUsersUsed = await _context.UserBusinessRoles.CountAsync(r =>
            r.BusinessId == businessId &&
            r.Role == BusinessRole.Staff &&
            r.IsActive &&
            r.AppUser.IsActive);
        var campaignsUsed = await _context.Campaigns.CountAsync(c => c.BusinessId == businessId);
        var rewardsUsed = await _context.Rewards.CountAsync(r => r.BusinessId == businessId);
        var qrCodesUsed = await _context.QrCodes.CountAsync(q => q.BusinessId == businessId);

        return new BusinessPlanUsageViewModel
        {
            PlanName = plan?.Name ?? subscriptionStatus.PlanName,
            HasValidSubscription = true,
            Products = CreateMetric("Ürünler", productsUsed, limits.Products),
            Categories = CreateMetric("Kategoriler", categoriesUsed, limits.Categories),
            StaffUsers = CreateMetric("Personel", staffUsersUsed, limits.StaffUsers),
            Campaigns = CreateMetric("Kampanyalar", campaignsUsed, limits.Campaigns),
            Rewards = CreateMetric("Ödüller", rewardsUsed, limits.Rewards),
            QrCodes = CreateMetric("QR Kodlar", qrCodesUsed, limits.QrCodes)
        };
    }

    public async Task<bool> IsLimitReachedAsync(int businessId, PlanLimitResource resource)
    {
        var usage = await GetUsageAsync(businessId);
        if (!usage.HasValidSubscription)
        {
            return true;
        }

        return GetMetric(usage, resource).IsLimitReached;
    }

    public AvailablePlanViewModel MapToAvailablePlan(SubscriptionPlan plan, int? currentPlanId)
    {
        var limits = ResolveLimits(plan);

        return new AvailablePlanViewModel
        {
            PlanId = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            IsCurrentPlan = currentPlanId.HasValue && plan.Id == currentPlanId.Value,
            IsActive = plan.IsActive,
            ProductLimitText = FormatLimitText(limits.Products),
            CategoryLimitText = FormatLimitText(limits.Categories),
            StaffLimitText = FormatLimitText(limits.StaffUsers),
            CampaignLimitText = FormatLimitText(limits.Campaigns),
            RewardLimitText = FormatLimitText(limits.Rewards),
            QrCodeLimitText = FormatLimitText(limits.QrCodes)
        };
    }

    public string GetLimitReachedMessage(PlanLimitResource resource, bool isBusinessOwner)
    {
        var resourceLabel = resource switch
        {
            PlanLimitResource.Products => "ürün",
            PlanLimitResource.Categories => "kategori",
            PlanLimitResource.StaffUsers => "personel",
            PlanLimitResource.Campaigns => "kampanya",
            PlanLimitResource.Rewards => "ödül",
            PlanLimitResource.QrCodes => "QR kod",
            _ => "kayıt"
        };

        if (isBusinessOwner)
        {
            return $"Plan limitinize ulaştınız. Daha fazla {resourceLabel} eklemek için planınızı yükseltmeniz gerekir.";
        }

        return $"Plan limitine ulaşıldı. Daha fazla {resourceLabel} eklemek için işletme sahibinizle iletişime geçin.";
    }

    private async Task<SubscriptionPlan?> GetActivePlanAsync(int businessId)
    {
        var now = DateTime.UtcNow;
        var subscription = await _context.BusinessSubscriptions
            .AsNoTracking()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.BusinessId == businessId && s.IsActive)
            .OrderByDescending(s => s.Status == SubscriptionStatus.Active)
            .ThenByDescending(s => s.Status == SubscriptionStatus.Trial)
            .ThenByDescending(s => s.StartDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription is null || !IsSubscriptionValid(subscription, now))
        {
            return null;
        }

        return subscription.SubscriptionPlan;
    }

    private static bool IsSubscriptionValid(BusinessSubscription subscription, DateTime now)
    {
        if (subscription.Status is SubscriptionStatus.Expired or SubscriptionStatus.Cancelled)
        {
            return false;
        }

        if (subscription.Status is not SubscriptionStatus.Active and not SubscriptionStatus.Trial)
        {
            return false;
        }

        if (subscription.StartDate > now)
        {
            return false;
        }

        if (subscription.EndDate.HasValue && subscription.EndDate.Value < now)
        {
            return false;
        }

        return true;
    }

    private static PlanLimits ResolveLimits(SubscriptionPlan? plan)
    {
        var fallback = GetFallbackLimits(plan?.Name);
        if (plan is null)
        {
            return fallback;
        }

        return new PlanLimits
        {
            Products = plan.MaxProducts > 0 ? plan.MaxProducts : fallback.Products,
            Categories = fallback.Categories,
            StaffUsers = fallback.StaffUsers,
            Campaigns = plan.MaxCampaigns > 0 ? plan.MaxCampaigns : fallback.Campaigns,
            Rewards = fallback.Rewards,
            QrCodes = fallback.QrCodes
        };
    }

    private static PlanLimits GetFallbackLimits(string? planName)
    {
        var normalized = (planName ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.Contains("enterprise"))
        {
            return UnlimitedLimits();
        }

        if (normalized.Contains("pro") || normalized.Contains("business"))
        {
            return new PlanLimits
            {
                Products = 500,
                Categories = 50,
                StaffUsers = 10,
                Campaigns = 25,
                Rewards = 50,
                QrCodes = 20
            };
        }

        if (normalized.Contains("starter"))
        {
            return new PlanLimits
            {
                Products = 100,
                Categories = 15,
                StaffUsers = 3,
                Campaigns = 5,
                Rewards = 10,
                QrCodes = 5
            };
        }

        return new PlanLimits
        {
            Products = 20,
            Categories = 5,
            StaffUsers = 1,
            Campaigns = 1,
            Rewards = 2,
            QrCodes = 1
        };
    }

    private static PlanLimits UnlimitedLimits() => new()
    {
        Products = UnlimitedLimit,
        Categories = UnlimitedLimit,
        StaffUsers = UnlimitedLimit,
        Campaigns = UnlimitedLimit,
        Rewards = UnlimitedLimit,
        QrCodes = UnlimitedLimit
    };

    private static string FormatLimitText(int limit) => limit < 0 ? "Limitsiz" : limit.ToString();

    private static PlanUsageMetricViewModel CreateMetric(string name, int used, int limit)
    {
        return new PlanUsageMetricViewModel
        {
            Name = name,
            Used = used,
            Limit = limit
        };
    }

    private static BusinessPlanUsageViewModel CreateEmptyUsage(string planName, bool hasValidSubscription)
    {
        return new BusinessPlanUsageViewModel
        {
            PlanName = planName,
            HasValidSubscription = hasValidSubscription
        };
    }

    private static PlanUsageMetricViewModel GetMetric(BusinessPlanUsageViewModel usage, PlanLimitResource resource)
        => resource switch
        {
            PlanLimitResource.Products => usage.Products,
            PlanLimitResource.Categories => usage.Categories,
            PlanLimitResource.StaffUsers => usage.StaffUsers,
            PlanLimitResource.Campaigns => usage.Campaigns,
            PlanLimitResource.Rewards => usage.Rewards,
            PlanLimitResource.QrCodes => usage.QrCodes,
            _ => usage.Products
        };

    private sealed class PlanLimits
    {
        public int Products { get; init; }

        public int Categories { get; init; }

        public int StaffUsers { get; init; }

        public int Campaigns { get; init; }

        public int Rewards { get; init; }

        public int QrCodes { get; init; }
    }
}
