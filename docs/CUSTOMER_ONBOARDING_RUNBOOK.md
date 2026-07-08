# Customer Onboarding Runbook

Satış talebi **Won** olduktan sonra müşteriyi kuruluma alma ve ilk değeri gösterme rehberi.

## Lead Won sonrası

1. Admin SalesRequests’te talebi Won yapın.
2. `BusinessId` yoksa işletme hesabı oluşturun / bağlayın (`/Admin/Businesses`).
3. Owner hesabı ve plan/abonelik ayarlarını doğrulayın.
4. `/Admin/Onboarding` üzerinden skoru izleyin.
5. Kickoff toplantısı için `docs/KICKOFF_MEETING_SCRIPT.md` kullanın.
6. Handoff için `docs/IMPLEMENTATION_HANDOFF_CHECKLIST.md` işaretleyin.

## İlk müşteri kickoff akışı

1. İletişim + WhatsApp numarası alın.
2. Menü kategorileri / ürün listesini toplayın (CSV mümkün).
3. Go-live tarihini netleştirin.
4. BusinessOwner’a `/Business/Onboarding` gösterin.
5. Public menü + QR afiş + test sipariş “aha moment”ini birlikte yapın.

## İşletme bilgileri

- Ayarlar: ad, slug, telefon, WhatsApp, adres/logo/açıklama.
- Owner yetkisi gerektiren adımlar Staff’a UI uyarısı ile gösterilir.

## Menü verisi

- En az 1 aktif kategori, 5+ ürün hedeflenir.
- MenuStudio + CSV import hızlandırır.
- Public `/m/{slug}` login istemez.

## QR poster

- `/Business/QrMenu/Print` ile afiş hazırlanır.
- Müşteriye teslim: masa / kasa yakını.

## Test sipariş

1. Public menüden sepet → WhatsApp sipariş.
2. Business Orders’ta görünür.
3. Kitchen’da Preparing / Completed ilerletin.
4. Confirmation / tracking durumunu gösterin.

## Personel eğitimi

- Kitchen ekranı.
- Sipariş durumları.
- Bildirim merkezi (spam üretmeden mevcut uyarılar).

## Kampanya / sadakat

- Opsiyonel: 1 kampanya veya ödül.
- Dönüşümü destekler; zorunlu onboarding adımı değildir (skora katkı verir).

## İlk hafta takip

- Admin Onboarding Board: düşük skor / ürün yok / sipariş yok filtreleri.
- Customer success: `docs/CUSTOMER_SUCCESS_PLAYBOOK.md`.

## Aha moment

1. QR menüyü açar.
2. Test sipariş gelir.
3. Kitchen’da durum değişir.
4. CRM / Rapor görünür.

## Başarı kriterleri

- 1 aktif kategori
- 5+ ürün (minimum bar 1 ürün; ideal 5+)
- WhatsApp hazır
- 1 test siparişi
- QR poster hazır
- 1 kampanya veya ödül (opsiyonel)

## Risk sinyalleri

- WhatsApp yok
- Ürün yok
- Sipariş yok
- Owner panele girmiyor
- Notification / audit boş (aktivite yok)

## Teknik not

Onboarding score mevcut entity verilerinden **read-only** hesaplanır. Migration / kalıcı onboarding tablosu yoktur.
