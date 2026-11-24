# PROMPT-0010: Multi-Warehouse Product Stock Allocation, Detailed Per-Unit Pricing & Packaging UX, Multi-Warehouse Availability Breakdown, and Sales Rep-to-Customer Linking

- **Status:** `COMPLETED — ARCHIVED (docs/agent-ops/prompt-history/0010-product-multi-warehouse-pricing-and-rep-customer-linking.md)`
- **Prompt ID:** `PROMPT-0010`
- **Scope:** `apps/backend-api` (Catalog multi-warehouse opening stock & per-warehouse availability breakdown, Customer rep-assignment & sales-rep lookup endpoints + tests; zero SQL migration needed), `packages/api-client` (typed multi-warehouse product creation, availability breakdown, and rep-customer assignment APIs), `apps/web-erp` (`/catalog` enhanced product creation & availability breakdown + `/customers` rep dropdown & bulk rep-customer linking), `docs/architecture/06-API-Specification.md`
- **Reference baseline:** Architecture v3.2 (`00-ADR`..`08-Development-Roadmap`); `PROMPT-0001`..`PROMPT-0009` completed (live `.NET 9` API + SignalR `/hubs/operations` on `:5000`, 45-table SQL Server `AlMomiaa`, 26/26 backend tests passing, 27 `@almomiaa/ui` components + 27 Storybook 8 stories, `@almomiaa/api-client` with 8/8 tests, `apps/web-erp` 11 routes on `:3000`, `apps/rep-pwa` on `:5173`)
- **Confirmed User Decisions:**
  1. **Order of Execution:** Deliver Product Multi-Warehouse Stock, Detailed Unit Pricing, Availability Breakdown & Rep-Customer Linking FIRST (`PROMPT-0010`), followed by Free HTTPS Cloud Demo Deployment (`PROMPT-0011`: Vercel `web-erp`/`rep-pwa` + Render/Koyeb free `.NET 9` Docker backend + demo DB).
  2. **Detailed Per-Unit Pricing & Multi-Unit Packaging:** Dynamic Base Unit selector (`Piece / حبة`, `Box / علبة`, `Carton / كرتون`, `Pallet / طبلية`) that clearly binds and explains `WAC`, `Min Allowed Floor`, `Wholesale Price`, and `Retail Price` per selected base unit (with 15% VAT preview & Arabic/English guidance) PLUS an optional **Additional Packaging Units** builder (e.g., `Carton = 24 Pieces`, `Pallet = 40 Cartons`) with auto-suggested and editable unit prices.
  3. **Multi-Warehouse Product Allocation & Breakdown:** When creating a product, support selecting all warehouses (or any subset) and setting distinct opening stock quantities per warehouse (e.g., `100` in Warehouse 1, `40` in Warehouse 2). In **Price & Availability Lookup (`استعلام السعر والتوفر`)**, display **Total Available Qty across ALL warehouses** PLUS a full **per-warehouse stock breakdown** (`Physical`, `Reserved`, `Incoming`, `Available` for every warehouse).
  4. **Rep-to-Customer Linking (`ربط مندوب بعميل أو أكثر`):** 1-to-Many model (`Customer.AssignedRepId` — zero DB migration needed): replace raw Rep GUID inputs with a live Sales Rep dropdown on Create Customer, add a per-customer "Change Rep" action, and add a bulk **"Assign Rep to 1 or More Customers"** drawer & atomic API endpoint.

---

## 1. Purpose of Prompt

Eliminate pricing ambiguity, enable realistic multi-warehouse product distribution from day one, provide complete cross-warehouse stock visibility, and make linking field sales representatives (`SALES_REP`) to one or more customers fast and error-free:
1. **Unambiguous Product Pricing & Multi-Unit Packaging (`/catalog`):** Users immediately see whether each price (`WAC`, `Min Allowed`, `Wholesale`, `Retail`) is for a **Piece (`حبة`)**, **Box (`علبة`)**, **Carton (`كرتون`)**, or **Pallet (`طبلية`)**, with clear Arabic/English explanations, VAT-inclusive previews, and optional multi-unit conversion packaging lines.
2. **Multi-Warehouse Opening Stock Allocation (`POST /api/v1/catalog/products` + `/catalog`):** Users can check **"Select All Warehouses"** or pick specific warehouses and set exact initial stock quantities per warehouse (e.g. 50 in WH-01, 20 in WH-02), persisted atomically into `[inv].[WarehouseStocks]` and immutable `[inv].[StockMovements]` (`MovementType = "OpeningBalance"`).
3. **Cross-Warehouse Availability Breakdown (`GET /api/v1/catalog/categories/{categoryId}/products` & `GET /api/v1/catalog/products/{id}/availability` + `/catalog`):** Show both the **grand total available across all warehouses** and the **exact stock inside each warehouse**.
4. **Rep-to-Customer Linking (`GET /api/v1/customers/sales-reps`, `PUT /api/v1/customers/{id}/assign-rep`, `POST /api/v1/customers/assign-rep/bulk` + `/customers`):** Enable `SUPER_ADMIN` and `SALES_MGR` to assign a `SALES_REP` to one or multiple customers using human-readable rep names/codes (no raw GUIDs).

