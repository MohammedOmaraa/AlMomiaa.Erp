# 06 - API Specification & Real-Time SignalR Contracts
**Project Name:** AlMomiaa Enterprise ERP & DMS  
**Base URL:** `https://api.almomiaa.sa/api/v1`  
**Version:** 3.2 (Synchronized with `05-Database.md`, `07-Security.md`, and implemented `.NET 9` WebApi controllers)

---

## 1. Global HTTP Conventions
- **Headers:**
  - `Authorization: Bearer <JWT>`
  - `Accept-Language: ar-SA` (default) or `en-US`
  - `X-Idempotency-Key: <UUID>` (mandatory on `POST /orders`, `POST /collections/cash`, `POST /collections/bank-transfer`, `POST /orders/{id}/confirm-shipment`)
- **Pagination Envelope:** All high-volume list endpoints support cursor/keyset or indexed offset pagination (`pageSize <= 250`) and return `{ items, totalCount, nextCursor }`.
- **OpenAPI Contract Sync:** `AlMomiaa.WebApi` publishes `/openapi/v1.json` consumed by `packages/api-client` codegen + `Zod` runtime schemas.

---

## 2. Complete REST API Endpoint Matrix

### 2.1 Identity & User Management (`/api/v1/auth`, `/api/v1/users`, `/api/v1/roles`)
| HTTP | Route | Permission Claim Required | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/auth/login` | Public (`[AllowAnonymous]`) | Authenticates by employee code or phone (`{ identifier, password }`). Returns 10-minute Bearer JWT (`expiresIn: 600`) + authenticated user context (`roleCodes`, `permissions`, `repType`, `defaultWarehouseId`, `assignedWarehouseIds`) and sets an `HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth` host-only refresh cookie (14-day max). Returns generic `401` ProblemDetails on invalid credentials/inactive account, `429` when throttled, `400` on validation errors. |
| `POST` | `/auth/refresh` | Public (`[AllowAnonymous]`, cookie-authenticated) | Empty request body. Validates trusted origin (`403` on cross-site untrusted origin), atomically rotates the `almomiaa_refresh` cookie, revokes the entire session family on replay/expiry/stamp mismatch, and returns the same `AuthenticationResponse` JSON as login. Clears cookie and returns `401` ProblemDetails on invalid session (`503` if Redis security-stamp store is unavailable). |
| `POST` | `/auth/logout` | Public (`[AllowAnonymous]`, cookie-authenticated, idempotent) | Validates trusted origin (`403` on untrusted origin), revokes the current refresh-session family when a valid cookie is present, always clears the `almomiaa_refresh` cookie (`Max-Age=0`), and returns `204 No Content`. |
| `GET` | `/users` | `users:manage` | Paginated list of employees, roles, rep types, and assigned warehouses/vans. |
| `GET` | `/users/{id}` | `users:manage` | Employee detail profile including assigned roles, assigned warehouses, and computed effective permissions. |
| `POST` | `/users` | `users:manage` | Creates user and auto-initializes `RepCustodyWallet` (and optional `MobileVan` warehouse) if `SALES_REP`. |
| `PATCH` | `/users/{id}/status` | `users:manage` | Activates or deactivates an employee account and rotates `SecurityStamp` on deactivation. |
| `PUT` | `/users/{id}/roles` | `permissions:manage` | Replaces roles assigned to a user (`SUPER_ADMIN` only) and rotates `SecurityStamp`. |
| `PUT` | `/users/{id}/warehouses` | `users:manage` | Replaces assigned warehouses and default warehouse for a user and rotates `SecurityStamp`. |
| `GET` | `/roles` | `permissions:manage` | Lists all canonical and custom roles and their assigned permission claims. |
| `POST` | `/roles` | `permissions:manage` | Creates a custom role (`SUPER_ADMIN` only) with a validated subset of canonical permission claims. |
| `PUT` | `/roles/{roleId}/permissions` | `permissions:manage` | Updates role permission claims (`SUPER_ADMIN` only) and rotates `SecurityStamp` for affected users. |

#### Authentication Contracts (`/openapi/v1.json`, mirrored in `@almomiaa/api-client`)
- **Login Request (`POST /api/v1/auth/login`):** `{ "identifier": "EMP-0001 | +9665XXXXXXXX", "password": "<secret>" }`
- **Authentication Response (`200 OK` on `/auth/login` and `/auth/refresh`):**
  ```json
  {
    "accessToken": "<10m-jwt>",
    "tokenType": "Bearer",
    "expiresIn": 600,
    "user": {
      "id": "01933c90-9999-7000-8000-abcdef123456",
      "employeeCode": "EMP-0001",
      "fullNameAr": "اسم المستخدم",
      "fullNameEn": "User Name",
      "roleCodes": ["SALES_MGR"],
      "permissions": ["orders:approve"],
      "repType": null,
      "defaultWarehouseId": null,
      "assignedWarehouseIds": []
    }
  }
  ```
- **Refresh Cookie (`Set-Cookie`):** `almomiaa_refresh=<opaque-token>; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Expires=<utc-max-14d>` (host-only, no `Domain` attribute; refresh tokens are never included in JSON responses or client-side storage).

### 2.2 Customer & Credit Grades (`/api/v1/customers`)
| HTTP | Route | Permission Claim Required | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/customers` | `customers:view` | Paginated list (auto-scoped to `AssignedRepId` for `SALES_REP`). |
| `GET` | `/customers/{id}/credit-status` | `customers:view` | **Order Step 2:** Returns `CurrentBalance`, `OverdueDebt`, `CreditLimit`, `Grade (A/B/C/D)`, `PriceListId`, `EffectivePaymentDays`, and last 10 purchases. |
| `POST` | `/customers` | `customers:manage` | Creates customer with Grade, Price List, Credit Limit, Category, and Assigned Representative. |
| `GET` | `/customers/grades` | `customers:view` | Lists customer credit grades (A, B, C, D) and default payment periods. |
| `PUT` | `/customers/grades` | `settings:manage` | Updates configurable payment periods for Grades A, B, C, and D. |

