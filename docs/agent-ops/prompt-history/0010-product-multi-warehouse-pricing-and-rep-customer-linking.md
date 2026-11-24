# PROMPT-0010: Multi-Warehouse Product Stock Allocation, Detailed Per-Unit Pricing & Packaging UX, Multi-Warehouse Availability Breakdown, and Sales Rep-to-Customer Linking

- **Status:** `COMPLETED — QA PASS`
- **Prompt ID:** `PROMPT-0010`
- **Archived At:** `2026-11-24`
- **Scope:** `apps/backend-api` (Catalog multi-warehouse opening stock & per-warehouse availability breakdown, Customer rep-assignment & sales-rep lookup endpoints + tests; zero SQL migration needed), `packages/api-client` (typed multi-warehouse product creation, availability breakdown, and rep-customer assignment APIs), `apps/web-erp` (`/catalog` enhanced product creation & availability breakdown + `/customers` rep dropdown & bulk rep-customer linking), `docs/architecture/06-API-Specification.md`
- **Reference baseline:** Architecture v3.2 (`00-ADR`..`08-Development-Roadmap`); `PROMPT-0001`..`PROMPT-0009` completed.

---

## 1. Execution Summary

1. **Backend `.NET 9` (`apps/backend-api`):**
   - Added `Customer.AssignRepresentative(Guid repId)` in `AlMomiaa.Domain/Customers/Customer.cs`.
   - Extended `CreateMasterProductRequest` with optional `InitialWarehouseStocks[]` (validated for uniqueness & non-negative quantities), persisting `[inv].[WarehouseStocks]`, immutable `"OpeningBalance"` `[inv].[StockMovements]`, and `[ops].[AuditLogs]` atomically in `CatalogModuleService.CreateMasterProductAsync`.
   - Added `GET /api/v1/catalog/products/{id:guid}/availability` (`ProductAvailabilityDto`, without `WeightedAvgCost`) and enriched `GET /api/v1/catalog/categories/{categoryId}/products` (`CategoryProductPriceDto`) with `BaseUnit`, `DefaultUnitNameAr/En`, `TotalPhysicalQty`, `TotalAvailableQty`, and per-warehouse `WarehouseStocks[]` while keeping `AvailableQty` 100% backward-compatible for `apps/rep-pwa`.
   - Added `GET /api/v1/customers/sales-reps`, `PUT /api/v1/customers/{id:guid}/assign-rep`, and `POST /api/v1/customers/assign-rep/bulk` with active-rep verification, transactional updates, audit logs, and enriched `CustomerDto` rep metadata (`AssignedRepCode`, `AssignedRepNameAr`, `AssignedRepNameEn`).
   - Added `CatalogWarehouseAndRepCustomerTests.cs` (3 integration tests covering multi-warehouse opening stock + `"OpeningBalance"` movements + cost masking + availability breakdown, single/bulk rep-to-customer linking + ABAC row scoping, and FluentValidation guards).
   - Synchronized `docs/architecture/06-API-Specification.md`.
2. **Typed API Client (`packages/api-client`):**
   - Added `InitialWarehouseStockRequest`, `ProductWarehouseStockBreakdownDto`, `ProductAvailabilityDto`, `SalesRepOptionDto`, `BulkAssignCustomersRepResponse`, `catalog.getProductAvailability`, `customers.getSalesReps`, `customers.assignCustomerRep`, and `customers.bulkAssignCustomersRep` + unit tests (**9/9 passing**).
3. **Web ERP (`apps/web-erp`):**
   - Overhauled `/catalog` (`CatalogWorkspace.tsx`): dynamic Base Unit selector (`Piece / حبة`, `Box / علبة`, `Carton / كرتون`, `Pallet / طبلية`), explicit per-unit Arabic/English pricing labels + hints + 15% VAT previews + margin badge, optional Additional Packaging Units builder, Multi-Warehouse Opening Stock Allocation with **Select All Warehouses / Clear**, upgraded **Price & Availability Lookup** with **"All Warehouses"** scope + Total Stock + inline per-warehouse badges, and full **Cross-Warehouse Stock Breakdown Drawer**.
   - Overhauled `/customers` (`CustomersWorkspace.tsx`): live Sales Rep dropdown on Create Customer, Assigned Sales Rep column + Rep filter, dedicated **Sales Reps** portfolio tab, single **"Change Rep"** drawer, and bulk **"Link Sales Rep to 1 or More Customers"** drawer.

---

## 2. Verification & QA Gate (`PASS`)

- [x] `dotnet build apps\backend-api\AlMomiaa.Erp.sln -c Release`: **0 warnings, 0 errors**.
- [x] `dotnet test apps\backend-api\AlMomiaa.Erp.sln -c Release`: **29/29 passed** (`1/1` ArchitectureTests + `28/28` Authentication & Integration Tests).
- [x] `pnpm.cmd --filter @almomiaa/api-client build` & `test`: **9/9 passed**.
- [x] `pnpm.cmd -r typecheck`: **5/5 packages/apps passed**.
- [x] `pnpm.cmd -r lint`: **5/5 packages/apps passed**.
- [x] `pnpm.cmd --filter @almomiaa/rep-pwa build`: **passed** (Vite + Workbox PWA).
- [x] `pnpm.cmd --filter @almomiaa/web-erp build`: **passed** (Next.js 15 production build across all 11 routes).
- [x] Static QA & Security audit: **0** hardcoded hex colors, **0** directional Tailwind utilities, **0** `any`/`@ts-ignore`, **0** API inspector references in `web-erp`, **0** committed secrets, cost masking & ABAC verified.