---

## 2. Target Execution Agents and Order

1. **`@Backend .NET`** —
   - **A. Domain (`AlMomiaa.Domain`):**
     - Add `AssignRepresentative(Guid repId)` to `Customer.cs` (validates `repId != Guid.Empty`).
   - **B. Catalog Multi-Warehouse Opening Stock & Availability Breakdown (`AlMomiaa.Application/Catalog/` & `CatalogAndInventoryServices.cs` & `CatalogController.cs`):**
     - Add `InitialWarehouseStockRequest(int WarehouseId, decimal InitialQty, decimal MinReorderQty = 0m)` and optional `InitialWarehouseStockRequest[]? InitialWarehouseStocks = null` on `CreateMasterProductRequest`.
     - Extend `CreateMasterProductCommandValidator` to validate `InitialWarehouseStocks`: each `WarehouseId > 0`, `InitialQty >= 0`, `MinReorderQty >= 0`, and no duplicate `WarehouseId` values.
     - In `CatalogModuleService.CreateMasterProductAsync`: when `InitialWarehouseStocks` is provided, verify all referenced warehouses exist (`NotFoundException` otherwise), create `WarehouseStock` rows (`WarehouseStock.Create(whId, product.Id, entry.MinReorderQty)`), call `stock.ReceiveGoods(entry.InitialQty, 0m)` when `entry.InitialQty > 0`, and append immutable `StockMovement.Append(whId, product.Id, "OpeningBalance", entry.InitialQty, stock.PhysicalQty, product.WeightedAvgCost, "MasterProduct", product.Id, currentUser.UserId, utcNow)` inside the same transaction!
     - Add `ProductWarehouseStockBreakdownDto(int WarehouseId, string WarehouseCode, string WarehouseNameAr, string? WarehouseNameEn, string WarehouseType, decimal PhysicalQty, decimal ReservedQty, decimal IncomingQty, decimal AvailableQty, decimal MinReorderQty)`.
     - Enrich `CategoryProductPriceDto` with:
       - `BaseUnit` (`string`)
       - `DefaultUnitNameAr` (`string`), `DefaultUnitNameEn` (`string`)
       - `TotalPhysicalQty` (`decimal`), `TotalAvailableQty` (`decimal`)
       - `WarehouseStocks` (`IReadOnlyList<ProductWarehouseStockBreakdownDto>`)
       - Keep `AvailableQty` backward-compatible: equals the filtered `warehouseId`'s `AvailableQty` when `warehouseId` is specified, or `TotalAvailableQty` when `warehouseId` is `null` (All Warehouses)!
     - Add `ProductAvailabilityDto(Guid MasterProductId, string SKU, string NameAr, string? NameEn, string BaseUnit, decimal WholesaleSellingPrice, decimal RetailSellingPrice, decimal MinAllowedSellingPrice, decimal VatRate, decimal TotalPhysicalQty, decimal TotalReservedQty, decimal TotalIncomingQty, decimal TotalAvailableQty, IReadOnlyList<ProductUnitDto> Units, IReadOnlyList<ProductWarehouseStockBreakdownDto> WarehouseStocks)` and `GetProductAvailabilityQuery(Guid MasterProductId)` exposed at `[Authorize(Policy = "catalog:view")] GET /api/v1/catalog/products/{id:guid}/availability`.
   - **C. Sales Rep-to-Customer Assignment (`AlMomiaa.Application/Customers/` & `IamAndCustomersServices.cs` & `CustomersController.cs`):**
     - Enrich `CustomerDto` with `string? AssignedRepCode = null`, `string? AssignedRepNameAr = null`, `string? AssignedRepNameEn = null` (populated via `AssignedRep` navigation in `GetCustomersAsync` and `CreateCustomerAsync`).
     - Add `SalesRepOptionDto(Guid Id, string EmployeeCode, string FullNameAr, string? FullNameEn, string Phone, string? RepType, int AssignedCustomersCount)` and `GetSalesRepsQuery` exposed at `[Authorize(Policy = "customers:view")] GET /api/v1/customers/sales-reps` (returns active users holding `SALES_REP` role or `orders:create`, ordered by `FullNameAr`).
     - Add `AssignCustomerRepRequest(Guid AssignedRepId)` + `AssignCustomerRepCommand(Guid CustomerId, Guid AssignedRepId)` exposed at `[Authorize(Policy = "customers:manage")] PUT /api/v1/customers/{id:guid}/assign-rep` (verifies target rep exists and is active, updates `Customer.AssignedRepId`, appends `[ops].[AuditLogs]` with old/new rep IDs, returns updated `CustomerDto`).
     - Add `BulkAssignCustomersRepRequest(Guid AssignedRepId, Guid[] CustomerIds)` + `BulkAssignCustomersRepCommand` + `BulkAssignCustomersRepResponse(Guid AssignedRepId, string AssignedRepCode, string AssignedRepNameAr, int UpdatedCount, IReadOnlyList<Guid> CustomerIds)` exposed at `[Authorize(Policy = "customers:manage")] POST /api/v1/customers/assign-rep/bulk` (validates `CustomerIds` non-empty & max 200, verifies rep exists and is active, verifies all requested customers exist, updates all in one ACID transaction, appends `[ops].[AuditLogs]`).
   - **D. Backend Tests (`CatalogWarehouseAndRepCustomerTests.cs`):**
     - Creating a product with `InitialWarehouseStocks` across 2 warehouses creates `WarehouseStock` rows + `"OpeningBalance"` `StockMovements`, and `GET /catalog/products/{id}/availability` & `GET /catalog/categories/{catId}/products` return accurate `TotalAvailableQty` (`qty1 + qty2`) AND exact per-warehouse breakdown!
     - `GET /customers/sales-reps` lists active `SALES_REP` accounts with assigned customer counts.
     - `PUT /customers/{id}/assign-rep` and `POST /customers/assign-rep/bulk` reassign 1 or multiple customers to a `SALES_REP`, and the assigned `SALES_REP` immediately sees ONLY those assigned customers on `GET /api/v1/customers` (ABAC verified!).
   - Sync `docs/architecture/06-API-Specification.md`.
