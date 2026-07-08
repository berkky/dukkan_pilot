# DukkanPilot — Proje Durumu (Checkpoint)

> Son güncelleme: **35B — Performance / Reliability Hardening** tamamlandı.

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

\* Repositories klasörü hazır; Services klasöründe seçili domain servisleri (`SupportTicketService`, `BillingOperationsService`, `SalesRequestService`, `AuditLogService`, `NotificationService`) ve çok sayıda read-only helper vardır. Genel mimari hâlâ MVC + doğrudan `AppDbContext` ağırlıklıdır; tam repository katmanı yoktur.

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

### 24A aşaması — Admin SaaS Yönetim Merkezi
- **Admin SaaS Yönetim Merkezi:** `GET /Admin/Dashboard` — platform KPI, abonelik özeti, plan dağılımı
- Platform KPI kartları: işletme, kullanıcı, sipariş, ciro (bugün / 7 gün / bu ay); iptal siparişler ciroya dahil değil
- Abonelik durumu özeti: Trial, Active, Expired, Cancelled, 7 gün içinde bitecek, aboneliği olmayan
- Plan dağılımı: işletme sayısı, aktif abonelik, potansiyel aylık gelir (`Plan.Price × aktif abonelik`)
- En aktif işletmeler (Top 10), riskli işletmeler, son kayıt olan işletmeler listeleri
- **Admin Businesses Index:** özet kartları, arama (ad/slug/telefon), aktiflik/abonelik/plan filtreleri, ürün/sipariş sayıları
- **Admin SubscriptionPlans Index:** plan kullanım sayısı, limit/fiyat okunaklı gösterim, aktif abonelikte silme uyarısı
- Admin sidebar: “SaaS Yönetim Merkezi” linki
- Admin yetkisi SuperAdmin ile korundu; Business tenant filtreleri bozulmadı
- Public menü, sepet, confirmation ve tracking akışı bozulmadı
- Migration / Identity / SignalR / yeni NuGet dependency yok

### 24B aşaması — Admin İşletme Operasyonları / Platform Kontrol Merkezi
- **Admin Business Details:** `/Admin/Businesses/Details/{id}` operasyon kontrol merkezine dönüştürüldü — profil, sağlık skoru, riskler, abonelik/plan kullanım, menü hazırlık, sipariş performansı, son siparişler, top ürünler, sahip/personel özeti
- **İşletme sağlık skoru (0-100):** aktiflik, abonelik, iletişim, kategori/ürün, son 30 gün sipariş, logo/açıklama, public menü hazırlığı
- **Risk nedenleri:** abonelik, pasiflik, menü eksikliği, iletişim ve sipariş riskleri badge ile
- **Admin Businesses Index:** sağlık skoru, risk badge, public menü linki, hızlı aktif/pasif toggle, CSV export butonu
- **CSV export:** `GET /Admin/Businesses/ExportCsv` — UTF-8 BOM, mevcut filtrelerle uyumlu
- **ToggleActive:** `POST /Admin/Businesses/ToggleActive/{id}` — SuperAdmin, anti-forgery, yalnızca `Business.IsActive`
- Admin dashboard listelerindeki detay linkleri `/Admin/Businesses/Details/{id}` route’una yönlendirir
- Business panel impersonation yok; public menü yalnızca `/m/{slug}` ile açılır
- Admin yetkisi SuperAdmin ile korundu; Business tenant filtreleri bozulmadı
- Public menü, sepet, confirmation ve tracking akışı bozulmadı
- Migration / Identity / SignalR / yeni NuGet dependency yok

### 25A aşaması — Public Sipariş Deneyimi Premium / Kampanya + Sadakat Akıllı Sepet
- **Kampanya vitrini:** `/m/{slug}` üst bölümde aktif kampanya kartları (IsActive + tarih aralığı + BusinessId filtresi)
- **Ödül vitrini:** aktif Reward listesi; otomatik puan harcama yok — ödül talebi sipariş notuna eklenir
- **Akıllı sepet:** ara toplam, kampanya bilgilendirme, sadakat kazanım önizlemesi (`PublicOrderPricingHelper`)
- **Server-side doğrulama:** `POST /m/{slug}/order` ve `POST /m/{slug}/preview-order` — DB fiyatları, aktif ürün/kategori, BusinessId kontrolü
- **Kampanya indirimi:** Campaign entity’de indirim alanı olmadığı için otomatik sepet indirimi uygulanmaz; banner + bilgilendirme mesajı gösterilir
- **Sadakat önizlemesi:** LoyaltyRule ile tahmini puan; gerçek kazanım 9C Completed akışında (bozulmadı)
- **WhatsApp mesajı:** ara toplam, toplam, kampanya/ödül talebi/not satırları
- **Confirmation/tracking:** premium bilgi kutuları, sadakat mesajı, “Siparişi Takip Et” butonu
- Public sepet, confirmation token, tracking polling ve Business/Admin akışları korundu
- Migration / Identity / SignalR / yeni NuGet dependency yok

