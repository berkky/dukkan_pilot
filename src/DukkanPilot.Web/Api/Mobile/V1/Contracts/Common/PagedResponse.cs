namespace DukkanPilot.Web.Api.Mobile.V1.Contracts.Common;

public sealed record PagedResponse<T>(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<T> Items);
