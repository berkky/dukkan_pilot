using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using DukkanPilot.Core.Enums;
using DukkanPilot.IntegrationTests.Infrastructure;
using DukkanPilot.Web.Api.Mobile.V1.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DukkanPilot.IntegrationTests.MobileApi;

public sealed class MobileAuthTests
{
    [Theory]
    [InlineData("owner.a@dukkanpilot.test", BusinessRole.Owner)]
    [InlineData("staff.a@dukkanpilot.test", BusinessRole.Staff)]
    public async Task Owner_and_staff_login_returns_real_token_pair_with_required_claims(
        string email,
        BusinessRole expectedRole)
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        var login = await MobileTestAuth.LoginSuccessAsync(client, email, factory.Data.TenantAId);

        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        Assert.Equal(expectedRole.ToString(), login.Business.Role);
        Assert.True(login.AccessTokenExpiresAtUtc > DateTime.UtcNow);
        Assert.True(login.RefreshTokenExpiresAtUtc > login.AccessTokenExpiresAtUtc);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        Assert.Equal(MobileTestAuth.Issuer, jwt.Issuer);
        Assert.Contains(MobileTestAuth.Audience, jwt.Audiences);
        Assert.Equal(login.User.Id.ToString(), jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(login.User.FullName, jwt.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Equal(email, jwt.Claims.Single(claim => claim.Type == ClaimTypes.Email).Value);
        Assert.Equal(expectedRole.ToString(), jwt.Claims.Single(claim => claim.Type == "BusinessRole").Value);
        Assert.Equal(factory.Data.TenantAId.ToString(), jwt.Claims.Single(claim => claim.Type == "BusinessId").Value);
        Assert.Equal(MobileAuthDefaults.ClientId, jwt.Claims.Single(claim => claim.Type == "client_id").Value);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Iat);

        var stored = await factory.DbAsync(db => db.MobileRefreshTokens.AsNoTracking().SingleAsync(
            token => token.AppUserId == login.User.Id));
        Assert.NotEqual(login.RefreshToken, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_have_identical_problem_response()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        using var unknown = await MobileTestAuth.LoginAsync(client, "unknown@dukkanpilot.test");
        using var wrong = await MobileTestAuth.LoginAsync(
            client,
            "owner.a@dukkanpilot.test",
            factory.Data.TenantAId,
            "WrongPassword!123");

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(unknown.StatusCode, wrong.StatusCode);
        Assert.Equal("invalid_credentials", await ReadCodeAsync(unknown));
        Assert.Equal("invalid_credentials", await ReadCodeAsync(wrong));
        Assert.Equal(unknown.Content.Headers.ContentType?.MediaType, wrong.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_rejects_foreign_business_and_requires_selection_for_multiple_memberships()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        using var foreign = await MobileTestAuth.LoginAsync(
            client,
            "owner.a@dukkanpilot.test",
            factory.Data.TenantBId);
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Equal("invalid_business", await ReadCodeAsync(foreign));

        using var selection = await MobileTestAuth.LoginAsync(client, MobileTestDataSeeder.MultiBusinessEmail);
        Assert.Equal(HttpStatusCode.Conflict, selection.StatusCode);
        using var document = JsonDocument.Parse(await selection.Content.ReadAsStringAsync());
        Assert.Equal("business_selection_required", document.RootElement.GetProperty("code").GetString());
        var businesses = document.RootElement.GetProperty("businesses");
        Assert.Equal(2, businesses.GetArrayLength());
        Assert.Contains(businesses.EnumerateArray(), item => item.GetProperty("id").GetInt32() == factory.Data.TenantAId);
        Assert.Contains(businesses.EnumerateArray(), item => item.GetProperty("id").GetInt32() == factory.Data.TenantBId);
    }

    [Fact]
    public async Task Invalid_expired_cookie_and_admin_tokens_cannot_enter_mobile_api()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();

        using (var invalidClient = factory.CreateClient())
        {
            var token = MobileTestAuth.CreateAccessToken(
                factory.Data.TenantAOwnerUserId,
                factory.Data.TenantAId,
                BusinessRole.Owner,
                UserRole.BusinessOwner,
                DateTime.UtcNow.AddMinutes(-1),
                DateTime.UtcNow.AddMinutes(5),
                "A-Different-Integration-Signing-Key-With-Enough-Bytes!");
            MobileTestAuth.UseBearer(invalidClient, token);
            using var response = await invalidClient.GetAsync("/api/mobile/v1/bootstrap");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("unauthorized", await ReadCodeAsync(response));
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        using (var expiredClient = factory.CreateClient())
        {
            var token = MobileTestAuth.CreateAccessToken(
                factory.Data.TenantAOwnerUserId,
                factory.Data.TenantAId,
                BusinessRole.Owner,
                UserRole.BusinessOwner,
                DateTime.UtcNow.AddMinutes(-10),
                DateTime.UtcNow.AddMinutes(-5));
            MobileTestAuth.UseBearer(expiredClient, token);
            using var response = await expiredClient.GetAsync("/api/mobile/v1/bootstrap");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("unauthorized", await ReadCodeAsync(response));
        }

        using (var cookieClient = TestClaims.CreateClient(factory, TestUser.Admin))
        {
            using var response = await cookieClient.GetAsync("/api/mobile/v1/orders");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("unauthorized", await ReadCodeAsync(response));
        }

        using (var adminClient = factory.CreateClient())
        {
            var token = MobileTestAuth.CreateAccessToken(
                factory.Data.AdminUserId,
                factory.Data.TenantAId,
                BusinessRole.Owner,
                UserRole.SuperAdmin,
                DateTime.UtcNow.AddMinutes(-1),
                DateTime.UtcNow.AddMinutes(5));
            MobileTestAuth.UseBearer(adminClient, token);
            using var response = await adminClient.GetAsync("/api/mobile/v1/orders");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("forbidden", await ReadCodeAsync(response));
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Me_revalidates_membership_against_database()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var login = await MobileTestAuth.LoginSuccessAsync(
            client,
            "owner.a@dukkanpilot.test",
            factory.Data.TenantAId);
        MobileTestAuth.UseBearer(client, login.AccessToken);

        using var ok = await client.GetAsync("/api/mobile/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        await factory.DbAsync(async db =>
        {
            var membership = await db.UserBusinessRoles.SingleAsync(role =>
                role.AppUserId == login.User.Id && role.BusinessId == factory.Data.TenantAId);
            membership.IsActive = false;
            await db.SaveChangesAsync();
        });

        using var denied = await client.GetAsync("/api/mobile/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Equal("unauthorized", await ReadCodeAsync(denied));
    }

    [Fact]
    public async Task Login_rate_limit_returns_problem_details_and_retry_after()
    {
        using var factory = new DukkanPilotWebApplicationFactory();
        await factory.InitializeAsync();
        using var client = factory.CreateClient();

        HttpResponseMessage? finalResponse = null;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            finalResponse?.Dispose();
            finalResponse = await MobileTestAuth.LoginAsync(
                client,
                "rate-limit@dukkanpilot.test",
                password: "WrongPassword!123");
        }

        using (finalResponse)
        {
            Assert.NotNull(finalResponse);
            Assert.Equal(HttpStatusCode.TooManyRequests, finalResponse.StatusCode);
            Assert.Equal("rate_limit_exceeded", await ReadCodeAsync(finalResponse));
            Assert.True(finalResponse.Headers.Contains("Retry-After"));
            Assert.Equal("application/problem+json", finalResponse.Content.Headers.ContentType?.MediaType);
        }
    }

    internal static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("traceId", out _));
        return document.RootElement.GetProperty("code").GetString();
    }
}
