using System.Net;
using System.Net.Http.Json;
using DukkanPilot.IntegrationTests.Infrastructure;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Auth;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.IntegrationTests.MobileApi;

public sealed class MobileRefreshTokenTests
{
    [Fact]
    public async Task Refresh_rotates_token_and_reuse_revokes_the_family()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var login = await MobileTestAuth.LoginSuccessAsync(
            client,
            "owner.a@dukkanpilot.test",
            factory.Data.TenantAId);

        using var refreshResponse = await client.PostAsJsonAsync(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<MobileAuthResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);
        Assert.NotEqual(login.AccessToken, rotated.AccessToken);

        var afterRotation = await factory.DbAsync(db => db.MobileRefreshTokens
            .AsNoTracking()
            .Where(token => token.AppUserId == login.User.Id && token.BusinessId == factory.Data.TenantAId)
            .OrderBy(token => token.CreatedAtUtc)
            .ToListAsync());
        Assert.Equal(2, afterRotation.Count);
        Assert.Equal("Rotated", afterRotation[0].RevocationReason);
        Assert.NotNull(afterRotation[0].ReplacedByTokenHash);
        Assert.Null(afterRotation[1].RevokedAtUtc);

        using var reused = await client.PostAsJsonAsync(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        Assert.Equal("refresh_token_reused", await MobileAuthTests.ReadCodeAsync(reused));

        var activeFamilyCount = await factory.DbAsync(db => db.MobileRefreshTokens.CountAsync(token =>
            token.AppUserId == login.User.Id &&
            token.BusinessId == factory.Data.TenantAId &&
            token.RevokedAtUtc == null));
        Assert.Equal(0, activeFamilyCount);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var login = await MobileTestAuth.LoginSuccessAsync(
            client,
            "owner.a@dukkanpilot.test",
            factory.Data.TenantAId);

        await factory.DbAsync(async db =>
        {
            var token = await db.MobileRefreshTokens.SingleAsync(item => item.AppUserId == login.User.Id);
            token.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });

        using var response = await client.PostAsJsonAsync(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("refresh_token_expired", await MobileAuthTests.ReadCodeAsync(response));
    }

    [Fact]
    public async Task Logout_is_idempotent_and_revokes_the_matching_token()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var login = await MobileTestAuth.LoginSuccessAsync(
            client,
            "staff.a@dukkanpilot.test",
            factory.Data.TenantAId);
        MobileTestAuth.UseBearer(client, login.AccessToken);

        using var first = await client.PostAsJsonAsync(
            "/api/mobile/v1/auth/logout",
            new { refreshToken = login.RefreshToken });
        using var second = await client.PostAsJsonAsync(
            "/api/mobile/v1/auth/logout",
            new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var token = await factory.DbAsync(db => db.MobileRefreshTokens.AsNoTracking().SingleAsync(
            item => item.AppUserId == login.User.Id));
        Assert.NotNull(token.RevokedAtUtc);
        Assert.Equal("Logout", token.RevocationReason);
    }

    [Fact]
    public async Task Logout_all_revokes_all_active_tokens_for_user_and_business_only()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var first = await MobileTestAuth.LoginSuccessAsync(
            client,
            "owner.a@dukkanpilot.test",
            factory.Data.TenantAId);
        var second = await MobileTestAuth.LoginSuccessAsync(
            client,
            "owner.a@dukkanpilot.test",
            factory.Data.TenantAId);
        var otherTenant = await MobileTestAuth.LoginSuccessAsync(
            client,
            "owner.b@dukkanpilot.test",
            factory.Data.TenantBId);
        MobileTestAuth.UseBearer(client, second.AccessToken);

        using var response = await client.PostAsync("/api/mobile/v1/auth/logout-all", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var tenantAActive = await factory.DbAsync(db => db.MobileRefreshTokens.CountAsync(token =>
            token.AppUserId == first.User.Id &&
            token.BusinessId == factory.Data.TenantAId &&
            token.RevokedAtUtc == null));
        var tenantBActive = await factory.DbAsync(db => db.MobileRefreshTokens.CountAsync(token =>
            token.AppUserId == otherTenant.User.Id &&
            token.BusinessId == factory.Data.TenantBId &&
            token.RevokedAtUtc == null));
        Assert.Equal(0, tenantAActive);
        Assert.Equal(1, tenantBActive);
    }
}
