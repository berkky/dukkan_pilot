using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DukkanPilot.Core.Entities;
using DukkanPilot.Core.Enums;
using DukkanPilot.Infrastructure.Data;
using DukkanPilot.Web.Api.Mobile.V1.Authorization;
using DukkanPilot.Web.Api.Mobile.V1.Configuration;
using DukkanPilot.Web.Constants;
using DukkanPilot.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DukkanPilot.Web.Api.Mobile.V1.Services;

public sealed class MobileTokenService : IMobileTokenService
{
    private readonly AppDbContext _context;
    private readonly MobileAuthOptions _options;
    private readonly BusinessSubscriptionStatusHelper _subscriptionStatus;

    public MobileTokenService(
        AppDbContext context,
        IOptions<MobileAuthOptions> options,
        BusinessSubscriptionStatusHelper subscriptionStatus)
    {
        _context = context;
        _options = options.Value;
        _subscriptionStatus = subscriptionStatus;
    }

    public async Task<MobileTokenPair> IssueAsync(
        AppUser user,
        Business business,
        BusinessRole businessRole,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rawRefreshToken = GenerateRefreshToken();
        var refreshExpiresAtUtc = now.AddDays(_options.RefreshTokenDays);

        _context.MobileRefreshTokens.Add(new MobileRefreshToken
        {
            AppUserId = user.Id,
            BusinessId = business.Id,
            TokenHash = HashRefreshToken(rawRefreshToken),
            FamilyId = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpiresAtUtc
        });

        await _context.SaveChangesAsync(cancellationToken);
        return CreatePair(user, business, businessRole, rawRefreshToken, refreshExpiresAtUtc, now);
    }

    public async Task<MobileRefreshResult> RefreshAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return new MobileRefreshResult(MobileRefreshFailure.Invalid);
        }

        var tokenHash = HashRefreshToken(rawRefreshToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var storedToken = await _context.MobileRefreshTokens
            .Include(t => t.AppUser)
            .Include(t => t.Business)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new MobileRefreshResult(MobileRefreshFailure.Invalid);
        }

        var now = DateTime.UtcNow;
        if (storedToken.RevokedAtUtc.HasValue)
        {
            await RevokeFamilyAsync(storedToken.FamilyId, now, "ReuseDetected", cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new MobileRefreshResult(MobileRefreshFailure.Reused);
        }

        if (storedToken.ExpiresAtUtc <= now)
        {
            storedToken.RevokedAtUtc = now;
            storedToken.RevocationReason = "Expired";
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new MobileRefreshResult(MobileRefreshFailure.Expired);
        }

        var membership = await _context.UserBusinessRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                role => role.AppUserId == storedToken.AppUserId &&
                        role.BusinessId == storedToken.BusinessId &&
                        role.IsActive,
                cancellationToken);

        if (!IsMobileAccessValid(storedToken, membership) ||
            !await _subscriptionStatus.HasValidSubscriptionAsync(storedToken.BusinessId))
        {
            storedToken.RevokedAtUtc = now;
            storedToken.RevocationReason = "AccessRevoked";
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new MobileRefreshResult(MobileRefreshFailure.AccessDenied);
        }

        var newRawRefreshToken = GenerateRefreshToken();
        var newTokenHash = HashRefreshToken(newRawRefreshToken);
        var refreshExpiresAtUtc = now.AddDays(_options.RefreshTokenDays);

        storedToken.RevokedAtUtc = now;
        storedToken.RevocationReason = "Rotated";
        storedToken.ReplacedByTokenHash = newTokenHash;

        _context.MobileRefreshTokens.Add(new MobileRefreshToken
        {
            AppUserId = storedToken.AppUserId,
            BusinessId = storedToken.BusinessId,
            TokenHash = newTokenHash,
            FamilyId = storedToken.FamilyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpiresAtUtc
        });

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var pair = CreatePair(
            storedToken.AppUser,
            storedToken.Business,
            membership!.Role,
            newRawRefreshToken,
            refreshExpiresAtUtc,
            now);

        return new MobileRefreshResult(
            MobileRefreshFailure.None,
            pair,
            storedToken.AppUser,
            storedToken.Business,
            membership.Role);
    }

    public async Task LogoutAsync(
        string rawRefreshToken,
        int userId,
        int businessId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var tokenHash = HashRefreshToken(rawRefreshToken);
        var token = await _context.MobileRefreshTokens.SingleOrDefaultAsync(
            candidate => candidate.TokenHash == tokenHash &&
                         candidate.AppUserId == userId &&
                         candidate.BusinessId == businessId,
            cancellationToken);

        if (token is null || token.RevokedAtUtc.HasValue)
        {
            return;
        }

        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevocationReason = "Logout";
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> LogoutAllAsync(
        int userId,
        int businessId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tokens = await _context.MobileRefreshTokens
            .Where(token => token.AppUserId == userId &&
                            token.BusinessId == businessId &&
                            token.RevokedAtUtc == null &&
                            token.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
            token.RevocationReason = "LogoutAll";
        }

        if (tokens.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return tokens.Count;
    }

    private MobileTokenPair CreatePair(
        AppUser user,
        Business business,
        BusinessRole businessRole,
        string rawRefreshToken,
        DateTime refreshExpiresAtUtc,
        DateTime now)
    {
        var accessExpiresAtUtc = now.AddMinutes(_options.AccessTokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(AuthClaimTypes.BusinessId, business.Id.ToString()),
            new(AuthClaimTypes.BusinessRole, businessRole.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
            new(MobileAuthDefaults.ClientIdClaim, MobileAuthDefaults.ClientId)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpiresAtUtc,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new MobileTokenPair(
            new JwtSecurityTokenHandler().WriteToken(token),
            rawRefreshToken,
            accessExpiresAtUtc,
            refreshExpiresAtUtc);
    }

    private async Task RevokeFamilyAsync(
        string familyId,
        DateTime revokedAtUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        var familyTokens = await _context.MobileRefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in familyTokens)
        {
            token.RevokedAtUtc = revokedAtUtc;
            token.RevocationReason = reason;
        }
    }

    private static bool IsMobileAccessValid(
        MobileRefreshToken token,
        UserBusinessRole? membership)
    {
        return token.AppUser.IsActive &&
               token.Business.IsActive &&
               token.AppUser.Role is UserRole.BusinessOwner or UserRole.Staff &&
               membership?.Role is BusinessRole.Owner or BusinessRole.Staff;
    }

    internal static string HashRefreshToken(string rawRefreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));
    }

    private static string GenerateRefreshToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
    }
}
