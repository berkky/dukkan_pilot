# ROI Calculator — Varsayımlar ve Limitasyonlar (34A)

## Varsayımlar

- Kullanıcı girdileri gerçekçi ve işletmeye özgüdür; varsayılan public değerler **örnektir**.
- Haftalık saatler aylığa `× 4,33` ile çevrilir.
- Tekrar ve kampanya etkileri **birlikte** ek gelir satırına eklenir (çifte sayım riski kullanıcıya bırakılır; konservatif senaryo önerilir).
- Yazılım maliyeti kullanıcı tarafından girilir; Pricing’den query ile prefill edilebilir.

## Limitasyonlar

| Konu | Durum |
|------|--------|
| KDV / vergi | Hesaplanmaz |
| Maliyet / marj | Hesaplanmaz |
| Resmi finansal danışmanlık | Değildir |
| Garanti gelir | Yok |
| DB kaydı | Yok |
| Audit log | Üretilmez |
| Otomatik lead | Yok (yalnızca mevcut Sales CTA) |

## Komisyon tasarrufu

Yalnızca üçüncü taraf platform veya komisyon kullanan işletmeler için anlamlıdır. Alanlar 0 ise satır 0’dır.

## Zaman tasarrufu

Menü güncelleme ve sipariş yönetimi tasarrufu işletme süreçlerine göre değişir. Ekip alışkanlığı ve mevcut araç seti sonucu etkiler.

## Senaryo çarpanları

- Temkinli: gerçekleşme ihtimali düşük uç
- Temel: girilen varsayımların doğrudan uygulanması
- İddialı: iyimser uç (yine garanti değil)

## Sonuçların garanti olmaması

Gerçek sonuçlar; fiyatlama, müşteri kitlesi, sezon, rekabet, kampanya kalitesi ve operasyon disiplinine bağlıdır. Hesaplayıcı yalnızca **what-if** aracıdır.
