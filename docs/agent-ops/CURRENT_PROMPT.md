# PROMPT-0005: Implement Full 8-Module .NET 9 Backend REST API Suite, Per-Column Validations, Categorized Postman Collection, and Backend Runbook

- **Status:** `COMPLETED (Archived as 0005-full-backend-api-and-postman-suite.md)`
- **Prompt ID:** `PROMPT-0005`
- **Scope:** `apps/backend-api` (`AlMomiaa.Domain`, `AlMomiaa.Application`, `AlMomiaa.Infrastructure`, `AlMomiaa.WebApi`, `tests/`) and downloadable API testing/documentation artifacts under `docs/api/`
- **Reference baseline:** Architecture v3.1 (`00-ADR`, `05-Database`, `06-API-Specification`, `07-Security`, `08-Development-Roadmap`); `PROMPT-0001`..`PROMPT-0004` completed (all 8 schemas & 44 tables live in SQL Server `AlMomiaa`)
- **Confirmed decisions:**
  - Implement **all 8 backend API modules** (`IAM`, `Customers`, `Catalog`, `Inventory`, `Procurement`, `Sales/ZATCA/Delivery`, `Collections/Finance`, `Dashboards/Audit/GPS`) with complete per-column FluentValidation rules and business guards.
  - Add a **Development-only** env-var bootstrap for an initial local `SUPER_ADMIN` user (strictly disabled in Production, zero credentials committed) plus a **Development-only** in-memory fallback for `ISecurityStampStore` / `ILoginAttemptLimiter` when `Redis:ConnectionString` is blank in `Development` (Production still strictly requires Redis and fails closed with `503`).
  - Standardize list endpoints on `{ items, totalCount, nextCursor }` (`pageSize <= 250`, `.AsNoTracking()` SQL projections) and RFC 7807 `ProblemDetails` (`400`, `401`, `403`, `404`, `409`, `422`, `429`, `503`).
  - Generate downloadable **Postman Collection v2.1** (`docs/api/AlMomiaa-ERP.postman_collection.json`), **Postman Local Environment** (`docs/api/AlMomiaa-Local.postman_environment.json`), and a detailed **Backend Runbook & Troubleshooting Guide** (`docs/api/BACKEND_API_GUIDE.md`).

---

## 1. Purpose of Prompt

Deliver the complete, production-structured `.NET 9` CQRS REST API layer over the 44-table `AlMomiaa` SQL Server database so every ERP module can be exercised end-to-end via Swagger/OpenAPI and an importable categorized Postman collection, with strict per-column validation, RBAC permission policies, row-level ABAC scoping, transactional audit logging (`ops.AuditLogs`) & outbox persistence (`ops.OutboxMessages`), idempotency protection, and full automated tests.

This prompt covers **Backend `.NET 9` + Postman Collection + API Runbook only**. Do not modify `apps/web-erp`, `apps/design-system-studio`, `packages/ui-kit`, or `packages/api-client`.

---

## 2. Target Execution Agents and Order

1. **`@Backend .NET`** — add Domain factory/behavior methods & domain exceptions, Application CQRS commands/queries/DTOs/FluentValidation validators for all 8 modules, Infrastructure services (current user, real permission checker, idempotency store, ACID transaction boundary + audit/outbox interceptor, ZATCA TLV QR/hash generator, Dev-only admin bootstrap & Redis fallback), WebApi controllers + global exception/concurrency mapping + OpenAPI metadata, automated unit/integration tests, and the Postman/runbook deliverables in `docs/api/`.
2. **`@QA & Security Auditor`** — audit EVERY endpoint for explicit `[Authorize(Policy = "...")]` attributes, row-level ABAC scoping (`SALES_REP` / `WH_KEEPER`), `orders:edit_submitted` 3-tier lock, per-column validation alignment with `05-Database.md`, idempotency, `ROWVERSION` `409` handling, append-only ledger integrity, fiscal period lock, ZATCA lock/math precision, Postman JSON validity, zero committed secrets, and full build/test pass.
3. **`@Orchestrator`** — coordinate execution and archive/log only after QA returns `PASS`.

---

## 3. Target Files to Create / Modify

### `apps/backend-api/src/AlMomiaa.Domain/` (Pure C# 13 — zero external NuGet/framework references)
- `Common/BusinessRuleException.cs`, `Common/NotFoundException.cs`, `Common/ConflictException.cs`, `Common/ForbiddenDomainException.cs`
- Add clean domain factories & state-transition guards on existing entities across `Identity/`, `Catalog/`, `Customers/`, `Inventory/`, `Procurement/`, `Sales/`, `Finance/`, and `System/` (`AlMomiaa.Domain.SystemOps`) — including `SalesOrder` edit/approve/reject/credit-coapprove/price-override guards, `WarehouseStock` reserve/deduct/adjust guards, `FiscalPeriod` lock guard, `JournalEntry` balanced double-entry invariant (`SUM(Debit) == SUM(Credit)`), and `AuditLog`/`OutboxMessage` factories.

