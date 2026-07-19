# Mobile API and Authentication Foundation (37A)

## Architecture

37A adds a versioned, controller-based API for the future DukkanPilot Owner/Staff mobile application. The API is rooted at `/api/mobile/v1`. It reuses the existing EF Core data model, password hashing, subscription checks, order rules, audit logging, and notification services. API contracts are dedicated DTOs; EF entities are never returned directly.

The existing ASP.NET Core Cookie scheme remains the default authentication scheme for MVC/Razor web requests. Mobile controllers explicitly use policies bound to the separate `MobileBearer` JWT scheme. A web cookie, the integration-test web scheme, or a SuperAdmin role does not grant mobile access.

## Configuration

`MobileAuthOptions` binds the `MobileAuth` configuration section:

- `Issuer` (default `DukkanPilot`)
- `Audience` (default `DukkanPilot.Mobile`)
- `SigningKey` (no repository default)
- `AccessTokenMinutes` (default 15, allowed 1-60)
- `RefreshTokenDays` (default 30, allowed 1-90)

The signing key must contain at least 32 UTF-8 bytes. Options validation runs at startup. A missing or weak key prevents the application from starting with an actionable configuration error. The key is not logged or returned by any endpoint.

For local development, set a user-secret from the web project directory:

```powershell
dotnet user-secrets init
dotnet user-secrets set "MobileAuth:SigningKey" "REPLACE-WITH-A-RANDOM-SECRET-OF-AT-LEAST-32-BYTES"
```

Alternatively set the environment variable `MobileAuth__SigningKey`. Production should obtain this value from the deployment secret store. Do not put it in any `appsettings*.json` file.

The integration-test factory supplies a deterministic test-only key in the `Testing` environment. It does not connect to LocalDB.

## JWT claims and validation

Access tokens are signed with HMAC SHA-256 and contain:

- `sub` and `ClaimTypes.NameIdentifier`
- `ClaimTypes.Name`
- `ClaimTypes.Email`
- `ClaimTypes.Role`
- `BusinessId`
- `BusinessRole`
- `jti`
- `iat`
- `client_id=dukkanpilot-mobile`

Validation requires issuer, audience, signing key, lifetime, expiration, and a signed token. Clock skew is 30 seconds. Only HMAC SHA-256 is accepted.

`BusinessId` and `BusinessRole` are resolved only from the validated principal. Mobile request DTOs do not define `BusinessId` for tenant resources. Unknown query/body fields cannot change the tenant used by order, kitchen, dashboard, bootstrap, logout, or `/me` queries.

## Authorization

The named policies are:

- `MobileAuthenticated`: a valid `MobileBearer` identity and mobile client claim.
- `MobileOwnerOrStaff`: valid mobile identity with exact `Owner` or `Staff` business role.
- `MobileOwnerOnly`: exact Owner business role plus the existing BusinessOwner user role.

Business-role parsing is case-sensitive. SuperAdmin is intentionally excluded from 37A mobile access.

Current permission summaries:

- Staff: `orders.read`, `orders.status.update`, `kitchen.read`, `dashboard.read`.
- Owner: all Staff permissions plus `business.manage`, `staff.manage`, `billing.read`.

## Endpoints

### Authentication

- `POST /api/mobile/v1/auth/login`
- `POST /api/mobile/v1/auth/refresh`
- `POST /api/mobile/v1/auth/logout`
- `POST /api/mobile/v1/auth/logout-all`
- `GET /api/mobile/v1/auth/me`

Login accepts email, password, and optional `BusinessId`. Email is normalized and the existing `PasswordHelper` verifies the password. Unknown email and wrong password return the same `invalid_credentials` problem after equivalent PBKDF2 work. Active user, active membership, Owner/Staff role, active business, and subscription access are checked.

If a verified user has multiple active memberships and did not select one, the API returns `business_selection_required` with only that user's selectable business `Id`, `Name`, and `Role` values.

Successful login and refresh return access token, refresh token, both UTC expirations, user summary, business summary, and permissions in the JSON body. Tokens are never written to cookies or URLs.

### Bootstrap and operations

- `GET /api/mobile/v1/bootstrap`
- `GET /api/mobile/v1/orders`
- `GET /api/mobile/v1/orders/{id}`
- `POST /api/mobile/v1/orders/{id}/status`
- `GET /api/mobile/v1/kitchen/orders`
- `GET /api/mobile/v1/dashboard/today`

