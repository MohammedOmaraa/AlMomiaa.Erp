# PROMPT-0005: Implement Full 8-Module .NET 9 Backend REST API Suite, Per-Column Validations, Categorized Postman Collection, and Backend Runbook

- **Status:** `COMPLETED (Archived as 0005-full-backend-api-and-postman-suite.md)`
- **Prompt ID:** `PROMPT-0005`
- **Completed Date:** `2026-11-23`
- **QA & Security Auditor Gate:** `PASS`
- **Scope:** `apps/backend-api` (`AlMomiaa.Domain`, `AlMomiaa.Application`, `AlMomiaa.Infrastructure`, `AlMomiaa.WebApi`, `tests/`), `docs/api/*`, `docs/architecture/06-API-Specification.md`

---

## Execution & Verification Summary

1. **Domain Layer (`AlMomiaa.Domain` — zero external/framework dependencies):**
   - Added `DomainExceptions.cs` (`BusinessRuleException`, `NotFoundException`, `ConflictException`, `ForbiddenDomainException`).
   - Added canonical `inventory:view`, `inventory:transfer`, `inventory:adjust` claims in `CanonicalRolesAndPermissions.cs`.
   - Added factories and domain state-transition invariants across all 38 ERP entities (including `SalesOrder` MR-02/MR-03/RISK-01/RISK-03 guards, `WarehouseStock` reservation/deduction/count guards, `MasterProduct.RecalculateWeightedAvgCost`, `FiscalPeriod.EnsureOpen/Close`, `JournalEntry.PostBalanced`, `ZatcaDeviceSequence.AllocateNext`, and `User.SetDefaultWarehouse`).
2. **Application Layer (`AlMomiaa.Application`):**
   - Added `PagedResult<T>`, `ICurrentUserService` (+ role helpers), `ModuleServices.cs`, and all 8 CQRS modules (`Users`, `Customers`, `Catalog`, `Inventory`, `Procurement`, `Sales`, `Finance`, `Operations`) with complete per-column FluentValidation rules.
3. **Infrastructure Layer (`AlMomiaa.Infrastructure`):**
   - Implemented all 8 module services (`IamAndCustomersServices.cs`, `CatalogAndInventoryServices.cs`, `ProcurementAndSalesServices.cs`, `FinanceAndOperationsServices.cs`), `AuditAndOutboxHelper.cs`, `ZatcaInvoiceHelper.cs`, `InMemoryDevSecurityServices.cs` (Development-only Redis fallback when `Redis:ConnectionString` is blank), and `DevelopmentAdminBootstrapper.cs` (Development-only env-var `SUPER_ADMIN` bootstrap).
   - Generated additive EF Core migration `20261123092021_AddInventoryPermissionClaims`, verified zero pending model changes, and applied it to live SQL Server `AlMomiaa` (3 migrations applied, 109 role-permission claims).
4. **WebApi Layer (`AlMomiaa.WebApi`):**
   - Implemented `HttpCurrentUserService.cs`, `IdempotencyHeaderHelper.cs`, and 14 thin REST controllers covering all 8 ERP modules with explicit `[Authorize(Policy = "...")]` policies, RFC 7807 `ProblemDetails` exception mapping (`400`, `403`, `404`, `409`, `422`, `503`), Bearer OpenAPI metadata, and `Properties/launchSettings.json`.
5. **Postman Suite & Documentation (`docs/api/*` + `06-API-Specification.md`):**
   - Generated valid Postman Collection v2.1 (`docs/api/AlMomiaa-ERP.postman_collection.json` — 10 categorized folders, 69 requests, auto-chaining test scripts, `X-Idempotency-Key: {{$guid}}`).
   - Generated Postman Local Environment (`docs/api/AlMomiaa-Local.postman_environment.json` — 32 variables, zero hardcoded secrets).
   - Authored complete runbook, per-column validation reference, and error troubleshooting playbook (`docs/api/BACKEND_API_GUIDE.md`) and synchronized `docs/architecture/06-API-Specification.md` (v3.2).
6. **Automated Tests & QA Gate:**
   - `dotnet build apps/backend-api/AlMomiaa.Erp.sln` → **0 Warnings, 0 Errors**.
   - `dotnet test apps/backend-api/AlMomiaa.Erp.sln` → **18/18 Passed** (`1/1` ArchitectureTests + `17/17` Authentication/Schema/8-Module End-to-End API tests).
   - Live SQL Server `AlMomiaa` verified via `sqlcmd` (`3` migrations, `109` role-permission rows).
   - Zero frontend/JS package modifications; zero committed secrets.
