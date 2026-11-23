# AlMomiaa ERP `.NET 9` Backend — Runbook, Per-Column Validation Reference & Troubleshooting Guide

> **Downloadable Postman Suite:**
> - **Collection (10 Categorized Folders, 73 Requests):** `docs/api/AlMomiaa-ERP.postman_collection.json`
> - **Local Environment (Auto-Chained Variables):** `docs/api/AlMomiaa-Local.postman_environment.json`

---

## 1. Prerequisites & Running the `.NET 9` Backend Locally (Windows PowerShell)

### 1.1 Machine Prerequisites
1. **Microsoft SQL Server** running locally on `localhost` with database **`AlMomiaa`** already provisioned (8 schemas: `id`, `cat`, `cust`, `inv`, `proc`, `sales`, `fin`, `ops`; 44 tables).
2. **.NET SDK** (Note: if your machine has `.NET 10` or `.NET 11` installed without the `.NET 9` runtime, always set `$env:DOTNET_ROLL_FORWARD = "Major"` before running `dotnet run`, `dotnet test`, or `dotnet ef`).
3. **Redis (Optional in Development):** In `Development` mode, if `Redis:ConnectionString` is left empty, the backend automatically uses an in-memory `ISecurityStampStore` and `ILoginAttemptLimiter` so you can test immediately without installing Redis. In `Production`, `Redis:ConnectionString` is strictly required.

### 1.2 Configure Local Secrets (Zero Secrets in Git)
Never commit JWT signing keys or passwords into `appsettings.json`. Set them via environment variables (or `dotnet user-secrets` in `apps/backend-api/src/AlMomiaa.WebApi`):

```powershell
# 1. Enable .NET 9 roll-forward on .NET 10/11 hosts
$env:DOTNET_ROLL_FORWARD = "Major"
$env:ASPNETCORE_ENVIRONMENT = "Development"

# 2. Required JWT Signing Key (MUST be at least 32 UTF-8 bytes)
$env:Auth__Jwt__Issuer = "https://api.almomiaa.local"
$env:Auth__Jwt__Audience = "almomiaa-erp-web"
$env:Auth__Jwt__SigningKey = "REPLACE_WITH_A_RANDOM_64_CHAR_SECRET_KEY_AT_LEAST_32_BYTES_LONG!!"

# 3. Trusted Frontend Origin (for refresh/logout Origin validation)
$env:Cors__AllowedOrigins__0 = "http://localhost:5173"

# 4. SQL Server Connection String (uses Windows Integrated Auth to AlMomiaa DB)
$env:ConnectionStrings__SqlServer = "Server=localhost;Database=AlMomiaa;Trusted_Connection=True;TrustServerCertificate=True;"

# 5. Optional Development-Only SUPER_ADMIN Bootstrap (runs once if user doesn't exist)
$env:ALMOMIAA_DEV_ADMIN_CODE = "ADMIN-001"
$env:ALMOMIAA_DEV_ADMIN_PHONE = "+966500000001"
$env:ALMOMIAA_DEV_ADMIN_PASSWORD = "ChangeMe!2026#Admin"
```

### 1.3 Apply Pending Database Migrations & Run the API
```powershell
# Apply latest EF Core migrations to AlMomiaa SQL Server database
dotnet ef database update `
  --project apps/backend-api/src/AlMomiaa.Infrastructure `
  --startup-project apps/backend-api/src/AlMomiaa.WebApi

# Start the Web API (listens on http://localhost:5000 and https://localhost:5001)
dotnet run --project apps/backend-api/src/AlMomiaa.WebApi
```

### 1.4 Verify Interactive Docs & Health
- **Health Check:** `GET http://localhost:5000/health` → `200 OK` `{ "status": "Healthy" }`
- **Swagger UI:** `http://localhost:5000/swagger`
- **OpenAPI v3 JSON:** `http://localhost:5000/openapi/v1.json`

### 1.5 Import & Run the Postman Suite
1. Open **Postman** → **Import** → select both:
   - `docs/api/AlMomiaa-ERP.postman_collection.json`
   - `docs/api/AlMomiaa-Local.postman_environment.json`
