# DukkanPilot — Masa QR / Table Service Rehberi

## Özet

Her masaya özel QR linki ile müşteri menüyü açar; sipariş **masa bilgisiyle** kaydedilir. Garson/mutfak ekranında **“Masa 7”** gibi etiket görünür.

## Genel menü vs masa QR

| Tür | URL örneği | Masa bilgisi |
|-----|------------|--------------|
| Genel QR menü | `/m/demo-kafe` | Yok (eski akış korunur) |
| Masa QR | `/m/demo-kafe?table=TBL-KAFE-1` | Var — `TableService` |

## PublicCode neden var?

- Kısa, tahmin edilmesi zor kod (`TBL-KAFE-1`, `TBL-A7K2`)
- URL'de masa adı yerine kod — etiket değişse bile QR geçerli kalır
- `TableLabelSnapshot` sipariş anındaki masa adını saklar

## İşletme tarafı

1. **Masalar:** `/Business/Tables` (Owner: oluştur/düzenle; Staff: görüntüle)
2. Her masa için otomatik `PublicCode` üretilir (elle girilmez)
3. **QR yazdır:** `/Business/Tables/Qr/{id}` — qrcodejs + yazdır
4. Link formatı: `{site}/m/{slug}?table={PublicCode}`

## Sipariş akışı

1. Müşteri masadaki QR'ı okutur
2. Menüde **“Masa: …”** badge görünür
3. Sepet + sipariş POST → sunucu `BusinessId + PublicCode` ile doğrular
4. `Order` alanları:
   - `ServiceType = TableService`
   - `BusinessTableId`
   - `TableLabelSnapshot`
5. WhatsApp mesajında üstte: `Masa:` ve `Servis: Masaya servis`

## Geçersiz masa kodu

`?table=INVALID` → menü açılır, uyarı gösterilir; sipariş masa bilgisi **olmadan** alınabilir (akış bozulmaz).

## Garson / mutfak

- **Siparişler:** liste sütunu “Masa”
- **Mutfak modu:** kart üstünde büyük masa badge
- **Sipariş detay:** servis tipi + masa

## Demo seed kodları

| İşletme | Örnek link |
|---------|------------|
| demo-kafe | `/m/demo-kafe?table=TBL-KAFE-1` |
| demo-restoran | `/m/demo-restoran?table=TBL-REST-1` |
| demo-tatlici | `/m/demo-tatlici?table=TBL-TATL-1` |
| demo-burgerci | `/m/demo-burgerci?table=TBL-BURG-1` |
| demo-nargile | `/m/demo-nargile?table=TBL-NARG-1` |

## Teknik notlar

- Migration: `AddTableServiceMode` — `BusinessTables` + `Orders` nullable alanları
- Client `TableLabel`'e güvenilmez; POST'ta yeniden DB doğrulaması
- Identity / SignalR / WhatsApp API yok
