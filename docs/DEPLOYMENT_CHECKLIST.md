# DukkanPilot — Deployment Checklist

Bu liste 27A production readiness çıktısıdır. Secret değerleri repoya yazmayın.

## 1. Build ve lokal doğrulama

```powershell
cd C:\Users\Lenovo\Desktop\DukkanPİlot
dotnet build src\DukkanPilot.Web\DukkanPilot.Web.csproj
```

Development run (localhost smoke):

```powershell
cd C:\Users\Lenovo\Desktop\DukkanPİlot\src\DukkanPilot.Web
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ASPNETCORE_URLS="http://0.0.0.0:5000"
dotnet run --no-launch-profile
```

Kontrol:
- `/` `/Pricing` `/Features` `/Demo`
- `/health`
- `/robots.txt` `/sitemap.xml`
- `/m/demo-kafe`
- `/Account/Login` `/Account/Register`
- Var olmayan URL → profesyonel 404

## 2. Veritabanı

- Connection string’i ortam değişkeni veya Production config ile verin.
- Development’ta uygulama `MigrateAsync` + idempotent `DbSeeder` çalıştırır.
- Production’da migrate/seed otomatik **kapalıdır**; migration’ı deployment pipeline’da bilinçli uygulayın:

```powershell
dotnet ef database update --project src\DukkanPilot.Infrastructure --startup-project src\DukkanPilot.Web
```

- EF model/entity değişikliği olmadan yeni migration üretmeyin.
- Seed destructive değildir; ancak **demo admin / owner şifrelerini Production’da mutlaka değiştirin**.

## 3. Production ortam değişkenleri

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS` / reverse proxy (HTTPS)
- `ConnectionStrings__DefaultConnection=...`
- `AllowedHosts` gerçek domain ile sınırlandırın

Örnek şablon: `src/DukkanPilot.Web/appsettings.Production.example.json`  
Gerçek secret içeren `appsettings.Production.json` commit etmeyin.

## 4. Güvenlik

- Cookie: `HttpOnly`, `SameSite=Lax`, Production’da `SecurePolicy=Always`
- Security headers: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`
- Production: `UseExceptionHandler` + HSTS; stack trace kullanıcıya gösterilmez
- Development: Developer Exception Page + HTTP localhost cookie (`SameAsRequest`)
- Online ödeme / mail / WhatsApp Business API yok (MVP)

## 5. Publish

```powershell
dotnet publish src\DukkanPilot.Web\DukkanPilot.Web.csproj -c Release -o .\publish
```

Reverse proxy (IIS/Nginx) arkasında HTTPS terminate edin. Domain ve sertifika hazır olsun.

## 6. Smoke test (Production)

| URL | Beklenen |
|-----|----------|
| `/health` | JSON `status: ok`, `database: ok` (aksi halde 503) |
| `/` | Landing 200 |
| `/Pricing` `/Features` `/Demo` | 200 |
| `/robots.txt` | text/plain |
| `/sitemap.xml` | application/xml |
| `/m/demo-kafe` | Public menü (login yok) |
| `/Account/Login` `/Account/Register` | 200 |
| `/Business/Dashboard` | Auth gerekli |
| `/Admin/Dashboard` | SuperAdmin |
| `/not-existing-xyz` | Profesyonel 404 |

## 7. Demo / satış gösterimi

- Public demo: `/m/demo-kafe` ve `/Demo`
- Panel şifrelerini landing’de paylaşmayın
- Go-Live Merkezi (`/Business/GoLive`) kurulum anlatımı için kullanın
- Kampanya indirimi ve confirmation/tracking demoda çalışır

## 8. Operasyon notları

- Log: Production’da Warning+ yeterli; hassas veri loglamayın
- Yedek: SQL Server düzenli backup alın
- Seed hesapları: production öncesi şifre rotasyonu
- Health endpoint’i uptime/probe için kullanın (`/health`)
