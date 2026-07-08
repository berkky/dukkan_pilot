# Pilot Müşteri Takip Planı

> İlk 5–10 pilot işletme için 90 günlük takip çerçevesi.
> Panel: `/Admin/CustomerSuccess`, `/Admin/Onboarding`, `/Business/Success`.

---

## Kohort tablosu (şablon)

| # | İşletme | Sektör | Won tarihi | Go-live | Plan | Pilot fiyat | Onboarding | Health | Son temas | Durum |
|---|---------|--------|------------|---------|------|-------------|------------|--------|-----------|-------|
| 1 | | | | | | | /100 | /100 | | Aktif |
| 2 | | | | | | | | | | |
| … | | | | | | | | | | |
| 10 | | | | | | | | | | |

**Durum değerleri:** Lead · Kurulum · Go-live · Aktif · Risk · Churn · Referans verdi

Excel/Google Sheet veya Admin notlarında tutulabilir; DB’de ayrı pilot tablosu yok.

---

## Fazlar

### Faz 0 — Pipeline (lead → Won)

| Gün | Aksiyon | Panel |
|-----|---------|-------|
| 0 | Demo + teklif | `/Admin/SalesRequests` |
| 1–3 | Follow-up | Status: Contacted / Qualified |
| Onay | Won + hesap | `/Admin/Businesses`, Subscription |
| 0 | Kickoff planla | `KICKOFF_MEETING_SCRIPT.md` |

### Faz 1 — Kurulum (gün 0–7)

| Gün | Kontrol | Kriter |
|-----|---------|--------|
| 0–1 | Kickoff, menü verisi | WhatsApp + kategori listesi |
| 2–3 | Menü canlı | ≥5 ürün, `/m/{slug}` OK |
| 4 | Test sipariş | Kitchen akışı tamam |
| 5–7 | Go-live | QR yerleşim, onboarding ≥70 |

**Admin:** `/Admin/Onboarding/Details/{id}` günlük veya gün aşırı

### Faz 2 — İlk hafta (gün 8–14)

| Kontrol | Soru | Kırmızı bayrak |
|---------|------|----------------|
| Sipariş | Test + gerçek var mı? | 0 sipariş 7 gün sonra |
| QR | Masalarda mı? | Sadece test, fiziksel yok |
| WhatsApp | Numara doğru mu? | Yanlış numara / boş |
| Panel | Owner giriş yaptı mı? | Hiç login yok |
| Destek | Ticket açıldı mı? | Açık kritik ticket |

**Mesaj:** `PILOT_WHATSAPP_SALES_MESSAGES.md` §9 (7. gün check-in)

### Faz 3 — İlk ay (gün 15–30)

| Hafta | Toplantı | Konu |
|-------|----------|------|
| 2 | 15 dk | İlk gerçek siparişler, menü güncelleme |
| 3 | 15 dk | Kampanya/sadakat denemesi |
| 4 | 30 dk | Raporlar, plan limitleri, ay sonu özeti |

**Metrikler (panel):**

- `/Business/Reports` — haftalık ciro, sipariş sayısı
- `/Business/Success` — health skoru, risk sinyalleri
- `/Admin/CustomerSuccess/Details/{id}` — churn risk, expansion

**Hedef (pilot):**

- ≥ 5 toplam sipariş (test hariç veya dahil — kohort standardı belirleyin)
- Onboarding skoru ≥ 85
- Health ≥ 60

### Faz 4 — 60. gün

- [ ] Health review — risk sinyalleri var mı?
- [ ] Plan yükseltme fırsatı (`UPGRADE_OPPORTUNITY_PLAYBOOK.md`)
- [ ] Ödeme / tahsilat gecikmesi (`/Admin/Billing`)
- [ ] Referans ön görüşmesi (`PILOT_TESTIMONIAL_PLAN.md`)

### Faz 5 — 90. gün (pilot kapanış)

- [ ] Pilot retrospektif (15–30 dk görüşme)
- [ ] Referans / testimonial toplama
- [ ] Pilot fiyat → liste fiyatı geçişi teyit
- [ ] SalesRequest notu + Customer Success özeti
- [ ] Ürün geri bildirimi → Support `FeatureRequest` veya iç backlog

---

## Haftalık operasyon ritmi (DukkanPilot ekibi)

| Gün | Rutin |
|-----|--------|
| Pazartesi | `/Admin/SalesCenter` + SalesRequests triage |
| Çarşamba | Onboarding board — düşük skorlar |
| Cuma | CustomerSuccess — risk listesi + pilot check-in özeti |

**15 dk stand-up soruları:**

1. Bu hafta go-live olan var mı?
2. Kırmızı bayraklı pilot var mı?
3. Tahsilat gecikmesi var mı?
4. Referans adayı kim?

---

## Risk tetikleyicileri ve müdahale

| Sinyal | Kaynak | Müdahale |
|--------|--------|----------|
| Onboarding < 50, 14 gün+ | `/Admin/Onboarding` | Menü kurulum call |
| Health < 40 | `/Admin/CustomerSuccess` | `CHURN_RISK_RUNBOOK.md` |
| 14 gün sipariş yok | Orders | WhatsApp + eğitim tekrarı |
| Abonelik expired | Billing | Uzatma / Won yenileme |
| Açık destek ticket 48s+ | `/Admin/Support` | Öncelik yükselt |
| Upgrade talebi | SalesRequests / Billing | Qualified → Won veya plan güncelle |

---

## Pilot başarı kriterleri (kohort düzeyi)

| KPI | Hedef (5–10 pilot) |
|-----|---------------------|
| Go-live oranı | ≥ %80 (Won → go-live) |
| 30 gün aktif kullanım | ≥ %70 (sipariş + login) |
| Churn (90 gün) | ≤ %20 |
| Referans onayı | ≥ 3 işletme |
| NPS (basit 0–10) | Ortalama ≥ 7 (manuel anket) |

---

## Basit NPS mesajı (60. gün)

```
DukkanPilot pilot anket — 1 dakika:

0–10 arası bizi bir arkadaşınıza önerir misiniz?
0 = hiç, 10 = kesinlikle

Neden? (1 cümle)

Teşekkürler!
```

Sonuçları kohort tablosuna işleyin.

---

## Panel kısayolları

| Amaç | URL |
|------|-----|
| Satış özeti | `/Admin/SalesCenter` |
| Kurulum board | `/Admin/Onboarding` |
| Health board | `/Admin/CustomerSuccess` |
| İşletme detay | `/Admin/Businesses/Details/{id}` |
| Tahsilat | `/Admin/Billing` |
| Destek | `/Admin/Support` |
