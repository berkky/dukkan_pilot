# DukkanPilot — Migration Runbook

Bu runbook **yeni migration üretme zorunluluğu getirmez**. Model değişmedikçe migration oluşturmayın.
İlgili: `scripts/db-generate-migration-script.ps1`, `db-migration-status.ps1`, `db-backup.ps1`, `DATABASE_BACKUP_AND_RECOVERY.md`.

## 1. Migration üretmeden / uygulamadan önce

- [ ] `git status` temiz veya bilinçli değişiklikler
- [ ] `dotnet build` → 0 hata 0 uyarı
- [ ] `powershell -File .\scripts\db-migration-status.ps1` (list + pending model)
- [ ] `powershell -File .\scripts\check-release.ps1`
- [ ] **FULL backup** alındı ve verify geçti
- [ ] Rollback planı hazır (önceki publish + `.bak`)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\db-backup.ps1 `
  -ServerInstance "(localdb)\MSSQLLocalDB" -DatabaseName "DukkanPilotDb"
```

## 2. Idempotent SQL script üretimi

Sadece SQL üretir; **hiçbir DB’ye uygulanmaz**.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\db-generate-migration-script.ps1
```

Çıktı: `artifacts\sql\DukkanPilot_migrations_idempotent_yyyyMMdd_HHmmss.sql`

- `--idempotent`: zaten uygulanmış migration’ları güvenle atlayan koşullu script
- İsteğe bağlı aralık: `-FromMigration X -ToMigration Y`
- Üretilen SQL’i review edin (DROP/data loss var mı?)

## 3. Production’a uygulama

### Küçük / tek müşteri kurulum

Geliştirme benzeri bilinçli update (Production connection string ile, **app offline** iken):

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
# Connection string Production config veya env üzerinden
dotnet ef database update --project src\DukkanPilot.Infrastructure --startup-project src\DukkanPilot.Web
```

### Gerçek / paylaşımlı production (önerilen)

1. Idempotent SQL üret  
2. DBA/lead review  
3. Bakım penceresi + backup  
4. SQL Server’da script uygula (SSMS / sqlcmd)  
5. App start + smoke  

**Development** ortamında uygulama start’ta `MigrateAsync` çalışır; **Production’da otomatik migrate/seed kapalıdır** (`Program.cs`).

## 4. Rollback

| Yaklaşım | Risk | Not |
|----------|------|-----|
| `dotnet ef database update <previous>` | Yüksek | Down migration her zaman güvenli/yazılmamış olabilir |
| Forward fix (yeni migration) | Orta | Tercih edilen yazılım düzeltmesi |
| **Backup restore** | Kontrollü | Veri kaybı penceresi yedek anına kadar; en güvenli DR |

Data loss içeren migration (kolon drop vb.) için tek gerçekçi geri dönüş: **backup restore**.

## 5. Migration sonrası doğrulama

- [ ] `/health` → status ok, database ok  
- [ ] `run-smoke-tests.ps1`  
- [ ] Login + Business Dashboard  
- [ ] Public `/m/{slug}` + test sipariş  
- [ ] Kitchen status + (varsa) sadakat  
- [ ] AuditLogs / Notifications görünür  
- [ ] Admin Operations Center migration sayıları tutarlı  

## 6. Pending model changes

`has-pending-model-changes` “changes” diyorsa:

1. Entity değişikliğini bilinçli mi kontrol et  
2. Yanlışlıkla mı değişti — geri al  
3. Gerçekten gerekiyorsa (ayrı onaylı aşama) migration üret  

29B kapsamında **yeni migration oluşturulmaz**.
