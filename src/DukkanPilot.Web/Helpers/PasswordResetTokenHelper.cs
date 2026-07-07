using System.Text.Json;
using DukkanPilot.Core.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace DukkanPilot.Web.Helpers;

public sealed class PasswordResetTokenHelper
{
    private const string ProtectorPurpose = "DukkanPilot.PasswordReset.v1";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    private readonly IDataProtector _protector;

    public PasswordResetTokenHelper(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    }

    public string GenerateToken(AppUser user)
    {
        var payload = new PasswordResetTokenPayload
        {
            UserId = user.Id,
            Email = user.Email,
            CreatedAtUtc = DateTime.UtcNow,
            PasswordHashFingerprint = user.PasswordHash ?? string.Empty
        };

        var json = JsonSerializer.Serialize(payload);
        return _protector.Protect(json);
    }

    public PasswordResetTokenValidationResult ValidateToken(string token, AppUser user, string normalizedEmail)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return PasswordResetTokenValidationResult.Invalid("Şifre sıfırlama bağlantısı geçersiz.");
        }

        PasswordResetTokenPayload payload;
        try
        {
            var json = _protector.Unprotect(token);
            payload = JsonSerializer.Deserialize<PasswordResetTokenPayload>(json)
                ?? throw new InvalidOperationException("Token payload is empty.");
        }
        catch
        {
            return PasswordResetTokenValidationResult.Invalid("Şifre sıfırlama bağlantısı geçersiz veya bozulmuş.");
        }

        if (!string.Equals(NormalizeEmail(payload.Email), normalizedEmail, StringComparison.Ordinal))
        {
            return PasswordResetTokenValidationResult.Invalid("Şifre sıfırlama bağlantısı geçersiz.");
        }

        if (payload.UserId != user.Id)
        {
            return PasswordResetTokenValidationResult.Invalid("Şifre sıfırlama bağlantısı geçersiz.");
        }

        if (payload.CreatedAtUtc.Add(TokenLifetime) < DateTime.UtcNow)
        {
            return PasswordResetTokenValidationResult.Invalid("Şifre sıfırlama bağlantısının süresi dolmuş. Lütfen yeni bir talep oluşturun.");
        }

        var currentFingerprint = user.PasswordHash ?? string.Empty;
        if (!string.Equals(payload.PasswordHashFingerprint, currentFingerprint, StringComparison.Ordinal))
        {
            return PasswordResetTokenValidationResult.Invalid("Bu şifre sıfırlama bağlantısı artık geçerli değil. Lütfen yeni bir talep oluşturun.");
        }

        return PasswordResetTokenValidationResult.Valid();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private sealed class PasswordResetTokenPayload
    {
        public int UserId { get; set; }

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public string PasswordHashFingerprint { get; set; } = string.Empty;
    }
}

public sealed class PasswordResetTokenValidationResult
{
    public bool IsValid { get; init; }

    public string? ErrorMessage { get; init; }

    public static PasswordResetTokenValidationResult Valid() => new() { IsValid = true };

    public static PasswordResetTokenValidationResult Invalid(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message
    };
}
