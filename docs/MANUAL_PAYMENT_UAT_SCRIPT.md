# 33A — Manual Payment / Billing UAT Script

## Roller

- **SuperAdmin**: tahsilat kaydı ve ödeme kaydı oluşturur
- **BusinessOwner**: ledger ekranlarını görüntüler (read-only)

## Senaryo

### 1) Admin: invoice oluştur

1. `/Admin/Billing/CreateInvoice`
2. Business seç
3. Başlık + tutar + vade gir
4. Kaydet

Beklenen:
- InvoiceNumber üretildi (`INV-YYYYMM-0001`)
- Status `Issued`, PaymentStatus `Unpaid`
- Business notification oluştu (`BillingInvoiceCreated`)
- Audit log oluştu (`Billing.InvoiceCreated`)

### 2) Admin: invoice detay kontrol

1. `/Admin/Billing/Details/{id}`

Beklenen:
- Ödeme özeti (ödenen/kalan) 0 görünür
- “Resmi belge değildir” uyarısı görünür

### 3) Admin: ödeme kaydı gir

1. `/Admin/Billing/RecordPayment?invoiceId={id}`
2. Tutar gir (toplamdan küçük → Partial testi)
3. Method: BankTransfer
4. Kaydet

Beklenen:
- Payment kaydı oluştu
- Invoice PaymentStatus `Partial`
- Audit: `Billing.PaymentRecorded`
- Business notification: `BillingPaymentRecorded`

### 4) Admin: ikinci ödeme ile Paid

1. Aynı invoice’a ikinci ödeme gir (kalanı kapat)

Beklenen:
- Invoice PaymentStatus `Paid`, Status `Paid`
- Business notification: `BillingInvoicePaid`
- UI’da “Ödeme kaydı aboneliği otomatik uzatmaz” notu görünür

### 5) BusinessOwner: ledger görüntüle

1. Owner login
2. `/Business/Billing/Invoices`
3. `/Business/Billing/Payments`

Beklenen:
- Sadece kendi BusinessId kayıtları görünür
- “Resmi belge değildir” disclaimer görünür

### 6) Admin: iptal testi

1. `/Admin/Billing/Details/{id}` → İptal

Beklenen:
- Invoice Status `Cancelled`, PaymentStatus `Cancelled`
- Audit: `Billing.InvoiceCancelled`

