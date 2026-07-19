# DukkanPilot Mobile Application Foundation (37B)

## Amaç ve kapsam

37B, 37A ile sağlanan `/api/mobile/v1` API'sini kullanan Android-first Owner/Staff uygulamasının ilk çalışan sürümüdür. UI .NET MAUI Blazor Hybrid, test edilebilir iş mantığı ise MAUI bağımlılığı olmayan `net10.0` sınıf kitaplığıdır.

Kapsam: güvenli login, çoklu işletme seçimi, session restore, bootstrap, dashboard, order list/detail/status update, kitchen polling, logout/logout-all, ProblemDetails ve bağlantı hataları. Push, analytics, crash SDK, SignalR, offline write queue, mağaza signing/yayınlama ve iOS dağıtımı kapsam dışıdır.

## Solution ve projeler

Mobil projeler backend release gate'ini MAUI workload'una bağlamamak için `DukkanPilot.slnx` içine eklenmemiştir.

| Proje | Hedef | Sorumluluk |
|---|---|---|
| `src/DukkanPilot.Mobile.Core` | `net10.0` | DTO, API client, ProblemDetails, session/token, state, polling, route guard |
| `src/DukkanPilot.Mobile` | Android ve Windows | MAUI host, Blazor UI, SecureStorage, connectivity, platform URL |
| `tests/DukkanPilot.Mobile.Tests` | `net10.0` xUnit | Fake HTTP/store/API ile 30 bağımsız test |
| `DukkanPilot.Mobile.slnx` | ayrı solution | Yalnızca mobil projeler |

Mobile ve test projeleri sadece Mobile.Core referansı taşır. Web, EF Core entity veya backend project reference yoktur.

## API eşlemeleri

Gerçek kaynaklar `src/DukkanPilot.Web/Api/Mobile/V1/Contracts`, `Controllers` ve `docs/MOBILE_API_AUTH_FOUNDATION.md` dosyalarıdır.

| Client | Endpoint |
|---|---|
| Login | `POST /api/mobile/v1/auth/login` |
| Refresh | `POST /api/mobile/v1/auth/refresh` |
| Logout / logout-all | `POST /api/mobile/v1/auth/logout` ve `logout-all` |
| Me / bootstrap | `GET /api/mobile/v1/auth/me` ve `/bootstrap` |
| Orders | `GET /orders`, `GET /orders/{id}`, `POST /orders/{id}/status` |
| Kitchen | `GET /kitchen/orders` |
| Dashboard | `GET /dashboard/today` |

Login işletme seçimi dışında tenant BusinessId query/body'ye eklenmez; tenant doğrulanmış bearer claim'inden gelir. Gerçek durumlar `Pending`, `Preparing`, `Completed`, `Cancelled`; geçişler Pending → Preparing/Cancelled ve Preparing → Completed/Cancelled'dır. Backend'de Ready olmadığı için mobil uygulama Ready KPI/geçişi üretmez.

## Base URL

Tek kaynak `ApiEndpointConfiguration` sınıfıdır.

| Debug platform | URL |
|---|---|
| Windows | `http://localhost:5000` |
| Android emulator | `http://10.0.2.2:5000` |

Fiziksel cihazda Hesap > Debug API alanına LAN adresi (ör. `http://192.168.1.25:5000`) yazılır. Bu hassas olmayan URL Preferences içinde saklanır ve restart sonrası uygulanır. Cihaz/bilgisayar aynı ağda olmalı; backend `http://0.0.0.0:5000` dinlemelidir.

Release URL build property ile verilir:

```powershell
dotnet build src\DukkanPilot.Mobile\DukkanPilot.Mobile.csproj `
  -f net10.0-android -c Release `
  -p:DukkanPilotApiBaseUrl=https://api.example.com
```

Release property eksik/HTTP ise startup açık configuration error gösterir. Release manifestinde cleartext yoktur; Debug cleartext `#if DEBUG` ile sınırlıdır. Accept-all certificate handler yoktur, TLS doğrulaması kapatılmaz.

## Token güvenliği ve restore

`ISecureTokenStore` platform sözleşmesi, `MauiSecureTokenStore` MAUI `ISecureStorage` implementasyonudur.

- Refresh token ve refresh expiry yalnızca SecureStorage'dadır.
- Access token yalnızca `SessionState` belleğindedir.
- Token Preferences, UI, URL veya loglara yazılmaz.
- Parola storage'a yazılmaz. Çoklu işletme seçiminde kısa ömürlü `char[]` temizlenir.
- Logout, logout-all, ikinci 401, invalid/reused/expired refresh ve bozuk storage local state'i temizler.
- Güvenli request log formatter yalnızca method/path döndürür.

Startup restore:

1. Release URL yapılandırmasını doğrula.
2. SecureStorage refresh kaydını oku.
3. Token varsa refresh rotation yap.
4. Yeni refresh'i güvenli sakla, access'i memory state'e uygula.
5. Bootstrap çağır.
6. Başarılıysa dashboard; başarısızsa cleanup + login.

Korumalı içerik restore bitmeden render edilmez.

## Refresh/retry

`BearerRefreshHandler` access token'ı merkezi ekler. İlk 401'de `SemaphoreSlim` ile single-flight refresh yapılır; eşzamanlı bekleyenler dönen yeni token'ı kullanır. Request method/URI/header/options/body clone edilir ve en fazla bir retry yapılır. İkinci 401 session'ı temizler. Login, refresh, logout ve logout-all refresh döngüsüne girmez. Refresh için handler dışı raw client kullanılır. Timeout 30 saniyedir ve CancellationToken desteklenir.

