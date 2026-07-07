# DukkanPilot — Proje Durumu (Checkpoint)

> Son güncelleme: 15A aşaması (abonelik durumu ve subscription gate) tamamlandı.

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

Sonraki MVP aşaması proje ihtiyacına göre belirlenecek (ör. ödeme entegrasyonu, public menü abonelik kontrolü, AI entegrasyonu).

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
| `/Admin/SubscriptionPlans` | Plan listesi |
| `/Business/Dashboard` | İşletme paneli özeti + sadakat özeti |
| `/Business/Categories` | Kategori listesi |
| `/Business/Products` | Ürün listesi |
| `/Business/Orders` | Sipariş listesi |
| `/Business/Orders/Details/{id}` | Sipariş detayı |
| `/Business/Customers` | Müşteri listesi |
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
| `/Business/Billing` | Abonelik durumu (BusinessOwner) |
| `/Business/Billing/Required` | Abonelik gerekli uyarı ekranı |
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
