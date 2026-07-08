# DukkanPilot — IIS Deployment Guide

Windows Server + IIS + ASP.NET Core Hosting Bundle.

## 1. Hosting Bundle

1. Sunucuya [.NET ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download) kurun (uygulama runtime’ı ile uyumlu major).
2. Kuruluysa IIS’i reset edin: `net stop was /y` && `net start w3svc` (maintenance penceresinde).

## 2. Site oluşturma

1. IIS Manager → Sites → Add Website
2. Site name: `DukkanPilot`
3. Physical path: örn. `C:\inetpub\DukkanPilot` (publish içeriği)
4. Binding: https + certificate / host name

## 3. App Pool

- .NET CLR version: **No Managed Code**
- Pipeline: Integrated
- Identity: ApplicationPoolIdentity veya özel servis hesabı (SQL erişimi için ayarlayın)
- Idle timeout: ihtiyaca göre (sık cold start istemiyorsanız artırın)

## 4. Publish kopyalama

Lokal:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-release.ps1
```

`artifacts\publish\DukkanPilot.Web\*` → sunucu physical path.

## 5. Production config

Physical path içine `appsettings.Production.json` koyun (example’dan üretin).  
veya App Pool / site Environment Variables:

- `ASPNETCORE_ENVIRONMENT` = `Production`
- `ConnectionStrings__DefaultConnection` = …

## 6. Database

Sunucu veya CI makineden:

```powershell
$env:ConnectionStrings__DefaultConnection="..."
dotnet ef database update --project src\DukkanPilot.Infrastructure --startup-project src\DukkanPilot.Web
```

Production uygulama start’ta migrate etmez.

## 7. Restart

- Site Start
- App Pool Recycle
- `/health` kontrol

## 8. Troubleshooting

| Belirti | Olası neden | Aksiyon |
|---------|-------------|---------|
| 500.30 | Startup crash | stdoutLogEnabled geçici aç; Event Viewer |
| DB hatası | Connection string / firewall | SQL erişim, Encrypt/TrustServerCertificate |
| Eski şema | Migration uygulanmamış | `database update` |
| 401/302 loop | Cookie Secure / HTTPS | HTTPS binding, Secure cookie |
| Static 404 | Path / izin | IIS_IUSRS read |
| Port conflict | Binding | Site bindings |

### Geçici stdout log

`web.config` içinde `stdoutLogEnabled="true"` ve log klasörü yazılabilir olsun.  
Sorun çözülünce **kapatın** (disk doldurur / hassas veri riski).

## 9. Güvenlik

- `appsettings.Production.json` ACL ile kısıtlı
- Demo şifreleri production’da değiştir
- Panel credential public sayfada yok