Order lists support `page`, `pageSize` (maximum 100), `status`, `fromUtc`, `toUtc`, and `serviceType`. Every query includes the claim-derived tenant filter and read queries use `AsNoTracking`. A foreign-tenant order identifier returns 404. Order DTOs preserve `ServiceType` and `TableLabelSnapshot`.

Order status changes use the shared `IOrderStatusService`, also used by the web Orders controller. Allowed transitions are Pending to Preparing/Cancelled and Preparing to Completed/Cancelled; same-status requests are idempotent. Completion loyalty effects, audit logging, and notifications remain centralized.

The bootstrap response contains the minimum app-start data: user, business, role, permissions, plan/subscription summary, available modules, and server UTC time. It excludes secrets, connection strings, and internal entities.

## Refresh-token lifecycle

Refresh values are generated from 64 cryptographically secure random bytes and returned only in the JSON response. The database stores only the uppercase SHA-256 hash. `TokenHash` has a unique index.

Each login starts a token family. Refresh runs in a serializable database transaction:

1. Hash the presented raw token and locate the stored record.
2. Check expiration, revocation, active user, business, membership, and subscription.
3. Revoke the used record with reason `Rotated`.
4. Store a new hash in the same family and link the old record through `ReplacedByTokenHash`.
5. Commit before returning the new pair.

Reuse of any revoked token revokes all still-active records in that family and returns `refresh_token_reused`. Logout revokes the matching user/business token and is idempotent. Logout-all revokes all active tokens for the authenticated user in the authenticated business only.

The mobile client is responsible for storing access and refresh tokens in OS-provided secure storage (Android Keystore-backed storage or iOS Keychain), never plain preferences, logs, analytics, URLs, or crash reports. A 401 should trigger at most one coordinated refresh attempt; reuse detection requires a full sign-in.

## ProblemDetails

Mobile errors use `application/problem+json` and include `code` and `traceId`. Validation errors also include a field-error map. Authentication never redirects to an HTML login page. Unexpected exceptions return a generic response without stack traces or database messages.

Defined codes include:

- `invalid_credentials`
- `business_selection_required`
- `invalid_business`
- `account_inactive`
- `business_inactive`
- `invalid_refresh_token`
- `refresh_token_expired`
- `refresh_token_reused`
- `unauthorized`
- `forbidden`
- `validation_failed`
- `resource_not_found`
- `invalid_order_status`
- `rate_limit_exceeded`
- `internal_error`

## Rate limiting

Only login and refresh use named built-in fixed-window rate-limit policies. Partitions use remote IP address. Login permits 5 requests per minute; refresh permits 10. Rejections return 429 ProblemDetails with `code=rate_limit_exceeded` and a `Retry-After` header. MVC, public menu, and other API routes are not globally rate-limited.

## Migration and database status

The model adds `MobileRefreshTokens` with restrictive AppUser and Business foreign keys, a unique token-hash index, family/revocation lookup index, and user/business/expiry lookup index.

Migration `20260719172749_AddMobileAuthenticationFoundation` has been generated and reviewed. It contains only the `MobileRefreshTokens` table, restrictive AppUser/Business foreign keys, and the required token/family/user-business indexes. EF reports no pending model changes. No `database update` was run, and the real `DukkanPilotDb` LocalDB was not touched.

## Tests

Run all SQLite integration tests:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-integration-tests.ps1
```

Run the dedicated mobile plus 36C regression gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-mobile-api.ps1
```

Use `-NoBuild` only after an explicit Release build. Tests use SQLite in-memory with foreign keys enabled and exercise the real `MobileBearer` JWT handler. The existing test authentication handler remains limited to web regression scenarios.

## Security boundaries and next step

37A supports only existing Owner and Staff accounts. It does not add customer ordering accounts, external identity providers, device attestation, push notifications, token introspection, Redis, background jobs, or SignalR. Access tokens remain valid until their short expiration; `/me` and bootstrap revalidate database access, while operational endpoints rely on the signed short-lived tenant claims and strict tenant query filters.

37B can build the Android/iOS client against the documented auth, bootstrap, orders, kitchen, dashboard, refresh, and logout endpoints. The client should implement secure token storage, single-flight refresh, business selection, ProblemDetails code handling, and Retry-After behavior.
