using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.IntegrationTests.Infrastructure;

public static class MobileTestDataSeeder
{
    public const string Password = "MobileTest!123";
    public const string MultiBusinessEmail = "multi.mobile@dukkanpilot.test";

    public static async Task SeedAsync(AppDbContext db, TestFixtureData data)
    {
        var mobileUsers = await db.AppUsers
            .Where(user => user.Id == data.TenantAOwnerUserId ||
                           user.Id == data.TenantAStaffUserId ||
                           user.Id == data.TenantBOwnerUserId ||
                           user.Id == data.TenantBStaffUserId)
            .ToListAsync();

        foreach (var user in mobileUsers)
        {
            user.PasswordHash = PasswordHelper.HashPassword(Password);
        }

        var multiBusinessUser = new AppUser
        {
            Email = MultiBusinessEmail,
            FullName = "Multi Business Owner",
            Role = UserRole.BusinessOwner,
            PasswordHash = PasswordHelper.HashPassword(Password)
        };
        db.AppUsers.Add(multiBusinessUser);
        await db.SaveChangesAsync();

        db.UserBusinessRoles.AddRange(
            new UserBusinessRole
            {
                AppUserId = multiBusinessUser.Id,
                BusinessId = data.TenantAId,
                Role = BusinessRole.Owner
            },
            new UserBusinessRole
            {
                AppUserId = multiBusinessUser.Id,
                BusinessId = data.TenantBId,
                Role = BusinessRole.Owner
            });

        await db.SaveChangesAsync();
    }
}
