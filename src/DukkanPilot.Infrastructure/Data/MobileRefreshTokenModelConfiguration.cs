using DukkanPilot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.Infrastructure.Data;

internal static class MobileRefreshTokenModelConfiguration
{
    public static void ConfigureMobileRefreshToken(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MobileRefreshToken>(entity =>
        {
            entity.ToTable("MobileRefreshTokens");

            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.FamilyId).HasMaxLength(32).IsRequired();
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(64);
            entity.Property(e => e.RevocationReason).HasMaxLength(100);

            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.FamilyId, e.RevokedAtUtc });
            entity.HasIndex(e => new { e.AppUserId, e.BusinessId, e.ExpiresAtUtc });

            entity.HasOne(e => e.AppUser)
                .WithMany(u => u.MobileRefreshTokens)
                .HasForeignKey(e => e.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.MobileRefreshTokens)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