### `apps/backend-api/src/AlMomiaa.Application/`
- `Common/Models/PagedResult.cs` (`PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, string? NextCursor)`)
- `Common/Abstractions/ICurrentUserService.cs` (`UserId`, `RoleCodes`, `Permissions`, `RepType`, `DefaultWarehouseId`, `AssignedWarehouseIds`, `IpAddress`, `IsInRole(...)`, `HasPermission(...)`)
- `Common/Abstractions/IApplicationDbContext.cs` (or feature-scoped service/repository abstractions keeping Application independent of EF Core SqlServer provider)
- Feature modules (`Commands/`, `Queries/`, `Contracts/`, `Validators/`):
  - `Users/` — `GetUsersQuery`, `CreateUserCommand`, `UpdateUserStatusCommand`, `GetRolesQuery`, `UpdateRolePermissionsCommand`
  - `Customers/` — `GetCustomersQuery`, `CreateCustomerCommand`, `GetCustomerCreditStatusQuery`, `GetCustomerGradesQuery`, `UpdateCustomerGradesCommand`
  - `Catalog/` — `GetCategoryTreeQuery`, `CreateCategoryCommand`, `GetBrandsQuery`, `CreateBrandCommand`, `GetProductsQuery`, `GetCategoryProductsQuery`, `CreateMasterProductCommand`, `LinkSupplierProductCommand`, `GetPriceListsQuery`, `CreatePriceListCommand`, `AddPriceListItemCommand`
  - `Inventory/` — `GetWarehousesQuery`, `CreateWarehouseCommand`, `GetWarehouseStocksQuery`, `CreateStockTransferCommand`, `ReceiveStockTransferCommand`, `SubmitInventoryCountCommand`, `GetStockMovementsQuery`
  - `Procurement/` — `GetSuppliersQuery`, `CreateSupplierCommand`, `GetPurchaseOrdersQuery`, `CreatePurchaseOrderCommand`, `ApprovePurchaseOrderCommand`, `CreateContainerCommand`, `UpdateContainerStatusCommand`, `CreateGoodsReceiptCommand`
  - `Sales/` — `GetSalesOrdersQuery`, `CreateSalesOrderCommand`, `EditSubmittedOrderByManagerCommand`, `OverrideOrderPriceCommand`, `CoApproveOrderCreditCommand`, `ApproveSalesOrderCommand`, `RejectSalesOrderCommand`, `ConfirmOrderShipmentCommand`, `GetInvoicePrintQuery`, `UploadInvoiceCopyCommand`, `CreateSalesReturnCommand`, `CreateDeliveryDispatchCommand`, `UpdateDeliveryDispatchItemPodCommand`
  - `Finance/` — `GetFinancialAccountsQuery`, `CreateFinancialAccountCommand`, `GetFiscalPeriodsQuery`, `CreateFiscalPeriodCommand`, `CloseFiscalPeriodCommand`, `RecordCashCollectionCommand`, `ConfirmCashCollectionByCustomerCommand`, `RecordTreasuryHandoverCommand`, `RecordBankTransferCommand`, `VerifyBankTransferCommand`, `GetJournalEntriesQuery`
  - `Operations/` — `GetExecutiveDashboardQuery`, `GetRoleDashboardQuery`, `GetAuditLogsQuery`, `RecordGpsPingsCommand`

### `apps/backend-api/src/AlMomiaa.Infrastructure/`
- `Authentication/InMemoryDevSecurityServices.cs` — **Development-only** in-memory `ISecurityStampStore` & `ILoginAttemptLimiter` registered ONLY when `IHostEnvironment.IsDevelopment()` AND `Redis:ConnectionString` is empty. Production/Staging ALWAYS bind real Redis and fail closed with `503`.
- `Authentication/DevelopmentAdminBootstrapper.cs` — **Development-only** idempotent bootstrap that reads `ALMOMIAA_DEV_ADMIN_CODE`, `ALMOMIAA_DEV_ADMIN_PHONE`, `ALMOMIAA_DEV_ADMIN_PASSWORD` from environment variables (never hardcoded!) and creates a local `SUPER_ADMIN` user if those env vars are explicitly provided in `Development`.
- `Persistence/AuditAndOutboxServices.cs` — real `IRequestPermissionChecker`, `IIdempotencyStore` (backed by DB/memory), `EfTransactionBoundary`, `AuditSaveChangesInterceptor` (writing to `[ops].[AuditLogs]` inside the same ACID transaction), and `OutboxWriter` (`[ops].[OutboxMessages]`).
- `Zatca/ZatcaInvoiceHelper.cs` — deterministic SHA-256 XML hash, TLV Base64 QR code (Tags 1–5/9), 15% VAT precision (`MidpointRounding.AwayFromZero`), and `sales.ZatcaDeviceSequences` transactional ICV/PIH progression.
- `DependencyInjection.cs` — wire all services and Dev-only fallbacks cleanly.

### `apps/backend-api/src/AlMomiaa.WebApi/`
- `Controllers/`:
  - `AuthController.cs` (existing — preserved untouched except Swagger tags)
  - `UsersController.cs` (`/api/v1/users`, `/api/v1/roles`)
  - `CustomersController.cs` (`/api/v1/customers`)
  - `CatalogController.cs` (`/api/v1/catalog`)
  - `WarehousesController.cs` (`/api/v1/warehouses`)
  - `ProcurementController.cs` (`/api/v1/procurement`)
  - `SalesOrdersController.cs` (`/api/v1/orders`)
  - `InvoicesController.cs` (`/api/v1/invoices`)
  - `ReturnsController.cs` (`/api/v1/returns`)
  - `DeliveryController.cs` (`/api/v1/delivery`)
  - `CollectionsController.cs` (`/api/v1/collections`)
  - `FinanceController.cs` (`/api/v1/finance`)
  - `DashboardsController.cs` (`/api/v1/dashboards`)
  - `AuditLogsController.cs` (`/api/v1/audit-logs`)
  - `GpsController.cs` (`/api/v1/gps`)
- `Services/HttpCurrentUserService.cs`
- `Program.cs` — map `BusinessRuleException` (`422`), `NotFoundException` (`404`), `ConflictException` / `DbUpdateConcurrencyException` (`409`), `ForbiddenDomainException` (`403`), `ValidationException` (`400`), and `SecurityStampUnavailableException` (`503`) to RFC 7807 `ProblemDetails`; register `HttpContextAccessor` and Dev-only bootstrapper hook; enrich OpenAPI (`v1`) with tags and security requirements.

### `apps/backend-api/tests/`
- Extend tests (in `AlMomiaa.AuthenticationTests` or a new `AlMomiaa.ApiIntegrationTests` project added to `AlMomiaa.Erp.sln`) covering:
  - Per-column validation failures (`400`) across modules
  - RBAC policy enforcement (`403`) including `SALES_REP` blocked on `PUT /api/v1/orders/{id}/manager-edit` and `WH_KEEPER` blocked on `POST /api/v1/orders/{id}/approve`
  - Row-level ABAC scoping (`SALES_REP` sees only own customers; `WH_KEEPER` scoped to assigned warehouses)
  - End-to-end ERP happy-path flow: Create User → Warehouse → Category/Brand/MasterProduct/PriceList → Supplier/PO/Container/GRN (stock + WAC update) → Customer → SalesOrder (credit/price checks + stock reservation) → Manager Edit → Approve → Confirm Shipment (ZATCA Invoice + stock deduction) → Delivery Dispatch/POD → Cash/Bank Collection (double-entry balanced journal + fiscal period lock rejection on closed period) → Executive Dashboard & Audit Logs!
  - Optimistic concurrency (`409`) and idempotency key deduplication!