2. Select the **`AlMomiaa ERP - Local Development`** environment (top-right dropdown).
3. Fill in `adminPassword` in the environment to match your `ALMOMIAA_DEV_ADMIN_PASSWORD`.
4. **Recommended Execution Order (so chained IDs populate automatically):**
   1. **Folder 01:** `Health Check` → `Login` (auto-saves `{{accessToken}}` & `{{currentUserId}}`)
   2. **Folder 05:** `Create Main Warehouse` & `Create Branch Warehouse` (saves `{{warehouseId}}` & `{{branchWarehouseId}}`)
   3. **Folder 04:** `Create Category` → `Create Brand` → `Create Master Product` → `Create Price List`
   4. **Folder 06:** `Create Supplier` → `Link Supplier` → `Create PO` → `Register Container` → `Create GRN` (loads stock + recalculates WAC!)
   5. **Folder 05 (Transfers/Counts):** `Get Stocks` → `Dispatch Transfer` → `Receive Transfer` → `Submit Inventory Count`
   6. **Folder 03:** `List Grades` → `Create Customer` → `Get Credit Status`
   7. **Folder 08 (Setup):** `Register ZATCA EGS Device` (saves `{{egsDeviceId}}`)
   8. **Folder 07:** `Submit Sales Order` → `Manager Edit` → (`Override Price` / `Co-Approve Credit` if flagged) → `Approve` → `Confirm Shipment` (issues ZATCA Invoice!)
   9. **Folder 08 (Fulfillment):** `Get Invoice Print` → `Upload Copy` → `Create Dispatch` → `Update POD` → `Create Sales Return`
   10. **Folder 09:** `Create Main Safe` → `Create Bank Account` → `Open Fiscal Period` → `Record Cash Collection` → `Confirm Cash` → `Treasury Handover` → `Submit Bank Transfer` → `Verify Bank Transfer` → `List Journal Entries` → `Close Fiscal Period`
   11. **Folder 10:** `Executive Dashboard` → `Role Dashboards` → `Audit Logs` (and `GPS Pings` using a `SALES_REP` token)

---

## 2. Complete Endpoint Catalog & Per-Column Validation Reference

All protected endpoints require `Authorization: Bearer <accessToken>`. All validation errors return RFC 7807 `application/problem+json` with per-field `errors`.

### Category 01 — Auth & Health (`/health`, `/api/v1/auth/*`)
| Endpoint | Auth / Policy | Required Headers | Payload Fields & Column Validations |
|---|---|---|---|
| `GET /health` | Anonymous | None | None |
| `POST /api/v1/auth/login` | Anonymous | `Content-Type: application/json` | `identifier` (string, required, max 60, employee code or phone), `password` (string, required, non-empty). Rate limited per IP + identifier (`429` on brute force). |
| `POST /api/v1/auth/refresh` | Anonymous (HttpOnly Cookie) | `Origin` (must match `Cors:AllowedOrigins`) | Uses HttpOnly `almomiaa_refresh` cookie. Rotates session atomically; replay revokes entire session family. |
| `POST /api/v1/auth/logout` | Anonymous | `Origin` (must match `Cors:AllowedOrigins`) | Revokes refresh family and clears cookie (`204 No Content`). |

### Category 02 — Users & Roles (`/api/v1/users`, `/api/v1/roles`)
| Endpoint | Required Policy | Payload Fields & Column Validations |
|---|---|---|
| `GET /api/v1/users` | `users:manage` | Query: `search` (max 100), `roleCode` (max 30), `isActive` (bool), `page` (>= 1), `pageSize` (1..250). |
| `GET /api/v1/users/{id}` | `users:manage` | Returns user profile, assigned roles, assigned warehouses, and computed `effectivePermissions[]`. |
| `POST /api/v1/users` | `users:manage` | `employeeCode` (required, max 30, regex `^[A-Za-z0-9\-_]+$`, unique → `409`), `fullNameAr` (required, max 150), `fullNameEn` (max 150), `phone` (required, max 20, regex `^\+?[0-9\s\-()]{9,20}$`, unique → `409`), `email` (valid email, max 150), `password` (required, min 8, max 128, must contain upper + lower + digit + special char), `repType` (`Wholesale`, `Retail`, `CashVan`, or null), `roleIds` (non-empty `int[]`), `assignedWarehouseIds` (`int[]`), `createMobileVan` (bool; auto-provisions `MobileVan` + `RepCustodyWallet` for `CashVan`), `vehiclePlateNumber` (max 30). |
| `PATCH /api/v1/users/{id}/status` | `users:manage` | `isActive` (bool). Setting `false` rotates `SecurityStamp` and revokes active tokens immediately. |
| `PUT /api/v1/users/{id}/roles` | `permissions:manage` | `roleIds` (non-empty `int[]`). Replaces user's roles, logs audit diff, and immediately rotates `SecurityStamp`. |
| `PUT /api/v1/users/{id}/warehouses` | `users:manage` | `defaultWarehouseId` (optional `int > 0`), `assignedWarehouseIds` (`int[]`). Replaces user's warehouse assignments and rotates `SecurityStamp`. |
| `GET /api/v1/roles` | `permissions:manage` | Lists all canonical and custom roles and their claims. |
| `POST /api/v1/roles` | `permissions:manage` | `roleCode` (required, max 30, regex `^[A-Za-z0-9_]+$`, unique → `409`), `nameAr` (max 100), `nameEn` (max 100), `permissions` (non-empty `string[]` from canonical permission allowlist). |
| `PUT /api/v1/roles/{roleId}/permissions` | `permissions:manage` | `permissions` (non-empty `string[]`, every value must be a canonical permission claim). Rotates security stamps for all users holding that role. |

