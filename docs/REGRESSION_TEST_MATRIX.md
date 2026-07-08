# Regression Test Matrix

| Modül | URL | Rol | Test adımı | Beklenen sonuç | Risk | Otomasyon |
|------|-----|-----|-----------|----------------|------|----------|
| Landing | `/` | Anon | Sayfa aç | 200 + CTA’lar | Med | Script |
| Landing | `/Pricing` | Anon | Sayfa aç | 200 | Med | Script |
| Landing | `/Features` | Anon | Sayfa aç | 200 | Low | Script |
| Demo | `/Demo` | Anon | Sayfa aç | 200, şifre yok | High | Script |
| Trust | `/Trust` | Anon | Sayfa aç | 200, taslak uyarısı | Med | Script |
| Legal | `/Privacy` | Anon | Sayfa aç | 200 | High | Script |
| Legal | `/Kvkk` | Anon | Sayfa aç | 200 | High | Script |
| Legal | `/Terms` | Anon | Sayfa aç | 200 | Med | Script |
| Legal | `/Cookies` | Anon | Sayfa aç | 200 | Med | Script |
| Legal | `/DataProcessing` | Anon | Sayfa aç | 200 | Med | Script |
| SalesRequest | `/Sales/RequestDemo` | Anon | Form aç | 200 | High | Script+Manual |
| SalesRequest | `/Sales/RequestPlan` | Anon | Form aç | 200 | High | Script+Manual |
| Account | `/Account/Login` | Anon | Form aç | 200 | High | Script |
| Account | `/Account/Register` | Anon | Form aç | 200 | Med | Script |
| Public menu | `/m/demo-kafe` | Anon | Menü aç | 200 + ürün/kategori görünür | High | Script |
| Cart/order | `/m/{slug}` | Anon | Sepete ekle → WhatsApp order | Order oluşur + WhatsApp metni | Critical | Manual |
| Tracking | `/m/{slug}/order-confirmation` | Anon | Order sonrası görüntüle | Doğru özet | High | Manual |
| Tracking | `/m/{slug}/order-status/{token}` | Anon | Status görüntüle | 200 | High | Manual |
| Business Dashboard | `/Business/Dashboard` | Auth | Aç | 200 + kartlar | High | Manual |
| Business GoLive | `/Business/GoLive` | Auth | Aç | 200 + checklist | High | Manual |
| Business Onboarding | `/Business/Onboarding` | Auth | Aç | 200 + score/checklist | High | Manual |
| Business Success | `/Business/Success` | Auth | Aç | 200 + risk/recommendation | High | Manual |
| Products/Categories | `/Business/Products` | Auth+sub | Liste aç | Tenant filtre | High | Manual |
| Campaigns | `/Business/Campaigns` | Auth+sub | Liste aç | Tenant filtre | High | Manual |
| Orders/Kitchen | `/Business/Orders/Kitchen` | Auth+sub | Status ilerlet | Workflow doğru | Critical | Manual |
| Customers/Reports | `/Business/Customers/Insights` | Auth+sub | İçgörü aç | Segmentler | Med | Manual |
| Audit/Notifications | `/Business/Notifications` | Auth | Aç | 200 | Med | Manual |
| Billing | `/Business/Billing/Requests` | Owner | Aç | 200 | Med | Manual |
| Help Center | `/Help` | Anon | Aç | 200, şifre yok | Med | Script |
| Help Center | `/Help/nedir` | Anon | Makale aç | 200 | Low | Script |
| Help Center | `/Business/HelpCenter` | Auth | Aç | 200 + arama | Med | Manual |
| Help Center | `/Admin/HelpCenter` | SuperAdmin | Aç | 200 | Med | Manual |
| ROI Calculator | `/RoiCalculator` | Anon | Aç + POST | 200, disclaimer | Med | Script |
| ROI Calculator | `/Business/ValueCalculator` | Auth | Aç | 200 + prefill | Med | Manual |
| ROI Calculator | `/Admin/ValueCalculator` | SuperAdmin | Aç | 200 | Med | Manual |
| Admin Dashboard | `/Admin/Dashboard` | SuperAdmin | Aç | 200 + KPI | High | Manual |
| Admin SalesRequests | `/Admin/SalesRequests` | SuperAdmin | Aç | 200 | High | Manual |
| Admin Onboarding | `/Admin/Onboarding` | SuperAdmin | Aç | 200 | High | Manual |
| Admin CustomerSuccess | `/Admin/CustomerSuccess` | SuperAdmin | Aç | 200 | High | Manual |
| Admin Operations | `/Admin/Operations` | SuperAdmin | Aç | 200 | Med | Manual |
| Admin Quality | `/Admin/Quality` | SuperAdmin | Aç | 200 + readiness cards | Med | Manual |
| System | `/health` | Anon | Aç | 200 JSON status ok | High | Script |
| SEO | `/robots.txt` | Anon | Aç | Disallow Admin/Business/Account | Med | Script |
| SEO | `/sitemap.xml` | Anon | Aç | Public URL var, private yok | Med | Script |
| Security | `/` | Anon | Header kontrol | nosniff/frame/referrer/permissions | Med | Script |