### `docs/api/` (Downloadable Postman & Runbook Deliverables)
- `docs/api/AlMomiaa-ERP.postman_collection.json` — Valid **Postman Collection v2.1** JSON organized into **10 categorized folders**:
  1. `00 - Health & OpenAPI`
  2. `01 - Auth (Login, Refresh, Logout)` (with Postman Test scripts that automatically save `{{accessToken}}`, `{{currentUserId}}`, and capture IDs across workflow requests!)
  3. `02 - IAM (Users & Roles)`
  4. `03 - Customers & Credit Grades`
  5. `04 - Product Catalog & Price Lists`
  6. `05 - Warehouses, Stock & Transfers`
  7. `06 - Procurement, Containers & Goods Receipts`
  8. `07 - Sales Orders, Approvals, ZATCA Invoices, Returns & Delivery POD`
  9. `08 - Collections, Treasury, Bank Verification & Fiscal Periods`
  10. `09 - Dashboards, Audit Logs & GPS Telemetry`
- `docs/api/AlMomiaa-Local.postman_environment.json` — Ready-to-import Postman Environment with `baseUrl`, `accessToken`, `origin`, and chained entity ID variables (`warehouseId`, `categoryId`, `brandId`, `productId`, `productUnitId`, `priceListId`, `supplierId`, `customerId`, `poId`, `containerId`, `orderId`, `invoiceId`, `dispatchId`, `collectionId`, `fiscalPeriodId`, `financialAccountId`, `egsDeviceId`, `repUserId`).
- `docs/api/BACKEND_API_GUIDE.md` — Complete developer & QA runbook containing:
  1. Step-by-step instructions to configure & run the `.NET 9` API locally (`user-secrets`/env vars, SQL Server `AlMomiaa`, Redis vs Dev fallback, local `SUPER_ADMIN` bootstrap, Swagger URL, Postman import order).
  2. Full per-category, per-endpoint reference: HTTP method, URL, required permission claim, headers (`Authorization`, `Accept-Language`, `X-Idempotency-Key`, `Origin`), exact JSON request/response examples, and **every column's validation rule**.
  3. Troubleshooting & Edge-Case Guide: exact causes, HTTP status codes (`400`, `401`, `403`, `404`, `409`, `422`, `429`, `503`), and recommended handling/fixes.

---

## 4. Complete API Matrix & Per-Column Validation Specification (All 8 Modules)

> **Global Validation Rules Applied Everywhere:**
> - Pagination query params: `page >= 1` (default `1`), `pageSize` between `1` and `250` (default `50`), optional `search` (`MaxLength(100)`).
> - Monetary fields (`decimal(18,2)`): precision `18`, scale `2`, non-negative unless explicitly signed.
> - Unit cost / conversion fields (`decimal(18,4)`): precision `18`, scale `4`, `> 0` (or `>= 0` where allowed).
> - Quantity fields (`decimal(18,3)`): precision `18`, scale `3`, `> 0` on orders/receipts/transfers.
> - Required headers on idempotent writes (`POST /orders`, `POST /orders/{id}/confirm-shipment`, `POST /collections/cash`, `POST /collections/bank-transfer`): `X-Idempotency-Key` must be a valid non-empty `Guid`.
> - All string fields trimmed before validation/storage; unique constraints checked cleanly and mapped to `409 Conflict` (`ProblemDetails`) on duplicate keys.

### Module 1: Identity & Access Management (`/api/v1/users`, `/api/v1/roles`)
- `GET /api/v1/users` — Policy: `users:manage` | Paged list of users (with roles & assigned warehouses). Query: `search`, `roleCode`, `isActive`, `page`, `pageSize`.
- `POST /api/v1/users` — Policy: `users:manage` | Creates employee (and auto-creates `RepCustodyWallet` in `[fin].[FinancialAccounts]` if `SALES_REP`, plus optional `MobileVan` warehouse if `createMobileVan == true`).
  - `employeeCode`: Required, `MaxLength(30)`, regex `^[A-Za-z0-9\-_]+$`, unique in `[id].[Users]`
  - `fullNameAr`: Required, `MaxLength(150)`
  - `fullNameEn`: Optional, `MaxLength(150)`
  - `phone`: Required, `MaxLength(20)`, normalized Saudi/E.164 phone regex `^\+?[0-9]{9,15}$`, unique in `[id].[Users]`
  - `email`: Optional, valid email format, `MaxLength(150)`
  - `password`: Required, `MinLength(8)`, `MaxLength(128)`, must contain upper, lower, digit, and special character
  - `repType`: Optional; when provided must be in `{"Wholesale", "Retail", "CashVan"}` (`MaxLength(20)`)
  - `defaultWarehouseId`: Optional; must exist in `[inv].[Warehouses]` when provided
  - `roleIds`: Required, non-empty array of valid `[id].[Roles].Id`
  - `assignedWarehouseIds`: Optional array of valid `[inv].[Warehouses].Id`
  - `createMobileVan`: Optional bool; if true, `vehiclePlateNumber` (`MaxLength(30)`) optional, creates `[inv].[Warehouses]` with `WarehouseType = 'MobileVan'`
- `PATCH /api/v1/users/{id}/status` — Policy: `users:manage` | Activates/deactivates user (`{ isActive: bool }`) and immediately rotates/invalidates Redis `SecurityStamp`!
- `GET /api/v1/roles` — Policy: `permissions:manage` (or `users:manage`) | Lists all roles and their assigned permission claims.
- `PUT /api/v1/roles/{roleId}/permissions` — Policy: `permissions:manage` (`SUPER_ADMIN` only) | Replaces permission claims for a role (`{ permissions: string[] }`, each non-empty `MaxLength(100)` and valid canonical claim), and immediately invalidates Redis `SecurityStamp` for all users assigned to that role!