### 25B aşaması — Gerçek Kampanya Motoru / Kontrollü Migration
- **Campaign entity kontrollü genişletildi:** `CampaignDiscountType` enum, `DiscountValue`, `MinimumOrderAmount`, `MaximumDiscountAmount`, `IsPublicVisible`, `IsAutoApply`, `Priority`
- **Migration:** `AddCampaignDiscountFields` (`20260707235034_AddCampaignDiscountFields`) — yalnızca `Campaigns` tablosuna alan eklendi
- **Campaign create/edit formları:** indirim tipi/değeri, min sepet, max indirim, public görünürlük, otomatik uygulama, öncelik + validasyon
- **Campaign listesi/detay:** indirim, auto apply, public visible, öncelik bilgileri
- **`CampaignDiscountHelper`:** indirim hesaplama, badge/metin üretimi
- **`PublicOrderPricingHelper`:** gerçek kampanya indirim motoru — uygun `IsAutoApply` kampanyalar arasından en yüksek indirim seçilir; eşitlikte `Priority`, sonra bitiş tarihi
- **Server-side doğrulama:** client fiyat/indirim/campaignId güvenilmez; DB fiyatları kullanılır
- **`Order.TotalAmount`:** server-side indirimli toplam; `OrderItem.UnitPrice` DB fiyatı
- **Public sepet/preview-order:** ara toplam, indirim, toplam, kampanya mesajı
- **WhatsApp mesajı:** ara toplam, indirim, toplam, kampanya adı
- **Confirmation/tracking:** confirmation'da indirim özeti; tracking'de kalemler toplamı vs `TotalAmount` farkından indirim gösterimi; kampanya adı yalnızca `Order.Notes`'tan güvenli okunur
- **Reports/Dashboard ciro:** indirimli `Order.TotalAmount` üzerinden doğal çalışır
- **9C Completed sadakat puanı kazanımı** bozulmadı
- **Seed (yeni kurulum):** demo kampanya %10, min 100₺, auto apply
- Identity yok; SignalR yok; yeni NuGet dependency yok
- **25B-FIX:** `DiscountType` için `HasDefaultValue` Fluent API kaldırıldı (entity default `Percentage`); `CampaignDiscountType.None = 0` sentinel eklendi; model/snapshot uyumu sağlandı, `PendingModelChanges` giderildi

### 25C aşaması — Kampanya Performans Analitiği / Gerçek İndirim Raporları
- **Order entity kontrollü genişletildi:** `SubtotalAmount`, `DiscountAmount`, `AppliedCampaignId`, `AppliedCampaignName` (FK yok; cascade riski yok)
- **Migration:** `AddOrderCampaignReportingFields` (`20260708120000`) — yalnızca `Orders` tablosu
- **Existing data backfill:** `SubtotalAmount = TotalAmount`, `DiscountAmount = 0`, kampanya alanları null
- **Public order POST:** pricing helper sonucundan Subtotal/Discount/Campaign alanları saklanır
- **Confirmation/tracking:** gerçek Order alanlarından indirim özeti (notes parse birincil değil)
- **`/Business/Reports`:** Kampanya Etkisi KPI + top kampanyalar tablosu
- **`/Business/Reports/Campaigns`:** kampanya performans ekranı + CSV export
- **Campaign Details:** sipariş/indirim/ciro kartları + son 10 sipariş
- **Dashboard:** bu ay kampanya indirimi / kampanyalı sipariş / en çok kullanılan kampanya
- Client fiyat/indirim verisine güvenilmiyor; 9C sadakat puanı kazanımı bozulmadı
- Identity yok; SignalR yok; yeni NuGet dependency yok

### 26A aşaması — İşletme Kurulum Sihirbazı / Go-Live Merkezi
- **Go-Live Merkezi:** `GET /Business/GoLive` — read-only kurulum rehberi (POST yok)
- **Kurulum checklist:** işletme bilgileri, WhatsApp, kategori, ürün, public menü, QR, kampanya, sadakat/ödül, test siparişi, mutfak, raporlar
- **Yayına hazır skoru (0–100):** zorunlu adımlar + opsiyonel bonus; eksik zorunlu adım varken “Yayına Hazır” etiketi verilmez
- **İlk eksik adım CTA:** zorunlu eksik adıma yönlendirir; Owner-only adımlarda Staff için disabled + uyarı
- **Public menü preview + link kopyalama:** absolute URL (`Scheme`/`Host`), clipboard JS + görünür fallback input
- **Plan kullanım özeti:** `BusinessPlanLimitHelper` metrikleri; limite yaklaşınca uyarı + Billing linki (Owner)
- **Test checklist + launch tips:** statik / hafif durumlu kontrol listesi; DB’de saklanmaz
- **Dashboard:** Go-Live Durumu kartı + hızlı aksiyon
- **MenuStudio / Sidebar:** Go-Live Merkezi kısayolları
- **Subscription gate:** onboarding/diagnostic için gate dışında bırakıldı (Dashboard/Billing/Settings gibi erişilebilir)
- Tenant filtresi `BusinessId` claim üzerinden korundu; Public sepet/order/confirmation/tracking’e dokunulmadı
- Migration yok; Identity yok; SignalR yok; yeni NuGet dependency yok

