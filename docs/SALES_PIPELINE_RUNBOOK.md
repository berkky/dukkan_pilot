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

1. `/Admin/Businesses/Subscription/{id}` planı güncelle
2. Talebi Won bırak / not ekle
3. Müşteriye paneli doğrula

## Checklist (müşteri dönüşü)

- [ ] İletişim kuruldu
- [ ] Plan teyit
- [ ] Legal/KVKK hatırlatma
- [ ] Abonelik manuel set
- [ ] Smoke: login + billing

## Spam / duplicate

- Public: aynı email + type + plan + açık status, 24 saat
- Business: aynı business + plan + açık status

## Legal

Formlar “açık rıza verdim” demez; Gizlilik/KVKK link inceleme onayı ister.
