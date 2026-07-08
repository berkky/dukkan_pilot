using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Infrastructure.Data.Seed;

public static class DbSeeder
{
    private const string DemoBusinessSlug = "demo-kafe";
    private static readonly string[] VerticalDemoSlugs =
    [
        "demo-kafe",
        "demo-tatlici",
        "demo-burgerci",
        "demo-restoran",
        "demo-nargile"
    ];
    private const string AdminEmail = "admin@dukkanpilot.local";
    private const string OwnerEmail = "owner@dukkanpilot.local";

    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedSubscriptionPlansAsync(context);
        await SeedDemoBusinessAsync(context);
        await EnrichDemoBusinessCatalogAsync(context);
        await EnsureVerticalDemoBusinessesAsync(context);
        await SeedDemoUsersAsync(context);
    }

    private static async Task EnsureVerticalDemoBusinessesAsync(AppDbContext context)
    {
        // Keep existing demo-kafe behavior. This method only ensures additional vertical demos exist and are enriched.
        var starterPlan = await context.SubscriptionPlans.FirstAsync(p => p.Name == "Starter");

        await EnsureDemoBusinessAsync(
            context,
            starterPlan,
            new DemoBusinessTemplate
            {
                Name = "Demo Tatlıcı",
                Slug = "demo-tatlici",
                Phone = "05550000001",
                Description = "Demo menü (tatlıcı): kampanya + sadakat + vitrin ürün akışı.",
                Address = "Demo Cad. Tatlı Sk. No:2",
                WhatsAppNumber = "905550000001",
                ThemeColor = "#b91c1c",
                Categories = new[]
                {
                    new DemoCategoryTemplate("Sütlü Tatlılar", 1),
                    new DemoCategoryTemplate("Şerbetli Tatlılar", 2),
                    new DemoCategoryTemplate("Pasta Dilimleri", 3),
                    new DemoCategoryTemplate("İçecekler", 4)
                },
                Products = new[]
                {
                    new DemoProductTemplate("Sütlü Tatlılar", "Magnolia", "Demo üründür.", 120m, 1),
                    new DemoProductTemplate("Sütlü Tatlılar", "Profiterol", "Demo üründür.", 110m, 2),
                    new DemoProductTemplate("Şerbetli Tatlılar", "Baklava", "Demo üründür.", 140m, 1),
                    new DemoProductTemplate("Sütlü Tatlılar", "Trileçe", "Demo üründür.", 130m, 3),
                    new DemoProductTemplate("Pasta Dilimleri", "Çilekli Pasta", "Demo üründür.", 160m, 1),
                    new DemoProductTemplate("İçecekler", "Limonata", "Demo üründür.", 70m, 1)
                },
                Reward = new DemoRewardTemplate("150 puana tatlı ikramı", "Demo ödül: puan karşılığı ikram.", 150),
                Campaign = new DemoCampaignTemplate("%12 sepet indirimi", "Sepette otomatik uygulanan demo kampanyası", CampaignDiscountType.Percentage, 12m, 200m),
                LoyaltyRule = new DemoLoyaltyRuleTemplate("Her 10 TL harcamada 1 puan kazanılır", 1m, 10m)
            });

        await EnsureDemoBusinessAsync(
            context,
            starterPlan,
            new DemoBusinessTemplate
            {
                Name = "Demo Burgerci",
                Slug = "demo-burgerci",
                Phone = "05550000002",
                Description = "Demo menü (burgerci): menü/combos + sepet artırma + kampanya.",
                Address = "Demo Cad. Burger Sk. No:3",
                WhatsAppNumber = "905550000002",
                ThemeColor = "#111827",
                Categories = new[]
                {
                    new DemoCategoryTemplate("Burgerler", 1),
                    new DemoCategoryTemplate("Menüler", 2),
                    new DemoCategoryTemplate("Yan Ürünler", 3),
                    new DemoCategoryTemplate("İçecekler", 4)
                },
                Products = new[]
                {
                    new DemoProductTemplate("Burgerler", "Classic Burger", "Demo üründür.", 190m, 1),
                    new DemoProductTemplate("Burgerler", "Double Burger", "Demo üründür.", 250m, 2),
                    new DemoProductTemplate("Burgerler", "Tavuk Burger", "Demo üründür.", 175m, 3),
                    new DemoProductTemplate("Menüler", "Combo Menü", "Demo üründür.", 320m, 1),
                    new DemoProductTemplate("Yan Ürünler", "Patates", "Demo üründür.", 85m, 1),
                    new DemoProductTemplate("İçecekler", "Kola", "Demo üründür.", 55m, 1)
                },
                Reward = new DemoRewardTemplate("200 puana patates + içecek", "Demo ödül: puan karşılığı ikram.", 200),
                Campaign = new DemoCampaignTemplate("Menülerde %15 avantaj", "Sepette otomatik uygulanan demo kampanyası", CampaignDiscountType.Percentage, 15m, 250m),
                LoyaltyRule = new DemoLoyaltyRuleTemplate("Her 10 TL harcamada 1 puan kazanılır", 1m, 10m)
            });

        await EnsureDemoBusinessAsync(
            context,
            starterPlan,
            new DemoBusinessTemplate
            {
                Name = "Demo Restoran",
                Slug = "demo-restoran",
                Phone = "05550000003",
                Description = "Demo menü (restoran): kategori akışı + kampanya + operasyon hikayesi.",
                Address = "Demo Cad. Restoran Sk. No:4",
                WhatsAppNumber = "905550000003",
                ThemeColor = "#1d4ed8",
                Categories = new[]
                {
                    new DemoCategoryTemplate("Başlangıçlar", 1),
                    new DemoCategoryTemplate("Ana Yemekler", 2),
                    new DemoCategoryTemplate("Salatalar", 3),
                    new DemoCategoryTemplate("İçecekler", 4)
                },
                Products = new[]
                {
                    new DemoProductTemplate("Başlangıçlar", "Mercimek Çorbası", "Demo üründür.", 85m, 1),
                    new DemoProductTemplate("Başlangıçlar", "Günün Meze Tabağı", "Demo üründür.", 140m, 2),
                    new DemoProductTemplate("Ana Yemekler", "Izgara Tavuk", "Demo üründür.", 260m, 1),
                    new DemoProductTemplate("Ana Yemekler", "Köfte", "Demo üründür.", 280m, 2),
                    new DemoProductTemplate("Ana Yemekler", "Makarna", "Demo üründür.", 210m, 3),
                    new DemoProductTemplate("Salatalar", "Çoban Salata", "Demo üründür.", 120m, 1)
                },
                Reward = new DemoRewardTemplate("250 puana başlangıç ikramı", "Demo ödül: puan karşılığı ikram.", 250),
                Campaign = new DemoCampaignTemplate("Akşam menüsünde %10", "Sepette otomatik uygulanan demo kampanyası", CampaignDiscountType.Percentage, 10m, 350m),
                LoyaltyRule = new DemoLoyaltyRuleTemplate("Her 10 TL harcamada 1 puan kazanılır", 1m, 10m)
            });

        await EnsureDemoBusinessAsync(
            context,
            starterPlan,
            new DemoBusinessTemplate
            {
                Name = "Demo Lounge",
                Slug = "demo-nargile",
                Phone = "05550000004",
                Description = "Demo menü (lounge): premium vitrin + hafta içi kampanya senaryosu. Demo içeriktir; işletme yerel mevzuata uymalıdır.",
                Address = "Demo Cad. Lounge Sk. No:5",
                WhatsAppNumber = "905550000004",
                ThemeColor = "#7c3aed",
                Categories = new[]
                {
                    new DemoCategoryTemplate("Lounge Menüsü", 1),
                    new DemoCategoryTemplate("Sıcak İçecekler", 2),
                    new DemoCategoryTemplate("Soğuk İçecekler", 3),
                    new DemoCategoryTemplate("Atıştırmalıklar", 4)
                },
                Products = new[]
                {
                    new DemoProductTemplate("Lounge Menüsü", "Fresh Mix", "Demo ürün adıdır.", 320m, 1),
                    new DemoProductTemplate("Lounge Menüsü", "Double Apple", "Demo ürün adıdır.", 300m, 2),
                    new DemoProductTemplate("Lounge Menüsü", "Ice Mint", "Demo ürün adıdır.", 310m, 3),
                    new DemoProductTemplate("Sıcak İçecekler", "Türk Kahvesi", "Demo üründür.", 70m, 1),
                    new DemoProductTemplate("Atıştırmalıklar", "Nachos", "Demo üründür.", 160m, 1)
                },
                Reward = new DemoRewardTemplate("300 puana içecek ikramı", "Demo ödül: puan karşılığı ikram.", 300),
                Campaign = new DemoCampaignTemplate("Hafta içi lounge indirimi", "Sepette otomatik uygulanan demo kampanyası", CampaignDiscountType.Percentage, 10m, 400m),
                LoyaltyRule = new DemoLoyaltyRuleTemplate("Her 10 TL harcamada 1 puan kazanılır", 1m, 10m)
            });
    }

    private static async Task EnsureDemoBusinessAsync(
        AppDbContext context,
        SubscriptionPlan starterPlan,
        DemoBusinessTemplate template)
    {
        var business = await context.Businesses
            .Include(b => b.Setting)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Slug == template.Slug);

        if (business is null)
        {
            business = new Business
            {
                Name = template.Name,
                Slug = template.Slug,
                Phone = template.Phone,
                Description = template.Description,
                Address = template.Address,
                IsActive = true
            };

            business.Setting = new BusinessSetting
            {
                WhatsAppNumber = template.WhatsAppNumber,
                ThemeColor = template.ThemeColor,
                Currency = "TRY"
            };

            business.Subscriptions.Add(new BusinessSubscription
            {
                SubscriptionPlanId = starterPlan.Id,
                StartDate = DateTime.UtcNow,
                Status = SubscriptionStatus.Active
            });

            foreach (var c in template.Categories)
            {
                business.Categories.Add(new Category
                {
                    Name = c.Name,
                    SortOrder = c.SortOrder,
                    IsActive = true
                });
            }

            context.Businesses.Add(business);
            await context.SaveChangesAsync();
        }
        else
        {
            business.IsActive = true;
            if (string.IsNullOrWhiteSpace(business.Name))
            {
                business.Name = template.Name;
            }

            if (string.IsNullOrWhiteSpace(business.Description))
            {
                business.Description = template.Description;
            }

            if (string.IsNullOrWhiteSpace(business.Address))
            {
                business.Address = template.Address;
            }

            if (business.Setting is null)
            {
                business.Setting = new BusinessSetting
                {
                    WhatsAppNumber = template.WhatsAppNumber,
                    ThemeColor = template.ThemeColor,
                    Currency = "TRY"
                };
            }
            else
            {
                if (string.IsNullOrWhiteSpace(business.Setting.WhatsAppNumber))
                {
                    business.Setting.WhatsAppNumber = template.WhatsAppNumber;
                }

                if (string.IsNullOrWhiteSpace(business.Setting.ThemeColor))
                {
                    business.Setting.ThemeColor = template.ThemeColor;
                }

                if (string.IsNullOrWhiteSpace(business.Setting.Currency))
                {
                    business.Setting.Currency = "TRY";
                }
            }

            foreach (var c in template.Categories)
            {
                if (!business.Categories.Any(x => x.Name == c.Name))
                {
                    context.Categories.Add(new Category
                    {
                        BusinessId = business.Id,
                        Name = c.Name,
                        SortOrder = c.SortOrder,
                        IsActive = true
                    });
                }
            }

            await context.SaveChangesAsync();
        }

        // Reload categories with IDs for product mapping
        business = await context.Businesses
            .Include(b => b.Categories)
            .FirstAsync(b => b.Id == business.Id);

        var existingProductNames = await context.Products
            .Where(p => p.BusinessId == business.Id)
            .Select(p => p.Name)
            .ToListAsync();

        var categoryByName = business.Categories.ToDictionary(c => c.Name, c => c.Id);

        var productsToAdd = template.Products
            .Where(p => categoryByName.ContainsKey(p.CategoryName))
            .Where(p => !existingProductNames.Contains(p.Name))
            .Select(p => new Product
            {
                BusinessId = business.Id,
                CategoryId = categoryByName[p.CategoryName],
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SortOrder = p.SortOrder,
                IsActive = true
            })
            .ToList();

        if (productsToAdd.Count > 0)
        {
            context.Products.AddRange(productsToAdd);
        }

        if (!await context.LoyaltyRules.AnyAsync(r => r.BusinessId == business.Id))
        {
            context.LoyaltyRules.Add(new LoyaltyRule
            {
                BusinessId = business.Id,
                PointsPerAmount = template.LoyaltyRule.PointsPerAmount,
                MinimumOrderAmount = template.LoyaltyRule.MinimumOrderAmount,
                Description = template.LoyaltyRule.Description
            });
        }

        if (!await context.Rewards.AnyAsync(r => r.BusinessId == business.Id && r.Name == template.Reward.Name))
        {
            context.Rewards.Add(new Reward
            {
                BusinessId = business.Id,
                Name = template.Reward.Name,
                Description = template.Reward.Description,
                RequiredPoints = template.Reward.RequiredPoints,
                IsActive = true
            });
        }

        var now = DateTime.UtcNow;
        var hasCampaign = await context.Campaigns.AnyAsync(c =>
            c.BusinessId == business.Id &&
            c.IsAutoApply &&
            c.DiscountType == template.Campaign.DiscountType &&
            c.DiscountValue == template.Campaign.DiscountValue &&
            c.MinimumOrderAmount == template.Campaign.MinimumOrderAmount);

        if (!hasCampaign)
        {
            context.Campaigns.Add(new Campaign
            {
                BusinessId = business.Id,
                Title = template.Campaign.Title,
                Description = template.Campaign.Description,
                StartDate = now,
                EndDate = now.AddMonths(1),
                DiscountType = template.Campaign.DiscountType,
                DiscountValue = template.Campaign.DiscountValue,
                MinimumOrderAmount = template.Campaign.MinimumOrderAmount,
                IsPublicVisible = true,
                IsAutoApply = true,
                Priority = 1,
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
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

    private sealed class DemoBusinessTemplate
    {
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string WhatsAppNumber { get; init; } = string.Empty;
        public string ThemeColor { get; init; } = string.Empty;
        public DemoCategoryTemplate[] Categories { get; init; } = Array.Empty<DemoCategoryTemplate>();
        public DemoProductTemplate[] Products { get; init; } = Array.Empty<DemoProductTemplate>();
        public DemoCampaignTemplate Campaign { get; init; } = new("", "", CampaignDiscountType.Percentage, 0m, 0m);
        public DemoRewardTemplate Reward { get; init; } = new("", "", 0);
        public DemoLoyaltyRuleTemplate LoyaltyRule { get; init; } = new("", 1m, 0m);
    }

    private sealed record DemoCategoryTemplate(string Name, int SortOrder);

    private sealed record DemoProductTemplate(string CategoryName, string Name, string Description, decimal Price, int SortOrder);

    private sealed record DemoCampaignTemplate(
        string Title,
        string Description,
        CampaignDiscountType DiscountType,
        decimal DiscountValue,
        decimal MinimumOrderAmount);

    private sealed record DemoRewardTemplate(string Name, string Description, int RequiredPoints);

    private sealed record DemoLoyaltyRuleTemplate(string Description, decimal PointsPerAmount, decimal MinimumOrderAmount);
}
