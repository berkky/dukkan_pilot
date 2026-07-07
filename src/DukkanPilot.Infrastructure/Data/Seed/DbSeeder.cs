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
            Description = "DükkanPilot demo işletmesi",
            IsActive = true
        };

        var setting = new BusinessSetting
        {
            WhatsAppNumber = "905550000000",
            ThemeColor = "#0d6efd",
            Currency = "TRY"
        };

        business.Setting = setting;

        business.Subscriptions.Add(new BusinessSubscription
        {
            SubscriptionPlanId = starterPlan.Id,
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        });

        var categories = new List<Category>
        {
            new() { Name = "Kahveler", SortOrder = 1 },
            new() { Name = "Tatlılar", SortOrder = 2 },
            new() { Name = "Soğuk İçecekler", SortOrder = 3 }
        };

        foreach (var category in categories)
        {
            business.Categories.Add(category);
        }

        context.Businesses.Add(business);
        await context.SaveChangesAsync();

        var kahveler = business.Categories.First(c => c.Name == "Kahveler");
        var tatlilar = business.Categories.First(c => c.Name == "Tatlılar");
        var sogukIcecekler = business.Categories.First(c => c.Name == "Soğuk İçecekler");

        var products = new List<Product>
        {
            new()
            {
                BusinessId = business.Id,
                CategoryId = kahveler.Id,
                Name = "Latte",
                Description = "Espresso ve buğulu süt",
                Price = 85m,
                SortOrder = 1
            },
            new()
            {
                BusinessId = business.Id,
                CategoryId = kahveler.Id,
                Name = "Türk Kahvesi",
                Description = "Geleneksel Türk kahvesi",
                Price = 60m,
                SortOrder = 2
            },
            new()
            {
                BusinessId = business.Id,
                CategoryId = tatlilar.Id,
                Name = "Cheesecake",
                Description = "Ev yapımı cheesecake",
                Price = 120m,
                SortOrder = 1
            },
            new()
            {
                BusinessId = business.Id,
                CategoryId = sogukIcecekler.Id,
                Name = "Limonata",
                Description = "Taze sıkılmış limonata",
                Price = 70m,
                SortOrder = 1
            }
        };

        context.Products.AddRange(products);

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
            Name = "Ücretsiz Kahve",
            Description = "50 puan ile ücretsiz kahve",
            RequiredPoints = 50
        });

        context.Campaigns.Add(new Campaign
        {
            BusinessId = business.Id,
            Title = "Açılışa Özel %10 İndirim",
            Description = "Tüm kahvelerde geçerli açılış kampanyası",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        });

        await context.SaveChangesAsync();
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
