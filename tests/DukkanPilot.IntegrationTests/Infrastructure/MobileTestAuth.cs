using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DukkanPilot.Core.Enums;
using DukkanPilot.Web.Api.Mobile.V1.Authorization;
using DukkanPilot.Web.Api.Mobile.V1.Contracts.Auth;
using DukkanPilot.Web.Constants;
using Microsoft.IdentityModel.Tokens;

namespace DukkanPilot.IntegrationTests.Infrastructure;

public static class MobileTestAuth
{
    public const string Issuer = "DukkanPilot.IntegrationTests";
    public const string Audience = "DukkanPilot.Mobile.IntegrationTests";
    public const string SigningKey = "DukkanPilot-37A-Integration-Test-Signing-Key-2026!";

    public static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        int? businessId = null,
        string password = MobileTestDataSeeder.Password)
    {
        return client.PostAsJsonAsync("/api/mobile/v1/auth/login", new
        {
            email,
            password,
            businessId
        });
    }

    public static async Task<MobileAuthResponse> LoginSuccessAsync(
        HttpClient client,
        string email,
        int? businessId = null)
    {
        using var response = await LoginAsync(client, email, businessId);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MobileAuthResponse>();
        return result ?? throw new InvalidOperationException("Mobile login response was empty.");
    }

    public static void UseBearer(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static string CreateAccessToken(
        int userId,
        int businessId,
        BusinessRole businessRole,
        UserRole userRole,
        DateTime notBeforeUtc,
        DateTime expiresUtc,
        string? signingKey = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? SigningKey));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "Mobile Test User"),
            new Claim(ClaimTypes.Email, "mobile.test@dukkanpilot.test"),
            new Claim(ClaimTypes.Role, userRole.ToString()),
            new Claim(AuthClaimTypes.BusinessId, businessId.ToString()),
            new Claim(AuthClaimTypes.BusinessRole, businessRole.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(notBeforeUtc).ToString(), ClaimValueTypes.Integer64),
            new Claim(MobileAuthDefaults.ClientIdClaim, MobileAuthDefaults.ClientId)
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            notBeforeUtc,
            expiresUtc,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
    }
}
