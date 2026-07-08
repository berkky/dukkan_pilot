using DukkanPilot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessSetting> BusinessSettings => Set<BusinessSetting>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<BusinessSubscription> BusinessSubscriptions => Set<BusinessSubscription>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<UserBusinessRole> UserBusinessRoles => Set<UserBusinessRole>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<LoyaltyRule> LoyaltyRules => Set<LoyaltyRule>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<QrCode> QrCodes => Set<QrCode>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SalesRequest> SalesRequests => Set<SalesRequest>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<BillingPayment> BillingPayments => Set<BillingPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureBusiness(modelBuilder);
        ConfigureBusinessSetting(modelBuilder);
        ConfigureSubscriptionPlan(modelBuilder);
        ConfigureBusinessSubscription(modelBuilder);
        ConfigureAppUser(modelBuilder);
        ConfigureUserBusinessRole(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderItem(modelBuilder);
        ConfigureLoyaltyRule(modelBuilder);
        ConfigureLoyaltyTransaction(modelBuilder);
        ConfigureReward(modelBuilder);
        ConfigureCampaign(modelBuilder);
        ConfigureQrCode(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureSalesRequest(modelBuilder);
        ConfigureBillingInvoice(modelBuilder);
        ConfigureBillingPayment(modelBuilder);
    }

    private static void ConfigureBusiness(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });
    }

    private static void ConfigureBusinessSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessSetting>(entity =>
        {
            entity.HasIndex(e => e.BusinessId).IsUnique();

            entity.Property(e => e.WhatsAppNumber).HasMaxLength(20);
            entity.Property(e => e.ThemeColor).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();

            entity.HasOne(e => e.Business)
                .WithOne(b => b.Setting)
                .HasForeignKey<BusinessSetting>(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSubscriptionPlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }

    private static void ConfigureBusinessSubscription(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessSubscription>(entity =>
        {
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Subscriptions)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SubscriptionPlan)
                .WithMany(p => p.BusinessSubscriptions)
                .HasForeignKey(e => e.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAppUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
        });
    }

    private static void ConfigureUserBusinessRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserBusinessRole>(entity =>
        {
            entity.HasIndex(e => new { e.AppUserId, e.BusinessId }).IsUnique();

            entity.HasOne(e => e.AppUser)
                .WithMany(u => u.BusinessRoles)
                .HasForeignKey(e => e.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.UserRoles)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => new { e.BusinessId, e.Name });

            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Categories)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => new { e.BusinessId, e.Name });

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.SizeOption).HasMaxLength(50);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Products)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => new { e.BusinessId, e.Phone });

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Customers)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.SubtotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.AppliedCampaignName).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(20);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Orders)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureOrderItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureBillingInvoice(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BillingInvoice>(entity =>
        {
            entity.Property(e => e.InvoiceNumber).HasMaxLength(60).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.PaymentStatus).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(40).IsRequired();
            entity.Property(e => e.AdminNotes).HasMaxLength(2000);
            entity.Property(e => e.BusinessVisibleNote).HasMaxLength(2000);
            entity.Property(e => e.OfficialInvoiceReference).HasMaxLength(200);
            entity.Property(e => e.CreatedByUserEmail).HasMaxLength(256);
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)");

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.BusinessId, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.BusinessId, e.Status });
            entity.HasIndex(e => new { e.BusinessId, e.PaymentStatus });
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.InvoiceNumber);
            entity.HasIndex(e => e.RelatedSalesRequestId);
        });
    }

    private static void ConfigureBillingPayment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BillingPayment>(entity =>
        {
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Method).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.PayerName).HasMaxLength(200);
            entity.Property(e => e.AdminNotes).HasMaxLength(2000);
            entity.Property(e => e.BusinessVisibleNote).HasMaxLength(2000);
            entity.Property(e => e.RecordedByUserEmail).HasMaxLength(256);
            entity.Property(e => e.MetadataJson).HasColumnType("nvarchar(max)");

            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.BusinessId, e.CreatedAtUtc });
            entity.HasIndex(e => e.BillingInvoiceId);
            entity.HasIndex(e => e.PaymentDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Method);
        });
    }

    private static void ConfigureLoyaltyRule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoyaltyRule>(entity =>
        {
            entity.Property(e => e.PointsPerAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinimumOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.LoyaltyRules)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureLoyaltyTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoyaltyTransaction>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.LoyaltyTransactions)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Reward)
                .WithMany(r => r.LoyaltyTransactions)
                .HasForeignKey(e => e.RewardId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureReward(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reward>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Rewards)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCampaign(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.DiscountType).HasConversion<int>();
            entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
            entity.Property(e => e.MinimumOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.MaximumDiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.IsPublicVisible).HasDefaultValue(true);
            entity.Property(e => e.IsAutoApply).HasDefaultValue(false);
            entity.Property(e => e.Priority).HasDefaultValue(0);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Campaigns)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureQrCode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QrCode>(entity =>
        {
            entity.Property(e => e.Label).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TargetUrl).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.Business)
                .WithMany(b => b.QrCodes)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.Property(e => e.Area).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500).IsRequired();
            entity.Property(e => e.EntityName).HasMaxLength(80);
            entity.Property(e => e.UserEmail).HasMaxLength(200);
            entity.Property(e => e.UserRole).HasMaxLength(50);
            entity.Property(e => e.Severity).HasMaxLength(20).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(400);
            entity.Property(e => e.MetadataJson).HasMaxLength(4000);

            entity.HasIndex(e => new { e.BusinessId, e.CreatedAtUtc });
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
        });
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.Property(e => e.Area).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.TargetRole).HasMaxLength(50);
            entity.Property(e => e.Severity).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ActionUrl).HasMaxLength(400);
            entity.Property(e => e.EntityName).HasMaxLength(80);
            entity.Property(e => e.MetadataJson).HasMaxLength(4000);

            entity.HasIndex(e => new { e.BusinessId, e.IsRead, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.Area, e.CreatedAtUtc });
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
        });
    }

    private static void ConfigureSalesRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesRequest>(entity =>
        {
            entity.ToTable("SalesRequests");

            entity.Property(e => e.Source).HasMaxLength(40).IsRequired();
            entity.Property(e => e.RequestType).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Priority).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ContactName).HasMaxLength(120);
            entity.Property(e => e.BusinessName).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(40);
            entity.Property(e => e.RequestedPlanName).HasMaxLength(100);
            entity.Property(e => e.CurrentPlanName).HasMaxLength(100);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.AdminNotes).HasMaxLength(2000);
            entity.Property(e => e.ClosedReason).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(400);
            entity.Property(e => e.MetadataJson).HasMaxLength(4000);

            entity.HasIndex(e => new { e.Status, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.BusinessId, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.Email, e.CreatedAtUtc });
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.RequestType);
            entity.HasIndex(e => e.RequestedPlanId);
            entity.HasIndex(e => e.Priority);
        });
    }
}