### 26B aşaması — SaaS Satış Sitesi / Landing Page + Pricing + Demo Funnel
- **Landing:** `GET /` — hero, problem/çözüm, nasıl çalışır, özellikler, demo, plan özeti, ürün rozetleri, FAQ, final CTA
- **Pricing:** `GET /Pricing` — `SubscriptionPlan` aktif planları + `BusinessPlanLimitHelper` limit metinleri; boşsa güvenli fallback
- **Features:** `GET /Features` — QR, sipariş, mutfak, kampanya, sadakat, CRM, rapor, Go-Live
- **Demo:** `GET /Demo` — `/m/demo-kafe` funnel rehberi; panel şifreleri public’te yok
- **Layout/nav/footer:** `_LandingLayout` (Home) + public `_Layout` (Account) satış navigasyonu; Business/Admin/PublicMenu layout’ları bozulmadı
- **CTA:** Register / Login / Demo menü / Pricing; auth-aware panele yönlendirme
- Business/Admin/Public menu/order/kampanya/sadakat akışına dokunulmadı
- Migration yok; Identity yok; SignalR yok; yeni NuGet dependency yok

### 27A aşaması — Production Readiness / Güvenlik / Hata Sayfaları / Canlıya Hazırlık
- **Error pages:** `/Error`, `/Error/404`, `/Error/500`, status re-execute; AccessDenied rol-aware CTA
- **Program.cs:** Development DeveloperExceptionPage + migrate/seed; Production ExceptionHandler + HSTS; HTTPS redirection yalnızca Production
- **Security headers:** nosniff, SAMEORIGIN, Referrer-Policy, Permissions-Policy (CSP yok — inline JS/CSS korunumu)
- **Cookie:** HttpOnly, SameSite=Lax, Secure SameAsRequest (Dev) / Always (Prod), SlidingExpiration
- **Health:** `GET /health` JSON (DB CanConnect; fail → 503, secret yok)
- **SEO:** `/robots.txt`, `/sitemap.xml` (host-aware; Admin/Business/Account/order tracking disallow)
- **Docs:** `docs/DEPLOYMENT_CHECKLIST.md`, `appsettings.Production.example.json`
- Admin Dashboard’a `/health` kısayolu
- Public/Business/Admin/Account akışları bozulmadı; Migration yok; Identity yok; SignalR yok; yeni NuGet yok

### 27B aşaması — Audit Log / Aktivite Geçmişi / Güvenli İşlem İzleme
- **Entity:** `AuditLog` (yalnızca yeni tablo; mevcut entity’ler değişmedi)
- **Migration:** `AddAuditLogs` (`20260708130220`) — yalnızca `AuditLogs` + indeksler
- **Service:** `IAuditLogService` / `AuditLogService` — fail-safe (log hatası ana işlemi kırmaz); şifre/token/cookie/tracking token loglanmaz
- **Business log:** Product/Category/Campaign/Reward/Order/Settings/Staff/Billing kritik başarı işlemleri
- **Admin log:** Business CRUD/toggle/subscription, Plan CRUD
- **Account log:** LoginSuccess/Failed, Logout, Registered, PasswordResetRequested/Completed
- **Public log:** `Public.OrderCreated` (preview-order hariç); telefon/token yok
- **UI:** `/Business/AuditLogs` (tenant filtresi), `/Admin/AuditLogs` (SuperAdmin); sidebar + dashboard kısayolları
- Identity yok; SignalR yok; yeni NuGet dependency yok

### 27C aşaması — Bildirim Merkezi / Akıllı Uyarılar
- **Entity:** `Notification` (yalnızca yeni tablo; mevcut entity’ler değişmedi)
- **Migration:** `AddNotifications` (`20260708132718`) — yalnızca `Notifications` + indeksler
- **Service:** `INotificationService` / `NotificationService` — fail-safe (bildirim hatası ana işlemi kırmaz); şifre/token/cookie/tracking token/telefon metadata’da yok; duplicate unread smart alerts skip
- **Persistent events:** Public `NewOrder`; Order status `OrderStatusChanged` / `OrderCompleted` / `OrderCancelled`; Campaign create/update (+expiring); Product create/import/duplicate plan limit uyarıları; Billing upgrade request; Settings WhatsApp Go-Live progress; Account register → Admin `NewBusinessRegistered`
- **Smart business alerts:** abonelik bitiyor/bitmiş, Go-Live eksik, aktif ürün/kategori yok, WhatsApp eksik, plan limit ≥80%/100%, kampanya yakında bitiyor — Dashboard/Notifications açılışında
- **Smart admin alerts:** ürünsüz/aktif siparişsiz/riskli işletmeler, abonelik expired/expiring özetleri — Admin Dashboard/Notifications açılışında
- **UI:** `/Business/Notifications` (tenant, subscription gate dışında), `/Admin/Notifications` (SuperAdmin); MarkRead / MarkAllRead; Summary JSON; sidebar + dashboard kartları
- **AccessDenied polish:** ReturnUrl `/Admin*` → SuperAdmin mesajı; Owner-only business hedefleri → Owner mesajı
- **Audit:** notification read eylemleri audit gürültüsü yaratmamak için loglanmadı
- Public/Business/Admin/Account/Audit akışları bozulmadı
- Identity yok; SignalR yok; yeni NuGet dependency yok; background job/push/email yok

