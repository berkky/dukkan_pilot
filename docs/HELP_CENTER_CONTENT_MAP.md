# 33B — Help Center Content Map

## Genel

Yardım içerikleri **static** (`HelpContentHelper`) + Razor view ile sunulur. DB/migration yok. Görüntüleme audit log üretmez.

## Public (`/Help`)

| Slug | Kategori | Bağlantılar |
|------|----------|-------------|
| nedir | Başlangıç | Landing, Features |
| demo-nasil-denenir | Başlangıç | /DemoPacks, /Demo, /m/demo-kafe |
| plan-talebi | Abonelik & Tahsilat | /Sales/RequestPlan |
| guven-ve-gizlilik | Legal & Güven | /Trust, /Privacy |
| qr-menu-nasil-calisir | QR Menü | Public menü |
| siparis-takibi | Sipariş & Mutfak | Public order flow |
| isletme-baslangic | Başlangıç | Register, Business Help |
| deger-hesaplayici | Başlangıç | /RoiCalculator |

**Güvenlik:** Demo/admin şifresi, secret, connection string yok.

## Business (`/Business/HelpCenter`)

Owner + Staff erişir. Read-only.

| Slug | Modül linki |
|------|-------------|
| ilk-kurulum | Onboarding, GoLive |
| isletme-ayarlari | Settings |
| kategori-urun-ekleme | Products, Categories |
| csv-urun-aktarimi | Products |
| qr-menu-yayinlama | MenuStudio, GoLive |
| qr-poster-paylasim | QrMenu/Print |
| siparis-mutfak | Orders, Kitchen |
| kampanya-olusturma | Campaigns |
| sadakat-odulleri | Rewards, Loyalty |
| musteri-crm | Customers |
| raporlar | Reports |
| bildirimler | Notifications |
| audit-log | AuditLogs |
| plan-talebi | Billing |
| tahsilat-kayitlari | Billing/Invoices |
| isletme-sagligi | Success |
| deger-senaryosu | ValueCalculator |
| personel-egitimi | Kitchen |

## Admin (`/Admin/HelpCenter`)

SuperAdmin only. Read-only.

| Slug | Modül linki |
|------|-------------|
| satis-pipeline | SalesRequests, SalesCenter |
| won-lead-onboarding | Onboarding |
| manuel-tahsilat | Billing |
| abonelik-yonetimi | Businesses |
| customer-success | CustomerSuccess |
| operations-center | Operations |
| quality-gate | Quality |
| backup-restore | scripts/db-* |
| incident-response | INCIDENT_RESPONSE_RUNBOOK |
| demo-gorusmesi | SalesCenter |
| satis-deger-hesaplayici | ValueCalculator |
| ilk-musteri-kurulumu | Businesses/Details |
| audit-notification-izleme | AuditLogs, Notifications |
| legal-readiness | Legal pages |

## Contextual help

Business/Admin ekranlarında `_HelpCard` üzerinden `HelpGuideUrl` ile makale linki verilir.

## Gelecek (35A)

Support ticket sistemi eklendiğinde Help Center makalelerinden ticket oluşturma CTA eklenebilir; bu aşamada ticket yok.
