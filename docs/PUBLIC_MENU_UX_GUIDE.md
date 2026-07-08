# Public Menu UX Guide

## Amaç

`/m/{slug}` public QR menü; ürünün vitrini ve satış demosunun en kritik ekranıdır.

Hedef: mobil-first, hızlı, güven veren, “uygulama gibi” hissettiren deneyim.

## Tasarım prensipleri

- **Tek el**: kategori nav + sepet bar kolay erişilebilir
- **Net fiyat**: ara toplam/indirim/toplam ayrımı
- **Güven**: WhatsApp akışı ve gizlilik notu kısa ve anlaşılır
- **Hafif**: küçük JS, az DOM, lazy loading
- **Erişilebilir**: label, focus, kontrast, yeterli dokunma alanı

## Sepet ve sipariş UX

- Sepet bar: ürün adedi + toplam + “Sepeti Gör”
- Drawer: satırlar net, adet kontrolü, sil, kampanya/puan mesajı
- Form: müşteri adı/telefon/not opsiyonel; validation mesajları sakin
- Akış: “sipariş oluştur → WhatsApp mesajı hazır açılır”

## Kampanya/ödül gösterimi

- Kampanya: “sepette otomatik uygulanır” net
- Minimum tutar varsa açıkça göster
- Ödül vitrini: “puan harcama otomatik yapılmaz” notu

## Tracking ekranı

- Timeline app-like, durum badge net
- Polling hata mesajı sakin: “birazdan tekrar denenecek”

## Demo anlatımı

- `/m/demo-kafe` → kategori gez → sepete ekle → kampanya indirimi → “WhatsApp ile sipariş”
- Confirmation → Tracking → Kitchen statüsü değişince tracking güncellendiğini göster