### 28A aşaması — UX Final Polish / Boş Ekranlar / Yardım Rehberi
- **Partials:** `_EmptyState.cshtml`, `_HelpCard.cshtml` + `EmptyStateViewModel` / `HelpCardViewModel` (`Models/Shared`); Business/Admin/_ViewImports namespaces
- **CSS:** `business.css` — empty state, help card stilleri
- **Business:** Products, Categories, Campaigns, Rewards, Orders, Kitchen, Customers, Insights, Reports, Campaigns report, AuditLogs, Notifications; Dashboard Go-Live eksik adım CTA; Go-Live/MenuStudio yardım kartları
- **Admin:** AuditLogs, Notifications, Businesses (empty), Dashboard riskli-boş metin polish
- **Auth:** Login/Register CTA polish (“Ücretsiz…”)
- Public sepet/order/confirmation/tracking’e dokunulmadı; campaign engine / audit / notification logic korunuyor
- Migration yok; Entity/DbContext değişmedi; Identity yok; SignalR yok; yeni NuGet yok

### 28B aşaması — Demo & Sales Mode / Satışa Hazır Demo Deneyimi
- **Landing:** `/Demo` satış akışı + özellik vitrini + checklist; Index “Neden DukkanPilot?” + kimler için polish; Pricing kimler için / “Bu planla başla” + demo CTA (ödeme vaadi yok)
- **Business:** `/Business/DemoCenter` read-only demo readiness skoru + 9 adım CTA; sidebar + Dashboard entegrasyonu; subscription gate dışında
- **Admin:** `/Admin/SalesCenter` platform KPI + demo-ready / dikkat listesi; sidebar + Dashboard CTA; SuperAdmin only
- **Seed:** `EnrichDemoBusinessCatalogAsync` — demo-kafe eksik kategori/ürün/kampanya/ödül (Atıştırmalıklar + 12 ürün, %10 auto-apply, 100 puan ödül); silme yok
- **Docs:** `SALES_DEMO_SCRIPT.md`, `FIRST_CUSTOMER_CHECKLIST.md`, `PRODUCT_POSITIONING.md`
- Public şifre paylaşımı yok; Migration yok; Entity/DbContext değişmedi; Identity/SignalR/NuGet yok

### 29A aşaması — Deployment / Publish / Release Package
- **Scripts:** `scripts/publish-release.ps1`, `check-release.ps1`, `run-smoke-tests.ps1`
- **Config:** `appsettings.Production.example.json` güçlendirildi (`App.PublicBaseUrl`, SupportEmail, placeholders)
- **Docs:** `DEPLOYMENT_CHECKLIST.md` güncellendi; `RELEASE_CHECKLIST.md`, `SMOKE_TEST_CHECKLIST.md`, `PRODUCTION_CONFIGURATION.md`, `IIS_DEPLOYMENT_GUIDE.md`, `Kestrel_SERVICE_GUIDE.md`
- **gitignore:** `appsettings.Production.json` ignore; `artifacts/` zaten ignore
- Migration yok; Entity/DbContext değişmedi; Program.cs dokunulmadı; Identity/SignalR/NuGet yok
- Public/Business/Admin/Account/Audit/Notification/Demo akışları bozulmadı

### 29B aşaması — Backup / Database Safety / Operational Recovery
- **Scripts:** `db-backup.ps1`, `db-verify-backup.ps1`, `db-restore-test.ps1` (test DB; DukkanPilotDb Force olmadan reddedilir), `db-generate-migration-script.ps1` (idempotent SQL), `db-migration-status.ps1`
- **Admin:** `/Admin/Operations` salt okunur Operasyon Merkezi (env, DB connect, migration counts, docs checklist, script hints; secret yok); sidebar + Dashboard CTA
- **Docs:** `DATABASE_BACKUP_AND_RECOVERY.md`, `MIGRATION_RUNBOOK.md`, `INCIDENT_RESPONSE_RUNBOOK.md`, `OPERATIONAL_SECURITY_CHECKLIST.md`, `FIRST_RELEASE_OPERATIONS.md`; DEPLOYMENT/RELEASE checklist 29B linkleri
- **gitignore:** `artifacts/db-backups/`, `artifacts/sql/`, `artifacts/db-restore-data/`, `*.bak`, `*.trn`, `*.diff.bak`, `*.sqlbak`
- Migration yok; Entity/DbContext değişmedi; Program.cs dokunulmadı; Identity/SignalR/NuGet yok
- Public/Business/Admin/Account/Audit/Notification/Demo/Deployment akışları bozulmadı

### 30A aşaması — Legal / KVKK / Privacy / Trust Center
- **Public:** `/Privacy`, `/Terms`, `/Kvkk`, `/Cookies`, `/DataProcessing`, `/Trust` (+ `/Legal/...` alias); taslak uyarıları; hukuki garanti iddiası yok
- **Cookie notice:** `_CookieNotice` + `cookie-notice.js` (localStorage); analytics/tracking yok
- **Landing/Account footer** legal linkleri; Register/Login yasal link metinleri (checkbox/POST yok)
- **Business:** DemoCenter/GoLive yasal hazırlık kartı; Settings gizlilik help card
- **Admin:** Operations legal readiness checklist; SalesCenter güven evrakları kartı
- **SEO:** sitemap’e Trust/Legal URL’leri
- **Docs:** `LEGAL_READINESS_CHECKLIST.md`, `PRIVACY_AND_DATA_MAP.md`, `COOKIE_AND_TRACKING_NOTES.md`, `TERMS_TEMPLATE_NOTES.md`
- Migration yok; Entity/DbContext değişmedi; Identity/SignalR/NuGet yok; Program.cs dokunulmadı
- Public/Business/Admin/Account/Audit/Notification/Demo/Deployment/Operations akışları bozulmadı

