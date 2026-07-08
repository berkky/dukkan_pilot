# Admin Support Knowledge Base

## Senaryo: İşletme menüsü görünmüyor

1. Admin → Businesses → slug doğru mu?
2. İşletme `IsActive` mi?
3. En az bir aktif kategori + ürün var mı?
4. Public URL: `/m/{slug}` — 404 ise slug/aktiflik
5. Rehber: `ilk-musteri-kurulumu`, Business `qr-menu-yayinlama`

## Senaryo: Sipariş gelmiyor

1. Public menüden test siparişi ver
2. Business Orders listesi — tenant doğru mu?
3. WhatsApp numarası ayarlarda dolu mu?
4. Abonelik gate aktif mi?

## Senaryo: Kampanya uygulanmıyor

1. Kampanya tarih aralığı ve `IsActive`
2. Minimum sepet tutarı
3. `IsAutoApply` ve public görünürlük
4. Business rehber: `kampanya-olusturma`

## Senaryo: Tahsilat kaydı görünmüyor

1. Admin Billing'de kayıt var mı?
2. Doğru `BusinessId` ile mi oluşturuldu?
3. Owner olarak `/Business/Billing/Invoices` (Staff erişemez)
4. Rehber: `manuel-tahsilat`

## Senaryo: Login sorunu

1. Doğru e-posta / şifre
2. Kullanıcı aktif mi?
3. Business kullanıcısında `BusinessId` claim
4. Cookie/auth — tarayıcı çerezleri

## Senaryo: Abonelik / gate

1. Admin Businesses → Subscription
2. Bitiş tarihi ve plan durumu
3. Tahsilat sonrası manuel uzatma gerekir

## Senaryo: Migration / deploy

1. `db-migration-status.ps1`
2. `MIGRATION_RUNBOOK.md`
3. `release-quality-gate.ps1`

## Senaryo: Public menü mobil görünüm

1. `/m/demo-kafe` smoke
2. `PUBLIC_MENU_UX_GUIDE.md`
3. Tarayıcı önbelleği / eski build

## Hangi panelden ne?

| Konu | Admin | Business |
|------|-------|----------|
| Lead | SalesRequests | Billing/Requests |
| Tahsilat | Billing | Billing/Invoices |
| Kurulum | Onboarding | Onboarding |
| Sağlık | CustomerSuccess | Success |
| Operasyon | Operations, Quality | GoLive, DemoCenter |
