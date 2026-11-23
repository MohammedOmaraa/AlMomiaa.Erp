# PROMPT-0004: Provision `AlMomiaa` SQL Server Database and Full Canonical ERP Schema via EF Core 9 Migrations

- **Status:** `COMPLETED (Archived as 0004-provision-sqlserver-almomiaa-database.md)`
- **Prompt ID:** `PROMPT-0004`
- **Scope:** `apps/backend-api` (`AlMomiaa.Domain`, `AlMomiaa.Infrastructure`, and `tests/`) and local Microsoft SQL Server provisioning (`AlMomiaa` database)
- **Reference baseline:** Architecture v3.1 (`docs/architecture/00-ADR-Architecture-Decisions.md`, `docs/architecture/05-Database.md`, `docs/architecture/07-Security.md`); `PROMPT-0001`..`PROMPT-0003` completed
- **Confirmed decisions:**
  - Full canonical ERP schema: all **8 schemas** (`[id]`, `[cat]`, `[cust]`, `[inv]`, `[proc]`, `[sales]`, `[fin]`, `[sys]`) and all **44 tables** from `05-Database.md`.
  - Provisioning mechanism: **EF Core 9 migrations** on `AlMomiaaDbContext` + `dotnet ef database update` so EF Core model snapshot, `__EFMigrationsHistory`, and SQL Server stay 100% synchronized.
  - Target SQL Server instance: local default instance (`Server=localhost;Database=AlMomiaa;Trusted_Connection=True;TrustServerCertificate=True;`), supplied via environment variable at execution time (never committed into source control).
  - Seeds & partitioning: canonical reference seeds only (`[id].[Roles]`, `[id].[RolePermissions]`, `[cust].[CustomerGrades]` A/B/C/D) + partition-ready clustered `(DateUtc, Id)` primary keys on append-only tables; **no default users or passwords**.

---

## 1. Purpose of Prompt

Model the remaining 7 canonical ERP bounded-context schemas (`[cat]`, `[cust]`, `[inv]`, `[proc]`, `[sales]`, `[fin]`, `[sys]`) in `AlMomiaa.Domain` and `AlMomiaa.Infrastructure`, generate a clean EF Core 9 SQL Server migration building on top of the existing `[id]` migration (`20261123063607_InitialIdentityAndRefreshSessions`), and provision the **`AlMomiaa`** database and all **44 tables** in the local Microsoft SQL Server instance via `dotnet ef database update`.

This prompt covers **domain entity modeling, EF Core 9 configurations, SQL Server migrations, database creation/migration execution, and schema verification tests only**. Do not add new HTTP endpoints, CQRS feature handlers, frontend pages, UI components, or seeded user accounts.

---

## 2. Target Execution Agents and Order

1. **`@Backend .NET`** — implement the pure Domain entities for `[cat]`, `[cust]`, `[inv]`, `[proc]`, `[sales]`, `[fin]`, and `[sys]`; add EF Core 9 `IEntityTypeConfiguration<T>` mappings and `DbSet<T>` properties to `AlMomiaaDbContext`; generate the additive EF Core migration `AddFullErpSchema`; apply all migrations to the local Microsoft SQL Server `AlMomiaa` database; verify live SQL Server schemas/tables/constraints/seeds; and add backend schema & architecture verification tests.
2. **`@QA & Security Auditor`** — verify 1:1 fidelity with `docs/architecture/05-Database.md`, Clean Architecture purity (`AlMomiaa.Domain` zero external references), zero committed secrets/connection strings, zero seeded users/passwords, live SQL Server verification results, and full solution build/test pass.
3. **`@Orchestrator`** — coordinate execution and archive/log only after QA returns `PASS`.

Do not invoke `@Design System UI` or `@Frontend Engineer`; no frontend or design-system files are in scope.

---

## 3. Target Files to Create / Modify

Preserve all existing `[id]` entities, configurations, and migration `20261123063607_InitialIdentityAndRefreshSessions` without rewriting or breaking them.

### `apps/backend-api/src/AlMomiaa.Domain/` (Pure C# 13, zero external NuGet/framework references)

Create bounded-context folders with clean domain entities matching `docs/architecture/05-Database.md`:

