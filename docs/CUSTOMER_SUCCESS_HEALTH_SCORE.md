# Customer Success Health Score

## Nedir?

DukkanPilot Customer Success Health Score, bir işletmenin ürünü gerçekten kullanıp kullanmadığını, churn riski taşıyıp taşımadığını ve büyüme fırsatı olup olmadığını hızlıca görmek için kullanılan **operasyonel önceliklendirme skorudur**.

Bu skor:

- kesin churn tahmini değildir
- finansal garanti değildir
- read-only hesaplanır
- yeni tablo/kolon olmadan mevcut verilerden türetilir

## Nasıl hesaplanır?

0–100 arası skor; pozitif sinyaller toplanır, risk penalty uygulanır.

Ana gruplar:

- Usage: sipariş, completed order, kitchen kullanımı
- Menu readiness: kategori, ürün, public menü
- Operational engagement: audit / notification / panel aktivitesi
- Revenue & customer activity: ciro, ort. sepet, tekrar eden müşteri
- Subscription health: plan geçerliliği, kalan gün, trial/active durumu
- Adoption depth: kampanya, ödül, CRM, staff
- Risk penalty / growth bonus

## Kullanım sinyalleri

- Son 7 / 30 günde sipariş
- Son 30 günde completed order
- Kitchen flow kullanımı
- Aktif kategori / ürün
- Public menu readiness

## Risk sinyalleri

- Aktif işletme ama ürün yok
- Son 30 günde sipariş yok
- WhatsApp eksik
- Abonelik geçersiz veya 7 gün içinde bitiyor
- Onboarding düşük
- Müşteri verisi zayıf
- Kritik bildirim var
- Audit aktivitesi yok
- Public menu hazır değil

## Growth / upgrade sinyalleri

- Plan limitlerine yaklaşma
- Son 30 gün yüksek sipariş hacmi
- Kampanya / ödül kullanımı
- Tekrar eden müşteri
- Açık upgrade request

## Status açıklamaları

- `Critical`: 0–34
- `AtRisk`: 35–59
- `Stable`: 60–79
- `Healthy`: 80–89
- `GrowthReady`: 90–100

## Churn risk

- `Low`
- `Medium`
- `High`
- `Critical`

## Expansion potential

- `None`
- `Watch`
- `GoodFit`
- `StrongFit`

## Nasıl yorumlanmalı?

- Düşük skor = bugün müdahale önceliği
- Orta skor = kullanım derinleştirme
- Yüksek skor = retention güçlü; upgrade konuşması için aday olabilir

Business ekranı: `/Business/Success`  
Admin ekranı: `/Admin/CustomerSuccess`