### 30B aşaması — Abonelik Satış Talep Akışı / Lead-to-Subscription Pipeline
- **Entity/Migration:** `SalesRequest` + `AddSalesRequests` (`20260708180000`) — yalnızca `SalesRequests` tablosu
- **Service:** `ISalesRequestService` / `SalesRequestService` — public/business create, status update, duplicate azaltma, fail-safe notification/audit
- **Public:** `/Sales/RequestDemo`, `/Sales/RequestPlan`, `/Sales/ThankYou`; Privacy/KVKK checkbox; Pricing/Demo/Index/Trust CTA
- **Business:** Billing RequestUpgrade → SalesRequest; `/Business/Billing/Requests` Owner-only
- **Admin:** `/Admin/SalesRequests` list/detail/status; sidebar + Dashboard KPI + SalesCenter link
- **Docs:** `SALES_PIPELINE_RUNBOOK.md`, `SALES_REQUEST_DATA_MAP.md`
- Identity/SignalR/ödeme/NuGet yok; mevcut Business/Order/Campaign entity’leri değişmedi
- Public/Business/Admin/Account/Audit/Notification/Demo/Legal/Operations akışları bozulmadı

### 30B-FIX — Public sales Privacy/KVKK checkbox validation
- Hata: `[Range(typeof(bool), "true", "true")]` checked olsa bile server validation fail ediyordu
- Düzeltme: `RequiredTrueAttribute` + ViewModel attribute değişimi; checkbox’lar zaten `asp-for` ile bağlı
- Migration yok; Entity/DbContext değişmedi; Identity/SignalR/NuGet yok

### 31A — Customer Onboarding & Implementation Center
- `CustomerOnboardingHelper` — mevcut veriden 0–100 skor + status (NotStarted → Live); DB yazmaz
- Business: `/Business/Onboarding` (ungated, read-only) + Dashboard/GoLive/DemoCenter/sidebar
- Admin: `/Admin/Onboarding` + `/Admin/Onboarding/Details/{businessId}` + Dashboard/SalesCenter/Operations/SalesRequests handoff
- Docs: CUSTOMER_ONBOARDING_RUNBOOK, KICKOFF_MEETING_SCRIPT, IMPLEMENTATION_HANDOFF_CHECKLIST, CUSTOMER_SUCCESS_PLAYBOOK
- Migration yok; Entity/DbContext değişmedi; Identity/SignalR/NuGet/ödeme yok; Notification/Audit gürültüsü yok (read-only)

### 31B — Customer Success / Retention / Business Health Score
- `CustomerSuccessHealthHelper` — mevcut Business/Order/Customer/Campaign/Reward/Notification/Audit/Subscription/SalesRequest verilerinden 0–100 health score + churn risk + expansion potential hesaplar
- Business: `/Business/Success` + Dashboard/Onboarding/GoLive/DemoCenter/sidebar entegrasyonu
- Admin: `/Admin/CustomerSuccess` + `/Admin/CustomerSuccess/Details/{businessId}` + Dashboard/SalesCenter/Operations/BusinessDetails/SalesRequests entegrasyonu
- Docs: CUSTOMER_SUCCESS_HEALTH_SCORE, RETENTION_PLAYBOOK, UPGRADE_OPPORTUNITY_PLAYBOOK, CHURN_RISK_RUNBOOK
- Migration yok; Entity/DbContext değişmedi; Identity/SignalR/NuGet/ödeme yok; Success ekranları read-only, audit üretmez, notification spam yaratmaz

### 32A — QA / Regression / UAT / Release Quality Gate
- Script tabanlı smoke / security headers / SEO / demo readiness kontrolleri
- Tek komut: `scripts/release-quality-gate.ps1`
- Admin: `/Admin/Quality` (read-only) kalite merkezi; script çalıştırmaz, secret göstermez
- Docs: QA_TEST_PLAN, REGRESSION_TEST_MATRIX, UAT_SCRIPT_FIRST_CUSTOMER, BUG_REPORT_TEMPLATE, RELEASE_QUALITY_GATE
- Migration yok; Entity/DbContext değişmedi; test project/NuGet yok

### 32B — Public Menü + Mobil Web Final Polish
- `/m/{slug}` hero + kategori sticky nav + aktif kategori state + premium kart/CTA polish
- Sepet bar/drawer + order form + pricing notları (read-only UI polish; pricing engine bozulmaz)
- Confirmation/Tracking ekranlarında polling mesajları ve mobil UX polish
- Demo readiness script: daha zengin HTML sinyalleri + PASS/WARN/FAIL
- Docs: PUBLIC_MENU_UX_GUIDE, MOBILE_WEB_POLISH_CHECKLIST, PUBLIC_ORDER_UAT_SCRIPT
- Migration yok; Entity/DbContext değişmedi; NuGet yok; public order güvenliği/price validation korunur