2. **`@Frontend Engineer`** —
   - **A. `@almomiaa/api-client` Updates:**
     - Extend `CreateMasterProductRequest` (`initialWarehouseStocks`), `CategoryProductPriceDto` (`baseUnit`, `defaultUnitNameAr`, `defaultUnitNameEn`, `totalPhysicalQty`, `totalAvailableQty`, `warehouseStocks`), and add `ProductWarehouseStockBreakdownDto`, `ProductAvailabilityDto`, and `getProductAvailability(productId)` in `src/modules/catalog.service.ts`.
     - Extend `CustomerDto` (`assignedRepCode`, `assignedRepNameAr`, `assignedRepNameEn`), add `SalesRepOptionDto`, `BulkAssignCustomersRepResponse`, `getSalesReps()`, `assignCustomerRep(customerId, assignedRepId)`, and `bulkAssignCustomersRep(assignedRepId, customerIds)` in `src/modules/customers.service.ts`.
     - Export all new types from `src/index.ts` and add unit tests in `tests/erp-client.test.mjs`.
   - **B. `apps/web-erp` `/catalog` (`CatalogWorkspace.tsx`) Overhaul:**
     - **Create Product Drawer:**
       - Add explicit **Base Unit selector** (`حبة / Piece`, `علبة / Box`, `كرتون / Carton`, `طبلية / Pallet`) and dynamically label every price input with the chosen base unit + rich Arabic/English helper text + live 15% VAT-inclusive preview:
         - `تكلفة الشراء المتوسطة (WAC) لكل {unit}` — average purchase cost per 1 base unit (excl. VAT)
         - `الحد الأدنى المسموح للبيع لكل {unit} (MR-02)` — minimum floor price per 1 base unit before manager override is required
         - `سعر الجملة لكل {unit}` — default wholesale price per 1 base unit (excl. VAT)
         - `سعر التجزئة لكل {unit}` — default retail price per 1 base unit (excl. VAT)
       - Add **Additional Packaging Units (`وحدات التعبئة الإضافية`)** section: e.g. `+ إضافة وحدة (كرتون / طبلية / علبة)` with Unit Name (AR/EN), Conversion Factor (`كم {baseUnit} داخل الوحدة؟`), Barcode, and auto-computed editable Wholesale / Retail / Min Floor prices!
       - Add **Warehouse Stock Allocation (`توزيع المخزون على المستودعات`)** section:
         - Load all active warehouses via `erpApi.inventory.getWarehouses()`.
         - Include **"Select All Warehouses (`تحديد الكل`)"** and **"Clear (`إلغاء التحديد`)"** buttons.
         - For every warehouse, render a checkbox + warehouse name/code + **Initial Quantity (`الكمية الابتدائية بالوحدة الأساسية`)** + **Min Reorder (`حد إعادة الطلب`)** inputs — allowing different quantities per warehouse (e.g., 100 in WH-01, 40 in WH-02, 0 in WH-03)!
     - **Master Products Table:**
       - Clearly display the product's **Base Unit** badge (`حبة` / `كرتون` / `طبلية`), all packaging units count, and a **"Stock Breakdown (`توزيع المستودعات`)"** button opening the multi-warehouse availability drawer (`GET /catalog/products/{id}/availability`).
     - **Price & Availability Lookup Tab (`استعلام السعر والتوفر`):**
       - Replace the raw numeric Warehouse ID input with a real **Warehouse Dropdown** including **"All Warehouses (`جميع المستودعات`)"** as default!
       - Display columns: `SKU`, `Product & Base Unit`, `Resolved Price (SAR)`, `Min Floor (MR-02)`, **`Total Available (All Warehouses)`** (`الكمية الكلية المتاحة`), **`Per-Warehouse Breakdown`** (compact badges showing exact available quantity in each warehouse, e.g., `WH-01: 100`, `WH-02: 40`), and a **"Full Breakdown"** drawer button showing `Physical`, `Reserved`, `Incoming`, `Available`, `Min Reorder` per warehouse + all packaging unit prices!
   - **C. `apps/web-erp` `/customers` (`CustomersWorkspace.tsx`) Overhaul:**
     - Load active sales reps via `erpApi.customers.getSalesReps()`.
     - Replace raw `Assigned Rep GUID` input in **Create Customer Drawer** with a clear **Sales Rep Select Dropdown** (`EmployeeCode — Name (current customers count)`).
     - Show the assigned **Sales Rep Name & Code** in the Customers Table (no raw GUID!) plus a **"Change Rep (`تغيير المندوب`)"** button per row.
     - Add a header button **"Assign Rep to Customers (`ربط مندوب بعملاء`)"** opening a dedicated bulk linking drawer:
       - Select **1 Sales Rep** from dropdown.
       - Search & check **1 or more customers** (with "Select All Shown" / "Clear Selection" toggles and live selected count badge).
       - Submit `POST /api/v1/customers/assign-rep/bulk` and refresh both the Customers table and Sales Rep customer counts!
