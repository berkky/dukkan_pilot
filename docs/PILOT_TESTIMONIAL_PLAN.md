# Pilot Referans / Testimonial Toplama Planı

> İlk pilot kohortundan onaylı sosyal kanıt toplama. Yayın öncesi **yazılı onay** zorunlu.

---

## Hedefler

| Metrik | Pilot kohort (5–10) |
|--------|---------------------|
| Kısa WhatsApp testimonial | 5+ |
| Landing’de kullanılabilir referans | 3+ |
| Video / ses (opsiyonel) | 1–2 |
| Case study (yazılı) | 1–2 |

---

## Zamanlama

| Dönem | Aksiyon |
|-------|---------|
| Go-live + 14 gün | Memnuniyet kontrolü; erken referans **istemeyin** |
| 30. gün | İlk referans talebi (memnun müşteriye) |
| 60. gün | NPS + testimonial hatırlatma |
| 90. gün | Case study görüşmesi (en başarılı 1–2 işletme) |

---

## Uygun aday kriterleri

**İyi aday:**

- Onboarding ≥ 80, Health ≥ 70
- Son 30 günde düzenli sipariş
- Destek ticket’larında kritik açık sorun yok
- İşletme sahibi iletişime açık
- Go-live sonrası en az 4 hafta aktif kullanım

**Erken / kaçının:**

- Henüz gerçek sipariş almamış
- Açık billing veya churn riski
- Olumsuz NPS (< 6)

**Panel:** `/Admin/CustomerSuccess` — Healthy + Growth listeleri

---

## Testimonial formatları

### Format A — Kısa (WhatsApp / landing)

```
"[2–3 cümle: ne sorunu çözdü, ne işe yaradı]"
— [Ad Soyad], [Unvan], [İşletme Adı], [Şehir]
```

**Örnek (uydurma — gerçek onaylı metin kullanın):**

> "QR menüden sipariş gelince WhatsApp mesajı hazır oluyor; mutfak ekranından takip etmek kolaylaştı."
> — Ayşe Y., İşletme Sahibi, Örnek Kafe, İstanbul

### Format B — Orta (landing / satış deck)

- Sorun (1 cümle)
- Çözüm (1 cümle)
- Sonuç (1 cümle — **garanti rakam kullanmayın**; “sipariş takibi kolaylaştı” gibi nitel ifadeler)
- Onaylı işletme adı + logo (varsa)

### Format C — Case study (blog / PDF)

1. İşletme profili (sektör, şube sayısı)
2. Önceki durum
3. Kurulum süreci (DukkanPilot)
4. Kullanılan modüller (QR, mutfak, kampanya…)
5. Sonuçlar (**tahmini / nitel**; müşteri onaylı rakam varsa belirtin)
6. Alıntı + fotoğraf (onaylı)

---

## Onay metni (yayın izni)

Müşteriye gönderin; “evet” yanıtı veya imzalı metin saklayın.

```
REFERANS İZİN METNİ — DukkanPilot

İşletme: [İşletme Adı]
Yetkili: [Ad Soyad], [Unvan]

Aşağıdaki ifadeyi DukkanPilot web sitesi, satış materyalleri ve sosyal medyada kullanmama izin veriyorum:

"[Testimonial metni]"

☐ İşletme adımın kullanılmasına izin veriyorum
☐ Şehir bilgimin kullanılmasına izin veriyorum
☐ Logo / fotoğraf kullanımına izin veriyorum (ek: ___)

Onay tarihi: [Tarih]
Onay: [WhatsApp "onaylıyorum" / e-posta / imza]

Not: İstediğim zaman kullanımı durdurmak için [iletişim] adresine yazabilirim.
```

---

## Talep mesajları

### 30. gün — ilk talep

`PILOT_WHATSAPP_SALES_MESSAGES.md` §11 kullanın.

### Olumsuz yanıt

```
Tabii ki, hiç sorun değil. Pilot geri bildiriminiz bizim için yeterli.
İleride fikriniz değişirse haber verin.
```

### Düzeltme talebi

Müşteriye taslak metni gönderin; onay almadan yayınlamayın.

```
Taslak referans metni:

"[taslak]"

Bu şekilde uygun mu? Düzeltme varsa yazın, onayınızla yayınlayalım.
```

---

## Yayın kontrol listesi

- [ ] Yazılı onay alındı (metin + ad + kullanım kanalları)
- [ ] Rakam iddiası yok veya müşteri teyitli
- [ ] Logo/fotoğraf telif ve KVKK açısından uygun
- [ ] Landing’de “pilot müşteri” etiketi (isteğe bağlı, şeffaflık)
- [ ] Müşteri isterse referans kaldırma prosedürü not edildi

**İç doküman:** onay ekran görüntüsü / e-posta arşivi

---

## Kullanım kanalları

| Kanal | Format | Onay |
|-------|--------|------|
| Landing `/` | Format A, carousel | Ad + işletme |
| `/Trust` | Format A + güven maddeleri | Ad |
| Satış WhatsApp | Format A kopyala | Hafif — yine onay |
| Demo görüşmesi | Format B slayt | — |
| Sosyal medya | Alıntı + foto | Logo izni |

Kod/deployment gerektirmez; metinleri manuel güncelleyin (MVP).

---

## Geri bildirim → ürün

Olumlu/olumsuz tüm geri bildirimler:

- `/Business/Support` — Category: FeatureRequest
- Admin: `/Admin/Support` triage
- `FEEDBACK_MANAGEMENT_PLAYBOOK.md`

Referans vermeyen ama detaylı geri bildirim veren müşteriler değerlidir — ayrı teşekkür edin.

---

## İlgili dokümanlar

- `PILOT_TRACKING_PLAN.md` — 60/90 gün zamanlama
- `CUSTOMER_SUCCESS_PLAYBOOK.md` — memnuniyet takibi
- `PRODUCT_POSITIONING.md` — mesaj tutarlılığı
