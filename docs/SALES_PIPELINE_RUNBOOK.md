# DukkanPilot — Sales Pipeline Runbook

Online ödeme yoktur. Talep → admin takip → manuel abonelik güncellemesi.

## Akışlar

### Public demo / plan talebi

1. `/Sales/RequestDemo` veya `/Sales/RequestPlan?planId=`
2. Form + Privacy/KVKK checkbox
3. `SalesRequests` kaydı (`New`)
4. Admin notification
5. Optional public audit (email/phone metadata yok)

### Business plan yükseltme

1. Owner: `/Business/Billing` → RequestUpgrade
2. `CreateBusinessPlanRequestAsync` (`BusinessBilling` / `UpgradeRequest`)
3. Aynı `BusinessId` + plan + açık status → duplicate engeli
4. Owner: `/Business/Billing/Requests`
5. Admin: `/Admin/SalesRequests`

## Status

| Status | Anlam |
|--------|--------|
| New | Yeni |
| Contacted | İletişime geçildi |
| Qualified | Nitelikli fırsat |
| WaitingCustomer | Müşteri yanıtı bekleniyor |
| Won | Kazanıldı → aboneliği Admin Businesses/Subscription ile manuel güncelle |
| Lost | Kaybedildi |
| Cancelled | İptal |

## Won sonrası

1. `BusinessId` bağlı değilse işletme oluştur / bağla
2. (Opsiyonel) `/Admin/Billing/CreateInvoice` ile **iç tahsilat kaydı** oluştur (resmi fatura değildir)
3. Ödeme geldiyse `/Admin/Billing/RecordPayment` ile manuel ödeme kaydı gir
4. Aboneliği manuel güncelle: `/Admin/Businesses/Details/{businessId}` (abonelik alanı)
5. Talebi Won bırak / not ekle
6. `/Admin/Onboarding` ile kurulum skorunu aç; Details’ten checklist izle
7. Kickoff: `docs/KICKOFF_MEETING_SCRIPT.md`
8. Handoff: `docs/IMPLEMENTATION_HANDOFF_CHECKLIST.md`
9. Müşteriye `/Business/Onboarding` göster
10. Go-live sonrası `/Admin/CustomerSuccess` ile health monitoring başlat

## Checklist (müşteri dönüşü)

- [ ] İletişim kuruldu
- [ ] Plan teyit
- [ ] Legal/KVKK hatırlatma
- [ ] Abonelik manuel set
- [ ] Onboarding handoff (`/Admin/Onboarding`)
- [ ] Health monitoring (`/Admin/CustomerSuccess` + `/Business/Success`)
- [ ] Smoke: login + billing + onboarding + success

## Spam / duplicate

- Public: aynı email + type + plan + açık status, 24 saat
- Business: aynı business + plan + açık status

## Legal

Formlar “açık rıza verdim” demez; Gizlilik/KVKK link inceleme onayı ister.
