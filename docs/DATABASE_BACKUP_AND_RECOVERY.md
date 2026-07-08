# DukkanPilot — Database Backup and Recovery

Secret ve gerçek connection string bu dokümana yazılmaz. Scriptler parametre ile çalışır.

İlgili: `scripts/db-backup.ps1`, `db-verify-backup.ps1`, `db-restore-test.ps1`, `MIGRATION_RUNBOOK.md`, `INCIDENT_RESPONSE_RUNBOOK.md`.

## 1. Backup stratejisi

| Ne zaman | Ne | Not |
|----------|-----|-----|
| Her release öncesi | FULL backup | Publish/migration’dan **önce** |
| Kritik migration öncesi | FULL backup + VERIFYONLY | Rollback için tek güvenli yol |
| Günlük (production) | FULL veya différential (SQL Agent) | LocalDB için Agent yok; manuel veya Task Scheduler |
| Haftalık | FULL + restore testi | `db-restore-test.ps1` ile test DB |
| Disk dolmadan önce | Eski `.bak` arşivle / sil (policy) | Repo’ya koyma |

Minimum ilk müşteri operasyonu:

1. Her release öncesi backup  
2. Her migration öncesi backup  
3. Haftalık bir restore testi (test DB)

## 2. Backup script

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\db-backup.ps1 `
  -ServerInstance "(localdb)\MSSQLLocalDB" `
  -DatabaseName "DukkanPilotDb"
```

- Çıktı: `artifacts\db-backups\DukkanPilotDb_yyyyMMdd_HHmmss.bak`
- `CHECKSUM` kullanılır; varsayılan sonra `RESTORE VERIFYONLY`
- `-UseCompression` isteğe bağlı (Express/LocalDB desteklemeyebilir)
- `sqlcmd` yoksa script anlaşılır hata verir; SSMS ile de alınabilir
- Script secret okumaz / DB silmez

Production örneği (placeholder):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\db-backup.ps1 `
  -ServerInstance "YOUR_SQL_SERVER" `
  -DatabaseName "DukkanPilotDb" `
  -BackupDirectory "D:\Backups\DukkanPilot"
```

## 3. Verify

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\db-verify-backup.ps1 `
  -ServerInstance "(localdb)\MSSQLLocalDB" `
  -BackupFile ".\artifacts\db-backups\DukkanPilotDb_yyyyMMdd_HHmmss.bak"
```

## 4. Test restore (güvenli)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\db-restore-test.ps1 `
  -ServerInstance "(localdb)\MSSQLLocalDB" `
  -BackupFile ".\artifacts\db-backups\DukkanPilotDb_yyyyMMdd_HHmmss.bak"
```

- Varsayılan hedef: `DukkanPilot_RestoreTest_yyyyMMddHHmmss` (production DB değil)
- `RestoreDatabaseName=DukkanPilotDb` → **Force olmadan reddedilir**
- `-Force` ile ana DB üzerine yazmak tehlikelidir; yalnızca bilinçli DR senaryosu
- Script **otomatik DROP yapmaz**; cleanup komutu çıktıda not edilir
- Restore sonrası `DBCC CHECKDB` çalıştırılır

## 5. Saklama ve güvenlik

- `.bak` dosyaları **git’e girmez** (`.gitignore`: `*.bak`, `artifacts/db-backups/`)
- Backup klasörüne NTFS/ACL ile sadece operasyon ekibi erişsin
- Backup’larda müşteri PII vardır; şifrelenmiş disk / offsite kopya düşünün
- Production DB üzerine yanlışlıkla restore etmeyin — önce test DB

## 6. LocalDB vs SQL Server

| | LocalDB | SQL Server |
|--|---------|------------|
| Development | Varsayılan DukkanPilot | Mümkün |
| Production | Önerilmez | Tercih |
| sqlcmd | Visual Studio / tooling ile | Command Line Utilities |
| Agent jobs | Yok | Günlük backup için ideal |

## 7. Production restore (gerçek DR)

1. Uygulamayı offline et (app pool / service stop)  
2. Son bilinen iyi `.bak`’i VERIFYONLY ile doğrula  
3. Mümkünse önce **test restore DB**  
4. Onay sonrası production restore (SSMS veya kontrollü script; `db-restore-test -Force` yalnızca son çare)  
5. `/health`, login, `/m/{slug}`, test sipariş  
6. Incident notunu `Admin/AuditLogs` / dahili ticket’a yaz  

Detay: `INCIDENT_RESPONSE_RUNBOOK.md`.