### Category 03 — Customers & Credit Grades (`/api/v1/customers/*`)
| Endpoint | Required Policy | Payload Fields & Column Validations |
|---|---|---|
| `GET /api/v1/customers` | `customers:view` | Query: `search` (max 100), `gradeId`, `city`, `isActive`, `page` (>=1), `pageSize` (1..250). **ABAC:** `SALES_REP` only sees customers where `AssignedRepId == currentUser.UserId`. |
| `GET /api/v1/customers/{id}/credit-status` | `customers:view` | Path `id` (GUID). Returns credit limit, balance, overdue debt, effective payment days, and recent orders. **ABAC:** `SALES_REP` gets `403` on unassigned customers. |
| `POST /api/v1/customers` | `customers:manage` | `customerCode` (required, max 30, unique → `409`), `nameAr` (required, max 200), `nameEn` (max 200), `phone` (required, max 20), `address` (required, max 500), `city` (max 100, default `Riyadh`), `category` (required enum: `Market`, `Store`, `Restaurant`, `Supermarket`, `Wholesale`), `vatNumber` (optional; if present must be 15 digits), `gradeId` (required > 0, FK), `priceListId` (optional FK), `customPaymentDays` (optional `0..365`), `creditLimit` (>= 0), `assignedRepId` (required GUID, FK), `latitude` (`-90..90`), `longitude` (`-180..180`). |
| `GET /api/v1/customers/grades` | `customers:view` | Returns seeded grades `A`, `B`, `C`, `D`. |
| `PUT /api/v1/customers/grades` | `settings:manage` | `grades[]`: `gradeCode` (`A`, `B`, `C`, `D`), `defaultPaymentDays` (`0..365`), `allowCredit` (bool). |

