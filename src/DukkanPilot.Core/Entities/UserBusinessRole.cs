using DukkanPilot.Core.Common;
using DukkanPilot.Core.Enums;

namespace DukkanPilot.Core.Entities;

public class UserBusinessRole : BaseEntity
{
    public int AppUserId { get; set; }
    public int BusinessId { get; set; }
    public BusinessRole Role { get; set; } = BusinessRole.Staff;

    public AppUser AppUser { get; set; } = null!;
    public Business Business { get; set; } = null!;
}
