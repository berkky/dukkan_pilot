# DukkanPilot — Feedback Management Playbook (35A)

Geri bildirim ayrı public form değildir. Özellik istekleri **giriş yapmış işletme kullanıcısı** ticket'ı olarak toplanır.

## Kanal matrisi

| Kanal | Kim | Amaç |
|-------|-----|------|
| `FeatureRequest` ticket | Owner / Staff | Ürün fikri, iyileştirme |
| Diğer Support kategorileri | Owner / Staff | Teknik, kurulum, billing |
| `/Help`, `/Business/HelpCenter` | Herkes / panel | Self-service |
| Sales pipeline | Public / Owner | Demo, plan, yükseltme |

Public anonim feedback **yok** — spam ve KVKK riski.

## FeatureRequest — ne zaman?

**Kullan:** ürün özelliği, rapor/export, entegrasyon, UI önerisi, MVP dışı istek kaydı.

**Kullanma:**
- Acil arıza → `Technical` + `Urgent`/`High`
- Fatura/abonelik → `Billing` (+ `Admin/Billing`)
- Plan değişikliği → `SalesRequests` / `RequestUpgrade`
- Kurulum → `Onboarding` / `GoLive`

**Admin:** filtre `FeatureRequest` → bug ise kategori düzelt; roadmap için iç not; satış fırsatıysa `SalesCenter` notu.

`Source = Feedback` işaretleme içindir; ayrı tablo yok.

## Help yönlendirmesi

1. Self-service: `/Help` veya `/Business/HelpCenter` (`HELP_CENTER_CONTENT_MAP.md`)
2. Çözülmediyse: `/Business/Support/Create`
3. Help makalelerinde "ticket aç" CTA (35A polish)

## Sales yönlendirmesi

| İhtiyaç | Kanal |
|---------|-------|
| Demo | `/Sales/RequestDemo` |
| Yükseltme | `/Business/Billing/RequestUpgrade` |
| Fiyat | `/Pricing` → sales form |

FeatureRequest satış pipeline değildir.

## Öncelik

| Sinyal | Öncelik |
|--------|---------|
| Sipariş/menü durdu | Urgent |
| Tek kullanıcı UX | Normal |
| Nice-to-have | Low |
| Çoklu işletme aynı istek | High → backlog |

## İlgili

`SUPPORT_CENTER_RUNBOOK.md` · `SUPPORT_TICKET_DATA_MAP.md` · `HELP_CENTER_CONTENT_MAP.md` · `SALES_PIPELINE_RUNBOOK.md`