### Category 04 — Catalog & Pricing (`/api/v1/catalog/*`)
| Endpoint | Required Policy | Payload Fields & Column Validations |
|---|---|---|
| `GET /api/v1/catalog/categories` | `catalog:view` | Hierarchical category tree. |
| `POST /api/v1/catalog/categories` | `catalog:manage` | `parentCategoryId` (optional FK), `nameAr` (required, max 150), `nameEn` (max 150), `displayOrder` (>= 0). |
| `GET /api/v1/catalog/brands` | `catalog:view` | Brand list. |
| `POST /api/v1/catalog/brands` | `catalog:manage` | `nameAr` (required, max 150), `nameEn` (max 150). |
| `GET /api/v1/catalog/products` | `catalog:view` | Query: `search`, `categoryId`, `brandId`, `isActive`, `page`, `pageSize`. **Field Masking:** `weightedAvgCost` is `null` unless caller is `SUPER_ADMIN`, `EXEC_MGR`, `ACCOUNTANT`, or `PURCH_MGR`. |
| `GET /api/v1/catalog/categories/{categoryId}/products` | `catalog:view` | Query: `customerId`, `warehouseId`, `page`, `pageSize`. Resolves customer tier price (MR-02) + live warehouse available stock. |
| `POST /api/v1/catalog/products` | `catalog:manage` | `sku` (required, max 50, unique → `409`), `barcode` (required, max 50, unique → `409`), `nameAr` (required, max 200), `nameEn` (max 200), `categoryId` (> 0, FK), `brandId` (> 0, FK), `baseUnit` (required, max 30), `weightedAvgCost` (>= 0), `minAllowedSellingPrice` (> 0), `wholesaleSellingPrice` (>= `minAllowedSellingPrice`), `retailSellingPrice` (>= `wholesaleSellingPrice`), `vatRate` (`0..100`, default `15.00`), `units[]` (non-empty; each: `unitNameAr` max 50, `unitNameEn` max 50, `conversionFactor` > 0, `barcode` unique max 50, `minAllowedPrice <= wholesalePrice <= retailPrice`). |
| `POST /api/v1/catalog/products/{id}/suppliers` | `catalog:manage` | `supplierId` (required GUID), `supplierSku` (max 100), `supplierBarcode` (max 100), `lastPurchasePrice` (> 0), `leadTimeDays` (>= 0), `isPreferredSupplier` (bool). |
| `GET /api/v1/catalog/price-lists` | `catalog:view` | Active tiered price lists and item tiers. |
| `POST /api/v1/catalog/price-lists` | `catalog:manage` | `priceListCode` (required, max 30, unique → `409`), `nameAr` (required, max 150), `nameEn` (max 150), `isDefault` (bool), `effectiveFromUtc`, `effectiveToUtc` (> `effectiveFromUtc`), `items[]` (`productUnitId` GUID, `minQuantity` > 0, `tierUnitPrice` > 0). |

### Category 05 — Warehouses & Inventory (`/api/v1/warehouses/*`)
| Endpoint | Required Policy | Payload Fields & Column Validations |
|---|---|---|
| `GET /api/v1/warehouses` | `inventory:view` | **ABAC:** `WH_KEEPER` only sees assigned/default warehouses. |
| `POST /api/v1/warehouses` | `settings:manage` | `warehouseCode` (required, max 20, unique → `409`), `nameAr` (required, max 150), `nameEn` (max 150), `warehouseType` (required enum: `MainWarehouse`, `BranchWarehouse`, `MobileVan`), `assignedRepId` (required when `MobileVan`), `vehiclePlateNumber` (max 30), `city` (max 100). |
| `GET /api/v1/warehouses/{id}/stocks` | `inventory:view` | Returns `physicalQty`, `reservedQty`, `incomingQty`, `availableQty`, `minReorderQty`. **ABAC:** `WH_KEEPER` gets `403` on unassigned warehouses. |
| `POST /api/v1/warehouses/transfers` | `inventory:transfer` | `sourceWarehouseId` (> 0), `destinationWarehouseId` (> 0, must differ from source), `items[]` (non-empty; `masterProductId` GUID, `requestedQty` > 0, `dispatchedQty` > 0 and <= `requestedQty`). Fails with `422` if `AvailableQty < dispatchedQty`. |
| `PUT /api/v1/warehouses/transfers/{id}/receive` | `inventory:transfer` | `items[]` (`stockTransferItemId` GUID, `receivedQty` >= 0). Fails with `422` if transfer is not `InTransit`. |
| `POST /api/v1/warehouses/{id}/inventory-count` | `inventory:adjust` | `items[]` (`masterProductId` GUID, `countedPhysicalQty` >= 0, `lotNumber` max 50, `expiryDate`). Fails with `422` if `countedPhysicalQty < ReservedQty`. Appends immutable `Adjustment` StockMovement. |

