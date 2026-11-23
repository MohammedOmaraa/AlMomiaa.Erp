# PROMPT-0004: Provision `AlMomiaa` SQL Server Database and Full Canonical ERP Schema via EF Core 9 Migrations

- **Status:** `COMPLETED`
- **Prompt ID:** `PROMPT-0004`
- **Executed On:** 2026-11-23
- **Scope:** `apps/backend-api` (`AlMomiaa.Domain`, `AlMomiaa.Infrastructure`, and `tests/`), synchronized `docs/architecture/05-Database.md`, and local Microsoft SQL Server provisioning (`AlMomiaa` database)
- **Reference baseline:** Architecture v3.1 (`docs/architecture/00-ADR-Architecture-Decisions.md`, `docs/architecture/05-Database.md`, `docs/architecture/07-Security.md`); `PROMPT-0001`..`PROMPT-0003` completed

## 1. Purpose

Model all remaining canonical ERP bounded-context schemas in `AlMomiaa.Domain` and `AlMomiaa.Infrastructure`, generate the additive EF Core 9 migration `AddFullErpSchema` on top of `20261123063607_InitialIdentityAndRefreshSessions`, and provision the **`AlMomiaa`** database with all **8 schemas** and **44 tables** in the local Microsoft SQL Server instance via `dotnet ef database update`.

## 2. Agents Executed

1. `@Backend .NET`
2. `@QA & Security Auditor` — **Verdict: `PASS`**
3. `@Orchestrator`

## 3. Key Files Created / Modified

- **Domain Entities (`AlMomiaa.Domain` — zero external dependencies):**
  - `Catalog/`: `Category.cs`, `Brand.cs`, `MasterProduct.cs`, `ProductUnit.cs`, `PriceList.cs`, `PriceListItem.cs`, `SupplierProduct.cs`
  - `Customers/`: `CustomerGrade.cs`, `CanonicalCustomerGrades.cs`, `Customer.cs`
  - `Inventory/`: `Warehouse.cs`, `WarehouseStock.cs`, `StockMovement.cs`, `StockTransfer.cs`, `StockTransferItem.cs`
  - `Procurement/`: `Supplier.cs`, `PurchaseOrder.cs`, `PurchaseOrderItem.cs`, `Container.cs`, `GoodsReceipt.cs`, `GoodsReceiptItem.cs`
  - `Sales/`: `SalesOrder.cs`, `SalesOrderItem.cs`, `ZatcaDeviceSequence.cs`, `Invoice.cs`, `SalesReturn.cs`, `SalesReturnItem.cs`, `DeliveryDispatch.cs`, `DeliveryDispatchItem.cs`
  - `Finance/`: `FiscalPeriod.cs`, `FinancialAccount.cs`, `Collection.cs`, `CollectionAllocation.cs`, `JournalEntry.cs`, `JournalLine.cs`, `DailyFinancialSnapshot.cs`
  - `System/`: `AuditLog.cs`, `OutboxMessage.cs`, `GpsRouteHistory.cs` (`AlMomiaa.Domain.SystemOps`)
- **EF Core 9 Persistence & Migrations (`AlMomiaa.Infrastructure`):**
  - `Persistence/AlMomiaaDbContext.cs` (all 44 `DbSet<T>`s + generalized SQLite `RowVersion` test guard)
  - `Persistence/Configurations/{Catalog,Customers,Inventory,Procurement,Sales,Finance,System}/*`
  - `Persistence/Configurations/Identity/IdentityConfigurations.cs` (added deferred `FK_UserWarehouses_Warehouse`)
  - `Persistence/Migrations/20261123080206_AddFullErpSchema.cs` (+ `.Designer.cs` and `AlMomiaaDbContextModelSnapshot.cs`)
- **Tests (`AlMomiaa.AuthenticationTests`):** `DatabaseSchemaModelTests.cs`, `TestHarness.cs`
- **Architecture Docs:** `docs/architecture/05-Database.md` (updated reserved SQL Server `[sys]` schema to `[ops]` for `AuditLogs`, `OutboxMessages`, `GpsRouteHistory`)

## 4. Acceptance Criteria (Definition of Done)

- [x] All 38 remaining canonical entities are implemented cleanly under `AlMomiaa.Domain` with zero external framework/NuGet dependencies.
- [x] All 8 schemas (`id`, `cat`, `cust`, `inv`, `proc`, `sales`, `fin`, `ops`) and all 44 tables are mapped in `AlMomiaaDbContext` matching `docs/architecture/05-Database.md` (columns, lengths, `DECIMAL` precisions, persisted computed columns, check constraints, filtered unique indexes, covering indexes, `ROWVERSION` tokens, and partition-ready clustered PKs).
- [x] Deferred foreign keys `FK_UserWarehouses_Warehouse` and `FK_SupplierProducts_Supplier` are created cleanly with no SQL Server multiple-cascade-path errors.
- [x] Additive EF Core 9 migration `20261123080206_AddFullErpSchema` is generated cleanly without modifying `20261123063607_InitialIdentityAndRefreshSessions`, and `dotnet ef migrations has-pending-model-changes` reports zero drift.
- [x] Canonical reference data (`7` roles, `100` role-permission rows across 31 canonical claims, `4` customer grades A/B/C/D) is seeded idempotently with **0 seeded users or passwords**.
- [x] Database `AlMomiaa` and all 44 tables are created and verified on the local Microsoft SQL Server default instance (`localhost`, Windows Integrated Auth) via `dotnet ef database update`.
- [x] `dotnet build AlMomiaa.Erp.sln` passes with 0 warnings and 0 errors, and all 14 backend tests (`AlMomiaa.ArchitectureTests`: 1/1; `AlMomiaa.AuthenticationTests`: 13/13) pass.
- [x] No secrets, hardcoded connection strings, frontend changes, or unrelated API endpoints were introduced.
- [x] `@QA & Security Auditor` returned `PASS`; archived and logged by `@Orchestrator`.

## 5. Verification Summary & Architectural Note

- **SQL Server Reserved `[sys]` Schema Resolution:** During live migration execution against Microsoft SQL Server, `CREATE TABLE [sys].[AuditLogs]` returned Error 2760 because SQL Server reserves `[sys]` for built-in system catalog views. Mapped `AuditLogs`, `OutboxMessages`, and `GpsRouteHistory` to `[ops]`, regenerated `20261123080206_AddFullErpSchema`, and synchronized `docs/architecture/05-Database.md`.
- **Live SQL Server Verification (`sqlcmd -S localhost -d AlMomiaa -E -C`):**
  - `DB_NAME = AlMomiaa`, `SCHEMA_COUNT = 8`, `TABLE_COUNT = 44`, `MIGRATIONS_COUNT = 2`
  - `ROLES_COUNT = 7`, `ROLE_PERMISSIONS_COUNT = 100`, `CUSTOMER_GRADES_COUNT = 4`, `USERS_COUNT = 0`
  - `DEFERRED_FKS_COUNT = 2`, `COMPUTED_COLS_COUNT = 4`, `CHECK_CONSTRAINTS_COUNT = 3`, `FILTERED_INDEXES_COUNT = 3`
- **Build & Tests:** `dotnet build` (**0 warnings, 0 errors**), `dotnet ef migrations has-pending-model-changes` (zero drift), `dotnet test` (**14/14 passing**).
