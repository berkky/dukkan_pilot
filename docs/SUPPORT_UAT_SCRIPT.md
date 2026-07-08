# 35A — Support / Ticket Center UAT Script

**Ön koşul:** Migration (`SupportTickets`, `SupportTicketMessages`); `owner@dukkanpilot.local`, `admin@dukkanpilot.local`.

## 1) Business: ticket oluştur

`/Business/Support/Create` → kategori `Technical`, konu + mesaj → kaydet.

**Beklenen:** `SUP-YYYYMMDD-####`, status `New`, admin `NewSupportTicket`, audit `Support.TicketCreated`.

## 2) Admin: public yanıt

`/Admin/Support/Details/{id}` → yanıt gönder.

**Beklenen:** status `InProgress`, business'ta görünür, `SupportTicketReplied`, audit `Support.AdminReplyAdded`.

## 3) Admin: iç not (gizli)

Aynı ticket → iç not ekle.

**Beklenen:** admin görür; business **görmez**; notification yok; audit `Support.InternalNoteAdded`.

## 4) Business: takip mesajı

Owner detay → yanıt (açık ticket).

**Beklenen:** thread güncellenir; admin `SupportTicketUpdated`; audit `Support.BusinessMessageAdded`.

## 5) Durum yaşam döngüsü

Admin: `WaitingCustomer` → business notification → `Resolved` + `ResolutionSummary` → `Closed`.

**Beklenen:** audit `Support.StatusChanged`; kapalı ticket'ta business mesaj reddedilir.

## 6) Tenant izolasyonu

Başka tenant kullanıcısı ile foreign ticket Id dene.

**Beklenen:** Forbid/404; veri sızıntısı yok.

## 7) FeatureRequest

Create → kategori `FeatureRequest`.

**Beklenen:** etiket "Özellik isteği"; normal ticket akışı.

## 8) Negatif

- [ ] Public anonim ticket formu yok
- [ ] E-posta gönderimi yok
- [ ] Expired abonelikte Support erişilebilir (gate dışı)

## Sonuç

| # | Senaryo | PASS/FAIL | Not |
|---|---------|-----------|-----|
| 1–8 | | | |