### Module 2: Customers & Credit Grades (`/api/v1/customers`)
- `GET /api/v1/customers` — Policy: `customers:view` | Paged list. **ABAC:** if caller is `SALES_REP` (and not `SUPER_ADMIN`/`EXEC_MGR`/`SALES_MGR`/`ACCOUNTANT`), automatically filtered to `AssignedRepId == CurrentUser.UserId`! Query: `search`, `gradeId`, `city`, `isActive`, `page`, `pageSize`.
- `GET /api/v1/customers/{id}/credit-status` — Policy: `customers:view` | Returns `currentBalance`, `overdueDebt`, `creditLimit`, `gradeCode`, `priceListId`, `effectivePaymentDays` (`CustomPaymentDays ?? Grade.DefaultPaymentDays`), `allowCredit`, and last 10 invoices/orders. Respects `SALES_REP` ownership scope (`403`/`404` if not assigned).
- `POST /api/v1/customers` — Policy: `customers:manage`
  - `customerCode`: Required, `MaxLength(30)`, unique in `[cust].[Customers]`
  - `nameAr`: Required, `MaxLength(200)`
  - `nameEn`: Optional, `MaxLength(200)`
  - `phone`: Required, `MaxLength(20)`, regex `^\+?[0-9]{9,15}$`
  - `address`: Required, `MaxLength(500)`
  - `city`: Optional (default `"Riyadh"`), `MaxLength(100)`
  - `category`: Required, `MaxLength(50)`, in `{"Market", "Store", "Restaurant", "Supermarket", "Wholesale"}`
  - `vatNumber`: Optional, if provided exact 15 digits (`^3[0-9]{13}3$` or `^[0-9]{15}$`, `MaxLength(15)`)
  - `gradeId`: Required, must exist in `[cust].[CustomerGrades]`
  - `priceListId`: Optional, must exist in `[cat].[PriceLists]`
  - `customPaymentDays`: Optional, `0..365`
  - `creditLimit`: Required, `>= 0`, `decimal(18,2)`
  - `assignedRepId`: Required, must exist in `[id].[Users]`
  - `latitude`: Optional, `-90..90` (`decimal(9,6)`)
  - `longitude`: Optional, `-180..180` (`decimal(9,6)`)
- `GET /api/v1/customers/grades` — Policy: `customers:view` | Returns Grades A/B/C/D.
- `PUT /api/v1/customers/grades` — Policy: `settings:manage` | Updates grade payment rules (`grades: [{ gradeCode, defaultPaymentDays (0..365), allowCredit }]`, `gradeCode` in `{"A","B","C","D"}`).

### Module 3: Product Catalog, Price Lists & Multi-Supplier (`/api/v1/catalog`)
- `GET /api/v1/catalog/categories/tree` — Policy: `catalog:view` | Returns hierarchical category tree (`ParentCategoryId` recursion).
- `POST /api/v1/catalog/categories` — Policy: `catalog:manage`
  - `parentCategoryId`: Optional, must exist in `[cat].[Categories]`
  - `nameAr`: Required, `MaxLength(150)`
  - `nameEn`: Optional, `MaxLength(150)`
  - `displayOrder`: `>= 0` (default `0`)
- `GET /api/v1/catalog/brands` — Policy: `catalog:view` | Lists brands.
- `POST /api/v1/catalog/brands` — Policy: `catalog:manage`
  - `nameAr`: Required, `MaxLength(100)`
  - `nameEn`: Optional, `MaxLength(100)`
- `GET /api/v1/catalog/products` — Policy: `catalog:view` | Paged master products with units & stock summary. **Security:** `weightedAvgCost` is omitted (`null`) unless caller holds `SUPER_ADMIN`, `EXEC_MGR`, `ACCOUNTANT`, or `PURCH_MGR` (matching `07-Security.md` `"All (No Cost)"` rule!).
- `GET /api/v1/catalog/categories/{id}/products` — Policy: `catalog:view` | Query: `customerId?`, `warehouseId?`, `page`, `pageSize` — returns category products with resolved Price List tier price & `availableQty`.
- `POST /api/v1/catalog/master-products` — Policy: `catalog:manage`
  - `sku`: Required, `MaxLength(50)`, unique in `[cat].[MasterProducts]`
  - `barcode`: Required, `MaxLength(50)`, unique in `[cat].[MasterProducts]`
  - `nameAr`: Required, `MaxLength(250)`
  - `nameEn`: Optional, `MaxLength(250)`
  - `categoryId`: Required, must exist in `[cat].[Categories]`
  - `brandId`: Required, must exist in `[cat].[Brands]`
  - `baseUnit`: Required, `MaxLength(30)`
  - `weightedAvgCost`: `>= 0`, `decimal(18,4)` (default `0`)
  - `wholesaleSellingPrice`: `> 0`, `decimal(18,2)`
  - `retailSellingPrice`: `> 0`, `decimal(18,2)`
  - `minAllowedSellingPrice`: `> 0`, `decimal(18,2)`, MUST be `<= wholesaleSellingPrice` and `<= retailSellingPrice` (`MR-02`)
  - `vatRate`: `0..100`, `decimal(5,2)` (default `15.00`)
  - `units`: Required non-empty array of `ProductUnit` items: `unitNameAr` (`MaxLength(50)`), `unitNameEn` (`MaxLength(50)`), `conversionFactor` (`> 0`, `decimal(18,4)`), `barcode` (`MaxLength(50)`, unique), `wholesalePrice` (`> 0`), `retailPrice` (`> 0`), `minAllowedPrice` (`> 0`, `<= wholesalePrice` and `<= retailPrice`), `isDefaultUnit` (exactly one default unit).
- `POST /api/v1/catalog/master-products/{id}/suppliers` — Policy: `catalog:manage`
  - `supplierId`: Required, must exist in `[proc].[Suppliers]`; `(supplierId, masterProductId)` unique
  - `supplierSku`: Optional, `MaxLength(50)`
  - `supplierBarcode`: Optional, `MaxLength(50)`
  - `lastPurchasePrice`: `> 0`, `decimal(18,4)`
  - `leadTimeDays`: Optional, `0..365`
  - `isPreferredSupplier`: bool
