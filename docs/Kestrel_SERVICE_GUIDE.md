# DukkanPilot — Kestrel Service Guide

IIS kullanmadan veya reverse proxy arkasında Kestrel ile çalıştırma (Windows odaklı).

## 1. Publish

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-release.ps1
```

Publish dizini: `artifacts\publish\DukkanPilot.Web`

## 2. Manuel çalıştırma

```powershell
cd C:\path\to\publish\DukkanPilot.Web
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:ASPNETCORE_URLS="http://127.0.0.1:5000"
# isteğe bağlı:
# $env:ConnectionStrings__DefaultConnection="..."
dotnet DukkanPilot.Web.dll
```

## 3. Reverse proxy

- Kestrel iç ağda (örn. `127.0.0.1:5000`) dinlesin
- IIS ARR / Nginx Windows / başka reverse proxy HTTPS ile dışarı açsın
- `AllowedHosts` gerçek domain

## 4. Windows Service (özet)

Seçenekler (birini seçin; yeni NuGet zorunlu değil):

- `sc.exe` ile custom wrapper / NSSM
- Scheduled Task (开机)
- IIS’e taşımak genelde daha basit (Hosting Bundle)

Servis hesabının:

- Publish klasörüne okuma
- SQL’e ağ erişimi
- Log klasörüne yazma (varsa)

hakları olmalı.

## 5. Restart / check

```powershell
# süreç
Get-Process -Name "dotnet" | Where-Object { $_.Path -like "*DukkanPilot*" }
# health
Invoke-WebRequest http://127.0.0.1:5000/health -UseBasicParsing
```

## 6. Logging

- Console / Event log ihtiyaca göre
- Production log seviyesi Warning+
- Token/şifre loglama yok

## 7. Migration

Kestrel process migration uygulamaz (Production).  
Deploy pipeline veya manuel `dotnet ef database update`.

## 8. Smoke

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-smoke-tests.ps1 -BaseUrl http://127.0.0.1:5000
```
