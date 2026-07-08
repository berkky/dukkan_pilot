# DukkanPilot — Legal Readiness Checklist

Bu liste hukuki danışmanlık değildir. Canlı satış / canlı müşteri verisi öncesi avukat veya KVKK uzmanı onayı önerilir.
“Tam KVKK uyumludur” iddiası kullanılmaz; hedef: KVKK süreçlerine uygun yönetilebilir yapı.

## Canlı öncesi kontrol

- [ ] Gizlilik Politikası (`/Privacy`) taslağı gözden geçirildi
- [ ] KVKK Aydınlatma Metni (`/Kvkk`) placeholder’lar gerçek ünvan/adres/e-posta ile dolduruldu
- [ ] Açık rıza gerekiyorsa **ayrı** rıza metni hazırlandı (aydınlatmadan ayrı)
- [ ] Çerez Politikası (`/Cookies`) güncel (analytics eklenirse güncelle)
- [ ] Kullanım Şartları (`/Terms`) hukukçu ile netleştirildi
- [ ] Veri İşleme / Trust (`/DataProcessing`, `/Trust`) satış mesajı ile uyumlu
- [ ] Veri sorumlusu / veri işleyen rolleri sözleşme ile belirlendi
- [ ] İşletme müşteri verileri (public sipariş + CRM) için işletmeye rehber verildi
- [ ] WhatsApp yönlendirme (`wa.me`, API yok) doğru anlatıldı
- [ ] Backup / Audit / Notification’da hassas veri minimizasyonu gözden geçirildi
- [ ] Public Demo sayfasında panel şifresi yok
- [ ] Cookie notice çalışıyor (zorunlu bilgilendirme; consent-management iddiası yok)
- [ ] Public sales form (`/Sales/RequestDemo`, `/Sales/RequestPlan`) Privacy/KVKK checkbox + link kontrolü
- [ ] SalesRequest data map gözden geçirildi (`SALES_REQUEST_DATA_MAP.md`)
- [ ] Avukat / KVKK uzmanı onayı alındı (tarih/not)

## Repo dokümanları

- `PRIVACY_AND_DATA_MAP.md`
- `COOKIE_AND_TRACKING_NOTES.md`
- `TERMS_TEMPLATE_NOTES.md`
- `OPERATIONAL_SECURITY_CHECKLIST.md`
- `FIRST_RELEASE_OPERATIONS.md`

## Smoke

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-smoke-tests.ps1 -BaseUrl http://localhost:5000
```

Legal URL’ler: `/Privacy`, `/Terms`, `/Kvkk`, `/Cookies`, `/DataProcessing`, `/Trust`.
