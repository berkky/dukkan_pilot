# Tenant and Table Service Integration Tests (36C)

## Purpose

This suite is the regression safety net for tenant isolation, table service, and table QR ordering. It exercises the MVC application through `WebApplicationFactory<Program>`; it does not call controllers directly.

## Test database isolation

- The test host uses `Testing` and replaces only its `AppDbContext` registration.
- Each factory owns one open `Data Source=:memory:` SQLite connection for its full lifetime.
- `SqliteTestAppDbContext` is test-only. After the production model is built it clears only explicit `nvarchar(max)` column types, which SQLite cannot parse. Production `AppDbContext`, entities, migrations, and snapshots remain unchanged.
- The factory executes `PRAGMA foreign_keys = ON`, reads it back, and fails startup unless the value is `1`.
- It verifies the provider is SQLite and that the `BusinessTables` and `Orders` DDL exists without `nvarchar(max)`.
- `EnsureCreated` is used only on the in-memory SQLite connection. The suite never calls `Database.Migrate`, `dotnet ef database update`, or a LocalDB/DukkanPilotDb connection.

The model still retains required fields, maximum lengths, precision, indexes, unique constraints, foreign keys, and delete behavior; only the SQLite-incompatible provider type token is adapted for the test model.

## Authentication and request safety

The Testing-only authentication handler creates normal application claim shapes (`NameIdentifier`, role, business id, and business role) from test request headers. It does not change the production cookie scheme. Tests keep real authorization attributes and validate antiforgery tokens for state-changing requests.

## Coverage

- SQLite factory/schema and foreign-key startup checks
- authenticated table page and QR access
- owner/staff cross-tenant table and order access (`404` and unchanged data)
- staff write denial and owner create/edit/toggle paths
- public invalid/foreign table code handling
- public order table binding and forged label rejection
- `Order.TableLabelSnapshot` preservation after table rename
- `DbSeeder.SeedAsync` idempotency, canonical records, and duplicate `(BusinessId, PublicCode)` protection

Run locally:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-integration-tests.ps1
```

Run a focused group:

```powershell
dotnet test tests\DukkanPilot.IntegrationTests -c Release --no-build --filter "FullyQualifiedName~CrossTenantTableTests"
```

## Adding tests

Keep a test independent: create a fresh `DukkanPilotWebApplicationFactory`, call `InitializeAsync`, use `TestClaims.CreateClient`, and perform state-changing requests with `AntiforgeryHelper`. Do not introduce a production model workaround, bypass authorization, disable antiforgery, share a database across factories, or add a migration for test setup.
