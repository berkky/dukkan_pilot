namespace DukkanPilot.Web.Areas.Business.Models;

public static class CampaignDisplayHelper
{
    public static bool IsPublished(bool isActive, DateTime startDate, DateTime? endDate, DateTime? referenceUtc = null)
    {
        if (!isActive)
        {
            return false;
        }

        var now = referenceUtc ?? DateTime.UtcNow;

        if (startDate > now)
        {
            return false;
        }

        if (endDate.HasValue && endDate.Value < now)
        {
            return false;
        }

        return true;
    }

    public static string GetPublishedBadgeClass(bool isPublished) =>
        isPublished ? "bg-success" : "bg-secondary";

    public static string GetPublishedLabel(bool isPublished) =>
        isPublished ? "Yayında" : "Yayında Değil";
}
