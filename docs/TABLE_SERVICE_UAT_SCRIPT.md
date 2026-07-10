# Table Service / Masa QR — UAT Script

## Önkoşul

- Migration `AddTableServiceMode` uygulandı
- Demo seed masaları mevcut (`TBL-KAFE-1` vb.)
- Owner: `owner@dukkanpilot.local` / `Owner123!`

---

## 1. Masa listesi (Owner)

- [ ] `/Business/Tables` — 200
- [ ] demo-kafe için en az 3 masa görünür
- [ ] QR link kopyala çalışır

## 2. Masa oluşturma (Owner)

- [ ] `/Business/Tables/Create` — yeni masa (ör. “Teras 5”)
- [ ] `PublicCode` otomatik üretildi
- [ ] Liste + QR sayfası açılır

## 3. Staff yetkisi

- [ ] Staff ile `/Business/Tables` — 200 (liste)
- [ ] Staff ile `/Business/Tables/Create` — AccessDenied veya 403

## 4. Public masa menü

- [ ] `/m/demo-kafe?table=TBL-KAFE-1` — 200
- [ ] Üstte “Masa: Masa 1” badge
- [ ] `/m/demo-kafe?table=INVALID` — menü açılır, uyarı metni

## 5. Masa ile sipariş

- [ ] Masa QR menüden sepete ürün ekle
- [ ] Sipariş oluştur → confirmation
- [ ] `/Business/Orders` — siparişte masa badge
- [ ] `/Business/Orders/Kitchen` — kartta masa badge belirgin
- [ ] Sipariş detay — servis tipi + masa
- [ ] Public order status — “Masa: …” kutusu
- [ ] WhatsApp mesajında `Masa:` satırı (wa.me önizleme)

## 6. Masa olmadan sipariş (regression)

- [ ] `/m/demo-kafe` (param yok) — sipariş ver
- [ ] Order'da `ServiceType` null / masa yok
- [ ] Eski WhatsApp formatı bozulmadı

## 7. Tenant izolasyonu

- [ ] Başka işletmenin masa ID'si ile Edit — 404/Forbid

## 8. Admin görünürlük

- [ ] `/Admin/Businesses/Details/{demo-kafe-id}` — masa sayısı satırı

---

## PASS kriteri

Tüm maddeler işaretli; `run-smoke-tests.ps1` ve `release-quality-gate.ps1` PASS.
