# Pilot WhatsApp Satış Mesajları

> Kopyala-yapıştır şablonlar. `[...]` alanlarını doldurun. Garanti gelir vaadi kullanmayın.
> WhatsApp Business API entegrasyonu yok; mesajlar manuel gönderilir.

---

## 1. İlk temas (soğuk / ılık lead)

```
Merhaba [İsim], ben [Adınız] — DukkanPilot ekibinden.

[Kafe/Restoran adı] için QR menü + WhatsApp sipariş + sadakat paneli sunuyoruz:
müşteri masadan menüye girer, sepete ekler; sipariş özeti WhatsApp'a hazır gelir;
siz mutfak ekranından durumu yönetirsiniz.

İlk 10 işletme için pilot programımız var — kurulum ve eğitim bizden.

2 dakikalık demo menü: [SITE_URL]/m/demo-kafe
Sektörünüze uygun örnekler: [SITE_URL]/DemoPacks

Uygun bir gün 10 dk görüşme yapabilir miyiz?
```

**Sektör varyantı (tatlıcı):**
```
... Örnek tatlıcı menüsü: [SITE_URL]/m/demo-tatlici — vitrin ürünlerinizi kategorilere ayırıp kampanya kurabilirsiniz.
```

---

## 2. Demo daveti (Qualified lead)

```
Merhaba [İsim],

DukkanPilot demo görüşmesi için önerdiğim saatler:
• [Gün 1] [Saat]
• [Gün 2] [Saat]

Görüşmede göstereceklerimiz (~10 dk):
1) QR menü + sepet + kampanya indirimi
2) WhatsApp sipariş mesajı
3) Mutfak / sipariş paneli
4) Kurulum skoru (go-live rehberi)

Hangisi size uygun? Görüşmeyi telefon veya WhatsApp görüntülü yapabiliriz.
```

---

## 3. Demo sonrası teşekkür + özet

```
Teşekkürler [İsim], bugünkü demo için.

Özet:
✓ QR menü + WhatsApp sipariş akışı
✓ Mutfak / sipariş takibi
✓ [Gösterdiğiniz ek: kampanya / CRM / rapor]

Sonraki adım: size özel pilot teklif metnini iletiyorum.
Sorularınızı buradan yazabilirsiniz.

Demo menü (tekrar): [SITE_URL]/m/[slug]
```

---

## 4. Fiyat teklifi (kısa WhatsApp)

```
Merhaba [İsim],

DukkanPilot pilot teklif özeti:

Plan: [Starter / Pro]
Pilot süre: 90 gün
Aylık: [299 / 599] ₺ ([pilot indirim varsa: ilk 3 ay X ₺])
Dahil: kurulum, eğitim, QR afiş rehberi, haftalık check-in

Başlangıç: menü + WhatsApp numarası + test sipariş → go-live

Detaylı teklif metnini ayrı mesajda paylaşıyorum.
Onaylarsanız hesabınızı aynı gün açabiliriz.
```

Detaylı metin: `PILOT_PRICE_QUOTE_TEMPLATES.md`

---

## 5. Teklif hatırlatma (48–72 saat)

```
Merhaba [İsim],

Geçen görüşmede paylaştığımız DukkanPilot pilot teklifi hâlâ geçerli.
Pilot kontenjanımızda [N] slot kaldı.

Teklifi tekrar ileteyim mi, yoksa kısa bir soru-cevap için 5 dk uygun musunuz?
```

---

## 6. Won / onay sonrası hoş geldiniz

```
Hoş geldiniz [İşletme adı] 🎉

DukkanPilot pilot programına dahil oldunuz.

Sıradaki adımlar:
1) Kickoff görüşmesi: [Tarih/Saat]
2) Bize iletecekleriniz: menü listesi (kategori + ürün + fiyat), WhatsApp sipariş numarası, logo (varsa)
3) Panel giriş: [LOGIN_URL]/Account/Login — e-posta: [email] (şifre ayrı kanaldan)

Kurulum rehberiniz panelde: /Business/Onboarding

Sorularınız için bu hattı kullanabilirsiniz.
```

> Şifreyi mümkünse WhatsApp dışı kanalda (telefon/e-posta) paylaşın.

---

## 7. Kickoff öncesi bilgi talebi

```
Kickoff için hazırlık listesi:

□ İşletme adı (menüde görünecek)
□ WhatsApp sipariş numarası (90XXXXXXXXX formatı)
□ Kısa açıklama + adres
□ Menü: en az 1 kategori, 5 ürün (Excel/CSV veya liste mesajı)
□ Go-live hedef tarihi
□ Personel e-postası (varsa)

Eksikleri kickoff'ta birlikte tamamlayabiliriz.
```

---

## 8. Go-live günü

```
Bugün yayına alıyoruz 🚀

Kontrol listesi:
□ Public menü: [SITE_URL]/m/[slug]
□ QR afiş yazdırıldı / masalara yerleşti
□ Test siparişi tamamlandı
□ Mutfak ekranı denendi

İlk gerçek siparişte bize yazın — ilk hafta yanınızdayız.
Panel: [LOGIN_URL]/Business/Dashboard
```

---

## 9. 7. gün check-in

```
Merhaba [İsim], pilot 1. hafta check-in:

1) Kaç sipariş aldınız? (test + gerçek)
2) QR masalarda mı?
3) Panelde takıldığınız bir adım var mı?

İsterseniz 15 dk hızlı görüşme yapalım.
Health skorunuz panelde: /Business/Success
```

---

## 10. Upgrade / plan konuşması (30. gün)

```
Pilot 1. ay özeti için kısa görüşme öneriyorum.

Konuşacağımızlar:
• Sipariş / ciro trendi (panel raporları)
• Plan limitleri (ürün/kampanya)
• [Pro'ya geçiş ihtiyacı varsa] yükseltme seçenekleri

Panelden plan talebi: /Business/Billing
```

---

## 11. Referans / testimonial talebi

```
Pilot sürecimizde çok yardımcı oldunuz — teşekkürler.

İzninizle kısa bir referans kullanmak istiyoruz:
• 2–3 cümle deneyiminiz
• İşletme adı + şehir (logo/isim onayı)

Onayınız olmadan yayınlamayız. Detay: PILOT_TESTIMONIAL_PLAN.md (iç kullanım)
```

---

## Mesaj kuralları

| Kural | Açıklama |
|-------|----------|
| Kısa tut | İlk mesaj 4–6 satırı geçmesin |
| Tek CTA | Her mesajda bir net istek (demo saati / onay / bilgi) |
| Link | `[SITE_URL]` → production veya staging public URL |
| Garanti yok | “Kesin kazanç”, “garanti ciro” kullanmayın |
| KVKK | İlk kayıtta Privacy/KVKK onayı form üzerinden (`/Sales/RequestDemo`) |
| Şifre | WhatsApp’ta mümkünse paylaşmayın |
