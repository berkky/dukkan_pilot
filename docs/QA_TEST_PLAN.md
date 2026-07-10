# QA Test Plan

## Amaç

Yeni özellik eklenince eski akışlar bozuldu mu sorusuna sistemli yanıt vermek.

Bu katman:
- unit/integration test projesi eklemez
- script tabanlı smoke + regression + release gate yaklaşımı kullanır
- kritik akışları UAT scriptleriyle manuel doğrular

## Test seviyeleri

- **Smoke**: public/legal/system + auth redirect kontrolü
- **Regression**: kritik modüller ve riskli akışlar
- **UAT**: ilk müşteri teslimi / demo / onboarding / success kabul testleri
- **Release gate**: tek komutla build + release check + migration status + web checks

## Kapsam

- Public: landing/pricing/features/demo/trust
- Legal: privacy/kvkk/terms/cookies/data processing
- Sales: request demo/plan
- Public menu: `/m/{slug}` (demo-kafe) + mobile polish checklist
- Table service: `/m/demo-kafe?table=TBL-KAFE-1` — `TABLE_SERVICE_UAT_SCRIPT.md`
- Order flow: cart → WhatsApp order → confirmation/tracking → kitchen
- Business: dashboard, onboarding, success, golive, demos
- Admin: dashboard, salescenter, onboarding, customersuccess, operations, quality
- Billing: admin invoice/payment operasyonu + business ledger (read-only)
- Help Center: `/Help`, `/Business/HelpCenter`, `/Admin/HelpCenter` + contextual links
- ROI Calculator: `/RoiCalculator` 200; `/Business/ValueCalculator` `/Admin/ValueCalculator` auth yok → 302; POST sonuç + disclaimer
- Demo Packs: `/DemoPacks` 200; `/m/demo-*` 200; `check-public-demo-readiness.ps1` multi-slug
- System: health, robots, sitemap, security headers
- Performance/reliability: `check-performance-smoke.ps1`, DLL lock troubleshooting (`RELIABILITY_RUNBOOK.md`)

## Scriptler neyi test eder?

- `scripts/run-smoke-tests.ps1`: HTTP 200/302 smoke + grup bazlı rapor
- `scripts/check-security-headers.ps1`: nosniff / frame / referrer / permissions-policy
- `scripts/check-seo-endpoints.ps1`: robots + sitemap (private URL yok)
- `scripts/check-public-demo-readiness.ps1`: demo slugs (tekil veya çoklu) read-only demo readiness
- `scripts/check-performance-smoke.ps1`: HTTP response time smoke (public routes, WARN/FAIL ms)
- `scripts/release-quality-gate.ps1`: build + check-release + migration status + opsiyonel web checks + performance smoke

## Manuel kalan kritik testler

- Public order POST (doğrulama + WhatsApp metni)
- Kitchen status ilerletme (Pending → Preparing → Completed)
- Tracking/confirmation sayfaları
- Mobile polish: hero/nav/cart bar/drawer/form/tracking (bkz. `MOBILE_WEB_POLISH_CHECKLIST.md`)
- Sales request form legal checkbox’ları
- Billing: invoice create, payment record, cancel, business ledger görüntüleme (bkz. `docs/MANUAL_PAYMENT_UAT_SCRIPT.md`)

## Kritik risk alanları

- Auth/cookie ve role erişimleri
- Tenant filter (BusinessId claim)
- Public order zinciri
- Campaign discount engine
- Subscription gate / plan limit
- Audit/Notification fail-safe davranışı
