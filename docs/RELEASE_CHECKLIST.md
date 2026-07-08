# DukkanPilot — Release Checklist

## Release öncesi

- [ ] `git status` temiz (veya yalnızca bilinçli değişiklikler staged)
- [ ] `dotnet build` → 0 hata 0 uyarı
- [ ] `powershell -File .\scripts\check-release.ps1` PASS
- [ ] `dotnet ef migrations has-pending-model-changes` → no changes
- [ ] Migration listesi beklenen set ile uyumlu (`AddNotifications` son migrations dahil)
- [ ] `powershell -File .\scripts\db-migration-status.ps1` PASS
- [ ] `db-backup.ps1` çalıştı mı? (veya SSMS FULL backup)
- [ ] `db-verify-backup.ps1` geçti mi?
- [ ] `db-generate-migration-script.ps1` üretildi mi? (değişiklik varsa)
- [ ] Migration script review edildi mi?
- [ ] Rollback/restore planı var mı? (`DATABASE_BACKUP_AND_RECOVERY.md`)
- [ ] Smoke test listesi hazır mı? (`SMOKE_TEST_CHECKLIST.md` + `run-smoke-tests.ps1`)
- [ ] `appsettings.Production.example.json` güncel; gerçek secret yok
- [ ] `appsettings.Production.json` / `.env` / connection string **commitlenmemiş**
- [ ] Demo/Owner/Admin şifreleri public landing/demo sayfasında yok
- [ ] Legal pages smoke: `/Privacy` `/Terms` `/Kvkk` `/Cookies` `/DataProcessing` `/Trust`
- [ ] Cookie notice kontrol edildi (tracking yok)
- [ ] Legal docs reviewed (`LEGAL_READINESS_CHECKLIST.md`) — taslak; garanti iddiası yok
- [ ] `docs/PROJECT_STATE.md` checkpoint güncel
- [ ] Değişen özellikler için smoke maddeleri biliniyor
- [ ] `/Admin/Operations` release + legal readiness checklist (SuperAdmin)

## Release üretimi

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-release.ps1
```

- [ ] `artifacts\publish\DukkanPilot.Web\DukkanPilot.Web.dll` var
- [ ] `web.config` (IIS publish) varsa kontrol
- [ ] Publish klasörü git’e eklenmedi (`artifacts/` ignore)

## Sunucuya yükleme

- [ ] Mevcut sitenin yedeğini al (publish klasörü + opsiyonel DB backup)
- [ ] Uygulamayı offline et / app pool stop (kısa kesinti)
- [ ] Yeni publish dosyalarını kopyala
- [ ] Sunucuda `appsettings.Production.json` yerinde (veya env vars)
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Database update bilinçli çalıştırıldı
- [ ] App pool / service start
- [ ] SSL binding doğru

## Release sonrası doğrulama

- [ ] `/health` → ok / database ok
- [ ] `/` landing
- [ ] `/Demo` ve `/m/demo-kafe`
- [ ] `/Account/Login`
- [ ] Owner: Business Dashboard + DemoCenter
- [ ] SuperAdmin: Admin Dashboard + SalesCenter
- [ ] Test sipariş (public → kitchen)
- [ ] Notification / Audit log görünür
- [ ] `/robots.txt` `/sitemap.xml`

## Rollback

- [ ] Önceki publish yedeğini geri kopyala
- [ ] App pool recycle
- [ ] **DB migration geri alma risklidir** — mümkünse forward fix; zorunluysa yedekten restore
- [ ] `/health` tekrar kontrol

## Notlar

- Seed otomatik Production’da çalışmaz.
- Secret rotation: demo hesaplar production’da değiştirilmeli.
- Backup / restore: `DATABASE_BACKUP_AND_RECOVERY.md`
- Incident: `INCIDENT_RESPONSE_RUNBOOK.md`
- Ops security: `OPERATIONAL_SECURITY_CHECKLIST.md`
