# DukkanPilot — Ürün Konumlandırması

## Ne satarız?
Kafe/restoran/tatlıcı için **QR Menü + WhatsApp Sipariş + Sadakat + Kampanya + CRM + Rapor** abonelik SaaS’ı.

## Hedef müşteri
- Tek şube kafe / kahve
- Fast-casual restoran
- Tatlıcı / pastane
- Küçük yiyecek zinciri noktaları

## Ana değer önerileri
1. Dakikalar içinde dijital menü (Go-Live)
2. WhatsApp’a hazır sipariş mesajı + panel takibi
3. Gerçek kampanya indirim motoru + performans
4. Sadakat puanı / ödül
5. Audit log + bildirim merkezi ile güven
6. Satış sonrası kurulum desteği ve onboarding (Kurulum Sihirbazı + Admin Kurulum Takibi)
7. Customer success / retention health (İşletme Sağlığı + Customer Success Merkezi)
8. Release quality gate + QA dokümanları (Kalite Merkezi + script tabanlı doğrulama)
9. Manuel tahsilat operasyonu (Admin Billing + işletme tahsilat defteri; resmi e-Belge değil)
10. Self-service Help Center + eğitim rehberleri (Public/Business/Admin)
11. ROI / Değer Hesaplayıcı — tahmini senaryo (garanti gelir vaadi değil; satış dönüşümü)
12. Vertical Demo Packs — sektörüne uygun demo menü örnekleri (kafe/tatlıcı/burgerci/restoran/lounge)
13. Performance / reliability hardening — AsNoTracking, liste limitleri, performance smoke script, Operasyon/Kalite checklist'leri

## Ayrışma
- Sadece PDF menü değil; sipariş + operasyon
- Sadece WhatsApp bot değil; tenant paneli + rapor
- Identity/SignalR karmaşası yok; hızlı MVP

## Plan anlatımı
- Free: deneme / düşük limit
- Starter: tek şube günlük operasyon
- Pro: yüksek ürün/kampanya kullanımı  
Plan yükseltme talebi paneldendir; ödeme gateway yok.
Satış akışı: **Talep oluştur → Admin SalesRequests takip → iç tahsilat kaydı + manuel ödeme → manuel abonelik güncellemesi** (`docs/SALES_PIPELINE_RUNBOOK.md`, `docs/MANUAL_BILLING_RUNBOOK.md`).
İç tahsilat kayıtları muhasebe/resmi fatura yerine geçmez.

## Demo cümleleri
- “Menüyü telefonla açın, sepete 100₺ koyun; indirim otomatik gelsin.”
- “Sipariş geldiğinde mutfak ekranı ve müşteri kaydı hazır.”
- “Kurulum ve health skorlarıyla eksikleri ve churn riskini adım adım görün.”

## Landing kısa metinler
- Hero: QR Menü, WhatsApp Sipariş ve Sadakat tek panelde
- CTA: Ücretsiz Başla / Demo QR Menüyü Gör
- Güven: Panel şifreleri public’te paylaşılmaz
