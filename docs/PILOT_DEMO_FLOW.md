# Pilot Demo Akışı

> İlk 5–10 pilot müşteri için standart demo senaryosu. Genel script: `SALES_DEMO_SCRIPT.md`. Sektör varyantları: `DEMO_PACK_SALES_SCRIPT.md`.

---

## Demo öncesi hazırlık (5 dk)

- [ ] Lead sektörünü not et (kafe / tatlıcı / burger / restoran / lounge)
- [ ] `/DemoPacks` üzerinden doğru demo slug’ı seç
- [ ] Telefonda veya paylaşım ekranında demo menüyü aç: `/m/demo-{sektor}`
- [ ] Business panel demo hesabı hazır (görüşme sırasında paylaşılacak; public’te şifre yok)
- [ ] `/Admin/ValueCalculator` veya `/RoiCalculator` — kabaca sipariş/sepet varsayımları
- [ ] SalesRequest oluşturmaya hazır ol (`/Sales/RequestDemo` veya görüşme sonrası manuel)

**Sektör → demo eşlemesi**

| Sektör | Demo menü | Vurgu |
|--------|-----------|--------|
| Kafe | `/m/demo-kafe` | Kahve, tatlı, kampanya %10 |
| Tatlıcı | `/m/demo-tatlici` | Vitrin kategorileri |
| Burgerci | `/m/demo-burgerci` | Combo, sepet artırma |
| Restoran | `/m/demo-restoran` | Kategori akışı, mutfak |
| Lounge | `/m/demo-nargile` | Premium vitrin |

---

## 5 dakikalık demo (hızlı ilgi)

**Hedef:** “QR menü + WhatsApp sipariş + panel” aha moment.

1. **(30 sn) Problem** — Dağınık WhatsApp siparişi, güncel olmayan menü, tekrar müşteri takibi zor
2. **(2 dk) Public menü** — `/m/demo-kafe` (veya sektör)
   - Kategori gezin
   - Sepete 2 ürün ekle (toplam 100₺+ → kampanya indirimi göster)
   - Sipariş oluştur → confirmation + takip linki
3. **(1,5 dk) Operasyon** — Business panel
   - `/Business/Orders/Kitchen` — siparişi Beklemede → Hazırlanıyor → Tamamlandı
   - “Tamamlanınca sadakat puanı otomatik” (müşteri kayıtlıysa)
4. **(1 dk) Kapanış** — “Ücretsiz kayıt veya pilot teklif; menünüz 1 günde yayında”
   - CTA: `/Account/Register` veya teklif + `/Sales/RequestPlan`

**Demo cümlesi:** “Müşteri masadan QR ile menüye girer; sipariş özeti WhatsApp’a hazır gelir; siz mutfak ekranından yönetirsiniz.”

---

## 10 dakikalık demo (pilot satış)

5 dakikalığa ek:

5. **(2 dk) Menü yönetimi** — `/Business/MenuStudio` veya Products
   - Hızlı fiyat güncelleme, aktif/pasif
   - (İsteğe bağlı) CSV import şablonu göster
6. **(1,5 dk) Kampanya** — `/Business/Campaigns`
   - Auto-apply, min sepet, performans raporu (`/Business/Reports/Campaigns`)
7. **(1 dk) CRM** — `/Business/Customers` + Insights
   - Segmentler, WhatsApp iletişim butonu
8. **(30 sn) Go-Live / Onboarding** — `/Business/GoLive`, `/Business/Onboarding`
   - Skor ve eksik adım CTA

**Değer anlatımı:** `/RoiCalculator` — önce **temkinli** senaryo; “garanti değil, varsayım” cümlesi zorunlu.

---

## 15 dakikalık demo (karar verici + operasyon)

10 dakikalığa ek:

9. **(2 dk) Raporlar** — `/Business/Reports` — bugün/7 gün ciro, top ürünler
10. **(1 dk) Bildirim + Audit** — `/Business/Notifications`, `/Business/AuditLogs`
11. **(1 dk) QR afiş** — `/Business/QrMenu/Print` — masa kartı
12. **(1 dk) Pilot teklif** — `PILOT_PRICE_QUOTE_TEMPLATES.md` özeti + sonraki adım

**Admin (yalnızca SaaS alıcısı / ortak görüşmesi):** `/Admin/SalesCenter` — platform KPI, onboarding board

---

## Demo sonrası hemen yapılacaklar

| Adım | Aksiyon |
|------|---------|
| 1 | WhatsApp teşekkür + özet mesajı (`PILOT_WHATSAPP_SALES_MESSAGES.md` → Demo sonrası) |
| 2 | Teklif gönder (24 saat içinde) |
| 3 | `/Admin/SalesRequests` — durum: Contacted → Qualified |
| 4 | Karar bekleniyorsa: WaitingCustomer + 48–72 saat follow-up |
| 5 | Evet ise: Won → `SALES_PIPELINE_RUNBOOK.md` Won sonrası akış |

---

## İtirazlar (pilot bağlamı)

| İtiraz | Cevap |
|--------|--------|
| “Zaten WhatsApp kullanıyorum” | Panelde mutfak, durum, CRM ve rapor birleşir; hata ve tekrar soru azalır |
| “Sadece QR menü yeter” | Sipariş + kampanya + sadakat operasyonu tek yerde |
| “Personel kullanır mı?” | Mutfak modu dokunmatik; Staff rolü |
| “Pahalı” | Pilot fiyat + ROI tahmini; garanti değil ama zaman tasarrufu senaryosu |
| “Verilerim güvende mi?” | Tenant izolasyonu; Trust Center + taslak KVKK sayfaları |

---

## Yapılmaması gerekenler

- Panel şifresini landing veya demo menü sayfasında yazmak
- “Kesin X TL kazanırsınız” demek
- Gerçek müşteri sipariş/telefon verisi göstermek
- Ödeme entegrasyonu vaat etmek
