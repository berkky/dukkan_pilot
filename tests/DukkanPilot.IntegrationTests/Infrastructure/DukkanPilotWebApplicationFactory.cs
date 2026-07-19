using DukkanPilot.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DukkanPilot.IntegrationTests.Infrastructure;

public sealed class DukkanPilotWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private bool initialized;
    public TestFixtureData Data { get; private set; } = null!;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<AppDbContext>>(); s.RemoveAll<AppDbContext>();
            s.AddSingleton(connection);
            s.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            s.AddAuthentication(o => { o.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName; o.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName; o.DefaultForbidScheme = TestAuthenticationHandler.SchemeName; })
             .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
            s.AddScoped<AppDbContext>(_ => new SqliteTestAppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options));
        });
    }
    public async Task InitializeAsync()
    {
        if (initialized) return;
        await connection.OpenAsync(); await using(var pragma=connection.CreateCommand()){pragma.CommandText="PRAGMA foreign_keys = ON;";await pragma.ExecuteNonQueryAsync();pragma.CommandText="PRAGMA foreign_keys;";if(Convert.ToInt64(await pragma.ExecuteScalarAsync())!=1)throw new InvalidOperationException("SQLite foreign keys must be enabled for integration tests.");}using var scope=Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();if(db.Database.ProviderName!="Microsoft.EntityFrameworkCore.Sqlite")throw new InvalidOperationException("Integration tests must use SQLite.");await using(var schema=connection.CreateCommand()){schema.CommandText="SELECT sql FROM sqlite_master WHERE type = 'table' AND name IN ('BusinessTables', 'Orders');";await using var reader=await schema.ExecuteReaderAsync();var definitions=new List<string>();while(await reader.ReadAsync())definitions.Add(reader.GetString(0));if(definitions.Count!=2||definitions.Any(sql=>sql.Contains("nvarchar(max)",StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("SQLite test schema was not created with the test-only type adaptation.");}Data=await TestDataSeeder.SeedAsync(db);initialized=true;
    }
    public async Task<T> DbAsync<T>(Func<AppDbContext, Task<T>> work) { using var s = Services.CreateScope(); return await work(s.ServiceProvider.GetRequiredService<AppDbContext>()); }
    public async Task DbAsync(Func<AppDbContext, Task> work) { using var s = Services.CreateScope(); await work(s.ServiceProvider.GetRequiredService<AppDbContext>()); }
    protected override void Dispose(bool disposing) { if (disposing) connection.Dispose(); base.Dispose(disposing); }
}
