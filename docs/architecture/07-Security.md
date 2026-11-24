# 07 - Security, Authentication & Authorization Specification
**Project Name:** AlMomiaa Enterprise ERP & DMS  
**Version:** 3.1 (Synchronized Role-Permission Matrix & 3-Tier Enforcement)

---

## 1. Complete Role-Permission Matrix

Seeded into `[id].[Roles]` and `[id].[RolePermissions]` and enforced identically across `AlMomiaa.WebApi`, `apps/web-erp`, and `apps/rep-pwa`:

| Permission Claim Code | Super Admin (`SUPER_ADMIN`) | Executive Mgr (`EXEC_MGR`) | Sales Mgr (`SALES_MGR`) | Sales Rep (`SALES_REP`) | Accountant (`ACCOUNTANT`) | Warehouse Keeper (`WH_KEEPER`) | Purchasing Mgr (`PURCH_MGR`) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `permissions:manage` | **YES** | - | - | - | - | - | - |
| `users:manage` | **YES** | - | Manage Reps | - | - | - | - |
| `settings:manage` | **YES** | Approve Strategic | - | - | - | - | - |
| `dashboards:executive` | **YES** | **YES** | - | - | - | - | - |
| `dashboards:profit` | **YES** | **YES** | - | - | **YES** | - | - |
| `dashboards:sales` | **YES** | **YES** | **YES** | Own KPIs | - | - | - |
| `dashboards:finance` | **YES** | **YES** | View Collections | Own Wallet | **YES** | - | - |
| `dashboards:inventory` | **YES** | **YES** | View Stock | View Available | View Valuation | **YES** | **YES** |
| `customers:view` | All | All | All | **Assigned Only** | All | Delivery Info | - |
| `customers:manage` | **YES** | - | **YES** | - | Adjust Ledger | - | - |
| `catalog:view` | All + Cost | All + Cost | All (No Cost) | All (No Cost) | All + Cost | All (No Cost) | All + Cost |
| `catalog:manage` | **YES** | - | Price Lists | - | - | - | **YES** |
| `orders:create` | **YES** | - | **YES** | **YES** | - | - | - |
| **`orders:edit_submitted`** | **YES** | - | **YES (Exclusive)** | **NO (Forbidden)** | **NO** | **NO** | **NO** |
| **`orders:override_price`** | **YES** | **YES** | **YES** | **NO** | **NO** | **NO** | **NO** |
| **`orders:coapprove_credit`** | **YES** | **YES** | **NO** | **NO** | **YES** | **NO** | **NO** |
| **`orders:approve`** | **YES** | Strategic | **YES** | **NO** | **NO** | **NO (Forbidden)** | **NO** |
| `warehouse:ship` | **YES** | - | - | - | - | **YES** | - |
| `delivery:dispatch` | **YES** | - | - | - | - | **YES** | - |
| `delivery:update_pod` | **YES** | - | - | Assigned | - | **YES** | - |
| `invoices:view` | **YES** | **YES** | **YES** | Assigned | **YES** | **YES (Print)** | - |
| `invoices:upload_copy` | **YES** | - | **YES** | **YES** | **YES** | **YES** | - |
| `sales:returns` | **YES** | **YES** | **YES** | - | **YES** | Receive Goods | - |
| `collections:create` | **YES** | - | - | **YES** | - | - | - |
| `finance:confirm_treasury` | **YES** | - | - | - | **YES** | - | - |
| `finance:verify_bank` | **YES** | - | - | - | **YES** | - | - |
| `finance:close_period` | **YES** | **YES** | - | - | **YES** | - | - |
| `procurement:manage` | **YES** | Approve Large | - | - | - | - | **YES** |
| `procurement:receive` | **YES** | - | - | - | - | **YES** | **YES** |
| `audit:view` | **YES** | **YES** | - | - | - | - | - |
| `gps:stream` | - | - | - | **YES (Flagged)** | - | - | - |

---

## 2. Authentication & Session Security Controls

1. **Password Hashing & Anti-Enumeration:**
   - Passwords are hashed using ASP.NET Core Identity `PasswordHasher` (PBKDF2-HMAC-SHA512 with random per-user salt); plaintext passwords are never stored or logged.
   - Unknown identifier, wrong password, and inactive accounts return the exact same generic RFC 7807 `401 Unauthorized` `ProblemDetails` response, with constant-work verification padding on missing users.
2. **Bounded Login Throttling:**
   - Redis-backed sliding/window rate limiting (`Auth:LoginRateLimit`) blocks excessive failed attempts with `429 Too Many Requests` (default: 5 failed attempts per `clientIp + normalizedIdentifier` and 20 per `normalizedIdentifier` within a 15-minute TTL window, avoiding permanent lockout DoS). Successful login resets the client+identifier counter.
3. **Short-Lived JWT & In-Memory Frontend Storage:**
   - Access JWTs expire every **10 minutes** (`expiresIn: 600`) and carry `UserId`, `EmployeeCode`, `RoleCodes`, `RepType`, `DefaultWarehouseId`, `AssignedWarehouseIds`, `SecurityStamp`, and effective `permission` claims derived exclusively from assigned database roles.
   - JWT `Issuer`, `Audience`, and `SigningKey` (>= 32 UTF-8 bytes) are required from environment/configuration; startup fails safely when missing and no default signing secret is committed.
   - In `apps/web-erp`, access tokens are stored **only in memory** inside `@almomiaa/api-client` (`createAccessTokenStore`) — never in `localStorage`, `sessionStorage`, IndexedDB, URLs, or logs.
