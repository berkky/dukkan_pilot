# Churn Risk Runbook

## Risk sinyalleri

- Son 30 günde sipariş yok
- Ürün / kategori yok
- WhatsApp eksik
- Abonelik bitmiş / bitiyor
- Onboarding düşük
- Aktivite zayıf

## İlk 24 saat müdahale

1. `CustomerSuccess` detayını aç
2. Top risk ve recommendations listesini çıkar
3. Owner ile bağlantı kur
4. Test sipariş gerekiyorsa birlikte yap

## Müşteriye sorulacaklar

- Menü aktif kullanılıyor mu?
- QR müşteriye sunuldu mu?
- WhatsApp numarası doğru mu?
- Teknik sorun mu, kullanım sorunu mu?
- Personel kitchen ekranını kullanıyor mu?

## Teknik sorun mu, kullanım sorunu mu?

- Menü açılmıyorsa teknik
- Menü var ama sipariş yoksa kullanım
- Abonelik geçersizse önce billing

## Menü boşsa

- Kategori + ürün ekle
- Public `/m/{slug}` kontrol
- QR tekrar üret / yazdır

## Sipariş yoksa

- Test sipariş
- Kitchen akışı
- WhatsApp yönlendirmesi
- Kampanya / sadakat önerisi

## Abonelik bitiyorsa

- Billing durumu netleştir
- Gerekirse SalesRequest aç / var olanı takip et

## Recovery checklist

- [ ] WhatsApp doğrulandı
- [ ] Ürün/kategori doğrulandı
- [ ] Public menü test edildi
- [ ] Test sipariş oluşturuldu
- [ ] Kitchen işlendi
- [ ] CustomerSuccess score yeniden kontrol edildi