- `Catalog/` — `Category.cs`, `Brand.cs`, `MasterProduct.cs`, `ProductUnit.cs`, `PriceList.cs`, `PriceListItem.cs`, `SupplierProduct.cs`
- `Customers/` — `CustomerGrade.cs`, `Customer.cs`, `CanonicalCustomerGrades.cs`
- `Inventory/` — `Warehouse.cs`, `WarehouseStock.cs`, `StockMovement.cs`, `StockTransfer.cs`, `StockTransferItem.cs`
- `Procurement/` — `Supplier.cs`, `PurchaseOrder.cs`, `PurchaseOrderItem.cs`, `Container.cs`, `GoodsReceipt.cs`, `GoodsReceiptItem.cs`
- `Sales/` — `SalesOrder.cs`, `SalesOrderItem.cs`, `ZatcaDeviceSequence.cs`, `Invoice.cs`, `SalesReturn.cs`, `SalesReturnItem.cs`, `DeliveryDispatch.cs`, `DeliveryDispatchItem.cs`
- `Finance/` — `FiscalPeriod.cs`, `FinancialAccount.cs`, `Collection.cs`, `CollectionAllocation.cs`, `JournalEntry.cs`, `JournalLine.cs`, `DailyFinancialSnapshot.cs`
- `System/` — `AuditLog.cs`, `OutboxMessage.cs`, `GpsRouteHistory.cs`

### `apps/backend-api/src/AlMomiaa.Infrastructure/`

- `Persistence/AlMomiaaDbContext.cs` — add typed `DbSet<T>` properties for all 38 new entities while preserving existing `[id]` `DbSet<T>` properties and SQLite `RowVersion` test compatibility.
- `Persistence/AlMomiaaDbContextFactory.cs` — keep reading `ALMOMIAA_DESIGNTIME_SQLSERVER` from environment with safe local default fallback (no credentials committed).
- `Persistence/Configurations/Catalog/CatalogConfigurations.cs`
- `Persistence/Configurations/Customers/CustomerConfigurations.cs`
- `Persistence/Configurations/Inventory/InventoryConfigurations.cs`
- `Persistence/Configurations/Procurement/ProcurementConfigurations.cs`
- `Persistence/Configurations/Sales/SalesConfigurations.cs`
- `Persistence/Configurations/Finance/FinanceConfigurations.cs`
- `Persistence/Configurations/System/SystemConfigurations.cs`
- `Persistence/Configurations/Identity/IdentityConfigurations.cs` — add only the deferred foreign key `FK_UserWarehouses_Warehouse` linking `[id].[UserWarehouses](WarehouseId)` → `[inv].[Warehouses](Id)` (preserving all existing `[id]` mappings intact).
- `Persistence/Migrations/<Timestamp>_AddFullErpSchema.cs` (+ `.Designer.cs` and updated `AlMomiaaDbContextModelSnapshot.cs`) generated via `dotnet ef migrations add AddFullErpSchema`.

### `apps/backend-api/tests/`

- Add schema/model alignment tests (in `AlMomiaa.AuthenticationTests` or a narrowly scoped test class in `AlMomiaa.ArchitectureTests`) verifying:
  - All 8 schemas and 44 entity table mappings exist in `AlMomiaaDbContext.Model`
  - Every mutable aggregate has a `RowVersion` concurrency token
  - Every persisted computed column, check constraint, filtered unique index, and partition-ready composite PK is configured
  - `CustomerGrades` canonical reference seed (A/B/C/D) is deterministic and zero users are seeded
  - All existing `AlMomiaa.ArchitectureTests` (Domain zero dependencies) and `AlMomiaa.AuthenticationTests` (10/10 auth E2E tests) continue to pass!

---

## 4. Database Schema & EF Core 9 Mapping Requirements (`docs/architecture/05-Database.md`)

All 8 schemas and 44 tables must match `05-Database.md` exactly:

