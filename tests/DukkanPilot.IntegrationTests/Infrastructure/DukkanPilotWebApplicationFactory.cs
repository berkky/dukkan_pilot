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
            s.AddAuthentication(o => { o.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme; o.DefaultChallengeScheme = TestAuthenticationHandler.Scheme; o.DefaultForbidScheme = TestAuthenticationHandler.Scheme; })
             .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.Scheme, _ => { });
            s.AddScoped<AppDbContext>(_ => new SqliteTestAppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options));
        });
    }
    public async Task InitializeAsync()
    {
        if (initialized) return;
        await connection.OpenAsync(); using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(); Data = await TestDataSeeder.SeedAsync(db); initialized = true;
    }
    public async Task<T> DbAsync<T>(Func<AppDbContext, Task<T>> work) { using var s = Services.CreateScope(); return await work(s.ServiceProvider.GetRequiredService<AppDbContext>()); }
    public async Task DbAsync(Func<AppDbContext, Task> work) { using var s = Services.CreateScope(); await work(s.ServiceProvider.GetRequiredService<AppDbContext>()); }
    protected override void Dispose(bool disposing) { if (disposing) connection.Dispose(); base.Dispose(disposing); }
}
