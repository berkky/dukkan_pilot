using DukkanPilot.Core.Common;

namespace DukkanPilot.Core.Entities;

public class Business : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }

    public BusinessSetting? Setting { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    public ICollection<LoyaltyRule> LoyaltyRules { get; set; } = new List<LoyaltyRule>();
    public ICollection<Reward> Rewards { get; set; } = new List<Reward>();
    public ICollection<BusinessSubscription> Subscriptions { get; set; } = new List<BusinessSubscription>();
    public ICollection<UserBusinessRole> UserRoles { get; set; } = new List<UserBusinessRole>();
    public ICollection<QrCode> QrCodes { get; set; } = new List<QrCode>();
}
