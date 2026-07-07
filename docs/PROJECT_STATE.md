# DukkanPilot — Proje Durumu (Checkpoint)

> Son güncelleme: 23A aşaması (Müşteri CRM Premium / Sadakat Zekası) tamamlandı.

---

## 1. Proje adı

**DukkanPilot**

---

## 2. Projenin amacı

QR menü + WhatsApp sipariş + müşteri/sadakat + abonelik tabanlı SaaS MVP.

Kafe, restoran, tatlıcı ve küçük yiyecek işletmeleri için dijital menü, sipariş yönlendirme, müşteri takibi ve sadakat sistemi sunan abonelik tabanlı web uygulaması.

---

## 3. Teknoloji

| Alan | Seçim |
|------|--------|
| Framework | ASP.NET Core MVC |
| View | Razor Views |
| UI | Bootstrap 5 |
| ORM | Entity Framework Core |
| Veritabanı | SQL Server LocalDB |

---

## 4. Katmanlar

```
DukkanPilot.sln
├── src/DukkanPilot.Web          → MVC, Areas (Admin, Business), Views
├── src/DukkanPilot.Core         → Entities, Enums, Common, Interfaces, DTOs
└── src/DukkanPilot.Infrastructure → AppDbContext, Migrations, Seed, Repositories*, Services*
```

\* Repositories ve Services klasörleri hazır; henüz implementasyon yok.

---

## 5. Tamamlanan aşamalar

### 1. aşama — Proje iskeleti
- ASP.NET Core MVC solution ve Web projesi
- Public, Admin ve Business layout'ları (Bootstrap 5)
- Ana sayfa (landing)
- Admin ve Business dashboard boş ekranları
- Area routing (`Admin`, `Business`)

### 2. aşama — Domain katmanı
- `DukkanPilot.Core` ve `DukkanPilot.Infrastructure` projeleri
- `BaseEntity`, enum'lar, 16 entity sınıfı
- `AppDbContext` + Fluent API (`OnModelCreating`)
- Proje referansları kuruldu

### 3. aşama — Veritabanı
- `DefaultConnection` → LocalDB (`DukkanPilotDb`)
- `Program.cs` içinde DbContext DI
- Migration: `InitialCreate`
- `DbSeeder` — idempotent demo veri
- Development ortamında otomatik migrate + seed

### 4A aşaması — Admin paneli
- **İşletme yönetimi:** Index, Create, Edit, Details, Delete (soft delete)
- **Abonelik planı yönetimi:** Index, Create, Edit, Details, Delete (soft delete)
- ViewModel'ler (`Areas/Admin/Models/`)
- Admin dashboard canlı istatistikler
- Sidebar menü güncellendi
- `_Alert.cshtml` — TempData mesajları

