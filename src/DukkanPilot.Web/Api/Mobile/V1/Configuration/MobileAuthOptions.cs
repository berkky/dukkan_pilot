using System.Text;
using Microsoft.Extensions.Options;

namespace DukkanPilot.Web.Api.Mobile.V1.Configuration;

public sealed class MobileAuthOptions
{
    public const string SectionName = "MobileAuth";

    public string Issuer { get; set; } = "DukkanPilot";
    public string Audience { get; set; } = "DukkanPilot.Mobile";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}

public sealed class MobileAuthOptionsValidator : IValidateOptions<MobileAuthOptions>
{
    private const int MinimumSigningKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, MobileAuthOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("MobileAuth:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("MobileAuth:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey) ||
            Encoding.UTF8.GetByteCount(options.SigningKey) < MinimumSigningKeyBytes)
        {
            failures.Add($"MobileAuth:SigningKey must contain at least {MinimumSigningKeyBytes} UTF-8 bytes and must be supplied through user-secrets or an environment variable.");
        }

        if (options.AccessTokenMinutes is < 1 or > 60)
        {
            failures.Add("MobileAuth:AccessTokenMinutes must be between 1 and 60.");
        }

        if (options.RefreshTokenDays is < 1 or > 90)
        {
            failures.Add("MobileAuth:RefreshTokenDays must be between 1 and 90.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
