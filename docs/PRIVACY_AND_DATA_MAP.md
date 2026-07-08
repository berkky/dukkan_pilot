# DukkanPilot — Privacy and Data Map (özet)

Taslak operasyon haritası. Hukuki sınıflandırma (sorumlu/işleyen) sözleşme ve fiili kontrol ile belirlenir.

| Veri / olay | Nerede oluşur | Kim görür | Neden |
|-------------|---------------|-----------|--------|
| Kayıt (ad, e-posta, telefon, şifre özeti) | `/Account/Register` | SuperAdmin (platform), sahibi | Hesap / işletme oluşturma |
| Login oturumu | Cookie `DukkanPilot.Auth` | Tarayıcı + sunucu | Kimlik doğrulama |
| İşletme ayarları | Business Settings / GoLive | Owner, Staff (kısmi) | Public menü / iletişim |
| Public sipariş formu | `/m/{slug}` sepet → sipariş | İlgili işletme paneli | Sipariş süreci |
| Orders | DB | Owner/Staff (tenant), SuperAdmin (platform) | Operasyon / rapor |
| Customers / loyalty | Business CRM | Owner/Staff (tenant) | Sadakat / CRM |
| Campaigns | Business panel | Owner/Staff | İndirim motoru |
| AuditLogs | DB + Admin/Business listeler | Yetkili roller | İzlenebilirlik |
| Notifications | DB + paneller | Yetkili roller | Uyarılar |
| Backups (`.bak`) | `artifacts/db-backups` / sunucu | Ops ekibi | Felaket kurtarma |

## Tutma

- SQL Server / LocalDB (ortama göre)
- Soft delete: `IsActive = false` (hard delete default değil)
- Saklama süreleri canlı politika + hukuki görüş ile tanımlanmalı

## Minimizasyon

- Notification/Audit metadata’sında secret yok hedefi
- Tracking token’ların access log’a yazılmaması önerilir
- Public Demo’da panel şifresi yok
- Production secret’lar repoda yok

## WhatsApp

Platform Business API kullanmaz; kullanıcı cihazından `wa.me` yönlendirmesi olabilir.
