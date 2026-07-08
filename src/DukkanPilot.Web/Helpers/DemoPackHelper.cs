using DukkanPilot.Web.Models.Demo;

namespace DukkanPilot.Web.Helpers;

public static class DemoPackHelper
{
    public static IReadOnlyList<DemoPackCardViewModel> GetPacks(string roiBaseUrl = "/RoiCalculator")
    {
        var packs = new List<DemoPackCardViewModel>
        {
            new()
            {
                Slug = "demo-kafe",
                Title = "Demo Kafe",
                VerticalName = "Kafe",
                ShortDescription = "Hızlı QR menü, WhatsApp sipariş ve sadakat vitrini.",
                BestFor = "Kahve dükkanları, küçük kafe işletmeleri",
                ScenarioSummary = "Müşteri masadan menüye girer, sepete ekler, kampanya uygulanır ve sipariş WhatsApp'a düşer.",
                PublicMenuUrl = "/m/demo-kafe",
                RoiCalculatorUrl = roiBaseUrl + "?vertical=kafe",
                SuggestedPlanName = "Starter",
                KeyFeatures = new[] { "QR Menü", "WhatsApp Sipariş", "Kampanya auto-apply", "Sadakat ödülü" },
                DemoTalkingPoints = new[]
                {
                    "Sepete 100₺ üstü ekleyin; kampanya otomatik görünsün.",
                    "Sipariş sonrası tracking akışını gösterin.",
                    "Sadakat ödülü bölümünü açın; tekrar sipariş konuşun."
                },
                SampleCategories = new[] { "Kahveler", "Soğuk İçecekler", "Atıştırmalıklar", "Tatlılar" },
                SampleCampaigns = new[] { "100₺ üzeri %10 indirim" },
                SampleRewards = new[] { "100 Puana Ücretsiz Kahve" },
                BadgeText = "En popüler",
                SortOrder = 1
            },
            new()
            {
                Slug = "demo-tatlici",
                Title = "Demo Tatlıcı",
                VerticalName = "Tatlıcı",
                ShortDescription = "Görsel menü, kampanya ve yüksek sepet senaryosu.",
                BestFor = "Tatlıcı, pastane, paket servis",
                ScenarioSummary = "Ürün çeşitliliği + kampanya ile sepet artırma; ödülle tekrar ziyaret.",
                PublicMenuUrl = "/m/demo-tatlici",
                RoiCalculatorUrl = roiBaseUrl + "?vertical=tatlici",
                SuggestedPlanName = "Starter",
                KeyFeatures = new[] { "Kategori/ürün vitrini", "Kampanya", "Sadakat" },
                DemoTalkingPoints = new[]
                {
                    "Ürün açıklamalarını ve kategori düzenini gösterin.",
                    "Kampanya kartını açıp 'Sepette otomatik' vurgulayın.",
                    "Ödül ile tekrar ziyaret senaryosu kurun."
                },
                SampleCategories = new[] { "Sütlü Tatlılar", "Şerbetli Tatlılar", "Pasta Dilimleri", "İçecekler" },
                SampleCampaigns = new[] { "%12 sepet indirimi" },
                SampleRewards = new[] { "150 puana tatlı ikramı" },
                BadgeText = "Kampanya odaklı",
                SortOrder = 2
            },
            new()
            {
                Slug = "demo-burgerci",
                Title = "Demo Burgerci",
                VerticalName = "Burgerci",
                ShortDescription = "Combo/menü mantığı ve sepet artırma vitrini.",
                BestFor = "Burger & fast-casual",
                ScenarioSummary = "Menüler ve kampanya ile sepet yükseltme; hızlı operasyon.",
                PublicMenuUrl = "/m/demo-burgerci",
                RoiCalculatorUrl = roiBaseUrl + "?vertical=burgerci",
                SuggestedPlanName = "Starter",
                KeyFeatures = new[] { "Menüler", "Kampanya", "Operasyon hız" },
                DemoTalkingPoints = new[]
                {
                    "Menüler kategorisini açın; sepet artışı konuşun.",
                    "Kampanya ile upsell senaryosu kurun.",
                    "Sipariş oluşturup mutfak akışını anlatın."
                },
                SampleCategories = new[] { "Burgerler", "Menüler", "Yan Ürünler", "İçecekler" },
                SampleCampaigns = new[] { "Menülerde %15 avantaj" },
                SampleRewards = new[] { "200 puana patates + içecek" },
                BadgeText = "Sepet artırma",
                SortOrder = 3
            },
            new()
            {
                Slug = "demo-restoran",
                Title = "Demo Restoran",
                VerticalName = "Restoran",
                ShortDescription = "Masa QR, mutfak operasyonu ve rapor hikayesi.",
                BestFor = "Restoran ve masa servis",
                ScenarioSummary = "Kategori akışı + kampanya; sipariş sonrası operasyon adımları.",
                PublicMenuUrl = "/m/demo-restoran",
                RoiCalculatorUrl = roiBaseUrl + "?vertical=restoran",
                SuggestedPlanName = "Pro",
                KeyFeatures = new[] { "Operasyon", "Mutfak", "Rapor" },
                DemoTalkingPoints = new[]
                {
                    "Başlangıç/Ana yemek akışını gösterin.",
                    "Akşam kampanyası ile talep artırma konuşun.",
                    "Demo sonrası panelde Kitchen/Reports anlatın."
                },
                SampleCategories = new[] { "Başlangıçlar", "Ana Yemekler", "Salatalar", "İçecekler" },
                SampleCampaigns = new[] { "Akşam menüsünde %10" },
                SampleRewards = new[] { "250 puana başlangıç ikramı" },
                BadgeText = "Operasyon",
                SortOrder = 4
            },
            new()
            {
                Slug = "demo-nargile",
                Title = "Demo Lounge",
                VerticalName = "Lounge",
                ShortDescription = "Premium deneyim, geniş kategori vitrini.",
                BestFor = "Lounge / gece işletmesi",
                ScenarioSummary = "Premium ürün vitrinleri; kampanya ile hafta içi doluluk konuşması.",
                PublicMenuUrl = "/m/demo-nargile",
                RoiCalculatorUrl = roiBaseUrl + "?vertical=lounge",
                SuggestedPlanName = "Pro",
                KeyFeatures = new[] { "Geniş kategori", "Kampanya", "Premium ürün" },
                DemoTalkingPoints = new[]
                {
                    "Kategori çeşitliliğiyle vitrin etkisini gösterin.",
                    "Hafta içi indirimi senaryosu kurun.",
                    "Demo içeriktir; işletme yerel mevzuata uymalıdır notunu kısaca geçin."
                },
                SampleCategories = new[] { "Lounge Menüsü", "Sıcak İçecekler", "Soğuk İçecekler", "Atıştırmalıklar" },
                SampleCampaigns = new[] { "Hafta içi lounge indirimi" },
                SampleRewards = new[] { "300 puana içecek ikramı" },
                BadgeText = "Premium vitrin",
                SortOrder = 5
            }
        };

        return packs.OrderBy(p => p.SortOrder).ToList();
    }

    public static DemoPacksPageViewModel BuildPublicPage()
    {
        return new DemoPacksPageViewModel
        {
            PageTitle = "İşletme tipinize uygun demoyu seçin",
            Intro = "Demo içerikler örnektir; gerçek işletme kurulumu menünüz ve süreçlerinize göre yapılır.",
            Packs = GetPacks()
        };
    }

    public static IReadOnlyList<string> GetDefaultDemoSlugs() =>
        new[] { "demo-kafe", "demo-tatlici", "demo-burgerci", "demo-restoran", "demo-nargile" };
}
