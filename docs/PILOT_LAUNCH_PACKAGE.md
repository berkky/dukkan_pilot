# DukkanPilot — İlk 5–10 Pilot Müşteri Satış Paketi

> **Amaç:** İlk pilot kohortuna (5–10 işletme) kontrollü satış, kurulum, teslim ve takip.
> **Kapsam:** Demo → teklif → Won → kurulum → go-live → referans.
> **Önemli:** Online ödeme yok; tahsilat manuel (`MANUAL_BILLING_RUNBOOK.md`). ROI/değer hesapları **tahmini**; garanti gelir vaadi yoktur.

---

## Paket içeriği

| # | Bileşen | Doküman |
|---|---------|---------|
| 1 | Demo akışı (5 / 10 / 15 dk) | `PILOT_DEMO_FLOW.md` |
| 2 | WhatsApp satış mesajları | `PILOT_WHATSAPP_SALES_MESSAGES.md` |
| 3 | Fiyat teklifi metinleri | `PILOT_PRICE_QUOTE_TEMPLATES.md` |
| 4 | Kurulum / onboarding checklist | `PILOT_ONBOARDING_CHECKLIST.md` |
| 5 | İlk müşteri teslim dokümanları | `PILOT_CUSTOMER_DELIVERY_KIT.md` |
| 6 | Pilot müşteri takip planı | `PILOT_TRACKING_PLAN.md` |
| 7 | Referans / testimonial planı | `PILOT_TESTIMONIAL_PLAN.md` |

**Destek dokümanlar:** `SALES_DEMO_SCRIPT.md`, `DEMO_PACK_SALES_SCRIPT.md`, `FIRST_CUSTOMER_CHECKLIST.md`, `KICKOFF_MEETING_SCRIPT.md`, `IMPLEMENTATION_HANDOFF_CHECKLIST.md`, `CUSTOMER_ONBOARDING_RUNBOOK.md`, `SALES_PIPELINE_RUNBOOK.md`, `VALUE_SELLING_PLAYBOOK.md`

---

## Pilot program özeti

### Kimler?

- Tek şube kafe, tatlıcı, burgerci, küçük restoran
- WhatsApp ile sipariş alan veya almak isteyen işletmeler
- Menüyü sık güncelleyen, kampanya/sadakat denemeye açık ekipler

### Pilot teklif çerçevesi (öneri)

| Öğe | Öneri |
|-----|--------|
| Kohort boyutu | 5–10 işletme |
| Süre | 90 gün pilot + aylık abonelik |
| Plan | Çoğunlukla **Starter**; yüksek ürün/kampanya ihtiyacında **Pro** |
| Fiyat | Seed plan fiyatları: Starter **299 ₺/ay**, Pro **599 ₺/ay** — pilot indirimi operasyon ekibi belirler (`PILOT_PRICE_QUOTE_TEMPLATES.md`) |
| Trial | Mevcut 14 gün trial veya pilot özel süre (Admin abonelik manuel) |
| Karşılık | Geri bildirim, haftalık 15 dk check-in, onaylı referans/testimonial hakkı |

### Satış hunisi (panel eşlemesi)

```
Outreach (WhatsApp) → Demo görüşmesi → Teklif → SalesRequest Won
    → Kickoff → Kurulum → Go-Live → Customer Success takibi → Referans
```

| Aşama | Panel / araç |
|-------|----------------|
| Lead | `/Sales/RequestDemo`, `/Sales/RequestPlan` |
| Pipeline | `/Admin/SalesRequests` |
| Satış merkezi | `/Admin/SalesCenter` |
| Değer anlatımı | `/RoiCalculator`, `/Admin/ValueCalculator` |
| Sektör demosu | `/DemoPacks`, `/m/demo-{sektor}` |
| Won sonrası kurulum | `/Admin/Onboarding`, `/Business/Onboarding` |
| Tahsilat | `/Admin/Billing` (iç kayıt; resmi fatura değil) |
| Sağlık takibi | `/Admin/CustomerSuccess`, `/Business/Success` |
| Destek | `/Business/Support`, `/Admin/Support` |

---

## 30 günlük pilot takvim (özet)

| Hafta | Satış / operasyon | Müşteri |
|-------|-------------------|---------|
| 1 | Outreach + demo + teklif | Demo menüyü dener |
| 2 | Won + kickoff + menü kurulumu | Menü verisi gönderir |
| 3 | Go-live + QR + test sipariş | Masalara QR, gerçek sipariş |
| 4 | Health check + feedback | İlk hafta rapor + geri bildirim |

Detay: `PILOT_TRACKING_PLAN.md`

---

## Pilot kohort kontrol listesi (operasyon)

- [ ] Pilot slot sayısı belirlendi (max 10)
- [ ] Her lead için sektör demosu seçildi (`/DemoPacks`)
- [ ] WhatsApp outreach şablonu gönderildi
- [ ] Demo görüşmesi yapıldı (`PILOT_DEMO_FLOW.md`)
- [ ] Teklif metni paylaşıldı (`PILOT_PRICE_QUOTE_TEMPLATES.md`)
- [ ] SalesRequest oluşturuldu ve triage edildi
- [ ] Won → işletme + abonelik + (opsiyonel) iç tahsilat kaydı
- [ ] Kickoff (`KICKOFF_MEETING_SCRIPT.md`)
- [ ] Kurulum tamam (`PILOT_ONBOARDING_CHECKLIST.md`)
- [ ] Teslim paketi gönderildi (`PILOT_CUSTOMER_DELIVERY_KIT.md`)
- [ ] 30/60/90 gün takip planı aktif (`PILOT_TRACKING_PLAN.md`)
- [ ] Referans/testimonial talebi planlandı (`PILOT_TESTIMONIAL_PLAN.md`)

---

## Dikkat edilecekler

- Public sayfada panel şifresi **paylaşılmaz**
- Gerçek müşteri verisi demo görüşmesinde gösterilmez
- KVKK/Privacy: `/Privacy`, `/Kvkk` — kesin uyumluluk iddiası yok
- İç tahsilat kayıtları muhasebe/resmi e-Fatura yerine geçmez
- WhatsApp Business API yok; yalnızca `wa.me` deep link + manuel satış mesajları

---

## Hızlı başlangıç (bugün)

1. `PILOT_WHATSAPP_SALES_MESSAGES.md` → **İlk temas** mesajını kopyala
2. Lead sektörüne göre `/DemoPacks` aç
3. `PILOT_DEMO_FLOW.md` → 10 dk demo yap
4. `PILOT_PRICE_QUOTE_TEMPLATES.md` → teklif gönder
5. `/Admin/SalesRequests` → talebi kaydet ve takip et
