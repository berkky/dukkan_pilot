# DukkanPilot — Support Ticket Data Map (35A)

Tablolar: `SupportTickets`, `SupportTicketMessages`. Kapanış `Status` ile; soft delete yok.

## SupportTicket (özet)

| Alan | Limit | Not |
|------|-------|-----|
| BusinessId | FK | **Tenant anahtarı** |
| TicketNumber | 30, unique | `SUP-YYYYMMDD-####` |
| Subject | 300 | |
| Category | 40 | `Technical`, `Order`, `Menu`, `Billing`, `Account`, `Campaign`, `Loyalty`, `Report`, `Onboarding`, `FeatureRequest`, `Other` |
| Priority | 20 | `Low`, `Normal`, `High`, `Urgent` |
| Status | 40 | Runbook'a bak |
| Source | 40 | `BusinessPanel`, `AdminCreated`, `Feedback` |
| CreatedBy* / AssignedAdmin* | | Kullanıcı snapshot |
| RelatedEntityName / Id | 80 / int? | İlgili ekran |
| LastMessageAtUtc / ByRole | | Liste özeti |
| ResolutionSummary / AdminInternalNote | 2000 | |
| ClosedAtUtc | | Kapalı durumlarda |
| MetadataJson | max | Şifre/token yok |

**İndeksler:** `TicketNumber` (unique) · `(BusinessId, CreatedAtUtc)` · `(BusinessId, Status)` · `(Status, Priority)` · `AssignedAdminUserId` · `Category` · `LastMessageAtUtc`

## SupportTicketMessage (özet)

| Alan | Limit | Not |
|------|-------|-----|
| SupportTicketId | FK | Restrict delete |
| BusinessId | int | Denormalize tenant |
| SenderRole | 20 | `Business`, `Admin`, `System` |
| Message | 4000 | |
| IsInternal | bool | Admin-only görünürlük |
| IsSystemMessage | bool | |

**İndeksler:** `(SupportTicketId, CreatedAtUtc)` · `(BusinessId, CreatedAtUtc)` · `SenderRole` · `IsInternal`

## Tenant kuralları

- Business sorguları: `BusinessId == claim`
- Business detay: `IsInternal = false` mesajlar
- Cross-tenant erişim yok
- Kapalı ticket'ta yeni mesaj reddedilir

## Audit actions

`Support.TicketCreated` · `Support.BusinessMessageAdded` · `Support.AdminReplyAdded` · `Support.InternalNoteAdded` · `Support.StatusChanged` · `Support.PriorityChanged` · `Support.Assigned`

Entity: `SupportTicket`. Metadata'da e-posta/telefon yok.

## Notification types

| Type | Hedef | Tetik |
|------|-------|-------|
| `NewSupportTicket` | Admin | Create |
| `SupportTicketUpdated` | Admin | Business mesaj |
| `SupportTicketReplied` | Business | Admin yanıt |
| `SupportTicketStatusChanged` | Business | WaitingCustomer vb. |
| `SupportTicketResolved` | Business | Resolved / Closed |

Related: `SupportTicket` + ticket Id. Fail-safe servis.

## Minimizasyon

Public anonim feedback yok. Ticket silinmez; `Closed` / `Cancelled` ile arşivlenir.
