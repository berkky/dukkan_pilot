using DukkanPilot.Web.Api.Mobile.V1.Contracts.Auth;

namespace DukkanPilot.Web.Api.Mobile.V1.Contracts.Bootstrap;

public sealed record MobilePlanSummary(
    string Name,
    string Status,
    DateTime? EndsAtUtc,
    bool HasValidSubscription);

public sealed record MobileBootstrapResponse(
    MobileUserSummary User,
    MobileBusinessSummary Business,
    string BusinessRole,
    IReadOnlyList<string> Permissions,
    MobilePlanSummary Subscription,
    IReadOnlyList<string> AvailableModules,
    DateTime ServerTimeUtc);
