# 33A — Billing Operations Data Map

Bu doküman, 33A manuel tahsilat operasyonunda tutulan verileri ve görünürlük sınırlarını özetler.

## Entities

### `BillingInvoice` (iç tahsilat kaydı)

- **Amaç**: Vade/tutar/ödeme durumu takibi
- **Resmi belge değil**: `IsOfficialInvoice=false` varsayılan
- **Temel alanlar**:
  - `BusinessId`
  - `InvoiceNumber` (iç takip no)
  - `Title`, `Description`
  - `Amount`, `TaxAmount`, `TotalAmount`, `Currency`
  - `IssueDate`, `DueDate`, `PeriodStart/PeriodEnd`
  - `Status`, `PaymentStatus`, `Source`
  - `RelatedSalesRequestId`
  - `AdminNotes`, `BusinessVisibleNote`

### `BillingPayment` (manuel ödeme kaydı)

- **Amaç**: Manuel tahsilat kaydı ve invoice payment-status hesaplaması
- **Hassas veri yok**:
  - Kart bilgisi yok
  - IBAN/banka hesap no yok
  - Dekont dosyası upload yok
- **Temel alanlar**:
  - `BusinessId`, `BillingInvoiceId?`
  - `Amount`, `Currency`, `PaymentDate`
  - `Method`, `Status`
  - `ReferenceNumber?`, `PayerName?`
  - `AdminNotes?`, `BusinessVisibleNote?`

## Kim neyi görür?

### BusinessOwner (Business Area)

- `/Business/Billing/Invoices`:
  - Kendi işletmesine ait invoice listesi + `BusinessVisibleNote`
- `/Business/Billing/Payments`:
  - Kendi işletmesine ait ödeme kayıtları + `BusinessVisibleNote`

> Bu ekranlar **read-only**; Business ödeme kaydı oluşturamaz.

### SuperAdmin (Admin Area)

- `/Admin/Billing`:
  - Tüm işletmelerde KPI + invoice listesi + ödeme özeti
- `/Admin/Billing/Details/{id}`:
  - Invoice detay + ilişkili payment listesi
- `/Admin/Billing/RecordPayment`:
  - Manuel ödeme girişi

## Audit / Notification minimizasyonu

- Audit metadata:
  - `invoiceNumber`, `totalAmount`, `dueDate`, `relatedSalesRequestId`
  - payment tarafında: `invoiceId`, `amount`, `method`
- AdminNotes içerikleri audit metadata’ya “tam metin” olarak basılmaz (kısa/özet).
- Notification metadata içinde token/secret/PII saklanmaz.

