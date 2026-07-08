# DukkanPilot — Operational Security Checklist

Release ve ilk müşteri öncesi kontrol listesi. Gerçek secret değerleri buraya yazılmaz.

## Secrets & config

- [ ] Repo’da gerçek connection string / parola yok
- [ ] `appsettings.Production.json` git ignore (`**/appsettings.Production.json`)
- [ ] Production example yalnızca placeholder içerir
- [ ] Environment variable veya sunucu-local config tercih edilir
- [ ] `.env` commitlenmemiş

## Backup & SQL artifacts

- [ ] `*.bak`, `artifacts/db-backups/`, `artifacts/sql/` git’te değil
- [ ] Backup klasörü ACL ile kısıtlı
- [ ] Release/migration öncesi backup alışkanlığı

## Auth & demo

- [ ] Admin / Owner şifreleri public Landing/Demo’da yok
- [ ] Demo hesaplar production’da değiştirildi veya kapatıldı
- [ ] Cookie auth + PasswordHelper bozulmadı
- [ ] HTTPS production’da zorunlu; cookie SecurePolicy=Always

## DataProtection & tokens

- [ ] Production’da DataProtection key persistence planlandı (cookie/reset token sürekliliği)
- [ ] Key ring kaybında tüm oturumlar düşer — deploy sonrası beklenen davranış bilinir
- [ ] Tracking token’lar access log’a yazılmamalı (PII)

## Application data hygiene

- [ ] Audit / Notification metadata’sında parola, connection string, ham kart verisi yok
- [ ] Soft delete (`IsActive=false`) korunuyor
- [ ] Tenant `BusinessId` claim izolasyonu bozulmadı

## SQL & sunucu

- [ ] App SQL kullanıcısı least privilege (db_owner yerine sınırlı yetki hedefi)
- [ ] RDP / SSH / firewall sadece yönetici erişimi
- [ ] SQL portu internete açık değil (mümkünse)
- [ ] Hosting Bundle / runtime güncel patch

## Process

- [ ] Güncelleme öncesi backup
- [ ] Idempotent migration SQL review (büyük ortam)
- [ ] Smoke + `/Admin/Operations` kontrol
- [ ] Incident runbook biliniyor

İlgili: `PRODUCTION_CONFIGURATION.md`, `DATABASE_BACKUP_AND_RECOVERY.md`, `INCIDENT_RESPONSE_RUNBOOK.md`.
