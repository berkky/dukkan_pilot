# DukkanPilot — Production Configuration

## Environment

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5000"   # örnek; IIS/ARR arkasında sık kullanılır
```

IIS App Pool → Environment Variables veya `web.config` aspNetCore env.

## Connection string

Öncelik sırası (ASP.NET Core standart):

1. Environment: `ConnectionStrings__DefaultConnection`
2. `appsettings.Production.json` (sunucuda; **commit yok**)
3. User secrets / vault (ileride)

Placeholder şablon: `src/DukkanPilot.Web/appsettings.Production.example.json`

## App section (örnek)

```json
"App": {
  "PublicBaseUrl": "https://your-domain.com",
  "CompanyName": "DukkanPilot",
  "SupportEmail": "support@your-domain.com"
}
```

Not: Kod bu anahtarları henüz zorunlu okumuyorsa bile deploy dokümantasyonu ve ilerideki absolute URL ihtiyaçları için tanımlıdır.

## Secret yönetimi

- Gerçek şifre/connection string **repoya yazılmaz**
- `appsettings.Production.json` sunucu-local
- `.gitignore` zaten `artifacts/`, `publish/`, `*.log` kapsar
- Demo sayfasında panel şifresi gösterme

## Cookie / security (27A)

| Ortam | Cookie Secure | Exception | Migrate/Seed |
|-------|---------------|-----------|--------------|
| Development | SameAsRequest | Developer page | Otomatik |
| Production | Always | `/Error/500` + HSTS | Kapalı |

Headers: nosniff, SAMEORIGIN, Referrer-Policy, Permissions-Policy.

## HTTPS

- Prefer reverse proxy terminate SSL
- Production `UseHttpsRedirection` + HSTS aktif

## DataProtection

Uygulama `AddDataProtection()` kullanır (şifre reset + public order tracking token).

Production notları (bu aşamada yalnızca doküman; yeni NuGet yok):

- App pool recycle / çok sunuculu kurulumda key persistence düşünülmeli
- Key’ler kaybolursa mevcut reset/tracking tokenları geçersiz olur
- Tek makine IIS için varsayılan local key store çoğu senaryoda yeterlidir; farm ise paylaşımlı key ring planlayın

## Logging

Production example: Default / AspNetCore / EF → Warning.  
Hassas veri (şifre, token, PII) loglanmamalı.

## Seed davranışı

- **Development:** Migrate + `DbSeeder` (idempotent enrich dahil)
- **Production:** Otomatik seed **yok**
- İlk kurulumda bilinçli migration + gerekirse kontrollü seed; ardından demo şifrelerini değiştirin

## Static assets

Publish çıktısı `wwwroot` içerir. IIS static file izinleri doğru olsun.

## Domain / PublicBaseUrl

QR menü linkleri request host üzerinden de üretilir; public base URL’yi doğru domain ile tutun.

## Backup

SQL düzenli backup alın. Detaylı backup/restore runbook 29B’de genişletilecek.