- `GET /api/v1/catalog/price-lists` — Policy: `catalog:view` | Lists price lists + items.
- `POST /api/v1/catalog/price-lists` — Policy: `catalog:manage`
  - `priceListCode`: Required, `MaxLength(30)`, unique
  - `nameAr`: Required, `MaxLength(150)`
  - `nameEn`: Optional, `MaxLength(150)`
  - `isDefault`: bool
  - `effectiveFromUtc`: Optional (default now)
  - `effectiveToUtc`: Optional, if set must be `> effectiveFromUtc`
  - `items`: Optional array of `{ productUnitId (exists), minQuantity (> 0, decimal(18,3)), tierUnitPrice (> 0, decimal(18,2)) }` with unique `(productUnitId, minQuantity)`.

### Module 4: Multi-Warehouse, Mobile Vans & Inventory (`/api/v1/warehouses`)
- `GET /api/v1/warehouses` — Policy: `dashboards:inventory` (or `catalog:view`) | Lists warehouses. **ABAC:** `WH_KEEPER` scoped to `AssignedWarehouseIds`.
- `POST /api/v1/warehouses` — Policy: `settings:manage` (or `users:manage`)
  - `warehouseCode`: Required, `MaxLength(20)`, unique
  - `nameAr`: Required, `MaxLength(150)`
  - `nameEn`: Optional, `MaxLength(150)`
  - `warehouseType`: Required, in `{"MainWarehouse", "BranchWarehouse", "MobileVan"}`
  - `assignedRepId`: Required when `warehouseType == "MobileVan"`, must exist in `[id].[Users]`
  - `vehiclePlateNumber`: Optional, `MaxLength(30)`
  - `city`: Optional (default `"Riyadh"`), `MaxLength(100)`
- `GET /api/v1/warehouses/{id}/stocks` — Policy: `dashboards:inventory` | Paged `WarehouseStocks` (`physicalQty`, `reservedQty`, `incomingQty`, `availableQty`, `minReorderQty`). **ABAC:** `WH_KEEPER` restricted to `AssignedWarehouseIds`.
- `POST /api/v1/warehouses/transfers` — Policy: `procurement:receive` (or `warehouse:ship` — register canonical `inventory:transfer` & `inventory:adjust` or map clearly to `07-Security.md` claims and document cleanly!) — Note: verify `06-API-Specification.md` lists `inventory:view`, `inventory:transfer`, `inventory:adjust`; add those 3 claims to `CanonicalRolesAndPermissions` (`SUPER_ADMIN`, `WH_KEEPER`, `PURCH_MGR`) with an additive seed check or map cleanly so every `06-API-Specification.md` claim works seamlessly!
  - `transferNumber`: Optional (auto-generated `TRF-...` if omitted) or `MaxLength(30)` unique
  - `sourceWarehouseId`: Required, exists in `[inv].[Warehouses]`
  - `destinationWarehouseId`: Required, exists, MUST differ from `sourceWarehouseId`
  - `items`: Required non-empty array of `{ masterProductId (exists), requestedQty (> 0, decimal(18,3)), dispatchedQty (> 0, <= requestedQty, decimal(18,3)) }`; validates sufficient `AvailableQty` in source warehouse, deducts `PhysicalQty`, increments destination `IncomingQty`, and appends `TransferOut` to `[inv].[StockMovements]`!
- `POST /api/v1/warehouses/transfers/{id}/receive` — Policy: `inventory:transfer` | Confirms receipt (`items: [{ stockTransferItemId, receivedQty (> 0, <= dispatchedQty) }]`), transitions `InTransit -> Received`, increments destination `PhysicalQty`, decrements `IncomingQty`, appends `TransferIn` to `[inv].[StockMovements]`.
- `POST /api/v1/warehouses/{id}/inventory-counts` — Policy: `inventory:adjust` | Adjusts stock (`items: [{ masterProductId, countedPhysicalQty (>= 0, decimal(18,3)), lotNumber? (MaxLength(50)), expiryDate? }]`), ensures `countedPhysicalQty >= ReservedQty`, appends `CountAdjustment` to `[inv].[StockMovements]`.

### Module 5: Purchasing, Suppliers, Containers & Landed Cost (`/api/v1/procurement`)
- `GET /api/v1/procurement/suppliers` — Policy: `procurement:manage` | Paged suppliers.
- `POST /api/v1/procurement/suppliers` — Policy: `procurement:manage`
  - `supplierCode`: Required, `MaxLength(30)`, unique
  - `nameAr`: Required, `MaxLength(200)`
  - `nameEn`: Optional, `MaxLength(200)`
  - `country`: Optional (default `"Saudi Arabia"`), `MaxLength(100)`
  - `phone`: Optional, `MaxLength(30)`
  - `vatNumber`: Optional, `MaxLength(20)`
- `GET /api/v1/procurement/purchase-orders` — Policy: `procurement:manage` | Paged POs + items.
- `POST /api/v1/procurement/purchase-orders` — Policy: `procurement:manage`
  - `supplierId`: Required, exists in `[proc].[Suppliers]`
  - `targetWarehouseId`: Required, exists in `[inv].[Warehouses]`
  - `orderType`: Required, in `{"ContainerImport", "LocalSupplierOrder"}`
  - `items`: Required non-empty array of `{ masterProductId (exists), orderedQty (> 0, decimal(18,3)), unitCostSAR (> 0, decimal(18,4)) }`. Computes `TotalAmountSAR`, creates PO (`Approved` or `Draft`), and increments `WarehouseStocks.IncomingQty`.
