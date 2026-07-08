# DukkanPilot — Performance Smoke Tests

## Script

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-performance-smoke.ps1 -BaseUrl http://localhost:5000
```

Uygulama çalışırken çalıştırın (`dotnet run` veya publish instance).

## Parametreler

| Parametre | Varsayılan | Açıklama |
|-----------|------------|----------|
| `BaseUrl` | `http://localhost:5000` | Hedef instance |
| `WarningMs` | `1500` | Bu sürenin üstü WARN |
| `FailMs` | `4000` | Bu sürenin üstü FAIL |
| `Repeat` | `1` | Path başına istek sayısı |
| `Paths` | (boş) | Virgülle ayrılmış özel path listesi |

Örnek:

```powershell
.\scripts\check-performance-smoke.ps1 -BaseUrl http://localhost:5000 -WarningMs 2000 -FailMs 5000 -Repeat 2
```

## Ölçülen varsayılan route'lar

- `/`, `/Pricing`, `/DemoPacks`, `/RoiCalculator`, `/Help`
- `/m/demo-kafe`, `/m/demo-tatlici`, `/m/demo-burgerci`, `/m/demo-restoran`, `/m/demo-nargile`
- `/health`

## Sonuç yorumlama

| Sonuç | Anlam | Exit code |
|-------|-------|-----------|
| PASS | 200 ve süre ≤ WarningMs | 0 |
| WARN | 200 ama WarningMs < süre ≤ FailMs | 0 |
| FAIL | non-200 veya süre > FailMs | 1 |

**WARN release'i bloklamaz**; FAIL bloklar.

## Cold-start notu

İlk HTTP isteği JIT, EF model compile ve connection pool nedeniyle yavaş olabilir. Şüphede `-Repeat 2` kullanın; ikinci istek daha temsili olabilir. Yine de bu script **benchmark değildir**.

## release-quality-gate entegrasyonu

`release-quality-gate.ps1` web checks çalışırken performance smoke'u da çalıştırır.

Atlamak için: `-SkipPerformanceSmoke`

Eşik ayarı: `-PerformanceWarningMs 2000 -PerformanceFailMs 5000`

## Ne yapmaz?

- Auth'lu Business/Admin sayfalarını ölçmez (public smoke).
- DB query sayısı / SQL süresi ölçmez.
- Load / stress test yapmaz.
- Harici araç gerektirmez (PowerShell + HttpClient).

## İlgili

- `PERFORMANCE_HARDENING_GUIDE.md`
- `RELIABILITY_RUNBOOK.md`
- `run-smoke-tests.ps1` (fonksiyonel smoke, süre ölçmez)
