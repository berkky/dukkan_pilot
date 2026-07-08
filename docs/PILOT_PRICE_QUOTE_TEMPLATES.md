# Pilot Fiyat Teklifi Metinleri

> Seed plan fiyatları: **Free 0 ₺**, **Starter 299 ₺/ay**, **Pro 599 ₺/ay**.
> Online ödeme yok; tahsilat manuel (`MANUAL_BILLING_RUNBOOK.md`). İç fatura kaydı resmi e-Belge değildir.
> `[...]` alanlarını doldurun. Pilot indirim oranını operasyon ekibi onaylar.

---

## Teklif 1 — Starter Pilot (önerilen, tek şube)

**Konu:** DukkanPilot Pilot Programı — [İşletme Adı]

Sayın [İsim],

[DukkanPilot / Şirket adı] olarak [İşletme Adı] için **DukkanPilot Starter** pilot programı teklifimiz aşağıdadır.

### Kapsam

- QR dijital menü (`/m/[slug]`)
- WhatsApp sipariş yönlendirme (hazır sipariş özeti)
- Sipariş / mutfak paneli + canlı takip
- Müşteri listesi ve temel CRM
- Kampanya motoru (otomatik indirim)
- Sadakat puanı ve ödül yönetimi
- Raporlar ve QR afiş yazdırma
- Kurulum sihirbazı + eğitim (yaklaşık 30 dk)

### Pilot koşulları

| Öğe | Değer |
|-----|--------|
| Plan | Starter |
| Liste fiyatı | 299 ₺ / ay + KDV |
| Pilot fiyatı | [ör. ilk 3 ay 199 ₺/ay] veya [sabit 299 ₺] |
| Pilot süresi | 90 gün |
| Kurulum ücreti | [0 ₺ / dahil] |
| Ödeme | Aylık havale/EFT — manuel tahsilat |
| Başlangıç | [Tarih] |

### Starter limitleri (özet)

- Ürün: 100’e kadar (plan tanımına göre)
- Kampanya, personel ve QR limitleri panelde görünür (`/Business/Billing`)

### Pilot karşılığında sizden beklenenler

- Haftalık 15 dk geri bildirim (ilk ay)
- Go-live sonrası en az 1 gerçek sipariş haftası
- Onayınızla kısa referans / testimonial (isteğe bağlı)

### Sonraki adım

Onayınız halinde:
1. İşletme hesabı açılır
2. Kickoff görüşmesi planlanır (`KICKOFF_MEETING_SCRIPT.md`)
3. Menü kurulumu ve test siparişi
4. Go-live + QR yerleşimi

Teklif geçerlilik: **[Tarih + 7 gün]**

Saygılarımızla,
[Ad Soyad]
[Telefon / E-posta]

---

## Teklif 2 — Pro Pilot (yüksek menü / kampanya ihtiyacı)

**Konu:** DukkanPilot Pro Pilot — [İşletme Adı]

[Starter teklif yapısı ile aynı; aşağıdaki farklar:]

| Öğe | Değer |
|-----|--------|
| Plan | Pro |
| Liste fiyatı | 599 ₺ / ay + KDV |
| Pilot fiyatı | [ör. ilk 3 ay 449 ₺/ay] |
| Neden Pro | [ör. 100+ ürün, çok kampanya, çok personel] |

---

## Teklif 3 — Trial → Starter dönüşüm

**Kısa metin (e-posta / WhatsApp):**

```
14 günlük denemeniz [tarih]te bitiyor.

Starter ile devam:
• 299 ₺/ay (pilot: [X ₺/ay] ilk 3 ay)
• Mevcut menünüz ve ayarlarınız korunur
• Kurulum skorunuz: [N]/100 — eksik adımları birlikte tamamlarız

Onay için bu mesaja "devam" yazmanız yeterli; fatura/tahsilat bilgisini ayrı iletiriz.
```

---

## Teklif 4 — Yıllık ön ödeme (opsiyonel)

> Yalnızca manuel tahsilat süreciniz hazırsa kullanın.

```
Yıllık ön ödeme seçeneği:
• Starter: [299 × 12 × 0,85] = [tutar] ₺ + KDV (%15 pilot indirim örneği)
• Tek seferlik tahsilat; abonelik bitiş tarihi Admin'den güncellenir
• Resmi e-Fatura süreci ayrı yürütülür (DukkanPilot iç kaydı resmi belge değildir)
```

---

## Teklif 5 — Red / erteleme yanıtı

```
Anlıyorum, şu an için uygun değil.

İleride tekrar değerlendirmek isterseniz demo menülerimiz açık:
[SITE_URL]/DemoPacks

İyi çalışmalar dilerim.
```

Admin: SalesRequest durumunu **Lost** veya not ile **WaitingCustomer** bırakın.

---

## Teklif eki — Değer özeti (opsiyonel)

ROI hesaplayıcı çıktısını eklerken şu uyarıyı ekleyin:

> *Aşağıdaki rakamlar `/RoiCalculator` ile üretilen **tahmini senaryodur**; gerçek sonuç işletme operasyonuna bağlıdır. Gelir garantisi verilmez.*

---

## Won sonrası operasyon eşlemesi

| Teklif maddesi | Panel aksiyonu |
|----------------|----------------|
| Plan Starter/Pro | `/Admin/Businesses/Subscription/{id}` |
| Tahsilat | `/Admin/Billing/CreateInvoice` + `RecordPayment` |
| Pipeline | `/Admin/SalesRequests` → Won |
| Kurulum | `/Admin/Onboarding/Details/{id}` |

---

## Fiyatlandırma notları (iç kullanım)

- İlk 5 pilot: agresif indirim + yoğun destek kabul edilebilir
- 6–10. pilot: indirim kademeli azaltılabilir
- Free plan: yalnızca kendi kayıt denemeleri; pilot satışta Starter/Pro önerilir
- Fiyat değişikliği seed’deki plan tablosundan okunur; canlıda Admin plan edit ile güncellenebilir
