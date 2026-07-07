using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

public class BusinessSubscriptionStatusHelper
{
    private readonly AppDbContext _context;

    public BusinessSubscriptionStatusHelper(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BusinessSubscriptionStatusViewModel> GetStatusAsync(int businessId)
    {
        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business is null)
        {
            return CreateInvalidStatus("İşletme bulunamadı.");
        }

        if (!business.IsActive)
        {
            return CreateInvalidStatus("İşletme aktif değil.");
        }

        var now = DateTime.UtcNow;
        var subscriptions = await _context.BusinessSubscriptions
            .AsNoTracking()
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.BusinessId == businessId)
            .ToListAsync();

        var validSubscription = subscriptions
            .Where(s => IsSubscriptionValid(s, now))
            .OrderByDescending(s => s.Status == SubscriptionStatus.Active)
            .ThenByDescending(s => s.Status == SubscriptionStatus.Trial)
            .ThenByDescending(s => s.StartDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        if (validSubscription is not null)
        {
            return MapValidSubscription(validSubscription, now);
        }

        var latestSubscription = subscriptions
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        if (latestSubscription is null)
        {
            return CreateInvalidStatus("Aktif abonelik bulunamadı.");
        }

        return MapInvalidSubscription(latestSubscription, now);
    }

    public async Task<bool> HasValidSubscriptionAsync(int businessId)
    {
        var status = await GetStatusAsync(businessId);
        return status.HasValidSubscription;
    }

    private static bool IsSubscriptionValid(BusinessSubscription subscription, DateTime now)
    {
        if (!subscription.IsActive)
        {
            return false;
        }

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

    private static BusinessSubscriptionStatusViewModel MapValidSubscription(
        BusinessSubscription subscription,
        DateTime now)
    {
        return new BusinessSubscriptionStatusViewModel
        {
            HasValidSubscription = true,
            PlanName = subscription.SubscriptionPlan.Name,
            StatusText = SubscriptionDisplayHelper.GetStatusLabel(subscription.Status),
            StatusCssClass = SubscriptionDisplayHelper.GetStatusBadgeClass(subscription.Status),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            DaysRemaining = CalculateDaysRemaining(subscription.EndDate, now),
            IsTrial = subscription.Status == SubscriptionStatus.Trial,
            Message = subscription.Status == SubscriptionStatus.Trial
                ? "Deneme aboneliğiniz aktif."
                : "Aboneliğiniz aktif."
        };
    }

    private static BusinessSubscriptionStatusViewModel MapInvalidSubscription(
        BusinessSubscription subscription,
        DateTime now)
    {
        var message = subscription.Status switch
        {
            SubscriptionStatus.Expired => "Aboneliğinizin süresi dolmuş.",
            SubscriptionStatus.Cancelled => "Aboneliğiniz iptal edilmiş.",
            _ when subscription.EndDate.HasValue && subscription.EndDate.Value < now
                => "Aboneliğinizin süresi dolmuş.",
            _ => "Aktif abonelik bulunamadı."
        };

        return new BusinessSubscriptionStatusViewModel
        {
            HasValidSubscription = false,
            PlanName = subscription.SubscriptionPlan?.Name ?? "-",
            StatusText = SubscriptionDisplayHelper.GetStatusLabel(subscription.Status),
            StatusCssClass = SubscriptionDisplayHelper.GetStatusBadgeClass(subscription.Status),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            DaysRemaining = CalculateDaysRemaining(subscription.EndDate, now),
            IsTrial = subscription.Status == SubscriptionStatus.Trial,
            Message = message
        };
    }

    private static BusinessSubscriptionStatusViewModel CreateInvalidStatus(string message)
    {
        return new BusinessSubscriptionStatusViewModel
        {
            HasValidSubscription = false,
            PlanName = "-",
            StatusText = "Geçersiz",
            StatusCssClass = "bg-danger",
            Message = message
        };
    }

    private static int? CalculateDaysRemaining(DateTime? endDate, DateTime now)
    {
        if (!endDate.HasValue)
        {
            return null;
        }

        var days = (int)Math.Ceiling((endDate.Value - now).TotalDays);
        return days < 0 ? 0 : days;
    }
}
