using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace DukkanPilot.Web.Helpers;

public sealed class PublicOrderTrackingTokenHelper
{
    private const string ProtectorPurpose = "DukkanPilot.PublicOrderTracking.v1";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(48);

    private readonly IDataProtector _protector;

    public PublicOrderTrackingTokenHelper(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    }

    public string CreateToken(int orderId, int businessId, DateTime createdAtUtc)
    {
        var payload = new PublicOrderTrackingTokenPayload
        {
            OrderId = orderId,
            BusinessId = businessId,
            CreatedAtUtc = createdAtUtc
        };

        var json = JsonSerializer.Serialize(payload);
        var protectedToken = _protector.Protect(json);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedToken));
    }

    public PublicOrderTrackingTokenValidationResult TryValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return PublicOrderTrackingTokenValidationResult.Invalid("Sipariş takip bağlantısı geçersiz.");
        }

        PublicOrderTrackingTokenPayload payload;
        try
        {
            var protectedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var json = _protector.Unprotect(protectedToken);
            payload = JsonSerializer.Deserialize<PublicOrderTrackingTokenPayload>(json)
                ?? throw new InvalidOperationException("Token payload is empty.");
        }
        catch
        {
            return PublicOrderTrackingTokenValidationResult.Invalid("Sipariş takip bağlantısı geçersiz veya bozulmuş.");
        }

        if (payload.OrderId <= 0 || payload.BusinessId <= 0)
        {
            return PublicOrderTrackingTokenValidationResult.Invalid("Sipariş takip bağlantısı geçersiz.");
        }

        if (payload.CreatedAtUtc.Add(TokenLifetime) < DateTime.UtcNow)
        {
            return PublicOrderTrackingTokenValidationResult.Expired("Bu takip bağlantısının süresi dolmuş.");
        }

        return PublicOrderTrackingTokenValidationResult.Valid(payload);
    }
}

public sealed class PublicOrderTrackingTokenPayload
{
    public int OrderId { get; set; }

    public int BusinessId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PublicOrderTrackingTokenValidationResult
{
    public bool IsValid { get; init; }

    public bool IsExpired { get; init; }

    public string? ErrorMessage { get; init; }

    public PublicOrderTrackingTokenPayload? Payload { get; init; }

    public static PublicOrderTrackingTokenValidationResult Valid(PublicOrderTrackingTokenPayload payload) => new()
    {
        IsValid = true,
        Payload = payload
    };

    public static PublicOrderTrackingTokenValidationResult Invalid(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message
    };

    public static PublicOrderTrackingTokenValidationResult Expired(string message) => new()
    {
        IsValid = false,
        IsExpired = true,
        ErrorMessage = message
    };
}
