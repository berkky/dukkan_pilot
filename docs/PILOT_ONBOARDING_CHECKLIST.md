# Pilot Kurulum / Onboarding Checklist

> Won sonrası ilk go-live için işaret listesi. Genişletilmiş sürüm: `FIRST_CUSTOMER_CHECKLIST.md`. Runbook: `CUSTOMER_ONBOARDING_RUNBOOK.md`.

---

## A. Satış kapanışı (Won günü)

- [ ] `/Admin/SalesRequests` → durum **Won**
- [ ] İşletme kaydı: `/Account/Register` veya Admin’den oluşturma
- [ ] `BusinessId` SalesRequest’e bağlandı
- [ ] Owner hesabı doğrulandı (login test)
- [ ] Plan / abonelik: `/Admin/Businesses/Subscription/{id}` (Trial veya Active, bitiş tarihi)
- [ ] (Opsiyonel) İç tahsilat kaydı: `/Admin/Billing/CreateInvoice`
- [ ] Kickoff planlandı (`KICKOFF_MEETING_SCRIPT.md`)
- [ ] WhatsApp hoş geldin mesajı (`PILOT_WHATSAPP_SALES_MESSAGES.md` §6)

---

## B. Kickoff (gün 0–1)

### İşletmeden alınacaklar

- [ ] Resmi işletme adı + menü slug tercihi
- [ ] WhatsApp sipariş numarası (zorunlu)
- [ ] Adres, kısa açıklama, telefon
- [ ] Logo URL (varsa)
- [ ] Menü: kategori + ürün + fiyat (liste veya CSV)
- [ ] Personel e-postası (Staff, opsiyonel)
- [ ] Go-live hedef tarihi
- [ ] Kampanya / sadakat beklentisi (opsiyonel)

### DukkanPilot tarafı

- [ ] `/Admin/Onboarding/Details/{id}` açıldı — başlangıç skoru not edildi
- [ ] Owner’a `/Business/Onboarding` gösterildi
- [ ] `/Business/Settings` — işletme profili + WhatsApp
- [ ] `/Business/HelpCenter` — eğitim makaleleri paylaşıldı

---

## C. Menü kurulumu (gün 1–3)

- [ ] En az **1 aktif kategori**
- [ ] En az **5 aktif ürün** (pilot minimum; ideal 10+)
- [ ] Fiyatlar doğru (TRY formatı)
- [ ] Tema rengi + açıklama (`/Business/Settings`)
- [ ] (Opsiyonel) CSV import: `/Business/Products/ImportCsv`
- [ ] `/Business/MenuStudio` — menü sağlık kontrolü yeşil/sarı
- [ ] Public menü doğrulandı: `/m/{slug}` (mobil görünüm)

---

## D. Sipariş akışı (gün 2–4)

- [ ] WhatsApp numarası ayarlarda doğru
- [ ] **Test siparişi** — sepet → POST → confirmation
- [ ] WhatsApp mesajı formatı kontrol (indirim satırları dahil)
- [ ] `/Business/Orders` — sipariş görünüyor
- [ ] `/Business/Orders/Kitchen` — durum ilerletme (Pending → Preparing → Completed)
- [ ] (Varsa müşteri telefonu) Completed sonrası sadakat puanı kontrolü
- [ ] Public takip: order-status linki çalışıyor

---

## E. QR ve fiziksel yayın (gün 3–5)

- [ ] `/Business/QrMenu` — link kopyalandı
- [ ] QR PNG indirildi veya `/Business/QrMenu/Print` afiş yazdırıldı
- [ ] Masalara / kasaya QR yerleşim planı müşteriyle teyit
- [ ] (Opsiyonel) WhatsApp’ta menü paylaşım linki test

---

## F. Opsiyonel ama önerilen (pilot değer gösterimi)

- [ ] Kampanya: min sepet + % indirim, auto-apply (`/Business/Campaigns`)
- [ ] Sadakat kuralı + en az 1 ödül (`/Business/Loyalty`, `/Business/Rewards`)
- [ ] Personel hesabı (`/Business/Staff`) — mutfak kullanıcısı
- [ ] `/Business/GoLive` skoru ≥ 80 veya tüm zorunlu adımlar tamam
- [ ] `/Business/DemoCenter` checklist yeşil

---

## G. Go-live onayı (gün 5–7)

- [ ] `/Business/Onboarding` → status **Live** veya ReadyToLaunch
- [ ] `/Admin/Onboarding` skoru güncellendi
- [ ] Gerçek sipariş hedefi müşteriyle konuşuldu (ilk hafta)
- [ ] Teslim paketi gönderildi (`PILOT_CUSTOMER_DELIVERY_KIT.md`)
- [ ] `/Admin/CustomerSuccess/Details/{id}` — baseline health notu
- [ ] Smoke (ortam uygunsa): login + public menü + test sipariş

---

## H. Eğitim (30 dk — müşteriye)

Sıra önerisi (`BUSINESS_USER_TRAINING_GUIDE.md`):

1. Public menü + sepet (5 dk)
2. Sipariş listesi + Mutfak (8 dk)
3. Ürün/kategori hızlı güncelleme (5 dk)
4. Kampanya / ödül (5 dk)
5. Raporlar + Bildirimler (5 dk)
6. Onboarding / Success / Destek (2 dk)

Personel özeti: `STAFF_TRAINING_CHEATSHEET.md`

---

## I. Pilot özel — operasyon notları

| Alan | Not |
|------|-----|
| SalesRequest ID | #____ |
| Go-live tarihi | ____ |
| Pilot fiyat | ____ ₺/ay |
| Check-in günleri | Hafta 1, 2, 4 |
| Referans izni | Evet / Hayır / Sonra |
| Risk | WhatsApp eksik / menü gecikmesi / personel direnci |

---

## Skor hedefleri

| Metrik | Go-live | 30. gün |
|--------|---------|---------|
| Onboarding skoru | ≥ 70 | ≥ 85 |
| Customer Success health | — | ≥ 60 |
| Gerçek sipariş (hafta) | 1 test | ≥ 5 toplam |
| Aktif ürün | ≥ 5 | ≥ 8 |

Takip: `PILOT_TRACKING_PLAN.md`