3. **`@QA & Security Auditor`** —
   - Verify `dotnet build` (0 warnings, 0 errors), `dotnet test` (all 26 existing + new `CatalogWarehouseAndRepCustomerTests` passing), `pnpm -r typecheck`, `pnpm -r lint`, `@almomiaa/api-client` tests, `apps/rep-pwa` build (ensuring enriched `CategoryProductPriceDto` remains 100% backward compatible in `rep-pwa`!), and `apps/web-erp` Next.js 15 production build.
   - Verify static rules: zero hardcoded hex colors, zero directional Tailwind classes (`ml-`/`mr-`/`pl-`/`pr-`/`left-`/`right-`/`text-left`/`text-right`), zero `any`/`@ts-ignore`, cost masking (`WeightedAvgCost == null` for `SALES_REP`) still intact, and ABAC row scoping (`SALES_REP` sees only newly assigned customers) verified!
4. **`@Orchestrator`** — Archive as `docs/agent-ops/prompt-history/0010-product-multi-warehouse-pricing-and-rep-customer-linking.md`, update `docs/agent-ops/PROJECT_LOG.md`, and mark `CURRENT_PROMPT.md` completed only after QA returns `PASS`.

---

## 3. Target Files to Create / Modify

### `apps/backend-api/`
- `src/AlMomiaa.Domain/Customers/Customer.cs` (modify — add `AssignRepresentative(Guid repId)`)
- `src/AlMomiaa.Application/Catalog/CatalogModule.cs` (modify — add `InitialWarehouseStockRequest`, `ProductWarehouseStockBreakdownDto`, `ProductAvailabilityDto`, `GetProductAvailabilityQuery`, extend `CreateMasterProductRequest` & `CategoryProductPriceDto`, validators & handlers)
- `src/AlMomiaa.Application/Customers/CustomersModule.cs` (modify — add `SalesRepOptionDto`, `GetSalesRepsQuery`, `AssignCustomerRepRequest`/`Command`, `BulkAssignCustomersRepRequest`/`Command`/`Response`, enrich `CustomerDto`, validators & handlers)
- `src/AlMomiaa.Application/Common/Abstractions/ModuleServices.cs` (modify — add new methods on `ICatalogModuleService` & `ICustomersModuleService`)
- `src/AlMomiaa.Infrastructure/Persistence/Services/CatalogAndInventoryServices.cs` (modify — allocate initial stocks + `"OpeningBalance"` `StockMovements` on `CreateMasterProductAsync`, return multi-warehouse breakdown in `GetCategoryProductsAsync` & `GetProductAvailabilityAsync`)
- `src/AlMomiaa.Infrastructure/Persistence/Services/IamAndCustomersServices.cs` (modify — include rep names on `CustomerDto`, implement `GetSalesRepsAsync`, `AssignCustomerRepAsync`, `BulkAssignCustomersRepAsync`)
- `src/AlMomiaa.WebApi/Controllers/CatalogController.cs` (modify — add `GET /api/v1/catalog/products/{id:guid}/availability`)
- `src/AlMomiaa.WebApi/Controllers/CustomersController.cs` (modify — add `GET /api/v1/customers/sales-reps`, `PUT /api/v1/customers/{id:guid}/assign-rep`, `POST /api/v1/customers/assign-rep/bulk`)
- `tests/AlMomiaa.AuthenticationTests/CatalogWarehouseAndRepCustomerTests.cs` (new)

