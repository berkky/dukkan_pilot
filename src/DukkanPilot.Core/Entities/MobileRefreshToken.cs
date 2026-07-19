namespace DukkanPilot.Core.Entities;

public class MobileRefreshToken
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public int BusinessId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? RevocationReason { get; set; }

    public AppUser AppUser { get; set; } = null!;
    public Business Business { get; set; } = null!;
}
