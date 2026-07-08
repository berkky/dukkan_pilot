# DukkanPilot — Cookie and Tracking Notes

## Şu anki durum

- Harici analytics / reklam / tracking script **yok**
- Zorunlu oturum çerezi: `DukkanPilot.Auth` (cookie authentication)
- Form antiforgery çerezleri çerçeveye göre değişken adlarla kullanılabilir
- Cookie notice: `localStorage` anahtarı `dp_cookie_notice_dismissed` (“Anladım”)
- Consent-management platformu / CMP iddiası **yok**

## Dosyalar

- `Views/Shared/_CookieNotice.cshtml`
- `wwwroot/js/cookie-notice.js`
- `Views/Legal/Cookies.cshtml`

## Analytics eklenirse checklist

- [ ] `/Cookies` politikası güncelle
- [ ] KVKK / Privacy metinleri güncelle
- [ ] Gerekirse açık rıza / tercih paneli (hukuki görüş)
- [ ] Smoke + legal readiness checklist güncelle
- [ ] Tracking script’i legal olmadan prod’a alma

## Not

Cookie notice yalnızca bilgilendirme amaçlıdır; “tüm çerezlere rıza alındı” iddiası taşımaz.
