# DukkanPilot — First Release Operations

İlk canlı müşteri / ilk production kurulumundan önce. Secret değerleri bu dosyaya yazılmaz.

## Sıra

1. **Domain + SSL**  
   DNS, sertifika, HTTPS redirect / reverse proxy.

2. **Production appsettings**  
   `appsettings.Production.example.json` → sunucuda `appsettings.Production.json`  
   `PublicBaseUrl`, `AllowedHosts`, `SupportEmail`, `CompanyName`, connection string (veya env).

2b. **Legal readiness**  
   `docs/LEGAL_READINESS_CHECKLIST.md` · Trust `/Trust` · Privacy/KVKK/Cookies/Terms  
   Placeholder şirket bilgilerini doldurun; avukat kontrolü önerilir.

3. **SQL Database oluştur**  
   Production SQL Server’da boş DB (örn. `DukkanPilotDb`). LocalDB production için önerilmez.

4. **Backup klasörü hazır**  
   Disk / ACL; henüz veri yoksa ilk baseline after migrate.

5. **Migration uygula**  
   Idempotent script review veya bilinçli `database update` (bkz. `MIGRATION_RUNBOOK.md`).  
   Production’da otomatik seed/migrate kapalıdır.

6. **Admin hesabı**  
   Seed kullanıldıysa demo admin şifresini **hemen değiştir**. Seed yoksa güvenli SuperAdmin oluşturma sürecini izle.

7. **İlk business**  
   Admin → İşletmeler → oluştur; abonelik planı bağla; Owner kullanıcı.

8. **Go-Live**  
   Business Go-Live merkezi: WhatsApp, menü, kategori/ürün, QR.

9. **Demo / test siparişi**  
   Public menü → sepet → WhatsApp/confirm → kitchen → tracking.

10. **QR poster**  
    Slug URL basılı / dijital.

11. **Backup**  
    `db-backup.ps1` + verify (veya SSMS).

12. **Smoke**  
    `run-smoke-tests.ps1` + `SMOKE_TEST_CHECKLIST.md` + `/Admin/Operations`.

12b. **Release quality gate**  
   `release-quality-gate.ps1` ile smoke + SEO + security headers + demo readiness.

12c. **Performance smoke**  
   `check-performance-smoke.ps1` — public route response süreleri (benchmark değil).

13. **Müşteriye teslim**  
    - Panel URL + roller  
    - Public menü URL  
    - Destek iletişim  
    - Demo şifre paylaşmama  
    - “Seed demo verisi production’da temizlendi/değiştirildi” notu  

## Hızlı komutlar (geliştirme doğrulama)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-release.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\db-migration-status.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\db-generate-migration-script.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\publish-release.ps1
```

Canlı sonrası: `DATABASE_BACKUP_AND_RECOVERY.md` (günlük/haftalık), `INCIDENT_RESPONSE_RUNBOOK.md`.

## Teslim sonrası ilk hafta

- [ ] Günlük backup doğrulandı mı?  
- [ ] Bir kez test restore yapıldı mı?  
- [ ] `/health` izleniyor mu?  
- [ ] Customer support kanalı net mi?  
