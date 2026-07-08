# Public Order UAT Script

## Rol: Public müşteri (anon)

1. QR menüyü aç: `/m/demo-kafe`
2. Kategori gez + ürün kartlarını kontrol et
3. Sepete ürün ekle
4. Sepet bar: adet/toplam/“Sepeti Gör”
5. Drawer: adet artır/azalt/sil
6. Kampanya indirimi varsa sepet özetinde gör
7. Ödül vitrini varsa “Siparişe Ekle” ile ödül talebini seç
8. Form alanlarını doldur (opsiyonel): ad/telefon/not
9. “WhatsApp ile Sipariş Ver” → sipariş oluştur
10. WhatsApp mesaj metnini kontrol et
11. Confirmation ekranı: sipariş no + toplam + CTA’lar
12. Tracking ekranı: timeline + status badge

## Rol: BusinessOwner/Staff

13. Kitchen ekranında siparişi ilerlet: Pending → Preparing → Completed
14. Tracking ekranında durumun güncellendiğini gör (polling)

## Kabul kriterleri

- Raw exception/stack trace görünmez
- Client fiyatına güvenilmez; server pricing doğrular
- Campaign/loyalty preview UI ile backend uyumlu
- Token güvenliği bozulmaz; URL yapısı değişmez