## ProblemDetails

Parser `type/title/status/detail/instance/code/traceId/errors` ve business selection `businesses` extension'ını okur. UI stack trace, raw exception veya database mesajı göstermez. Kodlar Türkçe kullanıcı mesajlarına çevrilir; güvenli ekranlarda traceId destek kodu olabilir.

## State ve navigation

DI state'leri: `SessionState`, `BootstrapState`, `DashboardState`, `OrderState` ve `KitchenState`. Global mutable static state yoktur.

Route'lar: `/`, `/login`, `/business-select`, `/dashboard`, `/orders`, `/orders/{id:int}`, `/kitchen`, `/account`. Alt navigasyon Ana Sayfa, Siparişler, Mutfak, Hesap'tır. Auth guard korumalı route'u login'e; authenticated login/root'u dashboard'a yönlendirir.

## Ekran davranışları

- Login: e-posta trim/lowercase, boş alan doğrulama, password toggle, busy/double-submit koruması, invalid credential/rate limit/connection mesajları.
- Business select: yalnızca 409 response'daki işletmeler seçilebilir; seçim bitince geçici parola temizlenir.
- Dashboard: today KPI'ları ve `orders?page=1&pageSize=5` ile son siparişler; tr-TR para, skeleton/empty/error/refresh.
- Orders: server pagination, durum filtresi, refresh, retry, duplicate Id engeli ve “Daha fazla yükle”.
- Detail: gerçek DTO alanları, güvenli 404, server doğrulamalı transition. Optimistic update yok; hata eski state'i korur.
- Kitchen: Pending/Preparing grupları, yaş/ürün/not/sonraki aksiyon; 20 sn polling, overlap engeli, dispose cancellation.
- Account: user/business/role/plan/modules/version; Debug API bilgisi; logout ve confirmation sonrası logout-all. Token gösterilmez.

## Connectivity/offline

Connectivity yardımcı sinyaldir; HTTP hataları ayrıca ele alınır. Global offline banner vardır. Order/kitchen write offline queue'ya alınmaz ve gönderilmez. Hassas order/customer cache dosyası yoktur.

## Android SDK/JDK

Açık kullanıcı onayıyla user-local kurulum:

```powershell
dotnet build src\DukkanPilot.Mobile\DukkanPilot.Mobile.csproj `
  -t:InstallAndroidDependencies -f net10.0-android `
  -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" `
  -p:JavaSdkDirectory="$env:LOCALAPPDATA\Android\jdk" `
  -p:AcceptAndroidSDKLicenses=True `
  -p:AndroidDependencyInstallationTimeout=30
```

Proje özel MSBuild path verilmemişse bu iki dizini koşullu bulur; CI/Visual Studio path'lerini ezmez.

## Build ve run

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-mobile-app.ps1

dotnet restore DukkanPilot.Mobile.slnx
dotnet build src\DukkanPilot.Mobile.Core -c Release
dotnet test tests\DukkanPilot.Mobile.Tests -c Release
dotnet build src\DukkanPilot.Mobile\DukkanPilot.Mobile.csproj -f net10.0-android -c Release
dotnet build src\DukkanPilot.Mobile\DukkanPilot.Mobile.csproj -f net10.0-windows10.0.19041.0 -c Release
```

Android emulator:

```powershell
dotnet build src\DukkanPilot.Mobile\DukkanPilot.Mobile.csproj -t:Run -f net10.0-android -c Debug
```

Windows:

```powershell
dotnet run --project src\DukkanPilot.Mobile\DukkanPilot.Mobile.csproj -f net10.0-windows10.0.19041.0 -c Debug
```

## Manuel API smoke

Backend otomatik başlatılmaz. Geliştirici önce 37A signing key ile çalıştırır:

```powershell
$env:MobileAuth__SigningKey = "LOCAL-ONLY-RANDOM-SECRET-AT-LEAST-32-BYTES"
dotnet run --project src\DukkanPilot.Web\DukkanPilot.Web.csproj --urls http://0.0.0.0:5000
```

Repository development seed Owner hesabı: `owner@dukkanpilot.local` / `Owner123!`. Yalnızca local seed içindir; token terminale/dokümana yazılmaz.

Smoke sırası:

1. Login ve varsa API listesinden business selection.
2. Bootstrap sonrası dashboard user/business/role/KPI.
3. Orders pagination/filter/detail ve güvenli 404.
4. Pending → Preparing, Preparing → Completed update.
5. Kitchen refresh/polling ve sayfadan ayrılınca cancellation.
6. Uygulama restart ile refresh/session restore.
7. Logout sonrası back guard.
8. Logout-all confirmation ve cleanup.
9. Backend kapalıyken bağlantı mesajı; ağ kapalıyken banner ve gönderilmeyen write.

## Testler ve sınırlamalar

30 test: 10 auth/session, 8 HTTP/bearer/ProblemDetails/log, 7 orders, 5 route/polling/offline. Fake handler/store/API/connectivity kullanır; backend, LocalDB, emulator gerekmez.

37B offline-first değildir; read cache/write queue yoktur. Production URL deployment'ta verilmelidir. Keystore/store signing yoktur.

## 37C ve iOS

37C: güvenli device registration/revocation, push opt-in, FCM/APNs abstraction, deep links, background lifecycle ve tenant-safe real-time stratejisi. iOS için Apple Developer hesabı, bundle/provisioning/signing, Keychain/APNs entitlement, privacy manifest, Mac/Xcode build host ve App Store hazırlığı gerekir.