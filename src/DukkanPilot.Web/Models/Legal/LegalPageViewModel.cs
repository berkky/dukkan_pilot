namespace DukkanPilot.Web.Models.Legal;

public class LegalPageViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string LastUpdatedDisplay { get; set; } = "8 Temmuz 2026";
    public string CompanyName { get; set; } = "DukkanPilot";
    public string SupportEmail { get; set; } = "support@your-domain.com";
    public string PublicBaseUrl { get; set; } = "https://your-domain.com";
    public string Disclaimer { get; set; } =
        "Bu sayfadaki metinler hukuki danışmanlık değildir; örnek taslak niteliğindedir. Canlı kullanım öncesi avukat veya KVKK uzmanı tarafından kontrol edilmesi önerilir.";
}

public class TrustPageViewModel : LegalPageViewModel
{
    public Landing.LandingAuthCtaViewModel AuthCta { get; set; } = new();
}
