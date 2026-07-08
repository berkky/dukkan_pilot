# DukkanPilot — Satış Demo Scripti

## 30 saniyelik pitch
“QR menü, WhatsApp sipariş, sadakat, kampanya ve raporları tek panelde sunuyoruz. Müşteri menüden sipariş verir; işletme mutfak ve CRM’den yönetir.”

## 5 dakikalık demo
1. Landing `/` → problem/çözüm + “Demo QR Menüyü Gör”
2. `/m/demo-kafe` → menü, sepete ürün, 100₺ üzeri %10 indirim
3. Sipariş oluştur → confirmation + tracking
4. Login → `/Business/Orders/Kitchen` siparişi ilerlet
5. `/Business/Reports` veya `/Business/DemoCenter` ile kapanış CTA: Ücretsiz kayıt

## 10 dakikalık demo
5 dakikalığa ek:
- MenuStudio / CSV import
- Kampanya create/edit (auto apply)
- Customers + Insights
- Notifications + AuditLogs
- Go-Live Merkezi skor
- Admin `/Admin/SalesCenter` (SaaS görünümü)

## Ekran eşlemesi
| Göster | Route |
|--------|--------|
| Public menü | `/m/demo-kafe` |
| Demo rehberi | `/Demo` |
| Demo Merkezi | `/Business/DemoCenter` |
| Mutfak | `/Business/Orders/Kitchen` |
| CRM | `/Business/Customers` |
| Rapor | `/Business/Reports` |
| Satış Merkezi | `/Admin/SalesCenter` |

## İtirazlara kısa cevap
- **WhatsApp sipariş bana yeter mi?** Panelde mutfak, durum, CRM ve rapor birleşir; dağınık sohbetten çıkar.
- **QR menüden farkı ne?** Sipariş + kampanya motoru + sadakat + operasyon paneli.
- **Kampanya yapmıyorum.** İsterseniz kapalı; açınca auto-apply ve performans raporu hazır.
- **Personel kullanır mı?** Staff/Owner roller; mutfak ve sipariş ekranları dokunmatik.
- **Verilerimi görür müyüm?** Tenant izolasyonu BusinessId claim; Admin kendi SaaS panelinde.

## Dikkat
- Public sayfada panel şifresi paylaşma.
- Gerçek müşteri verisi gösterme; Demo Kafe kullan.
- Ödeme entegrasyonu yok; abonelik talebi paneldendir.