### 2.3 Product Hierarchy, Price Lists & Multi-Supplier Catalog (`/api/v1/catalog`)
| HTTP | Route | Permission Claim Required | Description |
| :--- | :--- | :--- | :--- |
| `GET` / `POST` | `/catalog/categories` | `catalog:view` / `catalog:manage` | Gets hierarchical category tree or creates a category/subcategory. |
| `GET` / `POST` | `/catalog/brands` | `catalog:view` / `catalog:manage` | Lists or creates product brands. |
| `GET` / `POST` | `/catalog/products` | `catalog:view` / `catalog:manage` | Paginated Master Products list (cost masked for non-authorized roles) and multi-unit product creation. |
| `GET` | `/catalog/categories/{categoryId}/products` | `catalog:view` | **Order Step 3:** Loads products under category with customer's resolved Price List price and real-time `AvailableQty`. |
| `POST` | `/catalog/products/{id}/suppliers` | `catalog:manage` | Links a Supplier (`Supplier A` or `Supplier B`) to a single `MasterProduct`. |
| `GET` / `POST` | `/catalog/price-lists` | `catalog:view` / `catalog:manage` | Manages dynamic tiered price lists (`Wholesale`, `Retail`, `KeyAccount`) (`MR-02`). |

### 2.4 Multi-Warehouse, Mobile Vans & Inventory (`/api/v1/warehouses`)
| HTTP | Route | Permission Claim Required | Description |
| :--- | :--- | :--- | :--- |
| `GET` / `POST` | `/warehouses` | `WarehousesRead` (`inventory:view`, `orders:create`, `orders:approve`, or `dashboards:inventory`) / `settings:manage` | Lists warehouses (ABAC-scoped for `WH_KEEPER` and `SALES_REP`) or creates a `MainWarehouse`, `BranchWarehouse`, or `MobileVan`. |
| `GET` | `/warehouses/{id}/stocks` | `inventory:view` | Paginated `PhysicalQty`, `ReservedQty`, `IncomingQty`, and `AvailableQty` (supports `MainWarehouse`, `BranchWarehouse`, and `MobileVan`). |
| `POST` | `/warehouses/transfers` | `inventory:transfer` | Creates an inter-warehouse or van-loading stock transfer (`Status = InTransit`). |
| `PUT` | `/warehouses/transfers/{id}/receive` | `inventory:transfer` | Confirms receipt of transferred stock at destination warehouse/van. |
| `POST` | `/warehouses/{id}/inventory-count` | `inventory:adjust` | Submits physical count adjustments and appends to `inv.StockMovements`. |