### 33A — Manual Payment / Invoice / Subscription Operations
- Yeni tablolar: `BillingInvoices`, `BillingPayments` (manuel tahsilat takibi; resmi belge değildir)
- Admin: `/Admin/Billing` (Index/KPI), CreateInvoice, Details, RecordPayment, Payments, Cancel
- Business (Owner-only, read-only): `/Business/Billing/Invoices`, `/Business/Billing/Payments`
- Audit: `Billing.InvoiceCreated`, `Billing.PaymentRecorded`, `Billing.InvoiceCancelled`
- Notification: `BillingInvoiceCreated`, `BillingPaymentRecorded`, `BillingInvoicePaid`
- SalesRequests Won entegrasyonu: Details ekranında tahsilat CTA
- SalesCenter + Admin Dashboard + Operations + Quality + CustomerSuccess: billing snapshot/risk sinyalleri
- Migration: `AddManualBillingOperations` (yalnızca iki yeni tablo)

### 33B — Help Center / Eğitim Merkezi / Kullanım Kılavuzu
- Public: `/Help`, `/Help/{slug}` (anonymous)
- Business: `/Business/HelpCenter`, `/Business/HelpCenter/Article/{slug}` (Owner+Staff, read-only)
- Admin: `/Admin/HelpCenter`, `/Admin/HelpCenter/Article/{slug}` (SuperAdmin, read-only)
- `HelpContentHelper` static içerik; `help-center.js` client-side arama/filtre
- Contextual help: `_HelpCard` → `HelpGuideUrl` + sidebar/footer linkleri
- Sitemap: `/Help` + public makaleler; Admin/Business Help yok
- Docs: HELP_CENTER_CONTENT_MAP, BUSINESS_USER_TRAINING_GUIDE, STAFF_TRAINING_CHEATSHEET, ADMIN_SUPPORT_KNOWLEDGE_BASE
- Migration yok; Entity/DbContext değişmedi

### 34A — ROI Calculator / Değer Hesaplayıcı
- Public: `/RoiCalculator`, `/ValueCalculator` (anonymous, read-only hesaplama)
- Business: `/Business/ValueCalculator` (Owner+Staff; son 30 gün prefill)
- Admin: `/Admin/ValueCalculator` (SuperAdmin; opsiyonel businessId prefill)
- `ValueCalculatorHelper` + view models; Conservative/Base/Ambitious senaryolar
- Disclaimer: tahmini hesaplama, garanti gelir vaadi değil
- CTA: Landing/Pricing/Features/Demo/Help + Business Dashboard/Success/Onboarding/GoLive + Admin SalesCenter
- Help makaleleri: `deger-hesaplayici`, `deger-senaryosu`, `satis-deger-hesaplayici`
- Sitemap: `/RoiCalculator` (+ alias); Business/Admin calculator yok
- Docs: ROI_CALCULATOR_GUIDE, VALUE_SELLING_PLAYBOOK, ROI_CALCULATOR_ASSUMPTIONS
- Migration yok; Entity/DbContext değişmedi; DB’ye sonuç yazılmaz; audit yok

### 34B — Vertical Demo Packs
- Public demo gallery: `/DemoPacks` (alias: `/Demo/Packs`)
- `DemoPackHelper` + `DemoPacksController` + `DemoPackViewModels`
- Demo slugs: `demo-kafe`, `demo-tatlici`, `demo-burgerci`, `demo-restoran`, `demo-nargile`
- Seed: `DbSeeder` idempotent vertical demo seed (`EnsureVerticalDemoBusinessesAsync`); mevcut gerçek veriler silinmez; `demo-kafe` korunur
- Her demo işletmede: BusinessSetting, categories, products, campaign (public+auto-apply), reward, loyalty rule
- CTA: Landing/Demo/Pricing/ROI + Help Center + Admin SalesCenter + Business DemoCenter
- Scripts: `run-smoke-tests.ps1` multi-demo (44/44 PASS); `check-public-demo-readiness.ps1` multi-slug PASS
- `release-quality-gate.ps1` demo readiness: tüm vertical slug’lar (`-DemoSlugs`)
- Sitemap: `/DemoPacks` + demo menu slugs eklendi (Admin/Business sayfalar yok)
- Docs: VERTICAL_DEMO_PACKS_GUIDE, DEMO_PACK_SALES_SCRIPT, DEMO_DATA_SEEDING_NOTES
- Migration yok; Entity/DbContext değişmedi; NuGet/Identity/SignalR yok

### 34B-FIX — Dokümantasyon ve kalite gate housekeeping
- `PROJECT_STATE.md` checkpoint başlığı ve sıradaki aşama (35A) güncellendi
- `release-quality-gate.ps1` demo readiness: 5 vertical slug tek adımda
- Business sidebar: sabit `demo-kafe` metni kaldırıldı → `/DemoPacks` “Sektör demoları” linki
- Migration yok; Entity/DbContext değişmedi

