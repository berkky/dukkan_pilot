using System.Globalization;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Web.Helpers;

public static class CampaignDiscountHelper
{
    public static bool IsWithinDateRange(Campaign campaign, DateTime referenceUtc)
    {
        if (!campaign.IsActive)
        {
            return false;
        }

        if (campaign.StartDate > referenceUtc)
        {
            return false;
        }

        if (campaign.EndDate.HasValue && campaign.EndDate.Value < referenceUtc)
        {
            return false;
        }

        return true;
    }

    public static bool MeetsMinimumOrder(Campaign campaign, decimal subtotal)
    {
        if (!campaign.MinimumOrderAmount.HasValue)
        {
            return true;
        }

        return subtotal >= campaign.MinimumOrderAmount.Value;
    }

    public static decimal CalculateDiscountAmount(Campaign campaign, decimal subtotal)
    {
        if (subtotal <= 0 || campaign.DiscountValue <= 0)
        {
            return 0m;
        }

        decimal discount = campaign.DiscountType switch
        {
            CampaignDiscountType.Percentage => subtotal * campaign.DiscountValue / 100m,
            CampaignDiscountType.FixedAmount => campaign.DiscountValue,
            _ => 0m
        };

        if (campaign.DiscountType == CampaignDiscountType.Percentage
            && campaign.MaximumDiscountAmount.HasValue
            && campaign.MaximumDiscountAmount.Value > 0)
        {
            discount = Math.Min(discount, campaign.MaximumDiscountAmount.Value);
        }

        discount = Math.Min(discount, subtotal);
        return Math.Max(0m, discount);
    }

    public static string GetDiscountTypeLabel(CampaignDiscountType discountType) =>
        discountType switch
        {
            CampaignDiscountType.Percentage => "Yüzde",
            CampaignDiscountType.FixedAmount => "Sabit Tutar",
            _ => discountType.ToString()
        };

    public static string GetDiscountValueText(CampaignDiscountType discountType, decimal discountValue)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");

        return discountType switch
        {
            CampaignDiscountType.Percentage => $"%{discountValue.ToString("0.##", culture)}",
            CampaignDiscountType.FixedAmount => $"{discountValue.ToString("N2", culture)} ₺",
            _ => discountValue.ToString("N2", culture)
        };
    }

    public static string GetCampaignBadgeText(CampaignDiscountType discountType, decimal discountValue)
    {
        if (discountValue <= 0)
        {
            return "Kampanya";
        }

        return discountType switch
        {
            CampaignDiscountType.Percentage => $"%{discountValue:0.##} indirim",
            CampaignDiscountType.FixedAmount => $"{discountValue:N0} ₺ indirim",
            _ => "İndirim"
        };
    }

    public static string? GetMinimumOrderText(decimal? minimumOrderAmount)
    {
        if (!minimumOrderAmount.HasValue || minimumOrderAmount.Value <= 0)
        {
            return null;
        }

        var culture = CultureInfo.GetCultureInfo("tr-TR");
        return $"{minimumOrderAmount.Value.ToString("N0", culture)} ₺ üzeri geçerli";
    }

    public static string BuildAppliedCampaignMessage(Campaign campaign, decimal discountAmount)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var discountText = discountAmount.ToString("N2", culture);

        return campaign.DiscountType switch
        {
            CampaignDiscountType.Percentage =>
                $"{campaign.Title} — %{campaign.DiscountValue:0.##} indirim uygulandı (-{discountText} ₺)",
            CampaignDiscountType.FixedAmount =>
                $"{campaign.Title} — {campaign.DiscountValue:N2} ₺ indirim uygulandı (-{discountText} ₺)",
            _ => $"{campaign.Title} — indirim uygulandı (-{discountText} ₺)"
        };
    }
}
