using DukkanPilot.Core.Entities;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Areas.Business.Models;
using DukkanPilot.Web.Helpers;
using DukkanPilot.Web.Models.PublicMenu;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Web.Helpers;

public class PublicOrderPricingHelper
{
    private readonly AppDbContext _context;

    public PublicOrderPricingHelper(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PublicOrderPricingResult> CalculateAsync(
        int businessId,
        IEnumerable<PlaceOrderItemRequest> requestedItems,
        string? rewardRequestName = null)
    {
        var result = new PublicOrderPricingResult();

        var businessExists = await _context.Businesses
            .AsNoTracking()
            .AnyAsync(b => b.Id == businessId && b.IsActive);

        if (!businessExists)
        {
            result.Errors.Add("İşletme bulunamadı veya aktif değil.");
            return result;
        }

        var groupedItems = requestedItems
            .Where(i => i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        if (groupedItems.Count == 0)
        {
            result.Errors.Add("Sepetiniz boş.");
            return result;
        }

        var productIds = groupedItems.Select(i => i.ProductId).ToList();

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p =>
                p.BusinessId == businessId &&
                p.IsActive &&
                p.Category.IsActive &&
                productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            result.Errors.Add("Sepetteki bazı ürünler artık mevcut değil veya menüden kaldırılmış.");
            return result;
        }

        foreach (var item in groupedItems)
        {
            var product = products[item.ProductId];
            var lineTotal = product.Price * item.Quantity;

            result.Items.Add(new PublicOrderPricingItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                LineTotal = lineTotal
            });

            result.Subtotal += lineTotal;
        }

        result.DiscountAmount = 0m;
        result.Total = result.Subtotal;

        var now = DateTime.UtcNow;
        var eligibleCampaigns = await _context.Campaigns
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId
                && c.IsActive
                && c.IsAutoApply
                && c.DiscountValue > 0
                && c.StartDate <= now
                && (c.EndDate == null || c.EndDate >= now))
            .ToListAsync();

        var bestCampaign = SelectBestCampaign(eligibleCampaigns, result.Subtotal);

        if (bestCampaign is not null)
        {
            var discount = CampaignDiscountHelper.CalculateDiscountAmount(bestCampaign, result.Subtotal);
            result.DiscountAmount = discount;
            result.Total = Math.Max(0m, result.Subtotal - discount);
            result.AppliedCampaignId = bestCampaign.Id;
            result.AppliedCampaignName = bestCampaign.Title;
            result.AppliedCampaignDescription = bestCampaign.Description;
            result.DiscountTypeText = CampaignDiscountHelper.GetDiscountTypeLabel(bestCampaign.DiscountType);
            result.CampaignMessage = CampaignDiscountHelper.BuildAppliedCampaignMessage(bestCampaign, discount);
        }

        var loyaltyRule = await GetActiveLoyaltyRuleAsync(businessId);
        if (loyaltyRule is not null && result.Total > 0)
        {
            if (result.Total >= loyaltyRule.MinimumOrderAmount)
            {
                var points = OrderLoyaltyHelper.CalculateEarnedPoints(result.Total, loyaltyRule.PointsPerAmount);
                if (points > 0)
                {
                    result.EarnedPointsPreview = points;
                    result.LoyaltyPreviewMessage =
                        $"Sipariş tamamlandığında yaklaşık {points} sadakat puanı kazanabilirsiniz.";
                }
            }
            else
            {
                result.LoyaltyPreviewMessage =
                    $"Sadakat puanı için minimum sipariş tutarı {loyaltyRule.MinimumOrderAmount:N0} ₺.";
            }
        }

        if (!string.IsNullOrWhiteSpace(rewardRequestName))
        {
            result.RewardRequestName = rewardRequestName.Trim();
        }

        result.IsValid = true;
        return result;
    }

    internal static Campaign? SelectBestCampaign(IEnumerable<Campaign> campaigns, decimal subtotal)
    {
        Campaign? best = null;
        decimal bestDiscount = 0m;

        foreach (var campaign in campaigns)
        {
            if (!CampaignDiscountHelper.MeetsMinimumOrder(campaign, subtotal))
            {
                continue;
            }

            var discount = CampaignDiscountHelper.CalculateDiscountAmount(campaign, subtotal);
            if (discount <= 0)
            {
                continue;
            }

            if (best is null
                || discount > bestDiscount
                || (discount == bestDiscount && campaign.Priority > best.Priority)
                || (discount == bestDiscount
                    && campaign.Priority == best.Priority
                    && campaign.EndDate.HasValue
                    && (!best.EndDate.HasValue || campaign.EndDate.Value < best.EndDate.Value))
                || (discount == bestDiscount
                    && campaign.Priority == best.Priority
                    && Nullable.Equals(campaign.EndDate, best.EndDate)
                    && campaign.Id < best.Id))
            {
                best = campaign;
                bestDiscount = discount;
            }
        }

        return best;
    }

    public async Task<LoyaltyRule?> GetActiveLoyaltyRuleAsync(int businessId)
    {
        return await _context.LoyaltyRules
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.IsActive && r.PointsPerAmount > 0)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