### Category 06 — Procurement & Containers (`/api/v1/procurement/*`)
| Endpoint | Required Policy | Payload Fields & Column Validations |
|---|---|---|
| `GET /api/v1/procurement/suppliers` | `procurement:manage` | Paginated suppliers list. |
| `POST /api/v1/procurement/suppliers` | `procurement:manage` | `supplierCode` (required, max 30, unique → `409`), `nameAr` (required, max 200), `nameEn` (max 200), `country` (max 100), `phone` (max 30), `vatNumber` (max 20). |
| `GET /api/v1/procurement/purchase-orders` | `procurement:manage` | Paginated PO list with optional `status`. |
| `POST /api/v1/procurement/purchase-orders` | `procurement:manage` | `supplierId` (GUID), `targetWarehouseId` (> 0), `orderType` (`ContainerImport`, `LocalSupplierOrder`), `items[]` (`masterProductId` GUID, `orderedQty` > 0, `unitCostSAR` > 0). Increments `WarehouseStocks.IncomingQty`. |
| `POST /api/v1/procurement/containers` | `procurement:manage` | `purchaseOrderId` (GUID), `containerNumber` (required, max 50, unique → `409`), `billOfLading` (max 100), `shippingLine` (max 100), `portOfArrival` (required, max 100), `estimatedArrivalDate`, `actualArrivalDate`, `status` (`AtSea`, `ArrivedPort`, `CustomsClearance`, `InTransitRiyadh`, `ReceivedAtWarehouse`), `supplierGoodsCostSAR` (> 0), `freightAndCustomsCostSAR` (>= 0). |
| `POST /api/v1/procurement/goods-receipts` | `procurement:receive` | `purchaseOrderId` (GUID), `containerId` (optional GUID, must match PO), `warehouseId` (> 0), `items[]` (`masterProductId` must belong to PO, `receivedQty` > 0, `allocatedLandedUnitCostSAR` > 0, `lotNumber` max 50, `expiryDate`). Recalculates `MasterProduct.WeightedAvgCost`, increments `PhysicalQty`, decrements `IncomingQty`, updates supplier payable, appends `GoodsReceipt` StockMovement. |

### Category 07 — Sales Orders & Approval Workflow (`/api/v1/orders/*`)
| Endpoint | Required Policy | Required Headers | Payload Fields & Business Rules |
|---|---|---|---|
| `GET /api/v1/orders` | `customers:view` | Bearer | Query: `status`, `page`, `pageSize`. **ABAC:** `SALES_REP` sees only own orders; `WH_KEEPER` sees only assigned warehouses. |
| `POST /api/v1/orders` | `orders:create` | **`X-Idempotency-Key` (GUID)** | `orderChannel` (`PreSales` or `CashVan`), `customerId` (GUID), `warehouseId` (> 0), `items[]` (`masterProductId`, `productUnitId`, `orderedQty` > 0, `unitPrice` > 0). Evaluates **MR-01** (CashVan mobile warehouse), **MR-02** (`PriceOverridePending` if below tier/floor), **MR-03** (`RequiresFinanceCoApproval` if over credit limit / cash-only grade / overdue invoice), **RISK-01** (24h soft reservation), **RISK-03** (`HasStockShortage` flag — never drops order). |
| `PUT /api/v1/orders/{id}/manager-edit` | `orders:edit_submitted` | Bearer | `items[]` (`salesOrderItemId`, `approvedQty` > 0, `unitPrice` > 0). **Strict RBAC + Domain Guard:** `SALES_REP` always gets `403`. Reconciles `ReservedQty` delta and audits old/new JSON snapshots. |
| `POST /api/v1/orders/{id}/override-price` | `orders:override_price` | Bearer | Approves pending below-tier/below-floor price overrides (MR-02). |
| `POST /api/v1/orders/{id}/co-approve-credit` | `orders:coapprove_credit` | Bearer | Finance/Executive credit exception co-approval (MR-03). Returns `422` if order does not require co-approval. |
| `POST /api/v1/orders/{id}/approve` | `orders:approve` | Bearer | Transitions `PendingApproval` → `Approved`. Returns `422` if credit co-approval, price override, or stock shortage is unresolved. |
| `POST /api/v1/orders/{id}/reject` | `orders:approve` | Bearer | `rejectionReason` (required, max 500). Releases reserved stock and sets status `Rejected`. |
| `POST /api/v1/orders/{id}/confirm-shipment` | `warehouse:ship` | **`X-Idempotency-Key` (GUID)** | `egsDeviceId` (required, max 64, registered EGS device), `invoiceType` (`StandardB2B` or `SimplifiedB2C`), optional `items[]` (`salesOrderItemId`, `shippedQty` <= `approvedQty`). Deducts stock, appends `SalesShipment`, monotonically allocates ZATCA ICV/PIH/SHA-256 XML hash/TLV QR, debits customer balance, marks order `ShippedAndInvoiced`. |