### `packages/api-client/`
- `src/modules/catalog.service.ts` (modify — multi-warehouse stock requests/breakdowns + `getProductAvailability`)
- `src/modules/customers.service.ts` (modify — rep metadata + `getSalesReps`, `assignCustomerRep`, `bulkAssignCustomersRep`)
- `src/index.ts` & `tests/erp-client.test.mjs` (modify)

### `apps/web-erp/`
- `src/features/catalog/CatalogWorkspace.tsx` (modify — dynamic Base Unit + detailed Arabic/English per-unit price labels & hints + VAT preview + Additional Packaging Units builder + multi-warehouse Select All / per-warehouse opening stock inputs + cross-warehouse Total & Per-Warehouse Availability Lookup + Breakdown Drawer)
- `src/features/customers/CustomersWorkspace.tsx` (modify — live Sales Rep dropdown on Create Customer, assigned rep name display, single "Change Rep" drawer, and bulk "Assign Rep to 1+ Customers" drawer)

### `docs/architecture/`
- `06-API-Specification.md` (sync new Catalog availability & Customer rep-assignment endpoints)

---

## 4. Frontend & Backend Data Schema

### Product Creation with Multi-Warehouse Initial Stock (`POST /api/v1/catalog/products`)
```ts
interface InitialWarehouseStockRequest {
  warehouseId: number;     // > 0, unique in array
  initialQty: number;      // >= 0 (in BaseUnit)
  minReorderQty?: number;  // >= 0 (default 0)
}
// Added as optional field `initialWarehouseStocks?: InitialWarehouseStockRequest[]`
// on CreateMasterProductRequest alongside `baseUnit`, `weightedAvgCost`, `minAllowedSellingPrice`,
// `wholesaleSellingPrice`, `retailSellingPrice`, and `units[]`.
```

