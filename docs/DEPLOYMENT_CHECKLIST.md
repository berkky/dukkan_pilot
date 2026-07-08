# DukkanPilot — Deployment Checklist

Bu liste production kurulum adımlarını kapsar. Secret değerleri repoya yazmayın.
İlgili dokümanlar: `PRODUCTION_CONFIGURATION.md`, `IIS_DEPLOYMENT_GUIDE.md`, `Kestrel_SERVICE_GUIDE.md`, `RELEASE_CHECKLIST.md`, `SMOKE_TEST_CHECKLIST.md`.

## 1. Ön koşullar

- [.NET ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download) (IIS için) veya sunucuda .NET runtime
- SQL Server (LocalDB production için önerilmez)
- IIS veya reverse proxy + Kestrel
- Domain + geçerli SSL sertifikası
- Windows Server erişimi / App Pool yetkileri

## 2. Build ve lokal doğrulama

```powershell
cd C:\Users\Lenovo\Desktop\DukkanPİlot
dotnet build
powershell -ExecutionPolicy Bypass -File .\scripts\check-release.ps1
```

Development run (localhost smoke):

```powershell
cd C:\Users\Lenovo\Desktop\DukkanPİlot\src\DukkanPilot.Web
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ASPNETCORE_URLS="http://0.0.0.0:5000"
dotnet run --no-launch-profile
```

Ayrı terminalde:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-smoke-tests.ps1 -BaseUrl http://localhost:5000
```

## 3. Production appsettings

1. `src/DukkanPilot.Web/appsettings.Production.example.json` dosyasını kopyalayın → sunucuda `appsettings.Production.json`
2. `ConnectionStrings:DefaultConnection` gerçek SQL connection string ile doldurun
3. `App:PublicBaseUrl` = `https://your-domain.com`
4. `AllowedHosts` gerçek domain(ler) ile sınırlandırın
5. **Gerçek secret içeren `appsettings.Production.json` commit etmeyin**

Alternatif: connection string’i environment variable ile verin: `ConnectionStrings__DefaultConnection`.

## 4. Veritabanı / migration

- Development’ta uygulama `MigrateAsync` + idempotent `DbSeeder` çalıştırır.
- **Production’da migrate/seed otomatik kapalıdır.** Migration’ı bilinçli uygulayın:

```powershell
dotnet ef database update --project src\DukkanPilot.Infrastructure --startup-project src\DukkanPilot.Web
```

veya publish öncesi CI/CD’de aynı komut + Production connection string.

- EF model/entity değişikliği olmadan yeni migration üretmeyin.
- Seed destructive değildir; ancak **demo admin / owner şifrelerini Production’da mutlaka değiştirin**.

## 5. Production environment variables

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS` (doğrudan Kestrel) veya reverse proxy arkasında HTTPS
- `ConnectionStrings__DefaultConnection=...`
- `AllowedHosts` (gerekirse)

## 6. Publish alma

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-release.ps1
```

Çıktı: `artifacts\publish\DukkanPilot.Web`  
(Script migration uygulamaz; secrets içermez.)

Eski manuel komut:

```powershell
dotnet publish src\DukkanPilot.Web\DukkanPilot.Web.csproj -c Release -o .\artifacts\publish\DukkanPilot.Web
```

## 7. IIS kurulumu (özet)

Ayrıntı: `docs/IIS_DEPLOYMENT_GUIDE.md`

- Hosting Bundle kur
- Site + App Pool (No Managed Code)
- Physical path → publish klasörü
- `appsettings.Production.json` sunucuya koy
- Environment Variable: `ASPNETCORE_ENVIRONMENT=Production`
- Database update
- Siteyi başlat / recycle

## 8. Kestrel / Windows Service (özet)

Ayrıntı: `docs/Kestrel_SERVICE_GUIDE.md`

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:ASPNETCORE_URLS="http://127.0.0.1:5000"
dotnet DukkanPilot.Web.dll
```

Reverse proxy HTTPS terminate eder.

## 9. SSL / HTTPS

- Reverse proxy (IIS/Nginx) arkasında HTTPS terminate edin
- Production’da uygulama HSTS + HTTPS redirection kullanır
- Cookie `SecurePolicy=Always` (Production)

## 10. Güvenlik

- Cookie: `HttpOnly`, `SameSite=Lax`, Production’da `SecurePolicy=Always`
- Security headers: nosniff, SAMEORIGIN, Referrer-Policy, Permissions-Policy
- Production: `UseExceptionHandler` + HSTS; stack trace kullanıcıya gösterilmez
- Online ödeme / mail / WhatsApp Business API yok (MVP)
- Panel şifrelerini landing/demo sayfasında paylaşmayın

## 11. Health / SEO kontrol

| URL | Beklenen |
|-----|----------|
| `/health` | JSON `status: ok`, `database: ok` (aksi halde 503) |
| `/robots.txt` | text/plain |
| `/sitemap.xml` | application/xml |

## 12. Go-live smoke

| URL | Beklenen |
|-----|----------|
| `/` | Landing 200 |
| `/Pricing` `/Features` `/Demo` | 200 |
| `/m/demo-kafe` | Public menü (login yok) |
| `/Account/Login` `/Account/Register` | 200 |
| `/Business/Dashboard` | Auth gerekli |
| `/Admin/Dashboard` | SuperAdmin |
| `/Business/DemoCenter` | Auth |
| `/Admin/SalesCenter` | SuperAdmin |
| Var olmayan URL | Profesyonel 404 |

Tam liste: `docs/SMOKE_TEST_CHECKLIST.md`

## 13. Log ve hata

- Production’da Warning+ yeterli; hassas veri loglamayın
- IIS stdout log geçici açılabilir (troubleshooting); kalıcı açık bırakmayın
- `/Error/500` kullanıcıya stack vermez

## 14. Rollback (kısa)

- Önceki publish klasörünün yedeğini tutun
- DB migration rollback dikkatli olmalı; mümkünse forward-only
- Yedek/backup prosedürü 29B’de genişletilecek

## 15. Demo / satış

- Public demo: `/m/demo-kafe` ve `/Demo`
- Panel: `/Business/DemoCenter`, Admin: `/Admin/SalesCenter`
- Seed hesapları production’da rotasyon
