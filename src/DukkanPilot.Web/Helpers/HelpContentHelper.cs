using DukkanPilot.Web.Models.Help;

namespace DukkanPilot.Web.Helpers;

public static class HelpContentHelper
{
    public const string ScopePublic = "Public";
    public const string ScopeBusiness = "Business";
    public const string ScopeAdmin = "Admin";

    private static readonly IReadOnlyList<HelpArticle> AllArticles = BuildArticles();

    public static IReadOnlyList<HelpArticle> GetArticles(string roleScope) =>
        AllArticles.Where(a => string.Equals(a.RoleScope, roleScope, StringComparison.OrdinalIgnoreCase)).ToList();

    public static HelpArticle? GetArticle(string slug, string roleScope) =>
        AllArticles.FirstOrDefault(a =>
            string.Equals(a.Slug, slug, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.RoleScope, roleScope, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> GetPublicSitemapSlugs() =>
        GetArticles(ScopePublic).Select(a => a.Slug).ToList();

    public static HelpCenterIndexViewModel BuildIndex(string roleScope, string articleBasePath, string indexPath)
    {
        var articles = GetArticles(roleScope);
        var cards = articles.Select(ToCard).ToList();

        return roleScope switch
        {
            ScopePublic => new HelpCenterIndexViewModel
            {
                RoleScope = roleScope,
                PageTitle = "Yardım Merkezi",
                Intro = "DükkanPilot hakkında genel bilgi, demo deneyimi ve işletme başlangıç rehberleri.",
                ArticleBasePath = articleBasePath,
                Categories = GroupByCategory(cards),
                PopularArticles = Pick(cards, "nedir", "demo-nasil-denenir", "qr-menu-nasil-calisir"),
                QuickLinks = PublicQuickLinks()
            },
            ScopeBusiness => new HelpCenterIndexViewModel
            {
                RoleScope = roleScope,
                PageTitle = "Yardım & Eğitim Merkezi",
                Intro = "İşletme panelini öğrenin, personelinizi eğitin ve günlük operasyonu hızlandırın.",
                ArticleBasePath = articleBasePath,
                Categories = GroupByCategory(cards),
                PopularArticles = Pick(cards, "ilk-kurulum", "siparis-mutfak", "qr-menu-yayinlama", "kampanya-olusturma"),
                QuickLinks = BusinessQuickLinks(),
                StarterSteps = Pick(cards, "ilk-kurulum", "kategori-urun-ekleme", "qr-menu-yayinlama", "siparis-mutfak",
                    "kampanya-olusturma", "musteri-crm", "raporlar"),
                ShowStaffTraining = true,
                StaffTrainingArticleSlug = "personel-egitimi"
            },
            _ => new HelpCenterIndexViewModel
            {
                RoleScope = roleScope,
                PageTitle = "Admin Destek & Bilgi Bankası",
                Intro = "Satış, operasyon, tahsilat ve release süreçleri için admin rehberleri.",
                ArticleBasePath = articleBasePath,
                Categories = GroupByCategory(cards),
                PopularArticles = Pick(cards, "satis-pipeline", "manuel-tahsilat", "won-lead-onboarding", "quality-gate"),
                QuickLinks = AdminQuickLinks()
            }
        };
    }

    public static HelpArticleDetailViewModel BuildDetail(string slug, string roleScope, string articleBasePath, string indexPath)
    {
        var article = GetArticle(slug, roleScope);
        if (article is null)
        {
            return new HelpArticleDetailViewModel();
        }

        var related = article.RelatedArticleSlugs
            .Select(s => GetArticle(s, roleScope))
            .Where(a => a is not null)
            .Select(a => ToCard(a!))
            .ToList();

        return new HelpArticleDetailViewModel
        {
            Article = article,
            ArticleBasePath = articleBasePath,
            IndexPath = indexPath,
            RelatedArticles = related,
            ShowPublicCtas = string.Equals(roleScope, ScopePublic, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static HelpArticleCardViewModel ToCard(HelpArticle a) => new()
    {
        Slug = a.Slug,
        Title = a.Title,
        Summary = a.Summary,
        Category = a.Category,
        Difficulty = a.Difficulty,
        EstimatedReadMinutes = a.EstimatedReadMinutes,
        SearchKeywords = a.SearchKeywords
    };

    private static IReadOnlyList<HelpCategoryGroupViewModel> GroupByCategory(IReadOnlyList<HelpArticleCardViewModel> cards) =>
        cards.GroupBy(c => c.Category)
            .OrderBy(g => CategoryOrder(g.Key))
            .Select(g => new HelpCategoryGroupViewModel { Category = g.Key, Articles = g.OrderBy(a => a.Title).ToList() })
            .ToList();

    private static int CategoryOrder(string category) => category switch
    {
        "Başlangıç" => 0,
        "QR Menü" => 1,
        "Ürün & Kategori" => 2,
        "Sipariş & Mutfak" => 3,
        "Kampanya & Sadakat" => 4,
        "Müşteri CRM" => 5,
        "Raporlar" => 6,
        "Bildirimler & Audit" => 7,
        "Abonelik & Tahsilat" => 8,
        "Onboarding & Go-Live" => 9,
        "Customer Success" => 10,
        "Admin & Operasyon" => 11,
        "Legal & Güven" => 12,
        "QA & Release" => 13,
        "Satış" => 14,
        "Teknik & Release" => 15,
        "Destek & Incident" => 16,
        _ => 99
    };

    private static IReadOnlyList<HelpArticleCardViewModel> Pick(IReadOnlyList<HelpArticleCardViewModel> cards, params string[] slugs) =>
        slugs.Select(s => cards.FirstOrDefault(c => c.Slug == s)).Where(c => c is not null).Cast<HelpArticleCardViewModel>().ToList();

    private static IReadOnlyList<HelpQuickLinkViewModel> PublicQuickLinks() => new[]
    {
        Link("Demo QR Menü", "/m/demo-kafe", "📱"),
        Link("Ücretsiz Başla", "/Account/Register", "🚀"),
        Link("Plan Talebi", "/Sales/RequestPlan", "📋"),
        Link("Güven Merkezi", "/Trust", "🛡️"),
        Link("Fiyatlar", "/Pricing", "💳")
    };

    private static IReadOnlyList<HelpQuickLinkViewModel> BusinessQuickLinks() => new[]
    {
        Link("Kurulum Sihirbazı", "/Business/Onboarding", "🧭"),
        Link("Go-Live", "/Business/GoLive", "🚀"),
        Link("Demo Merkezi", "/Business/DemoCenter", "🎬"),
        Link("Ürünler", "/Business/Products", "📦"),
        Link("Siparişler / Mutfak", "/Business/Orders", "🍳"),
        Link("Tahsilat Kayıtları", "/Business/Billing/Invoices", "🧾"),
        Link("İşletme Sağlığı", "/Business/Success", "💚")
    };

    private static IReadOnlyList<HelpQuickLinkViewModel> AdminQuickLinks() => new[]
    {
        Link("Satış Merkezi", "/Admin/SalesCenter", "🎯"),
        Link("Satış Talepleri", "/Admin/SalesRequests", "📨"),
        Link("Billing", "/Admin/Billing", "💳"),
        Link("Customer Success", "/Admin/CustomerSuccess", "💚"),
        Link("Kurulum Takibi", "/Admin/Onboarding", "🧭"),
        Link("Operasyon Merkezi", "/Admin/Operations", "🛠️"),
        Link("Kalite Merkezi", "/Admin/Quality", "✅")
    };

    private static HelpQuickLinkViewModel Link(string title, string url, string icon) =>
        new() { Title = title, Url = url, Icon = icon };

    private static HelpArticle A(
        string scope,
        string slug,
        string category,
        string title,
        string summary,
        int minutes,
        string difficulty,
        string[] steps,
        string keywords,
        HelpRelatedLink[]? links = null,
        string[]? tips = null,
        string[]? warnings = null,
        string[]? related = null) => new()
    {
        Slug = slug,
        RoleScope = scope,
        Category = category,
        Title = title,
        Summary = summary,
        EstimatedReadMinutes = minutes,
        Difficulty = difficulty,
        Steps = steps,
        SearchKeywords = keywords,
        RelatedLinks = links ?? Array.Empty<HelpRelatedLink>(),
        Tips = tips ?? Array.Empty<string>(),
        Warnings = warnings ?? Array.Empty<string>(),
        RelatedArticleSlugs = related ?? Array.Empty<string>()
    };

    private static HelpRelatedLink L(string text, string url, bool external = false) =>
        new() { Text = text, Url = url, OpenInNewTab = external };

    private static IReadOnlyList<HelpArticle> BuildArticles() => new List<HelpArticle>
    {
        // —— Public ——
        A(ScopePublic, "nedir", "Başlangıç", "DükkanPilot nedir?",
            "QR menü, WhatsApp sipariş, sadakat ve kampanya özelliklerini tek panelde sunan abonelik tabanlı SaaS.",
            3, "Başlangıç",
            new[]
            {
                "İşletmeniz için dijital menü oluşturur; müşteriler QR ile menüyü açar.",
                "Sepet ve sipariş özeti WhatsApp mesajına dönüşür; panelden sipariş takip edilir.",
                "Kampanya indirimi, sadakat puanı ve müşteri CRM modülleri işletme panelindedir.",
                "Abonelik planları ile kullanım limitleri yönetilir; plan talebi panel veya web formu ile iletilir."
            },
            "nedir tanıtım özellik saas qr menü whatsapp",
            new[] { L("Özellikler", "/Features"), L("Fiyatlar", "/Pricing"), L("Demo", "/Demo") },
            new[] { "Canlı demo için /m/demo-kafe adresini telefonunuzdan açın." },
            related: new[] { "demo-nasil-denenir", "isletme-baslangic" }),

        A(ScopePublic, "demo-nasil-denenir", "Başlangıç", "Demo nasıl denenir?",
            "Kayıt olmadan demo menüyü ve temel akışı deneyimleyin.",
            4, "Başlangıç",
            new[]
            {
                "Tarayıcıda /Demo sayfasını açın veya doğrudan /m/demo-kafe adresine gidin.",
                "Kategoriler arasında gezinin; ürünleri sepete ekleyin.",
                "Sepet çekmecesinden müşteri bilgilerini girin ve sipariş oluşturun.",
                "WhatsApp mesajı hazırlanır; panel tarafı işletme hesabı ile görülür.",
                "Kendi işletmeniz için /Account/Register ile ücretsiz başlayabilirsiniz."
            },
            "demo deneme qr menü sepet test",
            new[] { L("Demo QR Menü", "/m/demo-kafe"), L("Ücretsiz Başla", "/Account/Register") },
            new[] { "Demo ortamı gerçek müşteri verisi içermez; test amaçlıdır." },
            warnings: new[] { "Demo hesap şifreleri bu sayfada paylaşılmaz. Giriş bilgilerinizi yalnızca kayıt sonrası veya destek kanalından alın." },
            related: new[] { "qr-menu-nasil-calisir", "siparis-takibi" }),

        A(ScopePublic, "plan-talebi", "Abonelik & Tahsilat", "Plan talebi nasıl oluşturulur?",
            "Ücretsiz başlangıç veya plan yükseltme talebi oluşturma.",
            3, "Başlangıç",
            new[]
            {
                "Fiyatlar sayfasından planları inceleyin.",
                "/Sales/RequestPlan formunu doldurun veya giriş yaptıktan sonra işletme panelinden plan talebi gönderin.",
                "Satış ekibimiz talebi SalesRequests üzerinden takip eder.",
                "Onay sonrası abonelik manuel güncellenir; online kart ödemesi bu aşamada yoktur."
            },
            "plan talep fiyat abonelik yükseltme",
            new[] { L("Plan Talebi Formu", "/Sales/RequestPlan"), L("Fiyatlar", "/Pricing") },
            related: new[] { "isletme-baslangic" }),

        A(ScopePublic, "guven-ve-gizlilik", "Legal & Güven", "Güven ve gizlilik",
            "Veri güvenliği, KVKK ve güven merkezi kaynakları.",
            4, "Başlangıç",
            new[]
            {
                "Güven Merkezi (/Trust) ürün güvenlik yaklaşımını özetler.",
                "Gizlilik, KVKK, çerez ve veri işleme sayfaları yasal bilgilendirme taslaklarıdır.",
                "Panel şifreleri public sayfalarda paylaşılmaz.",
                "Ödeme sağlayıcısı entegrasyonu olmadan tahsilat takibi iç operasyon kaydıdır."
            },
            "güven gizlilik kvkk güvenlik",
            new[] { L("Güven Merkezi", "/Trust"), L("Gizlilik", "/Privacy"), L("KVKK", "/Kvkk") },
            related: new[] { "nedir" }),

        A(ScopePublic, "qr-menu-nasil-calisir", "QR Menü", "QR menü nasıl çalışır?",
            "Müşteri tarafında dijital menü deneyimi.",
            4, "Başlangıç",
            new[]
            {
                "Her işletmenin benzersiz /m/{slug} adresi vardır.",
                "Müşteri QR kodu okutarak menüyü mobil tarayıcıda açar.",
                "Kategoriler ve ürünler işletme panelinden yönetilir.",
                "Aktif kampanyalar menüde gösterilir; indirim sepette uygulanır."
            },
            "qr menü slug public mobil",
            new[] { L("Demo Menü", "/m/demo-kafe") },
            related: new[] { "siparis-takibi", "demo-nasil-denenir" }),

        A(ScopePublic, "siparis-takibi", "Sipariş & Mutfak", "Sipariş takibi nasıl çalışır?",
            "Müşteri sipariş onayı ve durum takibi.",
            3, "Başlangıç",
            new[]
            {
                "Sipariş oluşturulunca onay sayfası ve takip linki sunulur.",
                "Durumlar işletme mutfak/panel ekranından güncellenir.",
                "Müşteri takip sayfası periyodik olarak durumu yeniler.",
                "WhatsApp iletişimi işletme numarası üzerinden yürür."
            },
            "sipariş takip onay durum",
            related: new[] { "qr-menu-nasil-calisir" }),

        A(ScopePublic, "isletme-baslangic", "Başlangıç", "İşletme için hızlı başlangıç",
            "Kayıttan yayına kadar özet yol haritası.",
            5, "Başlangıç",
            new[]
            {
                "/Account/Register ile işletme hesabı oluşturun.",
                "İşletme ayarlarından ad, slug ve WhatsApp numarasını girin.",
                "Kategori ve ürün ekleyin; Go-Live skorunu kontrol edin.",
                "QR menü linkini paylaşın ve test siparişi verin.",
                "Giriş yaptıktan sonra işletme panelinde detaylı eğitim merkezine erişin: /Business/HelpCenter"
            },
            "başlangıç kurulum kayıt işletme",
            new[] { L("Ücretsiz Başla", "/Account/Register"), L("Demo Talebi", "/Sales/RequestDemo") },
            related: new[] { "nedir", "plan-talebi" }),

        // —— Business ——
        A(ScopeBusiness, "ilk-kurulum", "Onboarding & Go-Live", "İlk kurulum rehberi",
            "Yeni işletme hesabını yayına hazırlama adımları.",
            8, "Başlangıç",
            new[]
            {
                "Ayarlar: işletme adı, slug, telefon/WhatsApp numarası.",
                "En az bir kategori ve birkaç ürün ekleyin.",
                "Tema rengi ve kısa açıklama girin.",
                "Go-Live Merkezi skorunu kontrol edin.",
                "Kurulum Sihirbazı'nda eksik adımları tamamlayın.",
                "QR menü linkini test edin; Demo Merkezi checklist'ini yeşile çekin.",
                "Test siparişi verin ve mutfak ekranında durumu ilerletin."
            },
            "kurulum onboarding başlangıç go-live",
            new[] { L("Kurulum Sihirbazı", "/Business/Onboarding"), L("Go-Live", "/Business/GoLive"), L("Ayarlar", "/Business/Settings") },
            related: new[] { "isletme-ayarlari", "qr-menu-yayinlama" }),

        A(ScopeBusiness, "isletme-ayarlari", "Başlangıç", "İşletme ayarları",
            "Profil, iletişim ve görünüm ayarları.",
            4, "Başlangıç",
            new[] { "Ayarlar ekranından işletme adı ve açıklama güncellenir.", "Slug değişikliği public menü URL'sini etkiler; dikkatli olun.", "WhatsApp numarası sipariş mesajları için kullanılır.", "Tema rengi public menüde görünür." },
            "ayarlar profil whatsapp slug tema",
            new[] { L("Ayarlar", "/Business/Settings") },
            related: new[] { "ilk-kurulum" }),

        A(ScopeBusiness, "kategori-urun-ekleme", "Ürün & Kategori", "Kategori ve ürün ekleme",
            "Menü içeriğini panelden yönetme.",
            6, "Başlangıç",
            new[] { "Önce kategori oluşturun; sıralama SortOrder ile ayarlanır.", "Ürün eklerken kategori, fiyat ve görsel girin.", "Pasif ürünler menüde görünmez.", "Fiyat değişiklikleri anında public menüye yansır." },
            "kategori ürün menü fiyat",
            new[] { L("Kategoriler", "/Business/Categories"), L("Ürünler", "/Business/Products") },
            related: new[] { "csv-urun-aktarimi" }),

        A(ScopeBusiness, "csv-urun-aktarimi", "Ürün & Kategori", "CSV ürün aktarımı",
            "Toplu ürün yükleme.",
            5, "Orta",
            new[] { "Ürünler ekranından CSV şablonunu indirin.", "Kategori adları mevcut kategorilerle eşleşmelidir.", "İçe aktarım sonrası public menüyü kontrol edin.", "Hatalı satırlar raporlanır; düzeltip tekrar deneyin." },
            "csv import toplu ürün",
            new[] { L("Ürünler", "/Business/Products") },
            related: new[] { "kategori-urun-ekleme" }),

        A(ScopeBusiness, "qr-menu-yayinlama", "QR Menü", "QR menü yayınlama",
            "Menüyü müşterilere açma.",
            5, "Başlangıç",
            new[] { "Public menü adresi: /m/{slug}", "QR Menü / Menu Studio ekranından link ve poster alın.", "Masaya QR afişi yerleştirin.", "Telefondan test edin; sepet ve kampanya çalışıyor mu kontrol edin." },
            "qr yayın poster menü link",
            new[] { L("Menu Studio", "/Business/MenuStudio"), L("Go-Live", "/Business/GoLive") },
            related: new[] { "qr-poster-paylasim" }),

        A(ScopeBusiness, "qr-poster-paylasim", "QR Menü", "QR poster paylaşımı",
            "Yazdırılabilir QR materyalleri.",
            3, "Başlangıç",
            new[] { "QR yazdır ekranından afiş önizleyin.", "PDF/yazdır ile masalara yerleştirin.", "Sosyal medyada menü linkini paylaşın." },
            "poster yazdır qr afiş",
            new[] { L("QR Yazdır", "/Business/QrMenu/Print") },
            related: new[] { "qr-menu-yayinlama" }),

        A(ScopeBusiness, "siparis-mutfak", "Sipariş & Mutfak", "Sipariş ve mutfak operasyonu",
            "Gelen siparişleri yönetme.",
            7, "Başlangıç",
            new[]
            {
                "Siparişler listesinde yeni siparişleri görün.",
                "Mutfak ekranında Pending → Preparing → Ready → Completed akışını kullanın.",
                "İptal yalnızca geçerli durumlarda yapın; müşteriye WhatsApp ile bilgi verin.",
                "Completed sonrası sadakat puanı (varsa) işlenir."
            },
            "sipariş mutfak kitchen durum",
            new[] { L("Siparişler", "/Business/Orders"), L("Mutfak", "/Business/Orders/Kitchen") },
            warnings: new[] { "Sipariş durumunu müşteriyle uyumsuz bırakmayın; takip sayfası güncellenir." },
            related: new[] { "personel-egitimi" }),

        A(ScopeBusiness, "kampanya-olusturma", "Kampanya & Sadakat", "Kampanya oluşturma",
            "İndirim kampanyaları ve otomatik uygulama.",
            6, "Orta",
            new[] { "Kampanya başlığı, indirim tipi ve değer girin.", "Minimum sepet ve tarih aralığı belirleyin.", "Otomatik uygulama (auto-apply) sepette indirimi hesaplar.", "Kampanya raporlarından performansı izleyin." },
            "kampanya indirim otomatik",
            new[] { L("Kampanyalar", "/Business/Campaigns"), L("Kampanya Raporu", "/Business/Reports/Campaigns") },
            related: new[] { "sadakat-odulleri" }),

        A(ScopeBusiness, "sadakat-odulleri", "Kampanya & Sadakat", "Sadakat ve ödüller",
            "Puan kazanma ve ödül kullanımı.",
            5, "Orta",
            new[] { "Sadakat kuralı tamamlanan siparişlerde puan üretir.", "Ödüller panelden tanımlanır.", "Müşteri CRM'de puan geçmişi görülür.", "Ödül kullanımı operasyon prosedürünüze göre yönetilir." },
            "sadakat puan ödül loyalty",
            new[] { L("Ödüller", "/Business/Rewards"), L("Müşteriler", "/Business/Customers") },
            related: new[] { "kampanya-olusturma" }),

        A(ScopeBusiness, "musteri-crm", "Müşteri CRM", "Müşteri CRM kullanımı",
            "Müşteri kayıtları ve içgörüler.",
            5, "Orta",
            new[] { "Siparişlerle müşteri kayıtları oluşur.", "Müşteri listesinde arama ve filtre kullanın.", "Insights ekranında tekrar eden müşterileri görün.", "Kişisel verileri KVKK'ya uygun kullanın." },
            "crm müşteri kayıt",
            new[] { L("Müşteriler", "/Business/Customers") },
            related: new[] { "raporlar" }),

        A(ScopeBusiness, "raporlar", "Raporlar", "Raporlar",
            "Satış, kampanya ve müşteri raporları.",
            4, "Başlangıç",
            new[] { "Genel rapor özetinden trendleri inceleyin.", "Kampanya ve müşteri raporları ayrı ekranlardadır.", "Veri yoksa önce test siparişi oluşturun." },
            "rapor analiz satış",
            new[] { L("Raporlar", "/Business/Reports") },
            related: new[] { "musteri-crm" }),

        A(ScopeBusiness, "bildirimler", "Bildirimler & Audit", "Bildirim merkezi",
            "Panel bildirimlerini okuma ve yönetme.",
            3, "Başlangıç",
            new[] { "Bildirimler işletme olaylarını özetler.", "Okunmamışları filtreleyin.", "Bildirim tıklanınca ilgili ekrana gidin." },
            "bildirim notification",
            new[] { L("Bildirimler", "/Business/Notifications") },
            related: new[] { "audit-log" }),

        A(ScopeBusiness, "audit-log", "Bildirimler & Audit", "Aktivite geçmişi (audit)",
            "Kritik işlemlerin iz kaydı.",
            4, "Orta",
            new[] { "Audit log salt okunurdur.", "Filtre ile tarih ve aksiyon arayın.", "Personel değişikliklerini Owner takip edebilir." },
            "audit log aktivite geçmiş",
            new[] { L("Aktivite Geçmişi", "/Business/AuditLogs") },
            related: new[] { "bildirimler" }),

        A(ScopeBusiness, "plan-talebi", "Abonelik & Tahsilat", "Plan talebi (işletme)",
            "Panelden plan yükseltme talebi.",
            3, "Başlangıç",
            new[] { "Abonelik ekranından mevcut planı görün.", "Plan talebi veya satış talebi oluşturun.", "Onay admin tarafından manuel işlenir." },
            "plan abonelik talep",
            new[] { L("Abonelik", "/Business/Billing"), L("Satış Taleplerim", "/Business/Billing/Requests") },
            related: new[] { "tahsilat-kayitlari" }),

        A(ScopeBusiness, "tahsilat-kayitlari", "Abonelik & Tahsilat", "Tahsilat kayıtları",
            "İç tahsilat takibi (resmi fatura değildir).",
            4, "Başlangıç",
            new[] { "Tahsilat Kayıtları ekranında fatura/tahsilat durumunu görürsünüz.", "Kayıtlar iç operasyon amaçlıdır; resmi e-Fatura/e-Arşiv değildir.", "Ödeme soruları için işletme sahibi admin ile iletişime geçer.", "Vade ve ödeme durumunu buradan takip edin." },
            "tahsilat fatura ödeme billing",
            new[] { L("Tahsilat Kayıtları", "/Business/Billing/Invoices") },
            warnings: new[] { "Bu ekran resmi belge yerine geçmez. Muhasebe süreçleri ayrı yürütülmelidir." },
            related: new[] { "plan-talebi" }),

        A(ScopeBusiness, "isletme-sagligi", "Customer Success", "İşletme sağlığı",
            "Health score ve öneriler.",
            5, "Orta",
            new[] { "İşletme Sağlığı skoru kullanım ve risk sinyallerinden hesaplanır.", "Öneriler eksik modüllere yönlendirir.", "Düşük skor churn riski gösterebilir; önerilen aksiyonları uygulayın." },
            "sağlık health score success",
            new[] { L("İşletme Sağlığı", "/Business/Success") },
            related: new[] { "ilk-kurulum" }),

        A(ScopeBusiness, "personel-egitimi", "Sipariş & Mutfak", "Personel eğitim paketi",
            "Staff için kısa operasyon rehberi.",
            6, "Başlangıç",
            new[]
            {
                "Yeni sipariş bildirimini kontrol edin.",
                "Mutfak ekranında siparişi Preparing yapın.",
                "Hazır olunca Ready, teslim sonrası Completed işaretleyin.",
                "İptal gerekiyorsa yönetici onayı ve müşteri bilgilendirmesi şart.",
                "Müşteri WhatsApp mesajlarına işletme numarasından dönün.",
                "Şifre ve admin ayarlarına dokunmayın."
            },
            "personel staff eğitim mutfak",
            new[] { L("Mutfak", "/Business/Orders/Kitchen") },
            warnings: new[] { "Staff kullanıcıları finans/abonelik ekranlarına erişemeyebilir." },
            related: new[] { "siparis-mutfak" }),

        // —— Admin ——
        A(ScopeAdmin, "satis-pipeline", "Satış", "Satış pipeline",
            "Lead'ten Won'a satış akışı.",
            6, "Orta",
            new[] { "SalesRequests'te yeni talepleri triage edin.", "Durum: New → Contacted → Qualified → Won/Lost.", "Won sonrası işletme bağlayın ve onboarding başlatın.", "SalesCenter özet KPI'ları izleyin." },
            "satış pipeline lead won",
            new[] { L("Satış Talepleri", "/Admin/SalesRequests"), L("Satış Merkezi", "/Admin/SalesCenter") },
            related: new[] { "won-lead-onboarding", "demo-gorusmesi" }),

        A(ScopeAdmin, "won-lead-onboarding", "Onboarding & Go-Live", "Won lead → onboarding",
            "Kazanılan müşteri kurulum handoff.",
            5, "Orta",
            new[] { "Won talepte BusinessId bağlı olmalı.", "Admin Onboarding board'da skoru izleyin.", "Eksik adımlar için müşteriye rehber linki gönderin.", "Go-Live ve Demo Center checklist'lerini doğrulayın." },
            "won onboarding kurulum handoff",
            new[] { L("Kurulum Takibi", "/Admin/Onboarding") },
            related: new[] { "satis-pipeline", "ilk-musteri-kurulumu" }),

        A(ScopeAdmin, "manuel-tahsilat", "Abonelik & Tahsilat", "Manuel tahsilat operasyonu",
            "İç tahsilat kaydı ve ödeme girişi.",
            7, "Orta",
            new[]
            {
                "Admin Billing'den iç tahsilat kaydı oluşturun (resmi fatura değildir).",
                "Won SalesRequest'ten CreateInvoice CTA kullanılabilir.",
                "Ödeme alındığında Record Payment ile kaydedin.",
                "Ödeme kaydı aboneliği otomatik uzatmaz; Businesses subscription ekranından güncelleyin."
            },
            "tahsilat billing ödeme fatura",
            new[] { L("Billing", "/Admin/Billing") },
            warnings: new[] { "Kayıtlar resmi e-Belge değildir; muhasebe süreci ayrıdır." },
            related: new[] { "abonelik-yonetimi" }),

        A(ScopeAdmin, "abonelik-yonetimi", "Abonelik & Tahsilat", "Abonelik yönetimi",
            "İşletme planı ve süre güncelleme.",
            5, "Orta",
            new[] { "Admin Businesses → Details → Subscription düzenle.", "Plan, bitiş tarihi ve durumu güncelleyin.", "Tahsilat sonrası manuel uzatma yapın.", "Gate kısıtları geçersiz abonelikte devreye girer." },
            "abonelik plan subscription",
            new[] { L("İşletmeler", "/Admin/Businesses") },
            related: new[] { "manuel-tahsilat" }),

        A(ScopeAdmin, "customer-success", "Customer Success", "Customer Success merkezi",
            "Health score ve risk yönetimi.",
            5, "Orta",
            new[] { "Customer Success board'da düşük skorlu işletmeleri filtreleyin.", "Overdue tahsilat ve onboarding risklerini kontrol edin.", "Retention playbook'a göre aksiyon alın." },
            "customer success health churn",
            new[] { L("Customer Success", "/Admin/CustomerSuccess") },
            related: new[] { "won-lead-onboarding" }),

        A(ScopeAdmin, "operations-center", "Admin & Operasyon", "Operasyon merkezi",
            "Günlük platform operasyon checklist.",
            4, "Başlangıç",
            new[] { "Operations ekranı release ve operasyon checklist sunar.", "Backup, migration ve incident linklerini takip edin.", "Billing ve QA maddelerini periyodik kontrol edin." },
            "operasyon operations checklist",
            new[] { L("Operasyon Merkezi", "/Admin/Operations") },
            related: new[] { "backup-restore", "quality-gate" }),

        A(ScopeAdmin, "quality-gate", "QA & Release", "Release quality gate",
            "Yayın öncesi kalite doğrulama.",
            6, "İleri",
            new[] { "Kalite Merkezi checklist'ini manuel işaretleyin.", "release-quality-gate.ps1 çalıştırın.", "Smoke, SEO ve demo readiness testlerini doğrulayın.", "Build 0 hata 0 uyarı olmalı." },
            "qa quality gate release smoke",
            new[] { L("Kalite Merkezi", "/Admin/Quality") },
            related: new[] { "operations-center" }),

        A(ScopeAdmin, "backup-restore", "Teknik & Release", "Yedekleme ve geri yükleme",
            "DB backup operasyonları.",
            5, "İleri",
            new[] { "db-backup.ps1 ile yedek alın.", "db-verify-backup.ps1 ile doğrulayın.", "Restore test ortamında db-restore-test.ps1 ile deneyin.", "Üretimde restore öncesi bakım penceresi planlayın." },
            "backup restore veritabanı yedek",
            related: new[] { "incident-response" }),

        A(ScopeAdmin, "incident-response", "Destek & Incident", "Olay müdahale",
            "Incident response özeti.",
            5, "İleri",
            new[] { "INCIDENT_RESPONSE_RUNBOOK.md dosyasını izleyin.", "Etki alanını belirleyin (public/business/admin).", "Log ve health endpoint kontrol edin.", "Gerekirse bakım modu ve iletişim planı devreye alın." },
            "incident olay müdahale",
            related: new[] { "backup-restore" }),

        A(ScopeAdmin, "demo-gorusmesi", "Satış", "Demo görüşmesi",
            "Satış demo script özeti.",
            5, "Başlangıç",
            new[] { "SALES_DEMO_SCRIPT.md akışını kullanın.", "Canlı /m/demo-kafe menüsünü gösterin.", "Sepet + kampanya + mutfak akışını anlatın.", "Plan talebi ve onboarding handoff'u kapatın." },
            "demo satış görüşme",
            new[] { L("Demo Menü", "/m/demo-kafe"), L("Satış Merkezi", "/Admin/SalesCenter") },
            related: new[] { "satis-pipeline" }),

        A(ScopeAdmin, "ilk-musteri-kurulumu", "Onboarding & Go-Live", "İlk müşteri kurulumu",
            "İlk gerçek müşteri checklist.",
            8, "Orta",
            new[] { "FIRST_CUSTOMER_CHECKLIST.md adımlarını uygulayın.", "İşletme + owner hesabı oluşturun.", "Menü, QR, test siparişi tamamlayın.", "Won → tahsilat → abonelik güncelleme zincirini kapatın." },
            "ilk müşteri kurulum checklist",
            new[] { L("İşletme Oluştur", "/Admin/Businesses/Create") },
            related: new[] { "won-lead-onboarding", "manuel-tahsilat" }),

        A(ScopeAdmin, "audit-notification-izleme", "Bildirimler & Audit", "Audit ve bildirim izleme",
            "Platform aktivite takibi.",
            4, "Başlangıç",
            new[] { "Admin Audit Logs'ta kritik aksiyonları filtreleyin.", "Billing ve satış olaylarını doğrulayın.", "Admin Notifications ile operasyon uyarılarını okuyun." },
            "audit notification izleme",
            new[] { L("Audit Log", "/Admin/AuditLogs"), L("Bildirimler", "/Admin/Notifications") },
            related: new[] { "manuel-tahsilat" }),

        A(ScopeAdmin, "legal-readiness", "Legal & Güven", "Legal readiness",
            "Yasal sayfa ve güven hazırlığı.",
            4, "Başlangıç",
            new[] { "LEGAL_READINESS_CHECKLIST.md maddelerini gözden geçirin.", "Privacy/Terms/KVKK sayfaları güncel mi kontrol edin.", "Tahsilat kayıtlarının resmi belge olmadığını müşteriye açıklayın." },
            "legal kvkk güven",
            new[] { L("Güven Merkezi", "/Trust", true) },
            related: new[] { "manuel-tahsilat" })
    };
}
