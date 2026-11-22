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

1. **Short-Lived JWT + Device-Bound Refresh Token:**
   - Access tokens expire every **10 minutes** and carry `UserId`, `RoleCodes`, `RepType`, `DefaultWarehouseId`, `AssignedWarehouseIds`, and `SecurityStamp`.
   - Refresh tokens (14 days) rotate on every refresh and are stored in `HttpOnly`, `Secure`, `SameSite=Strict` cookies (`web-erp`) or device-bound encrypted storage (`rep-pwa`).
2. **Instant Revocation via Redis `SecurityStamp`:**
   - `SecurityStampMiddleware` performs an `O(1)` lookup against Redis key `user:stamp:{userId}`.
   - Deactivating a user or modifying role permissions in the Super Admin dashboard rotates the stamp immediately, invalidating all active Web and offline-reconnecting PWA sessions.

---

## 3. Three-Tier Authorization Architecture

1. **Tier 1 — RBAC Policy Claims (API Boundary):** Every `/api/v1/*` endpoint enforces explicit permission claim policies (table above).
2. **Tier 2 — Strict Domain Workflow Rules (Domain Layer):**
   - `SalesOrder.EditAfterSubmission()` verifies `orders:edit_submitted` AND `Status ∈ {PendingApproval, Approved}` (pre-shipment). `SALES_REP` is physically blocked at UI, API, and Domain levels.
   - `SalesOrder.Approve()` verifies `FinanceCoApprovedByUserId != null` whenever `RequiresFinanceCoApproval == true`, and verifies all `PriceOverridePending` lines are resolved.
   - `JournalEntry.Post()` verifies the target `FiscalPeriod.IsClosed == false`.
3. **Tier 3 — Row-Level Ownership / ABAC (EF Core Global Query Filters):**
   - `SALES_REP` is automatically scoped to `Customer.AssignedRepId == CurrentUser.Id` and their own `RepCustodyWallet` / `MobileVan`.
   - `WH_KEEPER` is scoped to `Order.WarehouseId IN CurrentUser.AssignedWarehouseIds`.

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
