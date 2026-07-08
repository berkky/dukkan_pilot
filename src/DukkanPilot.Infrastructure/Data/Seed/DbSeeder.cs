using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Infrastructure.Data.Seed;

public static class DbSeeder
{
    private const string DemoBusinessSlug = "demo-kafe";
    private const string AdminEmail = "admin@dukkanpilot.local";
    private const string OwnerEmail = "owner@dukkanpilot.local";

    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedSubscriptionPlansAsync(context);
        await SeedDemoBusinessAsync(context);
        await EnrichDemoBusinessCatalogAsync(context);
        await SeedDemoUsersAsync(context);
    }

    private static async Task SeedSubscriptionPlansAsync(AppDbContext context)
    {
        if (await context.SubscriptionPlans.AnyAsync())
        {
            return;
        }

        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                Name = "Free",
                Description = "Başlangıç için ücretsiz plan",
                MaxProducts = 20,
                MaxCampaigns = 1,
                Price = 0m,
                SortOrder = 1
            },
            new()
            {
                Name = "Starter",
                Description = "Küçük işletmeler için",
                MaxProducts = 100,
                MaxCampaigns = 5,
                Price = 299m,
                SortOrder = 2
            },
            new()
            {
                Name = "Pro",
                Description = "Büyüyen işletmeler için",
                MaxProducts = 500,
                MaxCampaigns = 20,
                Price = 599m,
                SortOrder = 3
            }
        };

        context.SubscriptionPlans.AddRange(plans);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDemoBusinessAsync(AppDbContext context)
    {
        if (await context.Businesses.AnyAsync(b => b.Slug == DemoBusinessSlug))
        {
            return;
        }

        var starterPlan = await context.SubscriptionPlans
            .FirstAsync(p => p.Name == "Starter");

        var business = new Business
        {
            Name = "Demo Kafe",
            Slug = DemoBusinessSlug,
            Phone = "05550000000",
            Description = "Demo satış menüsü: QR sipariş, kampanya indirimi ve sadakat akışını gösterin.",
            Address = "Demo Mah. Örnek Cad. No:1",
            IsActive = true
        };

        var setting = new BusinessSetting
        {
            WhatsAppNumber = "905550000000",
            ThemeColor = "#0f766e",
            Currency = "TRY"
        };

        business.Setting = setting;

        business.Subscriptions.Add(new BusinessSubscription
        {
            SubscriptionPlanId = starterPlan.Id,
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        });

        foreach (var category in BuildDemoCategories())
        {
            business.Categories.Add(category);
        }

        context.Businesses.Add(business);
        await context.SaveChangesAsync();

        context.Products.AddRange(BuildDemoProducts(business));

        context.LoyaltyRules.Add(new LoyaltyRule
        {
            BusinessId = business.Id,
            PointsPerAmount = 1m,
            MinimumOrderAmount = 10m,
            Description = "Her 10 TL harcamada 1 puan kazanılır"
        });

        context.Rewards.Add(new Reward
        {
            BusinessId = business.Id,
            Name = "100 Puana Ücretsiz Kahve",
            Description = "Sadakat puanlarınızla ücretsiz kahve kazanın",
            RequiredPoints = 100
        });

        context.Campaigns.Add(new Campaign
        {
            BusinessId = business.Id,
            Title = "100₺ üzeri %10 indirim",
            Description = "Sepette otomatik uygulanan demo kampanyası",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            DiscountType = CampaignDiscountType.Percentage,
            DiscountValue = 10m,
            MinimumOrderAmount = 100m,
            IsPublicVisible = true,
            IsAutoApply = true,
            Priority = 1,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Existing demo-kafe installs: add missing categories/products/campaign/reward without deleting data.
    /// </summary>
    private static async Task EnrichDemoBusinessCatalogAsync(AppDbContext context)
    {
        var business = await context.Businesses
            .Include(b => b.Setting)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Slug == DemoBusinessSlug);

        if (business is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(business.Description))
        {
            business.Description = "Demo satış menüsü: QR sipariş, kampanya indirimi ve sadakat akışını gösterin.";
        }

        if (string.IsNullOrWhiteSpace(business.Address))
        {
            business.Address = "Demo Mah. Örnek Cad. No:1";
        }

        if (business.Setting is not null)
        {
            if (string.IsNullOrWhiteSpace(business.Setting.WhatsAppNumber))
            {
                business.Setting.WhatsAppNumber = "905550000000";
            }

            if (string.IsNullOrWhiteSpace(business.Setting.ThemeColor))
            {
                business.Setting.ThemeColor = "#0f766e";
            }
        }

        foreach (var template in BuildDemoCategories())
        {
            if (!business.Categories.Any(c => c.Name == template.Name))
            {
                context.Categories.Add(new Category
                {
                    BusinessId = business.Id,
                    Name = template.Name,
                    SortOrder = template.SortOrder,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();

        business = await context.Businesses
            .Include(b => b.Categories)
            .FirstAsync(b => b.Id == business.Id);

        var existingProductNames = await context.Products
            .Where(p => p.BusinessId == business.Id)
            .Select(p => p.Name)
            .ToListAsync();

        var productsToAdd = BuildDemoProducts(business)
            .Where(p => !existingProductNames.Contains(p.Name))
            .ToList();

        if (productsToAdd.Count > 0)
        {
            context.Products.AddRange(productsToAdd);
        }

        if (!await context.Rewards.AnyAsync(r => r.BusinessId == business.Id && r.Name == "100 Puana Ücretsiz Kahve"))
        {
            context.Rewards.Add(new Reward
            {
                BusinessId = business.Id,
                Name = "100 Puana Ücretsiz Kahve",
                Description = "Sadakat puanlarınızla ücretsiz kahve kazanın",
                RequiredPoints = 100,
                IsActive = true
            });
        }

        if (!await context.Campaigns.AnyAsync(c =>
                c.BusinessId == business.Id &&
                c.IsAutoApply &&
                c.DiscountType == CampaignDiscountType.Percentage &&
                c.DiscountValue == 10m &&
                c.MinimumOrderAmount == 100m))
        {
            context.Campaigns.Add(new Campaign
            {
                BusinessId = business.Id,
                Title = "100₺ üzeri %10 indirim",
                Description = "Sepette otomatik uygulanan demo kampanyası",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                DiscountType = CampaignDiscountType.Percentage,
                DiscountValue = 10m,
                MinimumOrderAmount = 100m,
                IsPublicVisible = true,
                IsAutoApply = true,
                Priority = 1,
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
    }

    private static List<Category> BuildDemoCategories() =>
    [
        new() { Name = "Kahveler", SortOrder = 1, IsActive = true },
        new() { Name = "Tatlılar", SortOrder = 2, IsActive = true },
        new() { Name = "Soğuk İçecekler", SortOrder = 3, IsActive = true },
        new() { Name = "Atıştırmalıklar", SortOrder = 4, IsActive = true }
    ];

    private static List<Product> BuildDemoProducts(Business business)
    {
        var kahveler = business.Categories.First(c => c.Name == "Kahveler");
        var tatlilar = business.Categories.First(c => c.Name == "Tatlılar");
        var soguk = business.Categories.First(c => c.Name == "Soğuk İçecekler");
        var atistirmalik = business.Categories.First(c => c.Name == "Atıştırmalıklar");

        return
        [
            new() { BusinessId = business.Id, CategoryId = kahveler.Id, Name = "Latte", Description = "Espresso ve buğulu süt", Price = 85m, SortOrder = 1, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = kahveler.Id, Name = "Türk Kahvesi", Description = "Geleneksel Türk kahvesi", Price = 60m, SortOrder = 2, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = kahveler.Id, Name = "Americano", Description = "Uzun espresso", Price = 75m, SortOrder = 3, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = kahveler.Id, Name = "Filtre Kahve", Description = "Taze demlenmiş filtre", Price = 70m, SortOrder = 4, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = tatlilar.Id, Name = "Cheesecake", Description = "Ev yapımı cheesecake", Price = 120m, SortOrder = 1, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = tatlilar.Id, Name = "Brownie", Description = "Sıcak brownie", Price = 95m, SortOrder = 2, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = tatlilar.Id, Name = "Cookie", Description = "Çikolatalı kurabiye", Price = 55m, SortOrder = 3, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = soguk.Id, Name = "Limonata", Description = "Taze sıkılmış limonata", Price = 70m, SortOrder = 1, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = soguk.Id, Name = "Ice Latte", Description = "Buzlu latte", Price = 90m, SortOrder = 2, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = soguk.Id, Name = "Smoothie", Description = "Mevsim meyveli smoothie", Price = 110m, SortOrder = 3, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = atistirmalik.Id, Name = "Kruvasan", Description = "Tereyağlı kruvasan", Price = 65m, SortOrder = 1, IsActive = true },
            new() { BusinessId = business.Id, CategoryId = atistirmalik.Id, Name = "Tost", Description = "Kaşarlı tost", Price = 80m, SortOrder = 2, IsActive = true }
        ];
    }

    private static async Task SeedDemoUsersAsync(AppDbContext context)
    {
        if (!await context.AppUsers.AnyAsync(u => u.Email == AdminEmail))
        {
            context.AppUsers.Add(new AppUser
            {
                Email = AdminEmail,
                PasswordHash = PasswordHelper.HashPassword("Admin123!"),
                FullName = "Sistem Yöneticisi",
                Role = UserRole.SuperAdmin,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        AppUser? ownerUser = null;
        if (!await context.AppUsers.AnyAsync(u => u.Email == OwnerEmail))
        {
            ownerUser = new AppUser
            {
                Email = OwnerEmail,
                PasswordHash = PasswordHelper.HashPassword("Owner123!"),
                FullName = "Demo Kafe Sahibi",
                Role = UserRole.BusinessOwner,
                IsActive = true
            };
            context.AppUsers.Add(ownerUser);
            await context.SaveChangesAsync();
        }
        else
        {
            ownerUser = await context.AppUsers.FirstAsync(u => u.Email == OwnerEmail);
        }

        var business = await context.Businesses.FirstOrDefaultAsync(b => b.Slug == DemoBusinessSlug);
        if (business is null || ownerUser is null)
        {
            return;
        }

        var hasOwnerRole = await context.UserBusinessRoles.AnyAsync(r =>
            r.AppUserId == ownerUser.Id && r.BusinessId == business.Id);

        if (!hasOwnerRole)
        {
            context.UserBusinessRoles.Add(new UserBusinessRole
            {
                AppUserId = ownerUser.Id,
                BusinessId = business.Id,
                Role = BusinessRole.Owner,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }
    }
}