### Category 08 — ZATCA Invoices, Returns & Delivery (`/api/v1/invoices/*`, `/api/v1/returns`, `/api/v1/delivery/*`)
| Endpoint | Required Policy | Payload Fields & Column Validations |
|---|---|---|
| `POST /api/v1/invoices/egs-devices` | `settings:manage` | `egsDeviceId` (required, max 64, unique → `409`), `warehouseId` (optional FK), `certificateSerial` (required, max 128). Initializes `CurrentIcv = 0` and initial PIH (`SHA256("0")`). |
| `GET /api/v1/invoices/{id}/print` | `invoices:view` | Returns invoice totals, ZATCA metadata (`zatcaIcv`, `zatcaUuid`, `zatcaPreviousInvoiceHash`, `zatcaXmlHash`, `zatcaQrCodeBase64`), customer details, and order lines. |
| `POST /api/v1/invoices/{id}/upload-copy` | `invoices:upload_copy` | `uploadedInvoiceCopyUrl` (required, max 500). |
| `POST /api/v1/returns` | `sales:returns` | `originalInvoiceId` (GUID), `warehouseId` (> 0), `returnReason` (required, max 500), `egsDeviceId` (required, max 64), `items[]` (`masterProductId`, `productUnitId`, `returnedQty` > 0, `isSellableRestock` bool, `unitPrice` > 0). Allocates ZATCA Credit Note ICV, credits customer, and restocks sellable items. |
| `POST /api/v1/delivery/dispatches` | `delivery:dispatch` | `warehouseId` (> 0), `driverOrRepId` (GUID), `vehiclePlateNumber` (max 30), `invoiceIds` (non-empty `Guid[]`). |
| `PUT /api/v1/delivery/dispatches/{id}/items/{invoiceId}/pod` | `delivery:update_pod` | `deliveryStatus` (`PreparedAtWarehouse`, `OutForDelivery`, `Delivered`, `FailedAttempt`), `signedPodImageUrl` (max 500), `deliveryNotes` (max 500). |

### Category 09 — Collections, Custody & Finance (`/api/v1/collections/*`, `/api/v1/finance/*`)
| Endpoint | Required Policy | Required Headers | Payload Fields & Column Validations |
|---|---|---|---|
| `GET /api/v1/finance/accounts` | `dashboards:finance` | Bearer | Lists chart of accounts (`MainTreasurySafe`, `BankAccount`, `RepCustodyWallet`, `AccountsReceivable`, etc.). |
| `POST /api/v1/finance/accounts` | `settings:manage` | Bearer | `accountCode` (required, max 30, unique → `409`), `nameAr` (required, max 150), `nameEn` (max 150), `accountType` (canonical enum), `ownerRepId` (optional GUID), `bankName` (max 100), `iban` (max 50). |
| `GET /api/v1/finance/fiscal-periods` | `dashboards:finance` | Bearer | Lists fiscal periods. |
| `POST /api/v1/finance/fiscal-periods` | `finance:close_period` | Bearer | `fiscalYear` (`2020..2100`), `fiscalMonth` (`1..12`), unique `(year, month)` → `409`. |
| `POST /api/v1/finance/fiscal-periods/{id}/close` | `finance:close_period` | Bearer | Locks fiscal period against future postings (MR-05). |
| `POST /api/v1/collections/cash` | `collections:create` | **`X-Idempotency-Key` (GUID)** | `customerId` (GUID), `amount` (> 0), optional `allocations[]` (`invoiceId`, `allocatedAmount` > 0, sum <= `amount`). Requires open fiscal period; posts balanced journal (`DR RepCustodyWallet / CR AccountsReceivable`). |
| `POST /api/v1/collections/{id}/confirm` | `collections:create` | Bearer | Sets `isCustomerConfirmed = true`. |
| `POST /api/v1/collections/handover` | `finance:confirm_treasury` | Bearer | `collectionId` (GUID, must be Cash + `InRepCustody`), `targetTreasuryAccountId` (GUID, must be `MainTreasurySafe`). Posts balanced journal (`DR MainTreasurySafe / CR RepCustodyWallet`). |
| `POST /api/v1/collections/bank-transfer` | `collections:create` | **`X-Idempotency-Key` (GUID)** | `customerId` (GUID), `amount` (> 0), `bankReferenceNumber` (required, max 100), `paymentProofImageUrl` (required, max 500), `paymentProofImageHash` (required 64-char SHA-256 hex `^[a-fA-F0-9]{64}$`, unique among non-rejected → **`409 Conflict`**), optional `originalValueDateUtc`, optional `allocations[]`. |
| `POST /api/v1/collections/{id}/verify-bank` | `finance:verify_bank` | Bearer | `targetBankAccountId` (required GUID when `approve: true`; must be `BankAccount`), `approve` (bool). When approved: requires open fiscal period for posting date while preserving `originalValueDateUtc` (MR-05), credits customer, updates invoice balances, posts balanced journal (`DR BankAccount / CR AccountsReceivable`). |
| `GET /api/v1/finance/journal-entries` | `dashboards:finance` | Bearer | Paginated immutable double-entry journal entries with debit/credit lines. |

