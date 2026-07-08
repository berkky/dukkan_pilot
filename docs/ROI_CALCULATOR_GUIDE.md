# ROI / Değer Hesaplayıcı — Teknik ve Kullanım Rehberi (34A)

## Amaç

Satış görüşmelerinde ve işletme panelinde **tahmini değer senaryosu** üretmek. Kesin kazanç vaadi yoktur.

## Rotalar

| Kapsam | Route | Auth |
|--------|-------|------|
| Public | `/RoiCalculator`, `/ValueCalculator` (alias) | Anonymous |
| Business | `/Business/ValueCalculator` | Owner + Staff |
| Admin | `/Admin/ValueCalculator` | SuperAdmin |

## Girdi alanları

- Aylık sipariş sayısı
- Ortalama sepet (₺)
- Mevcut tekrar müşteri oranı (%)
- Beklenen tekrar sipariş artışı (%)
- Kampanya etkisi tahmini (%)
- Platform komisyon oranı (%) — opsiyonel
- Platformdan gelen sipariş oranı (%) — opsiyonel
- Haftalık menü güncelleme saati
- Haftalık sipariş yönetiminde kazanılacak saat
- Saatlik operasyon maliyeti (₺)
- Aylık yazılım maliyeti (₺)
- Senaryo: Conservative / Base / Ambitious

## Formüller

Çarpanlar: Temkinli ×0,5 · Temel ×1,0 · İddialı ×1,5

1. **Ek gelir (aylık)**  
   `sipariş × sepet × ((tekrar_artış% + kampanya_artış%) / 100) × çarpan`

2. **Komisyon tasarrufu (aylık)**  
   Yalnızca platform oranı ve komisyon > 0 ise:  
   `sipariş × (platform_sipariş% / 100) × sepet × (komisyon% / 100) × çarpan`

3. **Zaman tasarrufu (aylık)**  
   `((haftalık_menü_saat + haftalık_sipariş_saat) × 4,33) × saatlik_maliyet × çarpan`

4. **Toplam tahmini değer** = ek gelir + komisyon tasarrufu + zaman tasarrufu  
5. **Net değer** = toplam − aylık yazılım maliyeti  
6. **Geri ödeme oranı** = toplam / yazılım maliyeti (maliyet > 0 ise)  
7. **Yıllık** = aylık × 12

## Senaryo mantığı

Üç senaryo aynı girdilerle farklı çarpanlarla hesaplanır. Kullanıcı “öne çıkan senaryo” olarak birini seçer; özet kartlar seçili senaryoyu gösterir.

## Garanti olmadığı

- UI ve sonuçlarda açık disclaimer bulunur.
- “Garanti”, “kesin kazanç”, “aylık şu kadar kazanırsın” dili kullanılmaz.
- Sonuçlar DB’ye yazılmaz; audit üretilmez.

## Hangi müşteriye nasıl anlatılır?

| Profil | Vurgu |
|--------|--------|
| Yeni işletme | QR menü + WhatsApp operasyon zamanı |
| Platform kullanan | Komisyon tasarrufu varsayımı (opsiyonel alanlar) |
| Sadakat odaklı | Tekrar sipariş artışı varsayımı |
| Kampanya yoğun | Kampanya etkisi varsayımı |

## Business prefill

Son 30 gün siparişleri (`BusinessId` filtresi, `AsNoTracking`, iptal hariç):
- Sipariş sayısı → aylık sipariş önerisi
- Ortalama `TotalAmount` → sepet önerisi
- Aynı müşteriden birden fazla sipariş → tekrar oranı tahmini

## CTA

Public: Pricing, RequestDemo, RequestPlan, demo menü  
Business: Kampanya, Sadakat, QR, Go-Live, Billing (Owner)  
Admin: SalesRequests, SalesCenter, DemoCenter, Help makaleleri

## İlgili dosyalar

- `ValueCalculatorHelper.cs`
- `ValueCalculatorViewModels.cs`
- Public/Business/Admin controllers + views
