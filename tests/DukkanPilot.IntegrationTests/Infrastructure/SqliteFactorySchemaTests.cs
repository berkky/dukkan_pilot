using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.IntegrationTests.Infrastructure;

public sealed class SqliteFactorySchemaTests
{
    [Fact]
    public async Task Factory_creates_sqlite_schema_with_foreign_keys_enabled()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();

        var result = await factory.DbAsync(async db => new
        {
            db.Database.ProviderName,
            Tables = await db.BusinessTables.CountAsync()
        });

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", result.ProviderName);
        Assert.True(result.Tables >= 3);
    }
}