### 35A — Support / Ticket / Feedback Center
- Entity: `SupportTicket`, `SupportTicketMessage` (yalnızca yeni tablolar)
- Migration: `AddSupportTicketCenter` — `SupportTickets` + `SupportTicketMessages`
- Service: `ISupportTicketService` / `SupportTicketService` — create, message, status, assign, close; audit + notification fail-safe
- Helper: `SupportTicketDisplayHelper` — status/priority/category labels, open/closed sets
- Business: `/Business/Support` (Index/Create/Details, Owner+Staff, subscription gate dışında)
- Admin: `/Admin/Support` (Index/Details, SuperAdmin, reply/internal note/status/priority/assign)
- Feedback: `Category=FeatureRequest`, `Source=Feedback`; public anonymous ticket yok
- Notification types: NewSupportTicket, SupportTicketUpdated, SupportTicketReplied, SupportTicketStatusChanged, SupportTicketResolved
- Audit: Support.TicketCreated, BusinessMessageAdded, AdminReplyAdded, InternalNoteAdded, StatusChanged, PriorityChanged, Assigned, Closed
- Entegrasyon: Dashboard, CustomerSuccess, Operations, Quality, Help Center makaleleri
- Docs: SUPPORT_CENTER_RUNBOOK, SUPPORT_TICKET_DATA_MAP, SUPPORT_UAT_SCRIPT, FEEDBACK_MANAGEMENT_PLAYBOOK
- Smoke: 47/47 PASS (Support auth redirects dahil)
- Identity/SignalR/NuGet/mail/chat/background job/upload yok

---

## 6. Veritabanı

| Öğe | Değer |
|-----|--------|
| Database | `DukkanPilotDb` |
| Connection | `Server=(localdb)\mssqllocaldb;...` |
| Migration | `InitialCreate`, `AddCampaignDiscountFields`, `AddOrderCampaignReportingFields` (`20260708120000`), `AddAuditLogs` (`20260708130220`), `AddNotifications` (`20260708132718`), `AddSalesRequests` (`20260708180000`), `AddManualBillingOperations` (`20260708170630`), `AddSupportTicketCenter` (`20260708213904`) |

### Seed verisi

| Kayıt | Detay |
|-------|--------|
| Demo işletme | **Demo Kafe** — slug: `demo-kafe` |
| Planlar | Free, Starter, Pro |
| Kategoriler | Kahveler, Tatlılar, Soğuk İçecekler, Atıştırmalıklar |
| Ürünler | Latte, Türk Kahvesi, Americano, Filtre Kahve, Cheesecake, Brownie, Cookie, Limonata, Ice Latte, Smoothie, Kruvasan, Tost |
| Diğer | BusinessSetting, BusinessSubscription, LoyaltyRule, Reward (100 puan kahve), Campaign (100₺ üzeri %10 auto-apply) |
| WhatsApp | `905550000000` (demo işletme ayarı) |
| Sadakat kuralı | Her 10 TL = 1 puan (seed) |
| Demo admin | admin@dukkanpilot.local / Admin123! (SuperAdmin) |
| Demo owner | owner@dukkanpilot.local / Owner123! (BusinessOwner, demo-kafe Owner) |

---

## 7. Bilinçli olarak yapılmayanlar

- ASP.NET Core Identity yok (cookie auth kullanılıyor)
- Repository pattern implementasyonu yok (tam katman yok)
- Service katmanı kısmi: kritik domain'ler için (`Support`, `Billing`, `Sales`, `Audit`, `Notification`); çoğu controller hâlâ doğrudan EF kullanır
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
- **Pagination / liste limitleri** — Audit/Notification sayfalı; Orders/Customers/Admin Businesses ve mutfak kolonları varsayılan `Take` limitli; tam sayfalama UI yok
- **File upload yok** — logo/görsel URL ile giriliyor
- **Plan silme** — aktif abonelikte kullanılan plan için ilişki kontrolü yok
- **Sipariş–müşteri eşleştirme** — WhatsApp siparişlerinde `CustomerId` null kalır; geçmiş telefon eşleşmesiyle gösterilir
- **Sadakat kuralı** — MinimumOrderAmount sabit 10 TL (yeni kural oluşturulurken); sadece PointsPerAmount düzenlenir
- **Otomatik puan formülü** — `floor(TotalAmount / PointsPerAmount)`; seed kuralında PointsPerAmount=1 olduğu için tutar kadar puan kazanılır
- **Tekrar puan engeli** — `LoyaltyTransaction.Description` üzerinden; migration ile Order–Transaction ilişkisi yok

---

### 35B — Performance / Reliability Hardening
- Read-only sorgularda `AsNoTracking` yaygınlaştırıldı (onboarding helper, support/sales summary, vb.)
- Admin Dashboard: platform KPI ve işletme sipariş istatistikleri DB-side aggregate; tüm `Orders` belleğe çekilmez
- `SupportTicketService` / `SalesRequestService` özetleri `CountAsync` ile DB-side
- Liste limitleri: Business Orders (100), Customers (100), Kitchen kolonları (50), Admin Businesses görünümü (100)
- `DemoPackHelper` default pack listesi lazy cache
- Script: `check-performance-smoke.ps1` — HTTP response time smoke (WARN/FAIL eşikleri)
- `release-quality-gate.ps1` performance smoke adımı (`-SkipPerformanceSmoke`, `-PerformanceWarningMs`, `-PerformanceFailMs`)
- Admin Operations: Performance & reliability checklist
- Admin Quality: performance readiness kartları + QA maddeleri
- Business GoLive: canlı öncesi hız kontrolü notu
- Docs: `PERFORMANCE_HARDENING_GUIDE.md`, `RELIABILITY_RUNBOOK.md`, `PERFORMANCE_SMOKE_TESTS.md`
- Migration yok; Entity/DbContext değişmedi; NuGet/cache/APM/Identity/SignalR yok

