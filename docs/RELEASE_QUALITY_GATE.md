# Release Quality Gate

## Nedir?

Release öncesi tek komutla kalite kapısından geçmek için kullanılan doğrulama setidir.

Amaç:
- build kırık mı?
- EF model değişmiş mi?
- migration durumu güvenli mi?
- public/auth redirect/SEO/security headers/demolar çalışıyor mu?

## Komut

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\release-quality-gate.ps1 -BaseUrl http://localhost:5000
```

Opsiyonlar:

- `-SkipWebChecks`: web testlerini atlar
- `-SkipMigrationScript`: migration script üretimini atlar

## PASS kriterleri

- `dotnet build` 0 hata/uyarı
- `check-release.ps1` PASS
- `db-migration-status.ps1` PASS
- (opsiyonel) `db-generate-migration-script.ps1` PASS
- App çalışıyorsa:
  - smoke PASS
  - security headers PASS
  - SEO endpoints PASS
  - demo readiness PASS

## FAIL olursa

- Önce en küçük fix ile ilerle (hot refactor yok)
- Public order / auth / tenant / subscription gate gibi kritik alanlarda ekstra smoke koş
- Gerekirse deploy’u durdur ve issue/bug template ile raporla

## App çalışmıyorsa

Gate web kontrollerini skip eder ve “app not reachable” notu düşer.

## Quality Center

Admin ekranı: `/Admin/Quality`  
Bu ekran **script çalıştırmaz**, sadece durum ve komutları gösterir.

