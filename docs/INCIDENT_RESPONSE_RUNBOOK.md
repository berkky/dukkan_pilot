# DukkanPilot — Incident Response Runbook

Amaç: ilk 15–30 dakikada stabilizasyon. Secret’ları chat/ticket’a yapıştırmayın.

## İlk 15 dakika checklist

1. [ ] `/health` — app + database  
2. [ ] Ortam: Production mi? Son deploy / migration ne zaman?  
3. [ ] App pool / Kestrel çalışıyor mu?  
4. [ ] Disk dolu mu? (log + backup klasörü)  
5. [ ] Son bilinen iyi backup var mı?  
6. [ ] Müşteri etkisi: sadece panel mi, public menü mü, sipariş mü?  
7. [ ] Değişiklik dondur (yeni deploy yok)  

## Senaryolar

### 500 / Error sayfası

- Production’da stack trace kullanıcıya gitmez; sunucu log / stdout / Event Viewer
- Son publish mi yoksa config mi?
- `/Error/500` sayfası geliyorsa uygulama ayakta ama exception var
- Geçici: app pool recycle; kalıcı: log + fix + forward deploy

### Login çalışmıyor

- Cookie Secure + HTTPS uyumu (HTTP’de Secure cookie düşmez)
- DataProtection key kaybı → cookie/reset token geçersiz (key ring persistence)
- Kullanıcı rol / IsActive
- Auth akışını bozacak hotfix’ten kaçın; config/HTTPS düzelt

### Database connection hatası

- `/health` database=fail → 503
- SQL Server ayakta mı, firewall, connection string (sunucu env, **repo değil**)
- Credential / least-privilege user lock?
- Admin Operations Center “Bağlantı yok” ile doğrula

### Migration yarıda kaldı

1. App offline  
2. Hata mesajı / uygulanan migration listesi (`db-migration-status` veya `__EFMigrationsHistory`)  
3. Forward fix mümkün mü? Değilse **backup restore**  
4. Yarım schema ile traffic açma  

### Public menu açılmıyor (`/m/{slug}`)

- İşletme aktif / slug doğru  
- DNS / HTTPS / reverse proxy  
- Sadece menü mü, tüm site mi?  
- Demo: `/m/demo-kafe`

### Sipariş oluşmuyor

- WhatsApp numarası ayarı  
- Kampanya/limit/subscription gate mesajları  
- Public cart → confirm → tracking zinciri  
- Notifications / Audit son olaylar  

### Kampanya indirimi yanlış

- Campaign aktif / tarih / MinimumOrderAmount  
- Engine’e hot refactor yapma; audit + order snapshot alanları  
- Gerekirse kampanyayı pasifleştir (soft)

### Notification / Audit çalışmıyor

- İlgili listeler SuperAdmin / Owner yetkisi  
- DB yazma hatası (disk, connection)  
- Fail-safe servis: kritik path’i bloklamamalı; log’a bak  

### Disk dolu / backup alınamıyor

- Eski `.bak` / log temizliği (policy)  
- `sqlcmd` / yetki  
- Backup başarısızsa release/migration dondur  

## Rollback kararı

| Soru | Evet ise |
|------|----------|
| Son deploy sonrası mı bozuldu? | Önceki publish yedeği |
| Schema migration mı? | Backup restore veya forward fix |
| Sadece config? | Config düzelt + recycle |
| Veri bozulması? | Backup restore + müşteri iletişimi |

## Roller (örnek)

| Kim | Ne |
|-----|-----|
| On-call / ops | Health, IIS/Kestrel, disk, backup |
| Backend | Log, migration status, hotfix |
| SuperAdmin | Operations Center, Audit, Notifications, Sales |
| Müşteri iletişim | Kısa durum + ETA; şifre paylaşma |

## Müşteri açıklama (şablon)

> Sistemde geçici bir kesinti yaşanıyor; ekibimiz inceliyor. Sipariş/menü durumu: [çalışıyor/kısmi/kapalı]. Bir sonraki güncelleme: [saat]. Hesap şifresi talep etmeyiz.

## Hızlı recovery adımları

1. Stabilize (offline veya read-only trafik)  
2. Backup locate + VERIFYONLY  
3. Root cause branch (config vs code vs DB)  
4. En düşük riskli fix  
5. Smoke: `/health`, login, `/m/{slug}`, test order  
6. Postmortem notu  

İlgili: `DATABASE_BACKUP_AND_RECOVERY.md`, `/Admin/Operations`.