### Category 10 — Dashboards, Audit Logs & GPS (`/api/v1/dashboards/*`, `/api/v1/audit-logs`, `/api/v1/gps/*`)
| Endpoint | Required Policy | Payload Fields & Column Validations |
|---|---|---|
| `GET /api/v1/dashboards/executive` | `dashboards:executive` | Real-time KPI snapshot across safe, rep custody, banks, receivables, payables, inventory valuation, pending orders, low stock. |
| `GET /api/v1/dashboards/role/{roleView}` | Role-matched (`dashboards:sales` / `inventory` / `finance`) | `roleView`: `sales`, `inventory`, `warehouse`, `finance`, or `rep` (unknown view → `422`). |
| `GET /api/v1/audit-logs` | `audit:view` | Query: `entityName` (max 100), `userId` (GUID), `cursor` (last `id` for keyset pagination), `pageSize` (`1..250`). |
| `POST /api/v1/gps/pings` | `gps:stream` (`SALES_REP`) | `pings[]` (`1..100` items; `latitude` `-90..90`, `longitude` `-180..180`, `accuracyMeters` >= 0, `nearCustomerId`, `recordedAtUtc`). Returns `202 Accepted`. |

---

## 3. Troubleshooting & Error Handling Playbook

### 3.1 HTTP API Errors (RFC 7807 `ProblemDetails`)

| HTTP Status | Cause & Why It Happened | How to Fix / Handle |
|---|---|---|
| **`400 Bad Request`** | 1) A column failed FluentValidation (e.g., invalid Saudi VAT regex, password missing uppercase/symbol, `minAllowedPrice > wholesalePrice`, `allocations` sum > `amount`).<br>2) Missing or invalid `X-Idempotency-Key` header on idempotent write endpoints (`POST /orders`, `confirm-shipment`, `collections/cash`, `collections/bank-transfer`). | Inspect the `errors` dictionary in the response JSON for the exact field message. On idempotent writes, always send header `X-Idempotency-Key: {{$guid}}`. |
| **`401 Unauthorized`** | 1) Missing `Authorization: Bearer <token>` header.<br>2) Access token expired (lifetime is **10 minutes**).<br>3) User was deactivated or their role permissions changed, rotating their `SecurityStamp` (immediate revocation). | Run `POST /api/v1/auth/refresh` (or re-run `POST /api/v1/auth/login`) to obtain a fresh JWT. Ensure `{{accessToken}}` is set in Postman. |
| **`403 Forbidden`** | 1) **RBAC:** Authenticated user's role lacks the endpoint's `[Authorize(Policy = "...")]` claim (e.g., `SALES_REP` calling `PUT /orders/{id}/manager-edit`, or `SUPER_ADMIN` calling `POST /gps/pings` without `gps:stream`).<br>2) **ABAC:** `SALES_REP` accessing an unassigned customer, or `WH_KEEPER` accessing an unassigned warehouse.<br>3) **Origin Check:** Calling `/auth/refresh` or `/auth/logout` with an `Origin` header not listed in `Cors:AllowedOrigins`. | Sign in with an employee holding the required role/permission, verify customer/warehouse assignment, or add your frontend origin to `Cors:AllowedOrigins`. |
| **`404 Not Found`** | Referenced entity ID (`customerId`, `productId`, `egsDeviceId`, `warehouseId`, etc.) does not exist in SQL Server. | Confirm earlier setup folders (Warehouse, Product, Supplier, EGS Device, Customer) were executed and that Postman environment variables are populated. |
| **`409 Conflict`** | 1) Duplicate unique natural key (`EmployeeCode`, `Phone`, `SKU`, `Barcode`, `CustomerCode`, `WarehouseCode`, `SupplierCode`, `ContainerNumber`, `AccountCode`, `(FiscalYear, FiscalMonth)`).<br>2) Duplicate `paymentProofImageHash` on `POST /collections/bank-transfer` (anti-fraud deduplication).<br>3) Optimistic concurrency conflict (`ROWVERSION` mismatch). | Use a unique code/hash for new records. For bank transfer retries of the same request, reuse the same `X-Idempotency-Key`. For concurrency conflicts, re-fetch the entity and retry. |
| **`422 Unprocessable Entity`** | A core ERP business rule was violated:<br>• **MR-02:** Approving an order with unresolved `HasPriceOverridePending` → call `POST /orders/{id}/override-price` first.<br>• **MR-03:** Approving an order with `RequiresFinanceCoApproval == true` → call `POST /orders/{id}/co-approve-credit` first.<br>• **RISK-03:** Approving an order with `HasStockShortage == true` → replenish stock via GRN or reduce approved quantity via `PUT /orders/{id}/manager-edit`.<br>• **MR-05:** Posting a cash collection, treasury handover, or bank verification when no open `FiscalPeriod` exists for the current UTC year/month (or the period is closed) → open the period via `POST /finance/fiscal-periods`.<br>• **Stock:** Transferring more than `AvailableQty` or counting physical stock below `ReservedQty`. | Follow the domain workflow sequence shown in the `detail` message (e.g., open fiscal period, receive GRN stock, co-approve credit, override price). |
| **`429 Too Many Requests`** | Failed login attempts exceeded the brute-force rate limit for that IP/identifier. | Wait for the rate-limit window to expire (default 15 minutes) or restart local dev server if using the Development in-memory limiter. |
| **`503 Service Unavailable`** | Redis is configured (`Redis:ConnectionString` non-empty) but unreachable, so `SecurityStampMiddleware` fails closed to protect revoked sessions. | Start your local Redis instance, **or** in `Development` mode unset `Redis:ConnectionString` to activate the built-in Development in-memory security store. |