### 4B aşaması — İşletme paneli (kategori & ürün)
- **Kategori yönetimi:** Index, Create, Edit, Details, Delete (soft delete)
- **Ürün yönetimi:** Index, Create, Edit, Details, Delete (soft delete)
- ViewModel'ler (`Areas/Business/Models/`)
- Business dashboard canlı istatistikler (tenant: oturum claim'i)
- Sidebar menü güncellendi (Kategoriler, Ürünler aktif)

### 5. aşama — Public QR Menü
- **Public menü:** `/m/{slug}` (örn. `/m/demo-kafe`)
- ViewModel'ler (`Models/PublicMenu/`)
- Mobil öncelikli `_PublicLayout.cshtml`
- Kategori navigasyonu (anchor scroll)
- Aktif kampanya banner'ları
- Menü bulunamadı (404) ekranı

### 6. aşama — WhatsApp Sipariş (Sepet)
- **Client-side sepet:** `sessionStorage` (işletme bazlı key: `cart-{businessId}`)
- Ürün kartlarında **Sepete Ekle** butonu
- Sabit sepet bar + Bootstrap offcanvas sipariş özeti
- Miktar artır/azalt, sil, sepeti temizle
- Müşteri adı, telefon ve sipariş notu alanları (opsiyonel)
- **POST** `/m/{slug}/order` — sipariş kaydı (`ValidateAntiForgeryToken`)
- Fiyatlar veritabanından okunur (client fiyatına güvenilmez)
- `Order` + `OrderItem` kaydı, `OrderNumber` üretimi
- `Status = Pending`, `Source = WhatsApp`, `CustomerId = null`
- Başarılı kayıt sonrası `wa.me` linki ile WhatsApp yönlendirme
- Türkçe, okunabilir sipariş özeti mesajı
- Sipariş sonrası sepet temizlenir

### 7. aşama — İşletme Sipariş Yönetimi
- **Sipariş listesi:** `/Business/Orders` (demo-kafe siparişleri, yeniden eskiye)
- **Sipariş detayı:** `/Business/Orders/Details/{id}`
- Sipariş kalemleri, müşteri bilgileri, tutarlar (TR formatı)
- Durum badge'leri: Beklemede, Hazırlanıyor, Tamamlandı, İptal
- **POST** `/Business/Orders/UpdateStatus/{id}` — durum güncelleme (`ValidateAntiForgeryToken`)
- `OrderStatus` enum: `Pending`, `Preparing`, `Completed`, `Cancelled`
- Business dashboard: son 5 sipariş tablosu
- Sidebar: Siparişler linki aktif

### 8. aşama — İşletme Müşteri Yönetimi
- **Müşteri listesi:** `/Business/Customers` (demo-kafe, sipariş sayısı + son sipariş tarihi)
- **CRUD:** Create, Edit, Details, Delete (soft delete: `IsActive = false`)
- Alanlar: Name, Phone, Notes, TotalPoints, IsActive
- Aynı işletmede telefon benzersizliği (Create/Edit validation)
- **Detay:** sipariş geçmişi (`CustomerId` veya `CustomerPhone` eşleşmesi)
- Business dashboard: aktif müşteri sayısı kartı eklendi
- Sidebar: Müşteriler linki aktif

### 9A aşaması — Sadakat Temeli
- **Sadakat özeti:** `/Business/Loyalty`
- **Kural düzenleme:** `/Business/Loyalty/EditRule` (PointsPerAmount, IsActive)
- **Puan hareketleri:** `/Business/Loyalty/Transactions`
- **Manuel puan işlemi:** `/Business/Loyalty/AddTransaction` (Earn / Redeem)
- `LoyaltyTransaction` kaydı + `Customer.TotalPoints` güncelleme
- Redeem'de yetersiz puan validation
- Müşteri detayında son 10 puan hareketi
- Dashboard sadakat özeti kartı
- Sidebar: Sadakat linki aktif
- Otomatik siparişten puan kazanımı yok (9B+)
- Reward/ödül yönetimi yok (9B)

### 9B aşaması — Ödül Yönetimi
- **Ödül listesi:** `/Business/Rewards` (demo-kafe ödülleri)
- **CRUD:** Create, Edit, Details, Delete (soft delete: `IsActive = false`)
- Alanlar: Name, Description, RequiredPoints, IsActive
- Aynı işletmede ödül adı benzersizliği (Create/Edit validation)
- **Ödül kullanımı:** `/Business/Rewards/Redeem/{id}`
- Aktif müşteri seçimi, yetersiz puan validation
- `LoyaltyTransaction` kaydı (`Type = Redeem`, `RewardId`, `Points = RequiredPoints`)
- `Customer.TotalPoints` düşümü, otomatik açıklama: `Ödül kullanıldı: {RewardName}`
- **Detay:** son 10 ödül kullanımı (`RewardId` ile filtre)
- Sadakat özeti: aktif ödül sayısı, en düşük puanlı aktif ödül, ödüller linki
- Müşteri detayında ödül kullanımı badge ile vurgulanır
- Sidebar: Ödüller + Puan Hareketleri linkleri aktif
- Otomatik siparişten puan kazanımı yok (9B+)

### 9C aşaması — Otomatik Puan Kazanımı
- **Sipariş tamamlandığında otomatik puan:** `OrderStatus.Completed` geçişinde tetiklenir
- Müşteri eşleştirme: `CustomerId` → yoksa `CustomerPhone` ile arama; otomatik müşteri oluşturulmaz
- Aktif `LoyaltyRule` + `PointsPerAmount > 0`; `earnedPoints = floor(TotalAmount / PointsPerAmount)`
- `LoyaltyTransaction` (`Type = Earn`, `Description = Sipariş tamamlandı: {OrderNumber}`)
- Tekrar puan engeli: aynı müşteri + aynı `Description` ile mevcut Earn kaydı kontrolü
- Sipariş detayında sadakat bilgi paneli (potansiyel / kazandırılan / müşteri yok)
- TempData: puan eklendi veya müşteri bulunamadı mesajları
- Müşteri detayı ve Puan Hareketleri ekranlarında otomatik kazanımlar görünür

### 10. aşama — Kampanya Yönetimi
- **Kampanya listesi:** `/Business/Campaigns` (demo-kafe kampanyaları)
- **CRUD:** Create, Edit, Details, Delete (soft delete: `IsActive = false`)
- Alanlar: Title, Description, StartDate, EndDate, IsActive
- Başlık benzersizliği (Create/Edit validation), EndDate ≥ StartDate
- Yayında kontrolü: `IsActive` + tarih aralığı (Public menü ile uyumlu)
- Dashboard kampanya özeti: toplam, aktif, yayında, en yakın bitiş
- Sidebar: Kampanyalar linki aktif
- Create/Edit: “AI ile Kampanya Metni Üret — Yakında” disabled buton
- Public QR menü kampanya banner mantığı değiştirilmedi

### 11. aşama — Raporlama ve Analitik
- **Rapor özeti:** `/Business/Reports`
- **Satış raporu:** `/Business/Reports/Sales` — ciro, durum sayıları, son 20 sipariş, 7 günlük ciro grafiği
- **Ürün raporu:** `/Business/Reports/Products` — top 10 ürün, top 5 satış grafiği
- **Müşteri raporu:** `/Business/Reports/Customers` — top 10 müşteri (telefon eşleşmesi dahil)
- Chart.js CDN ile bar grafikler
- Dashboard: “Raporları Gör” butonu + hızlı erişim linki
- Sidebar: Raporlar + alt rapor linkleri aktif

### 12. aşama — QR Menü Yönetimi
- **QR Menü ekranı:** `/Business/QrMenu`
- Public menü URL üretimi: `{Scheme}://{Host}/m/{slug}`
- Client-side QR kod (qrcodejs CDN, canvas)
- PNG indirme: `dukkanpilot-{slug}-qr.png`
- Linki kopyala (clipboard API + fallback)
- Menü önizleme linki (yeni sekme)
- Dashboard: “QR Menü Yönet” + “QR Kodunu İndir” hızlı linkleri
- Sidebar: QR Menü → `/Business/QrMenu`

### 13A aşaması — Cookie Authentication
- **Login / Logout / AccessDenied:** `/Account/Login`, `/Account/Logout`, `/Account/AccessDenied`
- Cookie authentication (ASP.NET Core Identity yok)
- PBKDF2 şifre hash (`PasswordHelper`)
- Admin Area: `SuperAdmin` rolü gerekli
- Business Area: `BusinessOwner` veya `Staff` rolü gerekli
- Public: `/`, `/m/{slug}`, sipariş POST herkese açık
- Claims: NameIdentifier, Name, Email, Role, BusinessId, BusinessRole
- Demo kullanıcılar seed (admin + owner)
- Admin/Business navbar: kullanıcı adı + POST logout

### 13B aşaması — Tenant oturumdan okuma
- **`BusinessBaseController`:** `CurrentBusinessId`, `CurrentBusinessRole`, `CurrentUserId`, `CurrentUserEmail`, `GetCurrentBusinessId()`, `GetCurrentBusinessIdOrForbid()`
- Business panel tenant kaynağı: claim içindeki `BusinessId` (sabit `demo-kafe` slug kaldırıldı)
- `DemoTenant.cs` silindi
- Tüm Business controller'lar (`Dashboard`, `Categories`, `Products`, `Orders`, `Customers`, `Loyalty`, `Rewards`, `Campaigns`, `Reports`, `QrMenu`) `BusinessId == CurrentBusinessId` ile filtreleniyor
- `BusinessId` claim yoksa veya geçersizse `Forbid()` → `/Account/AccessDenied`
- QR Menü: işletme `CurrentBusinessId` ile bulunur; public URL işletme `Slug` değerinden üretilir
- Admin panel ve Public menü (`/m/{slug}`) değiştirilmedi
- Seed: `owner@dukkanpilot.local` → demo-kafe `UserBusinessRole` (Owner) idempotent

### 13C aşaması — Personel (Staff) yönetimi
- **Staff CRUD:** `/Business/Staff` — Index, Create, Edit, Details, Delete (soft: `AppUser.IsActive = false`)
- Sadece `BusinessOwner` erişebilir (`[Authorize(Roles = BusinessOwner)]`); Staff kullanıcı `Forbid` → AccessDenied
- `AppUser` + `UserBusinessRole` ile personel oluşturma; `PasswordHelper` hash
- Rol eşlemesi: `BusinessRole.Staff/Owner` ↔ `UserRole.Staff/BusinessOwner`
- Email global benzersizlik; şifre create'de zorunlu, edit'te opsiyonel
- Kendi hesabını pasif yapma engeli
- Sidebar: Personel + Yeni Personel linkleri (yalnızca BusinessOwner görür)
- ViewModel'ler: `StaffListViewModel`, `StaffFormViewModel`, `StaffDetailsViewModel`

### 14A aşaması — Public işletme kayıt ekranı
- **Kayıt:** `/Account/Register` (GET/POST, `[AllowAnonymous]`)
- Kayıt sonucu: `Business` + `BusinessSetting` + `AppUser` (BusinessOwner) + `UserBusinessRole` (Owner)
- Opsiyonel: `BusinessSubscription` (Free plan, 14 gün Trial); plan yoksa atlanır
- `BusinessSlugHelper` — Türkçe karakter dönüşümü + benzersiz slug (`demo-kafe-2` vb.)
- Email global benzersiz; `PasswordHelper` hash; kayıt sonrası Login'e yönlendirme + TempData mesajı
- Telefon opsiyonel → `Business.Phone` + `BusinessSetting.WhatsAppNumber`
- Login sayfasına kayıt linki; kayıt sayfasına giriş linki
- Migration oluşturulmadı; mevcut entity yapısı korundu

### 14B aşaması — Şifre sıfırlama
- **Forgot password:** `/Account/ForgotPassword` (GET/POST)
- **Confirmation:** `/Account/ForgotPasswordConfirmation` — email enumeration yok
- **Reset password:** `/Account/ResetPassword` (GET/POST), `/Account/ResetPasswordConfirmation`
- `PasswordResetTokenHelper` — Data Protection ile token (UserId, Email, CreatedAt, PasswordHash fingerprint)
- Token süresi 30 dk; şifre değişince eski token geçersiz
- MVP: reset linki `TempData["ResetLink"]` ile confirmation sayfasında gösterilir (gerçek e-posta yok)
- Login sayfasına “Şifrenizi mi unuttunuz?” linki
- Migration / yeni tablo / Identity yok

### 15A aşaması — Abonelik durumu ve subscription gate
- **`BusinessSubscriptionStatusHelper`:** geçerli abonelik kontrolü (Trial/Active, tarih, işletme aktif)
- **Billing:** `/Business/Billing` (Owner), `/Business/Billing/Required` (Owner + Staff)
- **Dashboard:** abonelik özeti kartı + geçersiz abonelik uyarı banner'ı
- **`RequireActiveSubscription` filter:** Categories, Products, Orders, Customers, Loyalty, Rewards, Campaigns, Reports, QrMenu, Staff
- Muaf: Dashboard, Billing, Public menü, Admin, Account auth
- Sidebar: Abonelik linki (yalnızca BusinessOwner)
- Migration / Identity yok

### 15B aşaması — Plan kullanım limitleri
- **`BusinessPlanLimitHelper`:** plan limitleri (`MaxProducts`, `MaxCampaigns` + plan adına göre fallback)
- **Kullanım sayımı:** Products, Categories, Staff (aktif), Campaigns, Rewards, QrCodes — tenant bazlı
- **Dashboard:** kullanım/limit kartları (`_PlanUsageSummary`)
- **Billing:** kullanım/limit tablosu (`_PlanUsageTable`)
- **Create POST limit kontrolü:** Products, Categories, Staff, Campaigns, Rewards, QrMenu Generate
- **Limitsiz:** Enterprise planında `-1` = Limitsiz
- Migration / Identity yok

### 16A aşaması — Admin manuel abonelik yönetimi
- **Abonelik yönetimi:** `/Admin/Businesses/Subscription/{businessId}` (GET/POST)
- Admin işletme detayında ve listesinde **Abonelik Yönet** butonu
- Plan, status, başlangıç/bitiş tarihi ve `IsActive` düzenleme
- Mevcut en güncel `BusinessSubscription` güncellenir; yoksa yeni kayıt oluşturulur
- `BusinessSubscriptionEditViewModel` (`Areas/Admin/Models/`)
- Business Billing/Dashboard ve 15A/15B helper'lar yeni planı otomatik yansıtır
- Migration / Identity yok

### 16B aşaması — Owner plan karşılaştırma ve yükseltme talebi
- **Billing plan karşılaştırma:** `/Business/Billing` — aktif planlar, limit özeti, mevcut plan etiketi
- **Yükseltme talebi:** `/Business/Billing/RequestUpgrade/{planId}` (GET/POST, yalnızca BusinessOwner)
- **Onay ekranı:** `/Business/Billing/RequestUpgradeConfirmation` — hazır talep metni (kopyalanabilir)
- Talep DB'ye kaydedilmez; plan otomatik değişmez
- Admin plan güncellemesi: `/Admin/Businesses/Subscription/{businessId}` (16A)
- Billing ve RequestUpgrade subscription gate dışında
- Migration / Identity yok

### 17A aşaması — Business ayarları ekranı
- **Ayarlar:** `/Business/Settings` (GET/POST, yalnızca BusinessOwner)
- Owner işletme bilgileri ve `BusinessSetting` alanlarını düzenler
- Slug readonly; public menü linki (`/m/{slug}`) gösterilir
- `BusinessSetting` yoksa POST sırasında oluşturulur
- Subscription gate dışında; expired abonelikte erişilebilir
- Sidebar: Ayarlar linki (yalnızca BusinessOwner)
- Migration / Identity yok

### 17B aşaması — QR Menü görünüm kişiselleştirme
- Public QR menü (`/m/{slug}`) işletme ayarlarından kişiselleştirildi
- `PublicMenuViewModel`: logo, açıklama, adres, telefon, WhatsApp, tema rengi, para birimi
- WhatsApp önceliği: `BusinessSetting.WhatsAppNumber` → `Business.Phone` → sipariş engeli
- Tema rengi geçersizse `#2563eb` fallback; `--dp-theme-color` + `--menu-theme` CSS değişkenleri
- Para birimi: TRY → `₺`; diğerleri → `125,00 USD` formatı (tr-TR sayı formatı korunur)
- Header: logo, işletme adı, açıklama; bilgi kartı: adres, telefon
- Sepet + WhatsApp sipariş akışı korundu; `public-menu-cart.js` para birimi desteği
- Migration / Identity yok

### 18A aşaması — QR Menü paylaşım ve önizleme ekranı
- **QR Menü:** `/Business/QrMenu` — paylaşım ve önizleme ekranı geliştirildi
- Link kopyalama, Menüyü Görüntüle, WhatsApp'ta Paylaş (`wa.me/?text=...`)
- QR kod önizleme/indirme: DB kaydı varsa qrcodejs önizleme + PNG indir; yoksa Oluştur butonu
- Yeniden Oluştur: mevcut `Generate` POST + 15B plan limit kontrolü korundu
- Mobil önizleme kartı: işletme adı, açıklama, logo, tema rengi
- `[RequireActiveSubscription]` ve plan limitleri bozulmadı
- Migration / Identity yok

### 18B aşaması — QR Menü yazdırılabilir masa kartı / afiş
- **Yazdırılabilir afiş:** `/Business/QrMenu/Print` — browser print / PDF olarak kaydet
- `QrMenuPrintViewModel`; işletme adı, logo, açıklama, adres, telefon, tema rengi
- qrcodejs ile client-side QR (public menü URL); QR DB kaydı gerekmez
- Print CSS: A4, `@media print`, toolbar/sidebar gizleme
- `/Business/QrMenu` ekranına **Yazdırılabilir QR Afişi** butonu (yeni sekme)
- `[RequireActiveSubscription]` korundu; PDF kütüphanesi yok
- Migration / Identity yok

### 19A aşaması — Sipariş ekranı profesyonelleştirme
- **Sipariş listesi:** `/Business/Orders` — özet kartları (bugün, bekleyen, hazırlanan, ciro)
- Durum + tarih (bugün/7 gün/tümü) + müşteri/telefon/sipariş no arama filtreleri
- Durum rozetleri; liste satırında hızlı durum değiştirme (POST + antiforgery)
- **Sipariş detay:** hızlı aksiyonlar, WhatsApp iletişim butonu, sadakat paneli korundu
- `UpdateStatus` — Completed geçişinde 9C sadakat puanı mantığı korundu; çift puan engeli
- **Dashboard:** Sipariş Özeti kartı + Son 5 Sipariş tablosu
- Tenant filtresi `BusinessId == CurrentBusinessId` korundu
- Public menü ve WhatsApp sipariş akışı bozulmadı
- Migration / Identity yok

### 19B aşaması — Canlı sipariş takibi / yeni sipariş uyarısı
- **LiveSummary JSON:** `GET /Business/Orders/LiveSummary` — pending, preparing, today, revenue, latest order
- `[RequireActiveSubscription]` + `GetCurrentBusinessIdOrForbid()` + `ResponseCache(NoStore)`
- **Polling:** `wwwroot/js/business-orders-live.js` — 30 sn, `document.hidden` atlanır
- **Orders Index:** yeni sipariş uyarısı, özet kartları canlı güncelleme, opsiyonel sesli uyarı
- **Dashboard:** canlı takip badge + yeni sipariş uyarısı (aktif abonelikte)
- SignalR yok; yeni NuGet dependency yok; localStorage/sessionStorage minimal baseline
- Migration / Identity yok

### 19C aşaması — Mutfak / Operasyon Modu
- **Mutfak modu:** `GET /Business/Orders/Kitchen` — tablet dostu kolonlu kart görünümü
- Bekleyen, Hazırlanıyor, Bugün Tamamlanan, Bugün İptal kolonları
- Büyük dokunmatik aksiyon butonları; `UpdateStatus` POST + `returnTo=kitchen`
- LiveSummary polling + yeni sipariş uyarısı entegrasyonu
- Sidebar: Mutfak Modu linki; Dashboard: Mutfak Modunu Aç butonu
- Tenant filtresi ve 9C sadakat puanı akışı korundu
- Migration / Identity / SignalR yok

### 20A aşaması — Müşteri Sipariş Onay ve Takip Sayfası
- **Sipariş onay/takip:** `GET /m/{slug}/order-confirmation/{token}`, `GET /m/{slug}/order-status/{token}`
- DataProtection tabanlı public takip token'ı (`PublicOrderTrackingTokenHelper`, 48 saat geçerlilik)
- Token payload: OrderId, BusinessId, CreatedAtUtc — URL-safe Base64Url encoding
- **POST** `/m/{slug}/order` sonrası confirmation sayfasına yönlendirme; WhatsApp akışı korundu
- Confirmation/status sayfası: sipariş özeti, durum badge, WhatsApp'a devam et, takip linki kopyala, menüye dön
- **JSON özet:** `GET /m/{slug}/order-status/{token}/summary` — polling için minimal durum bilgisi
- `wwwroot/js/public-order-status.js` — 18 sn polling; Completed/Cancelled'da yavaşlatma
- Business Orders/Kitchen `UpdateStatus` değişikliği public takip ekranına DB üzerinden yansır
- Migration / Identity / SignalR / yeni NuGet dependency yok

### 20B aşaması — Müşteri Sipariş Takip Ekranı UI İyileştirme
- Public sipariş onay/takip ekranı (`OrderStatus.cshtml`) modernleştirildi — kart düzeni, büyük badge, okunaklı ürün listesi
- Durum zaman çizelgesi eklendi (Sipariş Alındı → Hazırlanıyor → Tamamlandı; İptal ayrı akış)
- `PublicOrderDisplayHelper`: durum mesajları + timeline adım mantığı
- Summary JSON: `statusMessage`, `timelineSteps` — polling ile badge, mesaj ve timeline güncellenir
- WhatsApp akışı ve tracking token güvenliği korundu
- Migration / Identity / SignalR / yeni NuGet dependency yok

### 21A aşaması — Business Dashboard Analitik İyileştirme
- **Dashboard KPI:** bugünkü ciro, son 7 gün ciro, aylık ciro, ortalama sepet tutarı
- **Operasyon:** bekleyen + hazırlanan sipariş (LiveSummary ile canlı güncelleme)
- **Sipariş durum dağılımı:** progress bar ile Pending/Preparing/Completed/Cancelled
- **En çok satan ürünler:** top 5 liste (iptal hariç)
- **Son siparişler** tablosu + **Hızlı aksiyonlar** (tenant slug ile QR menü linki)
- Migration / Identity / SignalR / yeni NuGet dependency yok

### 21B aşaması — Premium Raporlama Paneli
- **Reports:** `/Business/Reports` — tarih dönemi filtreleri (bugün, son 7 gün, bu ay, özel aralık)
- KPI: toplam ciro (iptal hariç), sipariş sayıları, ortalama sepet, min/max sipariş tutarı
- Günlük performans tablosu + progress bar; en çok satan ürünler (top 10); durum dağılımı; son 10 sipariş
- **CSV export:** `GET /Business/Reports/ExportCsv` — UTF-8 BOM, tenant filtresi
- Sales/Products/Customers alt raporları korundu; subscription gate korundu
- Migration / Identity / SignalR / yeni NuGet / Chart.js eklenmedi

### 22A aşaması — Menü Stüdyosu / Premium Menü Yönetimi
- **Menü Stüdyosu:** `/Business/MenuStudio` — menü sağlık kontrolü, özet kartlar, kategori bazlı ürün özeti, public menü önizleme
- Hızlı aksiyonlar: yeni kategori/ürün, QR menü, QR afişi, işletme ayarları, public link kopyalama
- **Products Index:** özet kartlar, kategori/durum/arama/fiyat filtreleri, public görünürlük, hızlı fiyat güncelleme
- **POST** `/Business/Products/ToggleActive/{id}`, `/Business/Products/UpdatePrice/{id}`, `/Business/Products/Duplicate/{id}` — antiforgery + tenant filtresi
- **GET** `/Business/Products/ExportCsv` — UTF-8 BOM CSV export
- **Categories Index:** özet kartlar, kategori başına ürün/aktif ürün/ortalama fiyat, public görünürlük
- **POST** `/Business/Categories/ToggleActive/{id}` — antiforgery + tenant filtresi
- Sidebar: Menü Stüdyosu linki; Dashboard hızlı aksiyonlara Menü Stüdyosu eklendi
- Plan limit entegrasyonu (ürün kopyalama + plan kullanım gösterimi) korundu
- Public menü, sepet, confirmation ve tracking akışı bozulmadı
- Migration / Identity / SignalR / yeni NuGet dependency yok

### 22B aşaması — Toplu Ürün Yönetimi / CSV İçe Aktarma
- **CSV içe aktarma:** `/Business/Products/ImportCsv` — önizleme (dry-run) ve import modları
- **Şablon:** `GET /Business/Products/DownloadImportTemplate` — UTF-8 BOM CSV şablonu
- CSV doğrulama: başlık zorunlu, max 500 satır, max 1 MB, kategori eşleşmesi, fiyat ≥ 0, duplicate ürün adı engeli
- Plan ürün limiti import sırasında korunur; limit aşımında kalan satırlar atlanır
- **Toplu aksiyon:** `POST /Business/Products/BulkAction` — aktif/pasif, fiyat % artır/azalt
- Products Index: CSV içe aktar, şablon indir, checkbox toplu seçim, bulk action bar
- MenuStudio hızlı aksiyonlarına import kısayolları eklendi
- Public menü ve sipariş takip akışı bozulmadı
- Migration / Identity / SignalR / yeni NuGet dependency yok

### 23A aşaması — Müşteri CRM Premium / Sadakat Zekası
- **Customers Index:** premium CRM paneli — KPI kartları, segment badge, filtreler, WhatsApp iletişim
- Segmentler: VIP (5+ sipariş veya 1000₺+), Tekrar Gelen, Yeni, Riskte, Pasif, Sipariş Yok
- **Customer Details:** performans kartları, sadakat özeti, sipariş geçmişi, top 5 ürün, aktivite özeti
- **CRM İçgörüleri:** `/Business/Customers/Insights` — segment dağılımı, en değerli/sık sipariş veren, riskteki ve yeni müşteriler
- **CSV export:** `GET /Business/Customers/ExportCsv` — UTF-8 BOM, filtre desteği
- Tenant filtresi `BusinessId` claim üzerinden korundu; 9C sadakat puanı akışı bozulmadı
- Public menü, sepet, confirmation ve tracking akışı bozulmadı
- Migration / Identity / SignalR / yeni NuGet dependency yok

---

## 6. Veritabanı

| Öğe | Değer |
|-----|--------|
| Database | `DukkanPilotDb` |
| Connection | `Server=(localdb)\mssqllocaldb;...` |
| Migration | `InitialCreate` (`20260706150101_InitialCreate`) |

### Seed verisi

| Kayıt | Detay |
|-------|--------|
| Demo işletme | **Demo Kafe** — slug: `demo-kafe` |
| Planlar | Free, Starter, Pro |
| Kategoriler | Kahveler, Tatlılar, Soğuk İçecekler |
| Ürünler | Latte, Türk Kahvesi, Cheesecake, Limonata |
| Diğer | BusinessSetting, BusinessSubscription, LoyaltyRule, Reward, Campaign |
| WhatsApp | `905550000000` (demo işletme ayarı) |
| Sadakat kuralı | Her 10 TL = 1 puan (seed) |
| Demo admin | admin@dukkanpilot.local / Admin123! (SuperAdmin) |
| Demo owner | owner@dukkanpilot.local / Owner123! (BusinessOwner, demo-kafe Owner) |

---

## 7. Bilinçli olarak yapılmayanlar

- ASP.NET Core Identity yok (cookie auth kullanılıyor)
- Repository pattern implementasyonu yok
- Service katmanı implementasyonu yok
- AI (OpenAI) entegrasyonu yok
- Ödeme entegrasyonu yok
- WhatsApp Business API yok (sadece `wa.me` deep link)
- Mobil uygulama yok
- Public siparişte otomatik müşteri oluşturma / `CustomerId` bağlama yok
- Çoklu tenant oturum seçimi yok (login'de ilk aktif `UserBusinessRole` kullanılır)
- Şifre sıfırlama tamamlandı (14B)

---

## 8. Bilinen sınırlamalar

- **Admin panel** — SuperAdmin cookie auth gerekli
- **Business panel** — BusinessOwner/Staff cookie auth; tenant `BusinessId` claim'inden okunur
- **Personel yönetimi** — yalnızca BusinessOwner; Staff rolü erişemez
- **Soft delete** — `IsActive = false`; pasif kayıtlar listede filtrelenmiyor
- **Pagination yok** — tüm kayıtlar tek sayfada
- **File upload yok** — logo/görsel URL ile giriliyor
- **Plan silme** — aktif abonelikte kullanılan plan için ilişki kontrolü yok
- **Sipariş–müşteri eşleştirme** — WhatsApp siparişlerinde `CustomerId` null kalır; geçmiş telefon eşleşmesiyle gösterilir
- **Sadakat kuralı** — MinimumOrderAmount sabit 10 TL (yeni kural oluşturulurken); sadece PointsPerAmount düzenlenir
- **Otomatik puan formülü** — `floor(TotalAmount / PointsPerAmount)`; seed kuralında PointsPerAmount=1 olduğu için tutar kadar puan kazanılır
- **Tekrar puan engeli** — `LoyaltyTransaction.Description` üzerinden; migration ile Order–Transaction ilişkisi yok

---

## 9. Sıradaki aşama

Sonraki MVP aşaması proje ihtiyacına göre belirlenecek.

23A tamamlandı — Müşteri CRM premium paneli, segment analizi ve Insights ekranı eklendi.

---

## 10. Yeni sohbette devam notu

Aşağıdaki metni yeni sohbete yapıştır:

```
docs/PROJECT_STATE.md dosyasını oku. DukkanPilot projesinde kaldığımız yerden devam edeceğiz. Mevcut mimariyi bozma.
```

---

## Hızlı referans — Önemli URL'ler

| URL | Açıklama |
|-----|----------|
| `/` | Ana sayfa |
| `/Admin/Dashboard` | Admin özet |
| `/Admin/Businesses` | İşletme listesi |
| `/Admin/Businesses/Subscription/{id}` | İşletme abonelik yönetimi |
| `/Admin/SubscriptionPlans` | Plan listesi |
| `/Business/Dashboard` | İşletme paneli özeti + sadakat özeti |
| `/Business/Products/ImportCsv` | CSV ile ürün içe aktarma |
| `/Business/Products/DownloadImportTemplate` | CSV ürün şablonu indirme |
| `POST /Business/Products/BulkAction` | Toplu ürün aktif/pasif ve fiyat işlemleri |
| `/Business/MenuStudio` | Menü Stüdyosu — sağlık kontrolü ve menü özeti |
| `/Business/Categories` | Kategori listesi |
| `/Business/Products` | Ürün listesi |
| `/Business/Orders` | Sipariş listesi |
| `/Business/Orders/LiveSummary` | Canlı sipariş özeti (JSON) |
| `/Business/Orders/Kitchen` | Mutfak / operasyon modu |
| `/Business/Orders/Details/{id}` | Sipariş detayı |
| `/Business/Customers` | Müşteri listesi (premium CRM) |
| `/Business/Customers/Insights` | CRM içgörüleri ve segment analizi |
| `GET /Business/Customers/ExportCsv` | Müşteri CSV export |
| `/Business/Customers/Details/{id}` | Müşteri detayı + sipariş/puan geçmişi |
| `/Business/Loyalty` | Sadakat özeti |
| `/Business/Loyalty/EditRule` | Sadakat kuralı düzenleme |
| `/Business/Loyalty/Transactions` | Puan hareketleri |
| `/Business/Loyalty/AddTransaction` | Manuel puan işlemi |
| `/Business/Rewards` | Ödül listesi |
| `/Business/Rewards/Create` | Yeni ödül |
| `/Business/Rewards/Edit/{id}` | Ödül düzenleme |
| `/Business/Rewards/Details/{id}` | Ödül detayı + kullanım geçmişi |
| `/Business/Rewards/Delete/{id}` | Ödül pasif yapma |
| `/Business/Rewards/Redeem/{id}` | Ödül kullanımı |
| `/Business/Campaigns` | Kampanya listesi |
| `/Business/Campaigns/Create` | Yeni kampanya |
| `/Business/Campaigns/Edit/{id}` | Kampanya düzenleme |
| `/Business/Campaigns/Details/{id}` | Kampanya detayı |
| `/Business/Campaigns/Delete/{id}` | Kampanya pasif yapma |
| `/Business/Reports` | Rapor genel özet |
| `/Business/Reports/Sales` | Satış raporu |
| `/Business/Reports/Products` | Ürün raporu |
| `/Business/Reports/Customers` | Müşteri raporu |
| `/Business/QrMenu` | QR menü yönetimi + QR kod |
| `/Business/QrMenu/Print` | Yazdırılabilir QR masa kartı / afiş |
| `/Business/Billing` | Abonelik durumu (BusinessOwner) |
| `/Business/Billing/RequestUpgrade/{planId}` | Plan yükseltme talebi (BusinessOwner) |
| `/Business/Billing/RequestUpgradeConfirmation` | Talep onay metni |
| `/Business/Billing/Required` | Abonelik gerekli uyarı ekranı |
| `/Business/Settings` | İşletme ayarları (BusinessOwner) |
| `/Business/Staff` | Personel listesi (BusinessOwner) |
| `/Business/Staff/Create` | Yeni personel |
| `/Business/Staff/Edit/{id}` | Personel düzenleme |
| `/Business/Staff/Details/{id}` | Personel detayı |
| `/Business/Staff/Delete/{id}` | Personel pasif yapma |
| `/Account/Login` | Giriş |
| `/Account/Register` | İşletme kaydı (public) |
| `/Account/ForgotPassword` | Şifre sıfırlama talebi |
| `/Account/ForgotPasswordConfirmation` | Talep onay sayfası |
| `/Account/ResetPassword` | Yeni şifre belirleme |
| `/Account/ResetPasswordConfirmation` | Şifre güncellendi |
| `/Account/Logout` | Çıkış (POST) |
| `/Account/AccessDenied` | Erişim reddedildi |
| `/m/demo-kafe` | Demo QR menü + sepet |
| `/m/{slug}/order-confirmation/{token}` | Sipariş onay ekranı (login gerekmez) |
| `/m/{slug}/order-status/{token}` | Sipariş takip ekranı |
| `/m/{slug}/order-status/{token}/summary` | Sipariş durum özeti (JSON, polling) |
| `POST /Business/Loyalty/EditRule` | Kural kaydetme |
| `POST /Business/Loyalty/AddTransaction` | Manuel puan kaydı |
| `POST /Business/Rewards/Redeem/{id}` | Ödül kullanım kaydı |
| `POST /Business/Orders/UpdateStatus/{id}` | Durum güncelleme + otomatik puan |

---

## Hızlı referans — Çalıştırma

```powershell
cd src/DukkanPilot.Web
dotnet run
```

Tarayıcı: `https://localhost:7136` veya `http://localhost:5139`
