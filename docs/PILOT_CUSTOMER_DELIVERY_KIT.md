# Pilot İlk Müşteri Teslim Dokümanları

> Won + go-live sonrası müşteriye iletilecek metinler ve iç özet şablonları.
> Kopyala-yapıştır; `[...]` alanlarını doldurun.

---

## 1. Teslim özeti e-postası / WhatsApp (go-live günü)

```
Konu: DukkanPilot yayına alındı — [İşletme Adı]

Merhaba [İsim],

[DukkanPilot] kurulumunuz tamamlandı. Özet bilgiler:

━━━ ERİŞİM ━━━
Panel giriş: [LOGIN_URL]/Account/Login
E-posta: [owner@email]
Şifre: [ayrı kanaldan iletildi]

━━━ MÜŞTERİ MENÜSÜ ━━━
QR menü linki: [SITE_URL]/m/[slug]
QR afiş: Panel → QR Menü → Yazdırılabilir afiş
Masa QR: Panel → Masa QR Kodları → QR yazdır (restoran/kafe pilot)

━━━ HIZLI LİNKLER ━━━
• Siparişler / Mutfak: /Business/Orders/Kitchen
• Menü düzenleme: /Business/Products
• Kurulum rehberi: /Business/Onboarding
• Yardım: /Business/HelpCenter
• Destek talebi: /Business/Support

━━━ İLK HAFTA ━━━
1) QR'ları masalara yerleştirin
2) En az 1 gerçek sipariş alın
3) Takıldığınız yerde bize yazın — pilot süresince öncelikli destek

İletişim: [destek telefon/e-posta]

İyi çalışmalar,
[Ad Soyad]
```

---

## 2. Go-live onay formu (müşteriye — basit metin)

```
GO-LIVE ONAY — [İşletme Adı] — [Tarih]

Aşağıdaki maddeleri birlikte doğruladık:

☐ Public menü açılıyor: /m/[slug]
☐ WhatsApp sipariş numarası doğru
☐ Test siparişi başarılı
☐ Mutfak ekranı denendi
☐ QR afiş hazır / yazdırıldı
☐ Owner panel girişi çalışıyor
☐ (Varsa) personel hesabı çalışıyor

İşletme yetkilisi: [İsim]
DukkanPilot: [İsim]

Notlar: _______________________________
```

Müşteri onayı: WhatsApp “onaylıyorum” veya e-posta yanıtı yeterli (MVP).

---

## 3. İç handoff özeti (Admin — SalesRequest / Onboarding notu)

```
PILOT HANDOFF #____
Tarih: [YYYY-MM-DD]
İşletme: [Ad] (ID: ____, slug: [slug])
Owner: [email]
Plan: [Starter/Pro] — [Active/Trial] — bitiş: [tarih]
Pilot fiyat: [X] ₺/ay
SalesRequest: #____ Won [tarih]

Kurulum özeti:
- Ürün: [N] | Kategori: [N] | Kampanya: [E/H] | Personel: [N]
- Onboarding skoru: [N]/100
- Go-live: [tarih]

Açık riskler:
- [ör. henüz gerçek sipariş yok / WhatsApp test edilmedi]

Sonraki check-in: [tarih]
Referans izni: [Bekleniyor / Evet / Hayır]
```

Şablon: `IMPLEMENTATION_HANDOFF_CHECKLIST.md`

---

## 4. Müşteri “hızlı başlangıç” kartı (tek sayfa metin)

```
DukkanPilot — 5 adımda günlük kullanım

1. SİPARİŞ GELDİ
   → /Business/Orders veya Mutfak Modu
   → Durumu ilerlet: Beklemede → Hazırlanıyor → Tamamlandı

2. MENÜ GÜNCELLE
   → /Business/Products — fiyat / aktif-pasif

3. MÜŞTERİ ARADI
   → /Business/Customers — telefon ile ara / WhatsApp

4. KAMPANYA
   → /Business/Campaigns — tarih ve indirim

5. GÜN SONU
   → /Business/Reports — bugünkü ciro

Destek: /Business/Support
Eğitim: /Business/HelpCenter
```

PDF üretimi yok; metni WhatsApp veya e-posta ile gönderin.

---

## 5. Pilot programı bilgi notu (müşteriye)

```
PILOT PROGRAMI — SİZİN HAKLARINIZ

✓ Kurulum ve kickoff desteği
✓ İlk 90 gün öncelikli yanıt (mesai saatleri)
✓ Haftalık kısa check-in (ilk ay)
✓ Panel içi yardım merkezi ve destek talebi

SİZDEN BEKLENTİLERİMİZ

• Geri bildirim (ne işe yaradı / ne zorladı)
• Go-live sonrası gerçek kullanım
• Onayınızla kısa referans (isteğe bağlı)

Ödeme: aylık manuel tahsilat — detaylar teklif metninde.
Resmi fatura süreci ayrı yürütülür.
```

---

## 6. Teslim sonrası Admin kontrol listesi

- [ ] Hoş geldin / teslim özeti gönderildi
- [ ] Go-live onay alındı
- [ ] Handoff notu SalesRequest veya iç CRM’e yazıldı
- [ ] `/Admin/CustomerSuccess` — işletme izlemeye alındı
- [ ] İlk check-in tarihi takvime eklendi (`PILOT_TRACKING_PLAN.md`)
- [ ] Referans planı 30. gün için işaretlendi (`PILOT_TESTIMONIAL_PLAN.md`)

---

## 7. Yasal / güven hatırlatması (kısa ek)

```
Gizlilik ve kullanım: [SITE_URL]/Privacy · [SITE_URL]/Kvkk
Güven merkezi: [SITE_URL]/Trust

Taslak metinlerdir; kesin hukuki danışmanlık yerine geçmez.
```

---

## İlgili panel URL’leri (teslimde paylaş)

| Konu | URL |
|------|-----|
| Owner giriş | `/Account/Login` |
| Dashboard | `/Business/Dashboard` |
| Onboarding | `/Business/Onboarding` |
| Go-Live | `/Business/GoLive` |
| QR Menü | `/Business/QrMenu` |
| Faturalar (Owner) | `/Business/Billing/Invoices` |
| Destek | `/Business/Support` |
