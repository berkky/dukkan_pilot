using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Infrastructure.Data.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace DukkanPilot.IntegrationTests.Seeding;
public sealed class DbSeederIdempotencyTests
{
 [Fact] public async Task SeedAsync_is_idempotent(){await using var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();await using var db=new SqliteTestAppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();await DbSeeder.SeedAsync(db);var a=await Counts(db);await DbSeeder.SeedAsync(db);Assert.Equal(a,await Counts(db));foreach(var slug in new[]{"demo-kafe","demo-restoran","demo-tatlici","demo-burgerci","demo-nargile"})Assert.Equal(1,await db.Businesses.CountAsync(x=>x.Slug==slug));Assert.Equal(1,await db.AppUsers.CountAsync(x=>x.Email=="admin@dukkanpilot.local"));Assert.Equal(1,await db.AppUsers.CountAsync(x=>x.Email=="owner@dukkanpilot.local"));Assert.Empty(await db.BusinessTables.GroupBy(x=>new{x.BusinessId,x.PublicCode}).Where(x=>x.Count()>1).ToListAsync());}
 static async Task<int[]> Counts(AppDbContext d)=>new[]{await d.SubscriptionPlans.CountAsync(),await d.Businesses.CountAsync(),await d.AppUsers.CountAsync(),await d.UserBusinessRoles.CountAsync(),await d.Categories.CountAsync(),await d.Products.CountAsync(),await d.Campaigns.CountAsync(),await d.Rewards.CountAsync(),await d.BusinessTables.CountAsync(),await d.BusinessSubscriptions.CountAsync(),await d.BusinessSettings.CountAsync()};
}
