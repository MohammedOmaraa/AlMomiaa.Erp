---
name: cqrs-efcore-patterns
description: Enforces .NET 9 Clean Architecture, Modular Monolith boundaries, MediatR CQRS conventions, FluentValidation pipelines, EF Core 9 AsNoTracking projections, UUIDv7 keys, ROWVERSION concurrency, and ACID Outbox patterns for AlMomiaa ERP.
---

# CQRS & EF Core 9 Patterns Skill — AlMomiaa ERP

Use this skill whenever creating or modifying domain entities, commands, queries, validators, EF Core configurations, or API endpoints in `apps/backend-api/`.

---

## 1. Clean Architecture & Modular Monolith Boundaries
- **`AlMomiaa.Domain`**: Entities, Value Objects, Domain Events (`IDomainEvent`), Domain Exceptions (`BusinessRuleException`). **Zero** NuGet references to EF Core, ASP.NET Core, MediatR handlers, or infrastructure SDKs.
- **`AlMomiaa.Application`**: CQRS Commands & Queries, `FluentValidation` validators, MediatR Pipeline Behaviors, DTOs, and Port Interfaces (`IApplicationDbContext`, `ICurrentUserService`, `IKpiSnapshotService`, `IZatcaSignerService`).
- **`AlMomiaa.Infrastructure`**: `AlMomiaaDbContext`, `IEntityTypeConfiguration<T>`, `AuditSaveChangesInterceptor`, SQL Server 2022 migrations, Redis 7 services, Hangfire jobs, S3/MinIO blob storage, ZATCA signer.
- **`AlMomiaa.WebApi`**: Thin REST endpoints (`/api/v1/*`), SignalR hubs (`OperationsHub`, `FieldTelemetryHub`), Middleware (`GlobalExceptionHandler`, `SecurityStampMiddleware`).
- **Module Boundary Enforcement:** Cross-module writes go through public module contracts or in-process MediatR domain events—never mutate another bounded context's tables directly in an unrelated write handler. Enforce via `ArchUnitNET` tests in `tests/`.

---

## 2. MediatR Naming & Pipeline Order
- **Commands:** `<Verb><Entity>Command` (e.g., `CreateSalesOrderCommand`, `EditSubmittedOrderByManagerCommand`, `VerifyBankTransferCommand`) returning `Result<TResponse>` or `Result`.
- **Queries:** `Get<EntityOrView>Query` (e.g., `GetProductsPagedQuery`, `GetCustomerCreditStatusQuery`, `GetExecutiveDashboardQuery`).
- **Validators:** `<CommandOrQueryName>Validator : AbstractValidator<...>` colocated in the same feature folder.
- **Mandatory MediatR Pipeline Order:**
  1. `IdempotencyBehavior` (enforces `X-Idempotency-Key` on commands such as `CreateSalesOrder`, `RecordCashCollection`, `RecordBankTransfer`)
  2. `ValidationBehavior` (runs `FluentValidation`, throws structured validation error mapped to RFC 7807 `400 Bad Request`)
  3. `AuthorizationBehavior` (verifies permission claims from `07-Security.md`, throws `403 Forbidden`)
  4. `TransactionBehavior` (wraps commands in an ACID SQL transaction with `AuditSaveChangesInterceptor` and `sys.OutboxMessages` persistence)

---

## 3. EF Core 9 Query & Write Standards
1. **Sequential Keys:** Generate all `UNIQUEIDENTIFIER` primary keys via `.NET 9` `Guid.CreateVersion7()` to prevent SQL Server clustered B-Tree fragmentation.
2. **Read Queries (`Queries/`):**
   - ALWAYS apply `.AsNoTracking()`.
   - ALWAYS project directly to immutable `record` DTOs via `.Select(x => new ...)` so SQL Server returns only required columns.
   - ALWAYS paginate collections using **keyset pagination** (`WHERE (CreatedAtUtc, Id) < (@CursorDate, @CursorId)`) for high-volume ledgers or indexed offset pagination for admin lookups, capping `PageSize <= 250`.
   - Never run unbounded `SUM()` / `COUNT()` aggregations over raw transactional tables for dashboards; read from `Redis` live KPI hashes or `fin.DailyFinancialSnapshots`.
3. **Optimistic Concurrency:**
   - Configure `.IsRowVersion()` on `WarehouseStocks`, `Customers`, `MasterProducts`, `SalesOrders`, `Invoices`, `FinancialAccounts`, and `Collections`.
   - Catch `DbUpdateConcurrencyException` and map to RFC 7807 `409 Conflict` (or retry idempotent stock reservation delta safely).
4. **Global Query Filters (Row-Level ABAC):**
   - Apply EF Core Global Query Filters automatically scoping `SALES_REP` queries to `Customer.AssignedRepId == _currentUser.UserId` and `WH_KEEPER` operational queries to `AssignedWarehouseIds`.
   - Only bypass with `.IgnoreQueryFilters()` inside explicit administrative/background handlers after permission verification.
5. **Append-Only Enforcement:**
   - Never issue `UPDATE` or `DELETE` against `inv.StockMovements`, `fin.JournalEntries`, `fin.JournalLines`, or `sys.AuditLogs`.
6. **Fiscal Period Guard:**
   - Before persisting any `fin.JournalEntries` row, verify the target `PostingDateUtc` falls within an open `fin.FiscalPeriods` record (`IsClosed == false`). If the source transaction date is in a closed period, post to the current open period while storing `OriginalValueDateUtc`.
7. **Transactional Outbox (`sys.OutboxMessages`):**
   - Persist SignalR broadcast payloads, ZATCA clearance/reporting jobs, and SMS OTP dispatches into `sys.OutboxMessages` inside the same database transaction; Hangfire processes and marks them dispatched.
