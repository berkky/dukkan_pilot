using DukkanPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.IntegrationTests.Infrastructure;

public sealed class SqliteTestAppDbContext : AppDbContext
{
    public SqliteTestAppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (string.Equals(property.GetColumnType(), "nvarchar(max)", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetColumnType(null);
                }
            }
        }
    }
}
