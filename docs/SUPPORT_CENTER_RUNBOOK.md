# DukkanPilot — Support Center Runbook (35A)

E-posta entegrasyonu yoktur. İletişim panel içi mesaj + in-app notification ile yürür.

## Roller

| Rol | Erişim |
|-----|--------|
| BusinessOwner / Staff | Kendi tenant ticket'ları |
| SuperAdmin | Tüm ticket'lar — yanıt, iç not, durum, atama |

Public anonim ticket yok. Giriş yapmış işletme kullanıcısı gerekir.

## URL'ler

| Ekran | URL |
|-------|-----|
| İşletme liste / oluştur / detay | `/Business/Support`, `/Create`, `/Details/{id}` |
| Admin liste / detay | `/Admin/Support`, `/Details/{id}` |

Support **subscription gate dışındadır** (abonelik sorunlarında da erişilebilir).

## Akış — işletme oluşturma

1. `/Business/Support/Create` → kategori, öncelik, konu, mesaj
2. `Status = New`, `Source = BusinessPanel`, numara `SUP-YYYYMMDD-####`
3. Admin notification: `NewSupportTicket` · audit: `Support.TicketCreated`

## Akış — admin yanıt

1. `/Admin/Support/Details/{id}` → public yanıt veya iç not
2. Public yanıt: `InProgress`, business notification `SupportTicketReplied`, audit `Support.AdminReplyAdded`
3. İç not: `IsInternal = true` — işletmeye görünmez, notification yok

## Durum yaşam döngüsü

| Status | Anlam |
|--------|--------|
| New | Yeni |
| Open | Atandı |
| InProgress | Admin yanıtladı |
| WaitingCustomer | İşletme yanıtı bekleniyor |
| WaitingAdmin | Destek yanıtı bekleniyor |
| Resolved / Closed / Cancelled | Kapalı — yeni mesaj yok |

**Otomatik:** Business mesaj → `Open`; admin yanıt (`New`/`WaitingAdmin`) → `InProgress`; atama (`New`) → `Open`.

**İşletmeye bildirim:** `WaitingCustomer`, `Resolved`, `Closed` durum değişimlerinde.

## Operasyon checklist

- [ ] Yeni ticket'ları günde kontrol et; Acil/Yüksek önce
- [ ] Yanıt sonrası durum güncelle; kapatırken `ResolutionSummary`
- [ ] İç not yalnızca ekip içi
- [ ] Billing/satış için `SalesRequests` / `Admin/Billing` çapraz bak

## Sınırlamalar

E-posta/push/WhatsApp yok · SLA/escalation yok · dosya eki yok · hard delete yok.

## İlgili

`SUPPORT_TICKET_DATA_MAP.md` · `SUPPORT_UAT_SCRIPT.md` · `FEEDBACK_MANAGEMENT_PLAYBOOK.md` · `ADMIN_SUPPORT_KNOWLEDGE_BASE.md`