| Schema | Tables | Count | Notes |
| :--- | :--- | :---: | :--- |
| `[id]` | `Users`, `Roles`, `RolePermissions`, `UserRoles`, `UserWarehouses`, `RefreshTokenSessions` | 6 | Already created in Migration 1; Migration 2 adds `FK_UserWarehouses_Warehouse` → `[inv].[Warehouses](Id)` |
| `[cat]` | `Categories`, `Brands`, `MasterProducts`, `ProductUnits`, `PriceLists`, `PriceListItems`, `SupplierProducts` | 7 | `IX_MasterProducts_Category` with `INCLUDE`; `UQ_PriceList_Unit_MinQty`; `UQ_Supplier_MasterProduct`; `FK_SupplierProducts_Supplier` → `[proc].[Suppliers](Id)` |
| `[cust]` | `CustomerGrades`, `Customers` | 2 | Seed `CustomerGrades` (`A=60d`, `B=45d`, `C=30d`, `D=0d/AllowCredit=0`); `IX_Customers_AssignedRep` with `INCLUDE` |
| `[inv]` | `Warehouses`, `WarehouseStocks`, `StockMovements`, `StockTransfers`, `StockTransferItems` | 5 | `WarehouseStocks.AvailableQty` persisted computed column + `CK_WarehouseStocks_NonNegative` + `UQ_Warehouse_Product`; `StockMovements` clustered PK `(CreatedAtUtc, Id)` |
| `[proc]` | `Suppliers`, `PurchaseOrders`, `PurchaseOrderItems`, `Containers`, `GoodsReceipts`, `GoodsReceiptItems` | 6 | `PurchaseOrderItems.LineTotalSAR` and `Containers.TotalLandedCostSAR` persisted computed columns |
| `[sales]` | `SalesOrders`, `SalesOrderItems`, `ZatcaDeviceSequences`, `Invoices`, `SalesReturns`, `SalesReturnItems`, `DeliveryDispatches`, `DeliveryDispatchItems` | 8 | `Invoices.RemainingBalance` persisted computed column; `UQ_Zatca_Device_Icv`; `IX_SalesOrders_Status_Created (Status, CreatedAtUtc DESC)`; `IX_Invoices_Customer_Status`; `UQ_Dispatch_Invoice` |
| `[fin]` | `FiscalPeriods`, `FinancialAccounts`, `Collections`, `CollectionAllocations`, `JournalEntries`, `JournalLines`, `DailyFinancialSnapshots` | 7 | `CK_FiscalPeriods_Month`; `UQ_FiscalPeriod_YearMonth`; filtered unique indexes `UX_FinancialAccounts_OwnerRep` and `UX_Collections_ProofHash`; `JournalLines` clustered PK `(CreatedAtUtc, Id)` + `CK_JournalLines_NonNegative` |
| `[sys]` | `AuditLogs`, `OutboxMessages`, `GpsRouteHistory` | 3 | `AuditLogs` clustered PK `(TimestampUtc, Id)`; `GpsRouteHistory` clustered PK `(RecordedAtUtc, Id)`; filtered index `IX_Outbox_Unprocessed` (`WHERE ProcessedAtUtc IS NULL`) |
| **Total** | **8 Schemas** | **44** | **100% aligned with `05-Database.md`** |

### Strict SQL Server & EF Core 9 Rules

1. **Multiple Cascade Paths Prevention:** Use `DeleteBehavior.Restrict` (or `DeleteBehavior.NoAction`) on all cross-entity foreign keys so SQL Server never raises Error 1785 (`may cause cycles or multiple cascade paths`). Use `DeleteBehavior.Cascade` ONLY for owned line-item compositions (`StockTransferItems`, `PurchaseOrderItems`, `GoodsReceiptItems`, `SalesOrderItems`, `SalesReturnItems`, `DeliveryDispatchItems`).
2. **Exact Numeric Precision:**
   - Money / balances / prices / VAT: `decimal(18,2)`
   - Unit costs / WAC / conversion factors: `decimal(18,4)`
   - Stock / order quantities: `decimal(18,3)`
   - VAT rate: `decimal(5,2)`
   - Latitude / Longitude: `decimal(9,6)`; GPS accuracy: `decimal(6,2)`