### 3.2 Startup & Infrastructure Troubleshooting

| Symptom | Why It Happens | Fix / Recommendation |
|---|---|---|
| **`You must install or update .NET to run this application (Microsoft.AspNetCore.App 9.0.0)`** | Host machine has `.NET 10` / `.NET 11` runtime installed without `.NET 9`. | Set `$env:DOTNET_ROLL_FORWARD = "Major"` in your PowerShell session before running `dotnet run`, `dotnet test`, or `dotnet ef`. |
| **`Auth:Jwt configuration (Issuer, Audience, and a SigningKey of at least 32 UTF-8 bytes) is required`** | Fail-fast startup guard prevents starting without a real JWT signing key. | Export `Auth__Jwt__Issuer`, `Auth__Jwt__Audience`, and a 32+ byte `Auth__Jwt__SigningKey` (or store them via `dotnet user-secrets`). |
| **SQL Server login/connection error on `localhost`** | SQL Server service stopped, named instance (`.\SQLEXPRESS`), or missing `TrustServerCertificate=True`. | Verify SQL Server service is running (`sqlcmd -S localhost -Q "SELECT DB_NAME()"`). Update `ConnectionStrings__SqlServer` if using a named instance. |
| **Postman HTTPS SSL warning on `https://localhost:5001`** | Self-signed ASP.NET Core dev certificate not trusted or Postman SSL verification enabled. | Use `http://localhost:5000` (default in `AlMomiaa-Local.postman_environment.json`) **or** run `dotnet dev-certs https --trust` and toggle Postman *SSL certificate verification* if needed. |

### 3.3 Recommended Production Hardening Before Go-Live
1. **Configure real Redis** (`Redis:ConnectionString`) so security-stamp revocation and login rate limiting are shared across API instances.
2. **Do not set `ALMOMIAA_DEV_ADMIN_*`** in Staging/Production (the bootstrapper only runs when `ASPNETCORE_ENVIRONMENT=Development`).
3. **Add a background worker** to process `[ops].[OutboxMessages]` (for real-time SignalR/push alerts & ZATCA Phase 2 Fatoora API clearance/reporting) and a scheduled job to auto-release expired 24-hour soft stock reservations (`RISK-01`).
4. **Integrate object storage (Azure Blob / S3)** with signed upload URLs for `UploadedInvoiceCopyUrl`, `SignedPodImageUrl`, and `PaymentProofImageUrl` with server-side SHA-256 hashing.
