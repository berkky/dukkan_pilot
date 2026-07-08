# DukkanPilot — Smoke Test Checklist

Uygulama çalışırken manuel veya `scripts/run-smoke-tests.ps1` ile doğrulayın.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-smoke-tests.ps1 -BaseUrl http://localhost:5000
```

## Support (auth redirect)

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/Business/Support` | Anon | 302 | Auth redirect |
| `/Business/Support/Create` | Anon | 302 | Auth redirect |
| `/Admin/Support` | Anon | 302 | SuperAdmin gerekli |

## Performance smoke

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-performance-smoke.ps1 -BaseUrl http://localhost:5000
```

| Kontrol | Beklenen |
|---------|----------|
| Public routes | 200 + süre tablosu |
| WARN | WarningMs üstü (release bloklamaz) |
| FAIL | FailMs üstü veya non-200 → exit 1 |
| Cold-start | İlk istek yavaş olabilir; bkz. `PERFORMANCE_SMOKE_TESTS.md` |

`release-quality-gate.ps1` web checks açıkken performance smoke'u da çalıştırır (`-SkipPerformanceSmoke` ile atlanabilir).

## Public

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/` | Anon | 200 | Landing |
| `/Pricing` | Anon | 200 | Plan kartları |
| `/Features` | Anon | 200 | |
| `/Demo` | Anon | 200 | Şifre yok; `/m/demo-kafe` CTA |
| `/Trust` | Anon | 200 | Güven Merkezi; taslak uyarısı |
| `/Privacy` | Anon | 200 | Gizlilik taslağı |
| `/Terms` | Anon | 200 | Kullanım şartları taslağı |
| `/Kvkk` | Anon | 200 | KVKK aydınlatma taslağı |
| `/Cookies` | Anon | 200 | Çerez politikası |
| `/DataProcessing` | Anon | 200 | Veri işleme yaklaşımı |
| `/m/demo-kafe` | Anon | 200 | Menü + sepet; cookie notice |
| `/health` | Anon | 200 JSON | DB fail → 503 |
| `/robots.txt` | Anon | 200 | Legal allow |
| `/sitemap.xml` | Anon | 200 | Trust/Legal URL’ler dahil |
| Yok URL | Anon | Profesyonel 404 | |

## Account

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/Account/Login` | Anon | 200 | |
| `/Account/Register` | Anon | 200 | |
| `/Account/ForgotPassword` | Anon | 200 | |
| Login POST | User | Dashboard redirect | Cookie auth |
| AccessDenied Admin | Owner | SuperAdmin mesajı | ReturnUrl `/Admin*` |

## Business (Owner/Staff)

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/Business/Dashboard` | Auth | 200 | Auth yok → 302 |
| `/Business/DemoCenter` | Auth | 200 | Readiness + adımlar |
| `/Business/GoLive` | Auth | 200 | |
| `/Business/Orders/Kitchen` | Auth+sub | 200 | |
| `/Business/Reports` | Auth+sub | 200 | |
| `/Business/AuditLogs` | Auth+sub* | 200 | Gate policy mevcut |
| `/Business/Notifications` | Auth | 200 | Gate dışı |
| MarkRead | Auth | Okundu | Antiforgery |

\*AuditLogs şu an subscription gate altında; Notifications dışı.

## Admin (SuperAdmin)

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/Admin/Dashboard` | SuperAdmin | 200 | Operations CTA |
| `/Admin/Operations` | SuperAdmin | 200 | Salt okunur; secret yok; auth yok → 302 |
| `/Admin/SalesCenter` | SuperAdmin | 200 | |
| `/Admin/Onboarding` | SuperAdmin | 200 | Auth yok → 302 |
| `/Admin/Onboarding/Details/{id}` | SuperAdmin | 200 | |
| `/Admin/CustomerSuccess` | SuperAdmin | 200 | Auth yok → 302 |
| `/Admin/CustomerSuccess/Details/{id}` | SuperAdmin | 200 | |
| `/Admin/Businesses` | SuperAdmin | 200 | |
| `/Admin/AuditLogs` | SuperAdmin | 200 | |
| `/Admin/Notifications` | SuperAdmin | 200 | |
| SalesCenter | Owner | AccessDenied | |
| `/Admin/Billing` | SuperAdmin | 200 | Auth yok → 302 |
| `/Admin/Billing/Payments` | SuperAdmin | 200 | Auth yok → 302 |

## Billing (BusinessOwner)

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/Business/Billing/Invoices` | Owner | 200 | Auth yok → 302; read-only; resmi belge değildir uyarısı |
| `/Business/Billing/Payments` | Owner | 200 | Auth yok → 302; read-only |

## Help Center

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/Help` | Anon | 200 | Public yardım |
| `/Help/nedir` | Anon | 200 | Makale |
| `/Business/HelpCenter` | Auth | 200 | Owner+Staff |
| `/Admin/HelpCenter` | SuperAdmin | 200 | Auth yok → 302 |

## ROI / Değer Hesaplayıcı

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/RoiCalculator` | Anon | 200 | Disclaimer görünür |
| `/ValueCalculator` | Anon | 200 | Alias |
| `/Business/ValueCalculator` | Auth | 200 | Auth yok → 302 |
| `/Admin/ValueCalculator` | SuperAdmin | 200 | Auth yok → 302 |

## Demo Packs / Vertical Demos

| URL | Rol | Beklenen | Not |
|-----|-----|----------|-----|
| `/DemoPacks` | Anon | 200 | Galeri |
| `/m/demo-kafe` | Anon | 200 | Mevcut demo bozulmaz |
| `/m/demo-tatlici` | Anon | 200 | Yeni demo |
| `/m/demo-burgerci` | Anon | 200 | Yeni demo |
| `/m/demo-restoran` | Anon | 200 | Yeni demo |
| `/m/demo-nargile` | Anon | 200 | Yeni demo |

## Sipariş / kampanya

| Adım | Beklenen |
|------|----------|
| Sepete ürün | Client sepet |
| 100₺ üzeri | Auto-apply %10 (demo) |
| POST order | Confirmation + tracking |
| preview-order | DB yazmaz / NewOrder bildirim yok |
| Kitchen Complete | 9C sadakat bozulmaz |

## Audit / Notification

| Kontrol | Beklenen |
|---------|----------|
| Public order | Audit + NewOrder notification |
| Business list | Tenant filtreli |
| Admin list | Platform geneli |
| Hassas veri | Token/şifre metadata’da yok |

## Demo / Sales / Onboarding / Success

| URL | Beklenen |
|-----|----------|
| `/Demo` | Satış akışı |
| `/Business/DemoCenter` | Skor / CTA + onboarding kart |
| `/Business/Onboarding` | Kurulum skoru + checklist (auth yok → 302) |
| `/Business/Success` | Health score + churn/expansion + recommendations (auth yok → 302) |
| `/Admin/SalesCenter` | Demo-ready + onboarding-ready list |
| `/Admin/Onboarding` | Board + Sales handoff |
| `/Admin/CustomerSuccess` | KPI + risk/growth board |
| `/Admin/CustomerSuccess/Details/{id}` | Risk / recommendation / usage / subscription |
| `/Admin/SalesRequests/Details/{id}` | BusinessId varsa onboarding mini kart |
| `/Admin/SalesRequests/Details/{id}` | BusinessId varsa customer success mini kart |

## Security / SEO

| Kontrol | Beklenen |
|---------|----------|
| Security headers | nosniff, frame, referrer, permissions |
| HSTS | Production |
| `/robots.txt` | Admin/Business disallow mantığı |
| Public menu polish | `MOBILE_WEB_POLISH_CHECKLIST.md` |
