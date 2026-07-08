namespace DukkanPilot.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public class ErrorPageViewModel
{
    public int StatusCode { get; set; }
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public bool IsAuthenticated { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool IsBusinessUser { get; set; }
}
