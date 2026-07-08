# 33A — Manual Billing Runbook (İç Tahsilat Operasyonları)

Bu doküman **gerçek ödeme sağlayıcısı/e-Fatura/e-Arşiv entegrasyonu olmadan** DukkanPilot içinde yapılan **manuel tahsilat takibi** operasyonunu anlatır.

## Önemli hukuki/mali not

- DukkanPilot’ta oluşturulan “invoice / tahsilat kaydı” **resmi e-Fatura/e-Arşiv/e-Belge değildir**.
- Bu kayıtlar yalnızca **iç operasyon / tahsilat takip** amaçlıdır.
- Resmi fatura/e-Belge süreçleri işletmenin muhasebe/mali müşavir ve GİB süreçleriyle ayrıca yürütülmelidir.

## Hızlı akış (Won → Tahsilat → Abonelik)

1. **SalesRequest “Won”** (Admin)
   - `/Admin/SalesRequests/Details/{id}`
   - İşletme bağlanmışsa “Bu talep için tahsilat kaydı oluştur” CTA’sı görünür.

2. **Tahsilat kaydı oluştur** (Admin)
   - `/Admin/Billing/CreateInvoice`
   - Business seç, başlık/tutar/vade gir
   - Kayıt oluşturulur: `BillingInvoice` (InvoiceNumber: `INV-YYYYMM-0001`)
   - Audit: `Billing.InvoiceCreated`
   - Business notification: `BillingInvoiceCreated`

3. **Ödeme kaydı işle** (Admin)
   - `/Admin/Billing/RecordPayment?invoiceId={id}`
   - Tutar/tarih/yöntem gir (Havale/EFT vb.)
   - Kayıt oluşturulur: `BillingPayment`
   - Invoice `PaymentStatus` otomatik güncellenir: `Unpaid` → `Partial` → `Paid`
   - Audit: `Billing.PaymentRecorded`
   - Business notification: `BillingPaymentRecorded` (+ paid olursa `BillingInvoicePaid`)

4. **Abonelik uzatma (manuel)** (Admin)
   - Ödeme kaydı aboneliği otomatik uzatmaz.
   - Abonelik güncellemesi mevcut akıştan yapılır: `/Admin/Businesses/Details/{businessId}` (abonelik alanı)

## Gecikme yönetimi (Overdue)

- Background job yok; gecikme mantığı invoice status’ünde taşınır (`Overdue`).
- Admin izleme: `/Admin/Billing` (KPI + gecikmiş liste)
- Business görünümü: `/Business/Billing/Invoices`
- Spam riskinden kaçınmak için otomatik “overdue notification” üretilmez (33A kapsamı).

## İptal akışı

- `/Admin/Billing/Details/{id}` üzerinden “İptal” (Cancel) yapılır.
- Invoice `Status=Cancelled`, `PaymentStatus=Cancelled`.
- Audit: `Billing.InvoiceCancelled`
- Business notification: `BillingInvoiceCancelled`