- `POST /api/v1/procurement/containers` — Policy: `procurement:manage`
  - `purchaseOrderId`: Required, exists in `[proc].[PurchaseOrders]`
  - `containerNumber`: Required, `MaxLength(50)`
  - `billOfLading`: Optional, `MaxLength(100)`
  - `shippingLine`: Optional, `MaxLength(100)`
  - `portOfArrival`: Required, `MaxLength(100)`
  - `estimatedArrivalDate`: Required (`DateOnly`)
  - `actualArrivalDate`: Optional (`DateOnly`)
  - `status`: Required, in `{"AtSea", "ArrivedPort", "CustomsClearance", "InTransitRiyadh", "ReceivedAtWarehouse"}`
  - `supplierGoodsCostSAR`: `> 0`, `decimal(18,2)`
  - `freightAndCustomsCostSAR`: `>= 0`, `decimal(18,2)`
- `POST /api/v1/procurement/goods-receipts` — Policy: `procurement:receive`
  - `purchaseOrderId`: Required, exists
  - `containerId`: Optional, if set must belong to `purchaseOrderId`
  - `warehouseId`: Required, exists (ABAC scoped for `WH_KEEPER`)
  - `items`: Required non-empty array of `{ masterProductId (exists), receivedQty (> 0), allocatedLandedUnitCostSAR (> 0, decimal(18,4)), lotNumber? (MaxLength(50)), expiryDate? }`.
  - Automatically distributes container `FreightAndCustomsCostSAR` (if linked & caller didn't override), recalculates `MasterProducts.WeightedAvgCost`, increments `WarehouseStocks.PhysicalQty`, decrements `IncomingQty`, updates `PurchaseOrderItems.ReceivedQty` & `PurchaseOrder.Status`, and appends `GoodsReceipt` rows to `[inv].[StockMovements]`!

### Module 6: Governed Sales Orders, ZATCA Invoicing, Returns & Delivery POD (`/api/v1/orders`, `/api/v1/invoices`, `/api/v1/returns`, `/api/v1/delivery`)
- `GET /api/v1/orders` — Policy: `orders:create` or `orders:approve` or `warehouse:ship` | Paged orders (**ABAC:** `SALES_REP` sees own orders; `WH_KEEPER` sees assigned warehouse orders).
- `POST /api/v1/orders` — Policy: `orders:create` | **Requires `X-Idempotency-Key` header (Guid)!**
  - `orderChannel`: Optional (default `"PreSales"`), in `{"PreSales", "CashVan"}`
  - `customerId`: Required, exists (ABAC: `SALES_REP` must own customer)
  - `warehouseId`: Required, exists (if `CashVan`, must be caller's assigned `MobileVan` warehouse!)
  - `items`: Required non-empty array of `{ masterProductId (exists), productUnitId (exists & belongs to masterProductId), orderedQty (> 0, decimal(18,3)), unitPrice (> 0, decimal(18,2)) }`.
  - **Domain Rules Enforced:**
    - Evaluates Customer Grade (`AllowCredit`), Credit Limit, and Overdue Debt → flags `ExceededCreditWarning` & `RequiresFinanceCoApproval` (`MR-03`).
    - Resolves `StandardTierPrice` from Customer's PriceList and checks `MinAllowedSellingPrice` / `MinAllowedPrice` → flags `PriceOverridePending` & `HasPriceOverridePending` (`MR-02`).
    - Evaluates `WarehouseStocks.AvailableQty` → reserves available stock with 24h `ReservationExpiresAtUtc` (`RISK-01`); if `AvailableQty < OrderedQty`, NEVER drops order — flags `HasStockShortage = true` (`RISK-03`).
    - Computes 15% VAT (`decimal(18,2)`, `MidpointRounding.AwayFromZero`).
    - Writes `[ops].[OutboxMessages]` event `OrderSubmitted` (and `CreditCoApprovalRequired` if flagged).
- `PUT /api/v1/orders/{id}/manager-edit` — Policy: **`orders:edit_submitted`** (`SALES_MGR` & `SUPER_ADMIN` ONLY — `SALES_REP` blocked at API AND Domain!)
  - Only allowed when `Status ∈ {PendingApproval, Approved}` (pre-shipment).
  - `items`: Required non-empty array of `{ salesOrderItemId, approvedQty (> 0), unitPrice (> 0) }`.
  - Reconciles `WarehouseStocks.ReservedQty` delta, recalculates totals/VAT, updates `EditedByManagerId`/`EditedAtUtc`, records audit snapshot in `[ops].[AuditLogs]`.
- `POST /api/v1/orders/{id}/override-price` — Policy: `orders:override_price` | Resolves `PriceOverridePending` on all/specified lines and clears `HasPriceOverridePending` (`MR-02`).
- `POST /api/v1/orders/{id}/coapprove-credit` — Policy: `orders:coapprove_credit` (`ACCOUNTANT`/`EXEC_MGR`/`SUPER_ADMIN`) | Sets `FinanceCoApprovedByUserId` & `FinanceCoApprovedAtUtc` (`MR-03`).
- `POST /api/v1/orders/{id}/approve` — Policy: `orders:approve` | Rejects (`422`) if `RequiresFinanceCoApproval && FinanceCoApprovedByUserId == null`, or if `HasPriceOverridePending`, or if `HasStockShortage`. Transitions `PendingApproval -> Approved` and emits `OrderApproved` outbox message!
- `POST /api/v1/orders/{id}/reject` — Policy: `orders:approve` | `{ rejectionReason: Required MaxLength(500) }`. Releases `ReservedQty`, transitions `PendingApproval -> Rejected`, emits `OrderRejected` outbox message!
- `POST /api/v1/orders/{id}/confirm-shipment` — Policy: `warehouse:ship` | **Requires `X-Idempotency-Key` header!**
  - `egsDeviceId`: Required, `MaxLength(64)` (must exist in `[sales].[ZatcaDeviceSequences]` or initialized via `POST /api/v1/invoices/egs-devices`)
  - `invoiceType`: Optional (default `"StandardB2B"`), in `{"StandardB2B", "SimplifiedB2C"}`
  - `items`: Optional array of `{ salesOrderItemId, shippedQty (> 0, <= approvedQty) }` (defaults to `ApprovedQty`).
  - Deducts `PhysicalQty` & `ReservedQty`, appends `SalesShipment` to `[inv].[StockMovements]`, locks `[sales].[ZatcaDeviceSequences]` (ICV + 1, PIH chain — `RISK-02`), generates signed ZATCA Phase 2 `Invoice` with SHA-256 XML hash & TLV Base64 QR, debits `Customer.CurrentBalance`, and transitions order to `ShippedAndInvoiced`!
- `POST /api/v1/invoices/egs-devices` — Policy: `settings:manage` | Registers a ZATCA EGS device sequence (`egsDeviceId`, `warehouseId?`, `certificateSerial`, initial PIH = Base64(SHA256("0"))).
- `GET /api/v1/invoices/{id}/print` — Policy: `invoices:view` | Returns invoice print payload (header, lines, ZATCA UUID, ICV, XML hash, TLV Base64 QR, customer VAT, payment status).
- `POST /api/v1/invoices/{id}/upload-copy` — Policy: `invoices:upload_copy` | Validates `{ uploadedInvoiceCopyUrl (MaxLength(500), valid URL/storage path) }` and stores signed invoice copy reference.
- `POST /api/v1/returns` — Policy: `sales:returns` | Creates ZATCA Credit Note (`381`) in `[sales].[SalesReturns]` + `[sales].[SalesReturnItems]` (`originalInvoiceId`, `warehouseId`, `returnReason (MaxLength(500))`, `egsDeviceId`, `items: [{ masterProductId, productUnitId, returnedQty (> 0), isSellableRestock, unitPrice (> 0) }]`), restocks sellable items (`SalesReturn` movement), and credits `Customer.CurrentBalance`.
- `POST /api/v1/delivery/dispatches` — Policy: `delivery:dispatch` | Creates delivery manifest (`warehouseId`, `driverOrRepId`, `vehiclePlateNumber? (MaxLength(30))`, `invoiceIds: Guid[]` non-empty unique).
- `PATCH /api/v1/delivery/dispatches/{id}/items/{invoiceId}` — Policy: `delivery:update_pod` | Updates delivery status (`deliveryStatus` in `{"PreparedAtWarehouse","OutForDelivery","Delivered","FailedAttempt"}`, `signedPodImageUrl? (MaxLength(500))`, `deliveryNotes? (MaxLength(500))`).

### Module 7: Collections, Treasury, Bank Verification & Fiscal Periods (`/api/v1/collections`, `/api/v1/finance`)
- `GET /api/v1/finance/accounts` — Policy: `dashboards:finance` | Lists financial accounts (`MainTreasurySafe`, `BankAccount`, `RepCustodyWallet`, etc.).
- `POST /api/v1/finance/accounts` — Policy: `settings:manage` (or `finance:close_period`)
  - `accountCode`: Required, `MaxLength(30)`, unique
  - `nameAr`: Required, `MaxLength(150)`
  - `nameEn`: Optional, `MaxLength(150)`
  - `accountType`: Required, in `{"MainTreasurySafe","BankAccount","RepCustodyWallet","AccountsReceivable","AccountsPayable","SalesRevenue","VatOutput","InventoryAsset","COGS"}`
  - `ownerRepId`: Optional (unique when set)
  - `bankName`: Optional, `MaxLength(100)`
  - `iban`: Optional, `MaxLength(50)`
- `GET /api/v1/finance/fiscal-periods` — Policy: `dashboards:finance` | Lists fiscal periods.
- `POST /api/v1/finance/fiscal-periods` — Policy: `finance:close_period` | Creates an open fiscal period (`fiscalYear (2020..2100)`, `fiscalMonth (1..12)`, unique `(fiscalYear, fiscalMonth)`).
- `POST /api/v1/finance/fiscal-periods/{id}/close` — Policy: `finance:close_period` | Locks period (`IsClosed = true`, `ClosedByUserId`, `ClosedAtUtc`) (`MR-05`).
- `POST /api/v1/collections/cash` — Policy: `collections:create` | **Requires `X-Idempotency-Key`!**
  - `customerId`: Required, exists (ABAC for `SALES_REP`)
  - `amount`: `> 0`, `decimal(18,2)`
  - `allocations`: Optional array of `{ invoiceId (exists & belongs to customer), allocatedAmount (> 0) }` with `SUM(allocatedAmount) <= amount`.
  - Records `InRepCustody` collection, updates `Customer.CurrentBalance` & `RepCustodyWallet.CurrentBalance`, updates invoice `PaidAmount`/`PaymentStatus`, and posts a balanced double-entry `[fin].[JournalEntries]` + `[fin].[JournalLines]` into an **open** `[fin].[FiscalPeriods]` (rejects `422` if no open fiscal period exists!).
- `POST /api/v1/collections/{id}/customer-confirm` — Policy: `collections:create` | Marks `IsCustomerConfirmed = true`, `CustomerConfirmedAtUtc = UtcNow`.
- `POST /api/v1/collections/treasury-handover` — Policy: `finance:confirm_treasury` (`ACCOUNTANT` / `SUPER_ADMIN`)
  - `collectionId`: Required, must be `Cash` in `InRepCustody`
  - `targetTreasuryAccountId`: Required, must be `MainTreasurySafe`
  - Moves funds from `RepCustodyWallet` → `MainTreasurySafe`, transitions status to `DepositedToTreasury`, posts balanced double-entry journal in open fiscal period, emits `FinancialKpiUpdated` outbox message!
- `POST /api/v1/collections/bank-transfer` — Policy: `collections:create` | **Requires `X-Idempotency-Key`!**
  - `customerId`: Required, exists
  - `amount`: `> 0`, `decimal(18,2)`
  - `bankReferenceNumber`: Required, `MaxLength(100)`
  - `paymentProofImageUrl`: Required, `MaxLength(500)`
  - `paymentProofImageHash`: Required, exact 64 lowercase hex chars (`^[a-f0-9]{64}$`), unique among non-rejected collections (`UX_Collections_ProofHash` → `409 Conflict` on duplicate receipt!)
  - `originalValueDateUtc`: Optional (default `UtcNow`)
  - `allocations`: Optional array.
  - Records `PendingBankVerification` collection (does NOT credit customer until Accountant verifies!).
- `POST /api/v1/collections/bank-transfer/{id}/verify` — Policy: `finance:verify_bank` (`ACCOUNTANT` / `SUPER_ADMIN`)
  - `targetBankAccountId`: Required, must be `BankAccount`
  - `approve`: bool (if `false`, marks `Rejected` without posting journal; if `true`, verifies open fiscal period, posts balanced double-entry journal preserving `OriginalValueDateUtc` (`MR-05`), credits `Customer.CurrentBalance`, increments bank account balance, updates invoice allocations, marks `VerifiedInBank`).
- `GET /api/v1/finance/journal-entries` — Policy: `dashboards:finance` | Paged strictly append-only journal entries + balanced lines.

### Module 8: Dashboards, Audit Logs & GPS Telemetry (`/api/v1/dashboards`, `/api/v1/audit-logs`, `/api/v1/gps`)
- `GET /api/v1/dashboards/executive` — Policy: `dashboards:executive` | Returns live KPI summary (`totalMainSafeCash`, `totalRepCustodyCash`, `totalBankBalance`, `totalReceivables`, `totalPayables`, `totalInventoryValuation`, `pendingOrdersCount`, `lowStockItemsCount`, `snapshotDate`).
- `GET /api/v1/dashboards/{roleView}` — Role-specific policy (`sales` → `dashboards:sales`, `inventory` → `dashboards:inventory`, `finance` → `dashboards:finance`, `warehouse` → `dashboards:inventory`, `rep` → `dashboards:sales`; rejects unknown `roleView` with `400`).
- `GET /api/v1/audit-logs` — Policy: `audit:view` | Keyset-paginated `[ops].[AuditLogs]` ordered by `(TimestampUtc DESC, Id DESC)`. Query: `entityName?`, `userId?`, `cursor?`, `pageSize (<= 250)`.
- `POST /api/v1/gps/pings` — Policy: `gps:stream` | Batched GPS pings (`pings: [{ latitude (-90..90), longitude (-180..180), accuracyMeters? (>= 0), nearCustomerId?, recordedAtUtc }]`, `1..100` items per batch) persisted to `[ops].[GpsRouteHistory]` under `CurrentUser.UserId`.

---

## 5. Security, Domain & Operational Rules

1. **Explicit Authorization Everywhere:** Every non-auth controller/action MUST declare `[Authorize(Policy = "...")]` matching `07-Security.md`. Only `/health` and `/api/v1/auth/{login,refresh,logout}` carry `[AllowAnonymous]`.
2. **No Hardcoded Secrets or Prod Bypass:** `appsettings.json` keeps empty `SqlServer`, `Redis`, `SigningKey`, and `AllowedOrigins`. Development-only admin bootstrap and Development-only in-memory Redis fallback MUST be gated strictly behind `builder.Environment.IsDevelopment()`.
3. **Append-Only Enforcement:** Never issue `UPDATE` or `DELETE` against `[inv].[StockMovements]`, `[fin].[JournalEntries]`, `[fin].[JournalLines]`, or `[ops].[AuditLogs]`.
4. **Optimistic Concurrency & Idempotency:** Catch `DbUpdateConcurrencyException` globally and return RFC 7807 `409 Conflict`. Enforce `X-Idempotency-Key` deduplication on `POST /orders`, `POST /orders/{id}/confirm-shipment`, `POST /collections/cash`, and `POST /collections/bank-transfer`.
5. **Audit Trail & Outbox Atomicity:** Record before/after JSON snapshots into `[ops].[AuditLogs]` and domain notifications into `[ops].[OutboxMessages]` inside the same ACID database transaction as business writes.
6. **Zero Frontend / UI Scope:** Do not touch `apps/web-erp`, `apps/design-system-studio`, `packages/ui-kit`, or `packages/api-client`.

---

## 6. Acceptance Criteria (Definition of Done)

- [x] All 8 API modules and all documented endpoints are implemented with thin Controllers, MediatR Commands/Queries, `.AsNoTracking()` projections, and `{ items, totalCount, nextCursor }` pagination (`pageSize <= 250`).
- [x] Every command/request has a colocated FluentValidation validator enforcing every column's required/length/range/precision/enum/format rules and mapping failures to RFC 7807 `400 ValidationProblemDetails`.
- [x] Every endpoint enforces explicit `[Authorize(Policy = "...")]` policies; `SALES_REP` is blocked on `PUT /api/v1/orders/{id}/manager-edit` at API & Domain layers; ABAC scoping isolates `SALES_REP` and `WH_KEEPER` queries.
- [x] Core ERP business invariants are enforced and tested: `MR-01` CashVan/MobileVan, `MR-02` Price Tiers & Floor Override, `MR-03` Two-Tier Credit Co-Approval, `MR-04` Delivery Manifest & POD, `MR-05` Fiscal Period Lock, `RISK-01` 24h Reservation TTL, `RISK-02` ZATCA ICV/PIH Sequence Lock, `RISK-03` Offline Stock Shortage flagging, balanced double-entry journals, and SHA-256 bank proof deduplication.
- [x] Development-only env-var `SUPER_ADMIN` bootstrap and Development-only in-memory Redis fallback work locally while Production remains strictly fail-closed with zero committed secrets.
- [x] Downloadable Postman Collection v2.1 (`docs/api/AlMomiaa-ERP.postman_collection.json`), Local Environment (`docs/api/AlMomiaa-Local.postman_environment.json`), and Backend Runbook & Troubleshooting Guide (`docs/api/BACKEND_API_GUIDE.md`) are generated, valid JSON, categorized into folders, and cover 100% of API requests.
- [x] `dotnet build AlMomiaa.Erp.sln` passes with **0 warnings and 0 errors**; `dotnet test` passes all architecture, auth, schema, RBAC, validation, and end-to-end ERP workflow tests.
- [x] `docs/architecture/06-API-Specification.md` stays synchronized with any added helper routes (e.g., `POST /api/v1/invoices/egs-devices`, `GET/POST /api/v1/finance/accounts`, `GET/POST /api/v1/finance/fiscal-periods`).
- [x] `@QA & Security Auditor` returns `PASS`, after which `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0005-full-backend-api-and-postman-suite.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks `CURRENT_PROMPT.md` completed.
