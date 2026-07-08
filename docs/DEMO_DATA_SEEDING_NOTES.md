# Demo Data Seeding Notes (34B)

## İlke

- Migration yok
- Entity/DbContext değişikliği yok
- Demo seed **idempotent** olmalı
- Gerçek işletme verisi silinmez
- `demo-kafe` bozulmaz

## Demo işletmeler

Seed edilen slugs:
- demo-kafe
- demo-tatlici
- demo-burgerci
- demo-restoran
- demo-nargile

## Duplicate önleme

- Business: `Slug` ile kontrol
- Category: aynı `Name` varsa eklenmez
- Product: aynı `Name` varsa eklenmez
- Reward: aynı `Name` varsa eklenmez
- Campaign: aynı discount/auto-apply/minimum kriterleri varsa eklenmez

## Demo siparişleri

Public menü üzerinden test siparişi verilirse sistemin mevcut davranışına göre DB’ye sipariş kaydı oluşabilir. Bu aşamada bu davranış değiştirilmez.

