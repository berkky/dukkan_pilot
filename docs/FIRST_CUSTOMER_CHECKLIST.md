# İlk Müşteri Kurulum Checklist

> **Pilot kohort (5–10 müşteri):** `docs/PILOT_LAUNCH_PACKAGE.md` ve `docs/PILOT_ONBOARDING_CHECKLIST.md`

## Kurulum günü
- [ ] İşletme kaydı (`/Account/Register`) veya Admin’den işletme oluşturma
- [ ] İşletme adı, slug, telefon/WhatsApp (`/Business/Settings`)
- [ ] En az 1 aktif kategori
- [ ] En az 3–5 aktif ürün (veya CSV import)
- [ ] Tema rengi / kısa açıklama
- [ ] QR Menü linki kontrol (`/m/{slug}`)
- [ ] QR afiş yazdır (`/Business/QrMenu/Print`)
- [ ] (Pilot restoran) Masa QR kurulumu (`/Business/Tables`, `TABLE_SERVICE_QR_GUIDE.md`)
- [ ] Test siparişi ver → confirmation/tracking
- [ ] Kitchen’da durumu ilerlet
- [ ] (Opsiyonel) Kampanya: 100₺ üzeri %10 auto-apply
- [ ] (Opsiyonel) Ödül + sadakat kuralı
- [ ] Go-Live skoru kontrol (`/Business/GoLive`)
- [ ] Kurulum Sihirbazı (`/Business/Onboarding`) — skor + next action
- [ ] Personel ekle (Owner)
- [ ] Demo Merkezi checklist yeşil (`/Business/DemoCenter`)
- [ ] Plan/talep ihtiyacı varsa `/Business/Billing` veya public `/Sales/RequestPlan`
- [ ] (Opsiyonel) Demo öncesi/sonrası değer senaryosu: `/RoiCalculator` veya `/Business/ValueCalculator` (tahmini; garanti değil)
- [ ] Admin satış taleplerini `/Admin/SalesRequests` ile takip
- [ ] Admin Kurulum Takibi (`/Admin/Onboarding`) — skor / risk / Won handoff
- [ ] Won talep sonrası Admin Billing (`/Admin/Billing`) — iç tahsilat kaydı oluştur (resmi fatura değil)
- [ ] Manuel ödeme kaydı gir (`/Admin/Billing/RecordPayment`) — abonelik otomatik uzamaz; gerekirse `/Admin/Businesses` abonelik düzenle
- [ ] İşletme sahibi tahsilat geçmişini `/Business/Billing/Invoices` üzerinden görebilir (Owner-only)

## Müşteriye eğitim (30 dk)
- Eğitim planı: `docs/BUSINESS_USER_TRAINING_GUIDE.md`
- Personel özeti: `docs/STAFF_TRAINING_CHEATSHEET.md`
- Panel rehberi: `/Business/HelpCenter`
1. Public menü + sepet
2. Sipariş listesi / Mutfak
3. Ürün/kategori hızlı güncelleme
4. Kampanya / ödül
5. Raporlar
6. Bildirimler + Go-Live + Kurulum Sihirbazı

## İlk hafta takip
- [ ] Gün 1: Test siparişi var mı?
- [ ] Gün 2–3: QR masada mı? WhatsApp numara doğru mu?
- [ ] Gün 5: En az bir gerçek sipariş / kampanya denemesi
- [ ] Gün 7: Rapor ekranı + upgrade ihtiyacı konuşması
- [ ] Admin Onboarding Board’da skor yükseldi mi?

## Not
Hukuki/KVKK metinleri bu checklist’te yok (30A). Gerçek ödeme sağlayıcısı / kart tahsilatı yok (33A).
DukkanPilot iç tahsilat kayıtları resmi e-Fatura/e-Arşiv değildir; muhasebe süreci ayrı yürütülmelidir.
Onboarding runbook: `docs/CUSTOMER_ONBOARDING_RUNBOOK.md`. Tahsilat runbook: `docs/MANUAL_BILLING_RUNBOOK.md`.