---

## 9. Sıradaki aşama

**Son tamamlanan checkpoint:** 35B — Performance / Reliability Hardening

**Sıradaki aşama:** Proje ihtiyacına göre belirlenecek (36A+).

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
| `/` | SaaS landing (satış ana sayfa) |
| `/Features` | Özellikler |
| `/Pricing` | Plan / fiyat karşılaştırma |
| `/Demo` | Demo funnel rehberi |
| `/DemoPacks` · `/Demo/Packs` | Sektör demo galerisi (vertical demo packs) |
| `/RoiCalculator` · `/ValueCalculator` | Değer hesaplayıcı (public) |
| `/Help` | Yardım merkezi (public) |
| `/Business/Support` | Destek Merkezi (işletme) |
| `/Admin/Support` | Destek talepleri (SuperAdmin) |
| `/health` | Sistem durumu JSON |
| `/robots.txt` | Arama motoru yönergeleri |
| `/sitemap.xml` | Public sayfa sitemap |
| `/Error/404` | Profesyonel 404 |
| `/Error/500` | Profesyonel 500 |
| `/Admin/Dashboard` | Admin özet |
| `/Admin/Businesses` | İşletme listesi |
| `/Admin/Businesses/Details/{id}` | İşletme operasyon kontrol merkezi |
| `/Admin/Businesses/ExportCsv` | İşletme CSV dışa aktarma |
| `/Admin/Businesses/Subscription/{id}` | İşletme abonelik yönetimi |
| `POST /Admin/Businesses/ToggleActive/{id}` | İşletme hızlı aktif/pasif |
| `/Admin/SubscriptionPlans` | Plan listesi |
| `/Business/Dashboard` | İşletme paneli özeti + sadakat özeti |
| `/Business/GoLive` | Go-Live Merkezi — kurulum sihirbazı / yayına hazırlık |
| `/Business/DemoCenter` | Satış / test Demo Merkezi |
| `/Business/AuditLogs` | İşletme aktivite geçmişi |
| `/Business/Notifications` | İşletme bildirim merkezi |
| `/Admin/AuditLogs` | Platform aktivite logları (SuperAdmin) |
| `/Admin/Notifications` | Platform bildirim merkezi (SuperAdmin) |
| `/Admin/SalesCenter` | Satış ve Demo Merkezi (SuperAdmin) |
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

## Hızlı referans — Deployment / Release

| Dosya | Açıklama |
|-------|---------|
| `scripts/publish-release.ps1` | Release publish → `artifacts/publish/DukkanPilot.Web` |
| `scripts/check-release.ps1` | Build + EF pending + kritik dosya kontrolü |
| `scripts/run-smoke-tests.ps1` | Çalışan instance HTTP smoke |
| `docs/DEPLOYMENT_CHECKLIST.md` | Canlıya çıkış checklist |
| `docs/RELEASE_CHECKLIST.md` | Release öncesi/sonrası |
| `docs/SMOKE_TEST_CHECKLIST.md` | Manuel smoke matrisi |
| `docs/PRODUCTION_CONFIGURATION.md` | Env, secrets, DataProtection notları |
| `docs/IIS_DEPLOYMENT_GUIDE.md` | IIS kurulum |
| `docs/Kestrel_SERVICE_GUIDE.md` | Kestrel / service |
| `src/DukkanPilot.Web/appsettings.Production.example.json` | Production şablon (secret yok) |
| `scripts/db-backup.ps1` | SQL FULL backup + VERIFYONLY |
| `scripts/db-verify-backup.ps1` | Mevcut .bak verify |
| `scripts/db-restore-test.ps1` | Test DB restore (production üzerine yazmaz) |
| `scripts/db-generate-migration-script.ps1` | Idempotent migration SQL |
| `scripts/db-migration-status.ps1` | Migration list + pending model |
| `docs/DATABASE_BACKUP_AND_RECOVERY.md` | Backup/restore stratejisi |
| `docs/MIGRATION_RUNBOOK.md` | Migration uygulama/rollback |
| `docs/INCIDENT_RESPONSE_RUNBOOK.md` | Olay müdahalesi |
| `docs/OPERATIONAL_SECURITY_CHECKLIST.md` | Ops güvenlik checklist |
| `docs/FIRST_RELEASE_OPERATIONS.md` | İlk canlı kurulum |
| `/Admin/Operations` | Salt okunur operasyon durumu |
| `/Trust` | Güven Merkezi |
| `/Privacy` · `/Terms` · `/Kvkk` · `/Cookies` · `/DataProcessing` | Legal taslak sayfalar |
| `docs/LEGAL_READINESS_CHECKLIST.md` | Canlı öncesi legal checklist |
