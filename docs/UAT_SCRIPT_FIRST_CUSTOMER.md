# UAT Script — First Customer (30 dk)

## Amaç

İlk müşteri tesliminden önce uçtan uca demo + onboarding + sipariş + operasyon + success kontrollerini tamamlamak.

## Roller

- **Public müşteri (anon)**: QR menü, sepet, WhatsApp order
- **BusinessOwner**: kurulum, go-live, success, raporlar
- **Staff**: orders/kitchen (yetki varsa)
- **SuperAdmin**: operations, onboarding, customer success, sales requests

## Akış (30 dk)

### 1) Public demo (5 dk)

- `/Demo` açılır
- `/m/demo-kafe` açılır
- Sepete ürün eklenir (JS)
- Kampanya banner/indirimi görsel olarak doğrulanır (varsa)
- Mobil polish hızlı kontrol: hero + kategori nav + sepet bar

### 2) Sipariş zinciri (8 dk)

- Public menüden “sipariş ver” akışı
- WhatsApp yönlendirme metni kontrol edilir
- Order confirmation/tracking sayfaları kontrol edilir

### 3) Business operasyon (8 dk)

- Business Dashboard açılır
- Go-Live checklist görülür
- Kitchen ekranında sipariş status ilerletilir (Pending → Preparing → Completed)
- Notification/Audit kayıtları kontrol edilir

### 4) Onboarding & Success (5 dk)

- `/Business/Onboarding` skoru ve next action görünür
- `/Business/Success` health score, churn risk ve recommendations görünür

### 5) Admin kontrol (4 dk)

- `/Admin/Dashboard` KPI
- `/Admin/Operations` bağlantılar
- `/Admin/Onboarding` ve `/Admin/CustomerSuccess` board’ları
- `/Admin/Quality` readiness cards + script komutları

## Kabul kriterleri

- Public/Legal sayfalar bozulmamış
- Public menü açılır; sepet çalışır
- Sipariş zinciri kırılmamış
- Business ve Admin kritik ekranlar açılır
- Onboarding/Success skorları görünür
- Audit/Notification gürültüsüz ve beklenen şekilde çalışır

## Müşteri onay notları

- Tarih:
- Müşteri adı:
- Onaylanan maddeler:
- Açık konular / risk:
