using BusinessEntity = DukkanPilot.Core.Entities.Business;
using DukkanPilot.Core.Entities;
using DukkanPilot.Web.Areas.Admin.Models;

namespace DukkanPilot.Web.Helpers;

public static class AdminBusinessHealthHelper
{
    public static AdminBusinessHealthViewModel Evaluate(AdminBusinessHealthInput input)
    {
        var risks = new List<AdminBusinessRiskItemViewModel>();
        var score = 0;

        if (input.IsActive)
        {
            score += 15;
        }
        else
        {
            risks.Add(CreateRisk("İşletme pasif", "bg-secondary"));
        }

        if (input.HasValidSubscription)
        {
            score += 20;
        }
        else if (!input.HasSubscription)
        {
            risks.Add(CreateRisk("Abonelik yok", "bg-danger"));
        }
        else if (input.IsExpiredSubscription)
        {
            risks.Add(CreateRisk("Abonelik süresi dolmuş", "bg-danger"));
        }

        if (input.IsExpiringSoon)
        {
            risks.Add(CreateRisk("Abonelik 7 gün içinde bitiyor", "bg-warning text-dark"));
        }

        if (!string.IsNullOrWhiteSpace(input.Phone) || !string.IsNullOrWhiteSpace(input.WhatsAppNumber))
        {
            score += 10;
        }
        else
        {
            risks.Add(CreateRisk("WhatsApp/telefon yok", "bg-warning text-dark"));
        }

        if (input.ActiveCategoryCount > 0)
        {
            score += 10;
        }
        else
        {
            risks.Add(CreateRisk("Aktif kategori yok", "bg-warning text-dark"));
        }

        if (input.ActiveProductCount > 0)
        {
            score += 15;
        }
        else
        {
            risks.Add(CreateRisk("Aktif ürün yok", "bg-warning text-dark"));
        }

        var last30Days = input.Now.AddDays(-30);
        if (input.LastOrderAt.HasValue && input.LastOrderAt.Value >= last30Days)
        {
            score += 15;
        }
        else
        {
            risks.Add(CreateRisk("Son 30 gün sipariş yok", "bg-warning text-dark"));
        }

        if (!string.IsNullOrWhiteSpace(input.LogoUrl) || !string.IsNullOrWhiteSpace(input.Description))
        {
            score += 5;
        }
        else
        {
            risks.Add(CreateRisk("Logo/açıklama eksik", "bg-secondary"));
        }

        var publicMenuReady = input.ActiveCategoryCount > 0 && input.ActiveProductCount > 0;
        if (publicMenuReady)
        {
            score += 10;
        }
        else
        {
            risks.Add(CreateRisk("Public menü eksik", "bg-info text-dark"));
        }

        score = Math.Clamp(score, 0, 100);

        var (label, badgeClass) = score switch
        {
            >= 80 => ("Sağlıklı", "bg-success"),
            >= 50 => ("Dikkat", "bg-warning text-dark"),
            _ => ("Riskli", "bg-danger")
        };

        return new AdminBusinessHealthViewModel
        {
            Score = score,
            Label = label,
            BadgeClass = badgeClass,
            Risks = risks
        };
    }

    public static AdminBusinessHealthInput CreateInput(
        BusinessEntity business,
        BusinessSubscription? latestSubscription,
        int activeCategoryCount,
        int activeProductCount,
        DateTime? lastOrderAt,
        DateTime now)
    {
        var hasSubscription = latestSubscription is not null;
        var hasValid = latestSubscription is not null
            && AdminSaasQueryHelper.IsSubscriptionValid(latestSubscription, now);
        var isExpired = AdminSaasQueryHelper.IsExpiredSubscription(latestSubscription, now);
        var isExpiring = latestSubscription is not null
            && AdminSaasQueryHelper.IsExpiringSoon(latestSubscription, now);

        return new AdminBusinessHealthInput
        {
            IsActive = business.IsActive,
            HasSubscription = hasSubscription,
            HasValidSubscription = hasValid,
            IsExpiredSubscription = isExpired,
            IsExpiringSoon = isExpiring,
            Phone = business.Phone,
            WhatsAppNumber = business.Setting?.WhatsAppNumber,
            ActiveCategoryCount = activeCategoryCount,
            ActiveProductCount = activeProductCount,
            LastOrderAt = lastOrderAt,
            LogoUrl = business.LogoUrl,
            Description = business.Description,
            Now = now
        };
    }

    private static AdminBusinessRiskItemViewModel CreateRisk(string reason, string badgeClass) => new()
    {
        Reason = reason,
        BadgeClass = badgeClass
    };
}

public class AdminBusinessHealthInput
{
    public bool IsActive { get; init; }

    public bool HasSubscription { get; init; }

    public bool HasValidSubscription { get; init; }

    public bool IsExpiredSubscription { get; init; }

    public bool IsExpiringSoon { get; init; }

    public string? Phone { get; init; }

    public string? WhatsAppNumber { get; init; }

    public int ActiveCategoryCount { get; init; }

    public int ActiveProductCount { get; init; }

    public DateTime? LastOrderAt { get; init; }

    public string? LogoUrl { get; init; }

    public string? Description { get; init; }

    public DateTime Now { get; init; }
}