4. **Rotating Opaque Refresh Cookie & Replay Detection (`[id].[RefreshTokenSessions]`):**
   - Opaque 384-bit cryptographically random refresh tokens are stored server-side only as SHA-256 hex hashes (`TokenHash`, compared in constant time with `CryptographicOperations.FixedTimeEquals`) under a session `FamilyId` with `ROWVERSION` optimistic concurrency and a strict **14-day maximum lifetime**.
   - Web refresh cookie `almomiaa_refresh` is always host-only (no `Domain`), `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth`, and is cleared (`Max-Age=0`) on logout and on invalid/expired/replayed refresh attempts.
   - Every successful `/api/v1/auth/refresh` atomically rotates the session (`ReplacedBySessionId`). Reusing any already-rotated or revoked token immediately revokes the entire `FamilyId`.
5. **Instant Revocation & Fail-Closed Redis `SecurityStamp` Validation:**
   - `SecurityStampMiddleware` performs an `O(1)` lookup against Redis key `user:stamp:{userId}` on authenticated requests.
   - Deactivating a user or rotating permissions invalidates/updates the stamp immediately, rejecting both active access JWTs (`401`) and refresh attempts (`401`).
   - If Redis is unavailable, protected requests and refresh validation **fail closed** with RFC 7807 `503 Service Unavailable`.
6. **Strict Credentialed CORS & Cross-Site Origin Protection:**
   - Credentialed CORS (`AllowCredentials`) accepts only explicitly configured origins from `Cors:AllowedOrigins` (wildcard `*` is rejected at startup; empty origins are rejected in Production).
   - Cookie-authenticated endpoints (`/api/v1/auth/refresh` and `/api/v1/auth/logout`) validate request `Origin`/`Referer` and `Sec-Fetch-Site` headers against trusted origins and reject cross-site requests with `403 Forbidden`.
   - Frontend route guards (redirecting unauthenticated visitors to `/login`) are UX-only; every protected API endpoint enforces server-side policies.

---

## 3. Three-Tier Authorization Architecture

1. **Tier 1 — RBAC Policy Claims (API Boundary):** Every `/api/v1/*` endpoint enforces explicit permission claim policies (table above).
2. **Tier 2 — Strict Domain Workflow Rules (Domain Layer):**
   - `SalesOrder.EditAfterSubmission()` verifies `orders:edit_submitted` AND `Status ∈ {PendingApproval, Approved}` (pre-shipment). `SALES_REP` is physically blocked at UI, API, and Domain levels.
   - `SalesOrder.Approve()` verifies `FinanceCoApprovedByUserId != null` whenever `RequiresFinanceCoApproval == true`, and verifies all `PriceOverridePending` lines are resolved.
   - `JournalEntry.Post()` verifies the target `FiscalPeriod.IsClosed == false`.
3. **Tier 3 — Row-Level Ownership / ABAC (EF Core Global Query Filters):**
   - `SALES_REP` is automatically scoped to `Customer.AssignedRepId == CurrentUser.Id` and their own `RepCustodyWallet` / `MobileVan`.
   - `WH_KEEPER` is strictly restricted to **exactly one assigned warehouse** (`AssignedWarehouseIds.Count == 1` and `DefaultWarehouseId == AssignedWarehouseIds[0]`) enforced on user creation, role assignment, warehouse assignment, and `Order.WarehouseId IN CurrentUser.AssignedWarehouseIds`.
   - Live GPS tracking (`GET /api/v1/gps/live`, `GET /api/v1/gps/reps/{repId}/history`) is restricted strictly to `SUPER_ADMIN`, `EXEC_MGR`, and `SALES_MGR` (`403 Forbidden` for all other roles).
   - Persistent notifications (`[ops].[Notifications]`) are strictly isolated per `RecipientUserId == CurrentUser.Id`.

---

## 4. Data Integrity, Audit Trail & Media Upload Security

1. **Tamper-Proof Audit Logging (`[sys].[AuditLogs]`):**
   - Enforced via EF Core `AuditSaveChangesInterceptor` inside the **same ACID transaction** as the business command.
   - Records `UserId`, `UserRole`, `ActionName`, `EntityName`, `EntityId`, `OldValuesJson`, `NewValuesJson`, `IpAddress`, and `TimestampUtc`.
   - Every `PUT /api/v1/orders/{id}/manager-edit`, credit co-approval, price override, treasury handover, and bank verification records full before/after JSON snapshots.
2. **Field Media Upload Security (POD & Bank Transfer Slips):**
   - Strictly validate MIME magic bytes (`image/jpeg`, `image/png`, `image/webp`, `application/pdf`).
   - Strip malicious EXIF metadata before persistence.
   - Compute `SHA-256` hash (`PaymentProofImageHash`) and enforce unique filtered index `UX_Collections_ProofHash` to prevent duplicate bank transfer receipt fraud.
   - Store files in private Saudi-resident S3/MinIO buckets served only via short-lived Pre-Signed URLs (5-minute expiry).
