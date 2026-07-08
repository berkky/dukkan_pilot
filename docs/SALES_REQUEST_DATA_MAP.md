# DukkanPilot — SalesRequest Data Map

Taslak. Hukuki sınıflandırma canlı öncesi netleştirilir.

| Alan | Neden | Kim görür |
|------|-------|-----------|
| ContactName, BusinessName, Email, Phone | Geri dönüş | SuperAdmin; Owner kendi taleplerinde |
| Message | İhtiyaç notu | SuperAdmin; Owner (kendi) |
| AdminNotes | İç not | SuperAdmin only (Owner’a tam metin gerekmez) |
| Requested/Current plan | Pipeline | Admin + Owner |
| Source / RequestType / Status | Operasyon | Admin + Owner (status) |
| IpAddress / UserAgent | Güvenlik | Admin |
| MetadataJson | source/plan id | Admin (email/phone yok) |
| Privacy/Kvkk flags | Form onayı | Admin |

Saklama: iş amaçlı süre + hukuki politika. Soft delete yok; Cancelled/Lost ile kapatılır.
Temizlik: eski Lost/Cancelled periyodik arşiv önerisi.

Minimizasyon: şifre/token/kart yok; audit metadata’da email/phone yok.
