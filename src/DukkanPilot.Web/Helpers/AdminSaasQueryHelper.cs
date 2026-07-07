using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Areas.Business.Models;

namespace DukkanPilot.Web.Helpers;

public static class AdminSaasQueryHelper
{
    public static BusinessSubscription? GetLatestSubscription(IEnumerable<BusinessSubscription> subscriptions)
    {
        return subscriptions
            .OrderByDescending(s => s.StartDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefault();
    }

    public static bool IsSubscriptionValid(BusinessSubscription subscription, DateTime now)
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

    public static bool IsExpiringSoon(BusinessSubscription subscription, DateTime now, int days = 7)
    {
        if (!subscription.EndDate.HasValue)
        {
            return false;
        }

        if (!IsSubscriptionValid(subscription, now))
        {
            return false;
        }

        var daysRemaining = (subscription.EndDate.Value - now).TotalDays;
        return daysRemaining >= 0 && daysRemaining <= days;
    }

    public static bool IsExpiredSubscription(BusinessSubscription? subscription, DateTime now)
    {
        if (subscription is null)
        {
            return true;
        }

        if (subscription.Status == SubscriptionStatus.Expired)
        {
            return true;
        }

        if (subscription.EndDate.HasValue && subscription.EndDate.Value < now)
        {
            return true;
        }

        return !IsSubscriptionValid(subscription, now) &&
               subscription.Status is not SubscriptionStatus.Cancelled;
    }

    public static string GetStatusLabel(SubscriptionStatus status)
        => SubscriptionDisplayHelper.GetStatusLabel(status);

    public static string GetStatusBadgeClass(SubscriptionStatus status)
        => SubscriptionDisplayHelper.GetStatusBadgeClass(status);
}
