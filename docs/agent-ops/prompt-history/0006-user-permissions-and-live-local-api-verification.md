# PROMPT-0006: Add User Role/Warehouse/Custom-Role Permission Assignment APIs, Bootstrap Local Dev Admin (`+966576171935`), Run Local API Against SQL Server `AlMomiaa`, and Verify All Endpoints Live

- **Status:** `COMPLETED (Archived as 0006-user-permissions-and-live-local-api-verification.md)`
- **Prompt ID:** `PROMPT-0006`
- **Completed Date:** `2026-11-23`
- **QA & Security Auditor Gate:** `PASS`
- **Scope:** `apps/backend-api`, `docs/api/*`, `docs/architecture/06-API-Specification.md`

---

## Execution & Verification Summary

1. **New IAM User & Custom-Role Permission Assignment Endpoints:**
   - `GET /api/v1/users/{id}` (`[Authorize(Policy = "users:manage")]`) — returns full user profile with `roleCodes`, `assignedWarehouseIds`, and computed `effectivePermissions`.
   - `PUT /api/v1/users/{id}/roles` (`[Authorize(Policy = "permissions:manage")]`) — replaces roles assigned to a user, records `[ops].[AuditLogs]` diff, and immediately rotates/invalidates `SecurityStamp`.
   - `PUT /api/v1/users/{id}/warehouses` (`[Authorize(Policy = "users:manage")]`) — replaces assigned warehouses & default warehouse and rotates `SecurityStamp`.
   - `POST /api/v1/roles` (`[Authorize(Policy = "permissions:manage")]`) — creates custom roles with strict allowlist validation against canonical ERP permission claims.
2. **Postman Suite & Documentation Sync:**
   - Updated [AlMomiaa-ERP.postman_collection.json](file:///d:/AlMomiaa-app/docs/api/AlMomiaa-ERP.postman_collection.json) to **10 folders / 73 requests** and fixed raw JSON serialization of integer Postman variables (`{{warehouseId}}`, `{{categoryId}}`, `{{brandId}}`, `{{customRoleId}}`, etc.) so they serialize as valid JSON numbers.
   - Updated [AlMomiaa-Local.postman_environment.json](file:///d:/AlMomiaa-app/docs/api/AlMomiaa-Local.postman_environment.json) (`33` variables, `adminIdentifier: "+966576171935"`, empty `adminPassword` in git).
   - Synchronized [BACKEND_API_GUIDE.md](file:///d:/AlMomiaa-app/docs/api/BACKEND_API_GUIDE.md) and [06-API-Specification.md](file:///d:/AlMomiaa-app/docs/architecture/06-API-Specification.md).
3. **Build, Automated Tests & Live SQL Server Verification:**
   - `dotnet build apps/backend-api/AlMomiaa.Erp.sln` → **0 Warnings, 0 Errors**.
   - `dotnet test apps/backend-api/AlMomiaa.Erp.sln` → **19/19 Passed** (`1/1` ArchitectureTests + `18/18` Authentication/Schema/8-Module E2E + Custom-Role/User-Permission Enforcement).
   - Bootstrapped local Development `SUPER_ADMIN` (`ADMIN-001` / `+966576171935`) via runtime environment variables against live SQL Server `Server=localhost;Database=AlMomiaa;Trusted_Connection=true;TrustServerCertificate=true;` (zero hardcoded secrets committed).
   - Executed live HTTP smoke test against `http://localhost:5000` — **67/67 live API calls passed** across all 10 Postman categories against live SQL Server `AlMomiaa`, and left the Development API running on `http://localhost:5000` for interactive Postman testing.