3. **UUIDv7 Keys:** Configure `.ValueGeneratedNever()` on all `UNIQUEIDENTIFIER` primary keys (IDs generated via `Guid.CreateVersion7()` in Domain).
4. **Optimistic Concurrency (`ROWVERSION`):** Configure `.IsRowVersion()` on `MasterProducts`, `Customers`, `WarehouseStocks`, `StockTransfers`, `Suppliers`, `PurchaseOrders`, `Containers`, `SalesOrders`, `Invoices`, `DeliveryDispatches`, `FinancialAccounts`, `Collections`, and `RefreshTokenSessions`. Update `AlMomiaaDbContext` SQLite test guard so all `RowVersion` properties remain compatible with existing SQLite integration tests.
5. **Append-Only Partition-Ready PKs:** Configure composite primary keys `(CreatedAtUtc, Id)` / `(TimestampUtc, Id)` / `(RecordedAtUtc, Id)` with `Id` as `BIGINT IDENTITY(1,1)` (`ValueGeneratedOnAdd`) and `.IsClustered()` on `inv.StockMovements`, `fin.JournalLines`, `sys.AuditLogs`, and `sys.GpsRouteHistory`.
6. **Deterministic `HasData` Seeding:** Use a fixed static UTC timestamp (`new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)`) for `CustomerGrades` seed rows so EF Core detects zero pending model changes (`dotnet ef migrations has-pending-model-changes` passes cleanly).
7. **Live SQL Server Provisioning:**
   - Provision database **`AlMomiaa`** on local default SQL Server (`Server=localhost;Database=AlMomiaa;Trusted_Connection=True;TrustServerCertificate=True;`) via `dotnet ef database update` using the `ALMOMIAA_DESIGNTIME_SQLSERVER` environment variable.
   - Verify via SQL query (`sqlcmd` or `Microsoft.Data.SqlClient` / PowerShell) against `AlMomiaa` that:
     - Database `AlMomiaa` exists
     - All 8 schemas and all 44 tables exist
     - `__EFMigrationsHistory` contains both `20261123063607_InitialIdentityAndRefreshSessions` and `<Timestamp>_AddFullErpSchema`
     - `[id].[Roles]` has 7 rows, `[cust].[CustomerGrades]` has 4 rows, and `[id].[Users]` has 0 rows
     - Both deferred FKs (`FK_UserWarehouses_Warehouse`, `FK_SupplierProducts_Supplier`) exist.

---

## 5. Security & Domain Rules

- **Zero Secrets in Source Control:** Do NOT commit passwords, SQL credentials, or hardcoded environment connection strings into `appsettings.json`, migrations, or source files. `appsettings.json` keeps `"ConnectionStrings": { "SqlServer": "" }`.
- **No Seeded Users or Passwords:** Seed only canonical reference tables (`[id].[Roles]`, `[id].[RolePermissions]`, `[cust].[CustomerGrades]`). Never seed an admin user or default password in migrations or startup code.
- **Clean Architecture Purity:** `AlMomiaa.Domain` must keep zero references to EF Core, ASP.NET Core, MediatR, or FluentValidation.
- **No Unrelated Scope:** Do not modify `apps/web-erp`, `apps/design-system-studio`, `packages/ui-kit`, `packages/api-client`, or add new Web API endpoints.

---

## 6. Design System Components Mapping

Not applicable — backend database schema and SQL Server provisioning only.

---

## 7. Acceptance Criteria (Definition of Done)

- [ ] All 38 remaining canonical entities are implemented cleanly under `AlMomiaa.Domain` with zero external framework/NuGet dependencies.
- [ ] All 8 schemas (`id`, `cat`, `cust`, `inv`, `proc`, `sales`, `fin`, `sys`) and all 44 tables are mapped in `AlMomiaaDbContext` matching `docs/architecture/05-Database.md` (columns, lengths, `DECIMAL` precisions, persisted computed columns, check constraints, filtered unique indexes, covering indexes, `ROWVERSION` tokens, and partition-ready clustered PKs).
- [ ] Deferred foreign keys `FK_UserWarehouses_Warehouse` and `FK_SupplierProducts_Supplier` are created cleanly with no SQL Server multiple-cascade-path errors.
- [ ] Additive EF Core 9 migration `AddFullErpSchema` is generated cleanly without modifying `20261123063607_InitialIdentityAndRefreshSessions`, and `dotnet ef migrations has-pending-model-changes` reports zero drift.
- [ ] Canonical reference data (`7` roles, `31` canonical permission claim types mapped across roles, `4` customer grades A/B/C/D) is seeded idempotently with **0 seeded users or passwords**.
- [ ] Database `AlMomiaa` and all 44 tables are created and verified on the local Microsoft SQL Server default instance (`localhost`, Windows Integrated Auth) via `dotnet ef database update`.
- [ ] `dotnet build AlMomiaa.Erp.sln` passes with 0 warnings and 0 errors, and all backend tests (Domain dependency tests, authentication E2E tests, and new schema/model verification tests) pass.
- [ ] No secrets, hardcoded connection strings, frontend changes, or unrelated API endpoints were introduced.
- [ ] `@QA & Security Auditor` returns `PASS`, after which `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0004-provision-sqlserver-almomiaa-database.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks `CURRENT_PROMPT.md` completed.
