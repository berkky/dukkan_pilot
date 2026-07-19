using DukkanPilot.Mobile.Core.Contracts;

namespace DukkanPilot.Mobile.Core.Session;

public enum LoginOutcomeKind
{
    Authenticated,
    BusinessSelectionRequired,
    Failed
}

public sealed record LoginOutcome(
    LoginOutcomeKind Kind,
    string? Message = null,
    string? Code = null,
    string? TraceId = null,
    IReadOnlyList<MobileBusinessOption>? Businesses = null)
{
    public static LoginOutcome Authenticated() => new(LoginOutcomeKind.Authenticated);

    public static LoginOutcome Selection(IReadOnlyList<MobileBusinessOption> businesses) =>
        new(LoginOutcomeKind.BusinessSelectionRequired, Businesses: businesses);

    public static LoginOutcome Failed(string message, string code, string? traceId = null) =>
        new(LoginOutcomeKind.Failed, message, code, traceId);
}

public interface IMobileSessionService
{
    IReadOnlyList<MobileBusinessOption> BusinessOptions { get; }
    Task<LoginOutcome> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
    Task<LoginOutcome> SelectBusinessAsync(
        int businessId,
        CancellationToken cancellationToken = default);
    Task RestoreAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task LogoutAllAsync(CancellationToken cancellationToken = default);
}