### Multi-Warehouse Stock Breakdown (`GET /api/v1/catalog/products/{id}/availability` & `CategoryProductPriceDto`)
```ts
interface ProductWarehouseStockBreakdownDto {
  warehouseId: number;
  warehouseCode: string;
  warehouseNameAr: string;
  warehouseNameEn: string | null;
  warehouseType: string;
  physicalQty: number;
  reservedQty: number;
  incomingQty: number;
  availableQty: number;
  minReorderQty: number;
}
```

### Bulk Rep-to-Customers Linking (`POST /api/v1/customers/assign-rep/bulk`)
```ts
interface BulkAssignCustomersRepRequest {
  assignedRepId: string; // Active user UUID (SALES_REP)
  customerIds: string[]; // 1..200 Customer UUIDs
}
```

---

## 5. Security & Domain Rules

1. **Cost Privacy Preserved (`07-Security.md`):** `WeightedAvgCost` remains masked (`null`) for `SALES_REP`, `SALES_MGR`, and `WH_KEEPER` across catalog read endpoints; `ProductAvailabilityDto` does NOT expose `WeightedAvgCost`.
2. **Inventory Auditability:** Every non-zero initial warehouse stock allocated during product creation appends an immutable `[inv].[StockMovements]` row (`MovementType = "OpeningBalance"`) and `[ops].[AuditLogs]` record inside the same ACID transaction.
3. **Rep Assignment Authorization & ABAC:** Only `customers:manage` (`SUPER_ADMIN`, `SALES_MGR`, `ACCOUNTANT`) may assign/reassign reps (`PUT /customers/{id}/assign-rep`, `POST /customers/assign-rep/bulk`). Target `assignedRepId` must reference an existing, active employee. After assignment, `SALES_REP` ABAC scoping (`Customer.AssignedRepId == CurrentUser.Id`) automatically reflects the updated customer portfolio.
4. **Strict Design System & RTL/LTR Hygiene:** Zero hardcoded hex colors in TSX/TS, zero directional Tailwind classes (`ml-`/`mr-`/`pl-`/`pr-`/`left-`/`right-`/`text-left`/`text-right`), `tabular-nums` on all prices & quantities, `font-mono dir="ltr"` on SKUs/codes, zero `any`/`@ts-ignore`.

---

## 6. Acceptance Criteria (Definition of Done)

- [ ] `POST /api/v1/catalog/products` supports optional `initialWarehouseStocks[]` (allocating different opening stock quantities across selected/all warehouses in `[inv].[WarehouseStocks]` and logging immutable `"OpeningBalance"` `[inv].[StockMovements]`).
- [ ] `GET /api/v1/catalog/categories/{categoryId}/products` and `GET /api/v1/catalog/products/{id}/availability` return both **total available stock across all warehouses** AND an exact **per-warehouse breakdown** (`Physical`, `Reserved`, `Incoming`, `Available`).
- [ ] `apps/web-erp` `/catalog` Create Product Drawer provides: dynamic Base Unit selector (`Piece`/`Box`/`Carton`/`Pallet`), crystal-clear Arabic/English per-unit labels + hints + 15% VAT previews for `WAC`, `Min Floor`, `Wholesale`, and `Retail`, optional **Additional Packaging Units** builder, and **Select All Warehouses / Per-Warehouse Initial Qty** allocation.
- [ ] `apps/web-erp` `/catalog` **Price & Availability Lookup** displays **"All Warehouses"** option, total stock across all warehouses, inline per-warehouse stock badges, and a full **Warehouse Stock Breakdown Drawer**.
- [ ] `GET /api/v1/customers/sales-reps`, `PUT /api/v1/customers/{id}/assign-rep`, and `POST /api/v1/customers/assign-rep/bulk` work end-to-end with audit logging, and `apps/web-erp` `/customers` uses a live Sales Rep dropdown on Create Customer, shows Rep names in the table, supports single "Change Rep", and supports bulk **"Assign Rep to 1 or More Customers"**.
- [ ] All backend builds (`0` warnings/errors), backend tests (`26+` including new `CatalogWarehouseAndRepCustomerTests`), `@almomiaa/api-client` tests, `pnpm -r typecheck`, `pnpm -r lint`, `apps/rep-pwa` PWA build, `apps/web-erp` Next.js 15 build, and `@QA & Security Auditor` gate return **`PASS`**, after which `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0010-product-multi-warehouse-pricing-and-rep-customer-linking.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks `CURRENT_PROMPT.md` completed.