### 2.5 Purchasing & Container Tracking (`/api/v1/procurement`)
| HTTP | Route | Permission Claim Required | Description |
| :--- | :--- | :--- | :--- |
| `GET` / `POST` | `/procurement/suppliers` | `procurement:manage` | Lists or creates local/international suppliers. |
| `GET` / `POST` | `/procurement/purchase-orders` | `procurement:manage` | Lists or creates Supplier Purchase Orders + items and increments `IncomingQty`. |
| `POST` | `/procurement/containers` | `procurement:manage` | Registers sea container (`ContainerNumber`, `ETA`, `FreightAndCustomsCostSAR`). |
| `POST` | `/procurement/goods-receipts` | `procurement:receive` | Records GRN + items, distributes container landed cost, recalculates `WeightedAvgCost`, increments `PhysicalQty`. |

### 2.6 Governed Sales Orders, Invoicing, Returns & Delivery POD (`/api/v1/orders`, `/api/v1/invoices`, `/api/v1/returns`, `/api/v1/delivery`)
| HTTP | Route | Permission Claim Required | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/orders` | `customers:view` | Paginated sales orders list (ABAC-scoped for `SALES_REP` and `WH_KEEPER`). |
| `POST` | `/orders` | `orders:create` | **Steps 4–5:** Submits order (`PendingApproval`), reserves stock with 24h TTL, evaluates Credit Hold (`MR-03`), Price Floor (`MR-02`), and Offline Shortage (`RISK-03`), locks Rep edits. |
| `PUT` | `/orders/{id}/manager-edit` | **`orders:edit_submitted`** | **Exclusive to `SALES_MGR` & `SUPER_ADMIN`:** Edits items/quantities/prices after submission; reconciles `ReservedQty` delta and logs audit diff. |
| `POST` | `/orders/{id}/override-price` | **`orders:override_price`** | Approves line-item price overrides below tier/`MinAllowedSellingPrice` (`MR-02`). |
| `POST` | `/orders/{id}/co-approve-credit` | **`orders:coapprove_credit`** | **Accountant / Exec Mgr:** Co-approves an order flagged with `RequiresFinanceCoApproval` (`MR-03`). |
| `POST` | `/orders/{id}/approve` | `orders:approve` | **Steps 6–7:** Approves order and enqueues `OrderApproved` outbox alert to `WH_KEEPER` and `PURCH_MGR`. |
| `POST` | `/orders/{id}/reject` | `orders:approve` | Rejects order, releases `ReservedQty`, enqueues `OrderRejected` alert. |
| `POST` | `/orders/{id}/confirm-shipment` | `warehouse:ship` | **Steps 8–9 (`WH_KEEPER`):** Deducts `PhysicalQty`/`ReservedQty`, advances `ZatcaDeviceSequences` ICV/PIH, generates ZATCA Phase 2 `Invoice`, debits Customer Balance. |
| `POST` | `/invoices/egs-devices` | `settings:manage` | Registers a ZATCA Phase 2 EGS device sequence counter (`ICV=0`, initial PIH). |
| `GET` | `/invoices/{id}/print` | `invoices:view` | Returns printable invoice payload with ZATCA ICV, PIH, XML hash, and TLV Base64 QR code. |
| `POST` | `/invoices/{id}/upload-copy` | `invoices:upload_copy` | Attaches photographed/scanned copy URL of signed customer invoice (POD). |
| `POST` | `/returns` | `sales:returns` | Creates Sales Return + ZATCA Credit Note (`381`), optionally restocks sellable items, credits Customer Receivable. |
| `POST` | `/delivery/dispatches` | `delivery:dispatch` | Creates Driver/Rep Delivery Manifest linking prepared invoices (`MR-04`). |
| `PUT` | `/delivery/dispatches/{id}/items/{invoiceId}/pod` | `delivery:update_pod` | Updates delivery status (`OutForDelivery -> Delivered`) and attaches signed POD photo (`MR-04`). |

### 2.7 Collections, Treasury Reconciliation & Fiscal Periods (`/api/v1/collections`, `/api/v1/finance`)
| HTTP | Route | Permission Claim Required | Description |
| :--- | :--- | :--- | :--- |
| `GET` / `POST` | `/finance/accounts` | `dashboards:finance` / `settings:manage` | Lists or creates financial accounts (`MainTreasurySafe`, `BankAccount`, `RepCustodyWallet`, etc.). |
| `GET` / `POST` | `/finance/fiscal-periods` | `dashboards:finance` / `finance:close_period` | Lists or opens monthly fiscal periods for double-entry ledger postings. |
| `POST` | `/finance/fiscal-periods/{id}/close` | `finance:close_period` | Locks a fiscal month (`IsClosed = true`) after VAT filing (`MR-05`). |
| `GET` | `/finance/journal-entries` | `dashboards:finance` | Lists append-only balanced double-entry journal entries and their lines. |
| `POST` | `/collections/cash` | `collections:create` | Records cash collection (`InRepCustody`), posts double-entry journal, updates Rep Custody Wallet & Customer Balance. |
| `POST` | `/collections/{id}/confirm` | `collections:create` | Marks cash collection as `IsCustomerConfirmed = true`. |
| `POST` | `/collections/handover` | `finance:confirm_treasury` | **Accountant:** Confirms Rep handed cash to HQ Treasury Safe; posts double-entry transfer & enqueues KPI update. |
| `POST` | `/collections/bank-transfer` | `collections:create` | Uploads bank transfer proof image (SHA-256 deduplicated) + reference (`PendingBankVerification`). |
| `POST` | `/collections/{id}/verify-bank` | `finance:verify_bank` | **Accountant:** Verifies bank transfer against open fiscal period preserving `OriginalValueDateUtc` (`MR-05`); credits Customer Balance & increments Bank Balance. |

### 2.8 Dashboards, Audit & GPS (`/api/v1/dashboards`, `/api/v1/audit-logs`, `/api/v1/gps`)
| HTTP | Route | Permission Claim Required | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/dashboards/executive` | `dashboards:executive` | Live executive KPIs (Safe Cash, Rep Custody Cash, Bank, Receivables, Payables, Inventory Value, Pending Orders, Low Stock). |
| `GET` | `/dashboards/role/{roleView}` | Role dashboard claim | Endpoints for `sales`, `inventory`, `finance`, `warehouse`, and `rep` dashboards. |
| `GET` | `/audit-logs` | `audit:view` | Keyset-paginated audit trail (`User`, `Action`, `Timestamp`, `OldValuesJson`, `NewValuesJson`). |
| `POST` | `/gps/pings` | `gps:stream` | Batched field sales rep GPS telemetry endpoint. |

