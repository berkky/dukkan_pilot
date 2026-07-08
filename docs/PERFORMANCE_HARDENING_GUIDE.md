# DukkanPilot — Performance Hardening Guide

## Amaç

35B aşaması yeni özellik eklemez; mevcut SaaS modüllerinde sorgu ve liste performansını güvenli şekilde iyileştirir.

## Read-only AsNoTracking politikası

- **Kullan:** Liste, detay (salt okunur), dashboard özet, export önizleme, public menü GET, polling summary JSON.
- **Kullanma:** `SaveChanges` öncesi güncellenecek entity, sipariş durumu değişimi, destek yanıtı, fatura/tahsilat kaydı, kampanya/ürün POST akışları.
- Projection (`Select` → DTO/ViewModel) sorgularında tracking etkisi düşüktür; yine de read-only path'lerde `AsNoTracking()` tercih edilir.

## Pagination / default limit politikası

- Büyük listelerde varsayılan **100** kayıt (`Orders`, `Customers`, Admin `Businesses` görünümü).
- Mutfak modu kolonları **50** sipariş ile sınırlı (operasyon ekranı).
- Admin destek/sales/billing listeleri mevcut `Take(200–300)` limitlerini korur.
- Audit/Notification sayfalıdır (`pageSize` 50–100); davranış değiştirilmez.
- Export endpoint'leri bilinçli olarak tam veri çekebilir; UI listesi ile karıştırılmaz.

## Dashboard summary prensipleri

- Platform KPI: `CountAsync` / `SumAsync` — tüm `Orders` tablosunu belleğe çekme.
- İşletme bazlı istatistik: `GroupBy` + SQL aggregate.
- Onboarding/Success kartları: yalnızca **aktif işletmeler** için hesaplanır (dashboard).
- Support/Sales özetleri: `CountAsync` ile DB-side sayım.

## Public menü performansı

- `/m/{slug}`: işletme, kategori, ürün, kampanya, ödül sorguları `AsNoTracking`.
- Fiyat doğrulama POST akışında tracking korunur (sipariş oluşturma).
- Demo pack listesi static cache (`DemoPackHelper` lazy default packs).

## N+1 risk kontrolü

- Admin board'larda `BuildForBusinessesAsync` hâlâ işletme başına sorgu üretir; büyük tenant sayısında ileride batch özet gerekebilir.
- CRM müşteri listesi tüm müşteri+sipariş yükler; filtre KPI'ları doğru kalsın diye export dışında liste `Take(100)`.
- `Include` yalnızca gerçekten navigation gerektiğinde.

## Local smoke vs gerçek load test

| Tür | Araç | Ne ölçer? |
|-----|------|-----------|
| HTTP smoke | `run-smoke-tests.ps1` | 200/302, route varlığı |
| Performance smoke | `check-performance-smoke.ps1` | Tek istek süresi (ms), WARN/FAIL eşiği |
| Demo readiness | `check-public-demo-readiness.ps1` | HTML sinyalleri |
| Load test | **Bu aşamada yok** | k6/JMeter/Locust |

Performance smoke **benchmark değildir**; cold-start ve local dev makine gürültüsü içerebilir.

## Bu aşamada yapılmayanlar

- Cache / Redis / distributed cache
- Harici APM (Application Insights, Datadog, vb.)
- Yeni index migration
- Background job
- Büyük repository refactor
- Identity / SignalR / yeni NuGet

## İlgili dosyalar

- `scripts/check-performance-smoke.ps1`
- `scripts/release-quality-gate.ps1`
- `docs/PERFORMANCE_SMOKE_TESTS.md`
- `docs/RELIABILITY_RUNBOOK.md`
