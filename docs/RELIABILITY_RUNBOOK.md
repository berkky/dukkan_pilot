# DukkanPilot — Reliability Runbook

Release öncesi ve geliştirme ortamında güvenilirlik kontrolü. Secret paylaşmayın.

## Release öncesi sıra (önerilen)

1. Çalışan `dotnet run` / eski process'leri kapat (DLL lock önleme).
2. `dotnet build -c Release` → 0 hata / 0 uyarı.
3. `scripts/check-release.ps1`
4. `scripts/db-migration-status.ps1` → pending yok.
5. Uygulamayı başlat (`http://localhost:5000`).
6. `scripts/run-smoke-tests.ps1`
7. `scripts/check-security-headers.ps1` + `check-seo-endpoints.ps1`
8. `scripts/check-public-demo-readiness.ps1` (5 demo slug)
9. `scripts/check-performance-smoke.ps1`
10. `scripts/release-quality-gate.ps1` (hepsini tek komutta da çalıştırabilirsiniz)

Veya tek adım: `release-quality-gate.ps1 -BaseUrl http://localhost:5000`

## DLL lock / port conflict troubleshooting

### Belirti

- `dotnet build` → `CS2012` / file being used by another process
- `release-quality-gate` build adımı fail, web adımları PASS
- Port 5000 meşgul / eski instance yanıt veriyor

### Çözüm (Windows, geliştirme)

```powershell
taskkill /IM DukkanPilot.Web.exe /F
taskkill /IM dotnet.exe /F
```

Ardından yeniden build. Production'da app pool stop/start veya service restart kullanın; `taskkill` tüm dotnet process'lerini kapatır — dikkatli olun.

### Port 5000

- `ASPNETCORE_URLS=http://0.0.0.0:5000` ile smoke script'leri bu adrese bakar.
- Farklı port kullanıyorsanız `-BaseUrl` parametresini güncelleyin.

## Migration status

```powershell
dotnet ef migrations has-pending-model-changes --project src/DukkanPilot.Infrastructure --startup-project src/DukkanPilot.Web
powershell -File .\scripts\db-migration-status.ps1
```

Pending model veya bekleyen migration varsa release dondurulmalı.

## Support urgent ticket kontrolü

- SuperAdmin: `/Admin/Support`
- Urgent/High öncelik + açık status (New, Open, InProgress, WaitingAdmin)
- Release öncesi kritik müşteri ticket'ı varsa deploy ertelenebilir

## Backup / restore safety

- `scripts/db-backup.ps1` + `db-verify-backup.ps1`
- Restore yalnızca test DB veya bilinçli DR senaryosunda
- Bkz. `DATABASE_BACKUP_AND_RECOVERY.md`

## Yavaş route triage

1. Performance smoke çıktısında hangi path FAIL/WARN?
2. Yalnızca public menü mü, tüm site mi?
3. İlk istek mi (cold-start), tekrarlayan mı?
4. SQL Server LocalDB disk/CPU; çalışan başka ağır job var mı?
5. Kod hotfix yerine önce ölçüm tekrarla; local WARN production'da OK olabilir.

## Incident escalation

- `INCIDENT_RESPONSE_RUNBOOK.md`
- `/Admin/Operations` — operasyon checklist
- `/health` — app + database

## İlgili checklist'ler

- `RELEASE_CHECKLIST.md`
- `SMOKE_TEST_CHECKLIST.md`
- `PERFORMANCE_SMOKE_TESTS.md`
- `PERFORMANCE_HARDENING_GUIDE.md`