---

## 3. Real-Time SignalR Hub Contracts

### 3.1 `OperationsHub` (`/hubs/operations`)
- **Groups:** `role:EXEC_MGR`, `role:SALES_MGR`, `role:ACCOUNTANT`, `role:PURCH_MGR`, `warehouse:{warehouseId}`, `user:{userId}`
- **Server-to-Client Events:**
  - `OrderSubmitted(orderId, orderNumber, repName, exceededCredit, hasStockShortage)` -> `role:SALES_MGR`
  - `CreditCoApprovalRequired(orderId, orderNumber, customerName, overdueAmount)` -> `role:ACCOUNTANT`, `role:EXEC_MGR`
  - `OrderApproved(orderId, orderNumber, repId, warehouseId, totalAmount, approvedAtUtc)` -> `warehouse:{warehouseId}`, `role:PURCH_MGR`, `role:SALES_MGR`, `role:SUPER_ADMIN`, `user:{repId}`
  - `OrderRejected(orderId, orderNumber, repId, rejectionReason, rejectedAtUtc)` -> `user:{repId}`, `role:SALES_MGR`, `role:SUPER_ADMIN`
  - `OrderUpdated(orderId, orderNumber, repId, status, totalAmount, changeType)` -> `user:{repId}`, `role:SALES_MGR`, `role:SUPER_ADMIN`
  - `FinancialKpiUpdated(safeCash, repCustodyCash, bankBalance, receivables)` -> `role:EXEC_MGR`, `role:SUPER_ADMIN`, `role:ACCOUNTANT`
  - `LowStockAlert(warehouseId, sku, productNameAr, availableQty, minReorderQty)` -> `role:PURCH_MGR`, `warehouse:{warehouseId}`

### 3.2 `FieldTelemetryHub` (`/hubs/telemetry`) — Feature-Flagged
- Streams live Rep coordinates into Redis `GEOADD` and batches history into `sys.GpsRouteHistory`.

---

## 4. Standard Error Contract (RFC 7807 `ProblemDetails`)
```json
{
  "type": "https://api.almomiaa.sa/errors/domain-rule-violation",
  "title": "Unauthorized Order Modification",
  "status": 403,
  "detail": "Sales Representatives cannot edit an order after submission. Only a Sales Manager may modify submitted orders.",
  "instance": "/api/v1/orders/01933c90-9999-7000-8000-abcdef123456",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```
