# PROMPT-0006: Add User Role/Warehouse/Custom-Role Permission Assignment APIs, Bootstrap Local Dev Admin (`+966576171935`), Run Local API Against SQL Server `AlMomiaa`, and Verify All Endpoints Live

- **Status:** `COMPLETED (Archived as 0006-user-permissions-and-live-local-api-verification.md)`
- **Prompt ID:** `PROMPT-0006`
- **Scope:** `apps/backend-api` (`AlMomiaa.Domain`, `AlMomiaa.Application`, `AlMomiaa.Infrastructure`, `AlMomiaa.WebApi`, `tests/`), `docs/api/*`, `docs/architecture/06-API-Specification.md`
- **Reference baseline:** Architecture v3.2 (`00-ADR`..`08-Development-Roadmap`); `PROMPT-0001`..`PROMPT-0005` completed (8 schemas / 44 tables live in SQL Server `AlMomiaa`, 8-module `.NET 9` API, 18/18 tests passing, Postman v2.1 collection with 10 folders / 69 requests)
- **Confirmed User Requirements:**
  - Local SQL Server connection: `Server=localhost;Database=AlMomiaa;Trusted_Connection=true;TrustServerCertificate=true;`
  - Local **Development-only** `SUPER_ADMIN` user provisioned via runtime environment variables (NEVER hardcoded in source control / `appsettings.json`):
    - `ALMOMIAA_DEV_ADMIN_CODE = ADMIN-001`
    - `ALMOMIAA_DEV_ADMIN_PHONE = +966576171935`
    - `ALMOMIAA_DEV_ADMIN_PASSWORD = admin`
  - Full ability for an authorized admin to **create users**, **create custom roles with exact permission sets**, **assign/replace roles and warehouses on any existing user**, inspect a user's effective permissions, and immediately enforce updated permissions via `SecurityStamp` rotation.
  - Run `AlMomiaa.WebApi` in `Development` mode against live SQL Server `AlMomiaa`, execute a live end-to-end verification of every API request (including user creation, permission assignment, login as the newly granted user, and all 10 Postman categories), and update the Postman collection/environment + runbook so the user can test every request directly in Postman and share pass/fail feedback.

---

## 1. Purpose of Prompt

Close the IAM user-permission assignment gap (`GET /users/{id}`, `PUT /users/{id}/roles`, `PUT /users/{id}/warehouses`, `POST /roles`), verify the full `.NET 9` API running live in `Development` against the user's local SQL Server `AlMomiaa` database with the requested local Dev `SUPER_ADMIN` account (`+966576171935` / `admin`), and ensure the Postman collection and environment match 100% of live endpoints so the user can run interactive QA in Postman immediately.

This prompt covers **Backend `.NET 9` + Live Local SQL Server Verification + Postman/Docs Sync ONLY**. Do NOT modify `apps/web-erp`, `apps/design-system-studio`, `packages/ui-kit`, or `packages/api-client`.

---

## 2. Target Execution Agents and Order

1. **`@Backend .NET`** — implement the 4 missing IAM user-permission management endpoints (`GET /api/v1/users/{id}`, `PUT /api/v1/users/{id}/roles`, `PUT /api/v1/users/{id}/warehouses`, `POST /api/v1/roles`), add Domain/Application/Infrastructure/WebApi logic + FluentValidation + audit logs + `SecurityStamp` rotation, add automated tests, update Postman collection/environment & `docs/api/BACKEND_API_GUIDE.md` & `docs/architecture/06-API-Specification.md`, and run a live `Development` verification against `Server=localhost;Database=AlMomiaa;Trusted_Connection=true;TrustServerCertificate=true;` provisioning the env-var Dev admin (`+966576171935` / `admin`) and verifying all API categories live.
2. **`@QA & Security Auditor`** — verify explicit `[Authorize(Policy = "...")]` on every new endpoint, verify `SecurityStamp` rotation on role/warehouse reassignment, confirm zero hardcoded passwords/keys in committed files (`appsettings.json`, Postman environment, C# source), confirm `dotnet build` (0 warnings, 0 errors), confirm all `dotnet test` tests pass, confirm live SQL Server `AlMomiaa` health + Dev admin login, and confirm zero frontend files changed.
3. **`@Orchestrator`** — coordinate execution and archive/log as `0006-user-permissions-and-live-local-api-verification.md` only after QA returns `PASS`.

---

## 3. Target Files to Create / Modify

### `apps/backend-api/src/AlMomiaa.Domain/`
- `Identity/Role.cs` — add factory `Role.Create(string roleCode, string nameAr, string nameEn)` for creating custom roles with validated uppercase `RoleCode`.
- `Identity/User.cs` — add helper methods `ReplaceRoles(IEnumerable<int> roleIds)` and `ReplaceWarehouses(IEnumerable<int> warehouseIds, int? defaultWarehouseId)` (both rotating `SecurityStamp`).

### `apps/backend-api/src/AlMomiaa.Application/`
- `Common/Abstractions/ModuleServices.cs` — extend `IIamModuleService` with:
  - `Task<UserDetailDto> GetUserByIdAsync(Guid userId, CancellationToken ct)`
  - `Task<UserDetailDto> UpdateUserRolesAsync(UpdateUserRolesCommand command, CancellationToken ct)`
  - `Task<UserDetailDto> UpdateUserWarehousesAsync(UpdateUserWarehousesCommand command, CancellationToken ct)`
  - `Task<RoleDto> CreateRoleAsync(CreateRoleCommand command, CancellationToken ct)`
- `Users/UsersModule.cs` — add:
  - `UserDetailDto` (all `UserSummaryDto` fields + `string[] EffectivePermissions`)
  - `GetUserByIdQuery(Guid UserId)`
  - `UpdateUserRolesRequest(int[] RoleIds)` + `UpdateUserRolesCommand(Guid UserId, int[] RoleIds)` + validator (`UserId` non-empty, `RoleIds` non-empty, every ID `> 0`)
  - `UpdateUserWarehousesRequest(int? DefaultWarehouseId, int[] AssignedWarehouseIds)` + `UpdateUserWarehousesCommand` + validator
  - `CreateRoleRequest(string RoleCode, string NameAr, string NameEn, string[] Permissions)` + `CreateRoleCommand` + validator (`RoleCode` required, max 30, regex `^[A-Z0-9_]+$`; `NameAr` required max 100; `NameEn` required max 100; `Permissions` non-empty, every claim must be in canonical `CanonicalRolesAndPermissions.Permissions`)

### `apps/backend-api/src/AlMomiaa.Infrastructure/`
- `Persistence/Services/IamAndCustomersServices.cs` — implement the 4 new `IamModuleService` methods with:
  - Unique `RoleCode` check (`409 Conflict`)
  - Existence validation for `User`, `Roles`, and `Warehouses` (`404 Not Found`)
  - `SecurityStamp` rotation + `ISecurityStampStore.InvalidateAsync(user.Id, ct)` on role or warehouse reassignment
  - Immutable `[ops].[AuditLogs]` entries with old/new JSON snapshots inside the same ACID transaction
- `Authentication/DevelopmentAdminBootstrapper.cs` — ensure idempotency: if a `SUPER_ADMIN` with `ALMOMIAA_DEV_ADMIN_PHONE` or `ALMOMIAA_DEV_ADMIN_CODE` already exists in `Development`, keep it active and ensure it holds `SUPER_ADMIN` role; if absent, create it using the env vars (`ALMOMIAA_DEV_ADMIN_CODE`, `ALMOMIAA_DEV_ADMIN_PHONE`, `ALMOMIAA_DEV_ADMIN_PASSWORD`) without logging the plaintext password.

### `apps/backend-api/src/AlMomiaa.WebApi/`
- `Controllers/UsersController.cs` — add:
  - `GET /api/v1/users/{id:guid}` — `[Authorize(Policy = CanonicalRolesAndPermissions.Permissions.UsersManage)]`
  - `PUT /api/v1/users/{id:guid}/roles` — `[Authorize(Policy = CanonicalRolesAndPermissions.Permissions.PermissionsManage)]`
  - `PUT /api/v1/users/{id:guid}/warehouses` — `[Authorize(Policy = CanonicalRolesAndPermissions.Permissions.UsersManage)]`
  - `POST /api/v1/roles` — `[Authorize(Policy = CanonicalRolesAndPermissions.Permissions.PermissionsManage)]`

### `apps/backend-api/tests/`
- `AlMomiaa.AuthenticationTests/ErpApiEndToEndTests.cs` — add automated integration test `Admin_Can_Create_Custom_Role_Assign_Permissions_To_User_And_Enforce_Immediately`:
  - Admin creates a custom role (`CUSTOM_VIEWER` with `catalog:view` + `customers:view` only)
  - Admin creates a user (`EMP-CUSTOM-01`)
  - Admin assigns custom role to user via `PUT /api/v1/users/{id}/roles` and warehouse via `PUT /api/v1/users/{id}/warehouses`
  - Admin inspects `GET /api/v1/users/{id}` and verifies `EffectivePermissions` matches exactly
  - New user logs in: `GET /api/v1/catalog/products` returns `200 OK`, `POST /api/v1/orders` returns `403 Forbidden`
  - Admin grants `orders:create` via `PUT /api/v1/roles/{roleId}/permissions` → old JWT is immediately revoked (`401 Unauthorized`), and after re-login the user has `orders:create`!

### `docs/api/` & `docs/architecture/`
- `docs/api/AlMomiaa-ERP.postman_collection.json` — add the 4 new IAM requests into Folder `02 - Users & Roles (IAM)` (auto-saving `{{customRoleId}}` and demonstrating full "create user → create/assign role permissions → login as new user" workflow) and set `adminIdentifier` default in `AlMomiaa-Local.postman_environment.json` to `+966576171935` (keeping `adminPassword` empty in git for security!).
- `docs/api/BACKEND_API_GUIDE.md` — add the 4 new endpoints + a step-by-step "Start Local Server & Test in Postman" quickstart using the exact `Server=localhost;Database=AlMomiaa;Trusted_Connection=true;TrustServerCertificate=true;` connection string.
- `docs/architecture/06-API-Specification.md` — add `GET /users/{id}`, `PUT /users/{id}/roles`, `PUT /users/{id}/warehouses`, and `POST /roles` to Section 2.1.

---

## 4. Live Local Verification Plan (Executed by `@Backend .NET` in `Development`)

1. Build `apps/backend-api/AlMomiaa.Erp.sln` (`0 warnings, 0 errors`) and run `dotnet test` (100% pass).
2. Verify `AlMomiaa` SQL Server on `localhost` has zero pending migrations.
3. Launch `AlMomiaa.WebApi` locally in `Development` (outside sandbox so it binds `http://localhost:5000` and connects to `Server=localhost;Database=AlMomiaa;Trusted_Connection=true;TrustServerCertificate=true;`) with runtime environment variables:
   - `DOTNET_ROLL_FORWARD=Major`
   - `ASPNETCORE_ENVIRONMENT=Development`
   - `ConnectionStrings__SqlServer=Server=localhost;Database=AlMomiaa;Trusted_Connection=true;TrustServerCertificate=true;`
   - `Auth__Jwt__Issuer=https://api.almomiaa.local`
   - `Auth__Jwt__Audience=almomiaa-erp-web`
   - `Auth__Jwt__SigningKey=<32+ byte runtime-only dev key>`
   - `Cors__AllowedOrigins__0=http://localhost:5173`
   - `ALMOMIAA_DEV_ADMIN_CODE=ADMIN-001`
   - `ALMOMIAA_DEV_ADMIN_PHONE=+966576171935`
   - `ALMOMIAA_DEV_ADMIN_PASSWORD=admin`
4. Run a live HTTP smoke test against `http://localhost:5000` verifying:
   - `GET /health` → `200`
   - `POST /api/v1/auth/login` with `+966576171935` / `admin` → `200` with `SUPER_ADMIN` JWT
   - `POST /api/v1/users`, `POST /api/v1/roles`, `PUT /api/v1/users/{id}/roles`, `GET /api/v1/users/{id}` → `201`/`200`
   - End-to-end sample flow across Catalog, Warehouses, Procurement, Customers, Sales Orders, ZATCA Invoicing, Collections/Finance, Dashboards, and Audit Logs against live SQL Server `AlMomiaa`!
5. Leave the local API cleanly ready (or running if requested as a local dev daemon) and report the exact startup command + live verification matrix.

---

## 5. Security & Domain Rules

1. **Zero Hardcoded Secrets in Git:** Never commit `"admin"`, `+966576171935`, or any JWT `SigningKey` into `appsettings.json`, `launchSettings.json`, C# source files, or `AlMomiaa-Local.postman_environment.json`. Pass them strictly as runtime environment variables during local Development execution.
2. **Explicit RBAC Policies:**
   - `PUT /api/v1/users/{id}/roles` and `POST /api/v1/roles` MUST require `[Authorize(Policy = "permissions:manage")]` (`SUPER_ADMIN` only) to prevent privilege escalation by `SALES_MGR` (who has `users:manage` but MUST NOT be able to promote anyone to `SUPER_ADMIN` or `ACCOUNTANT`!).
   - `GET /api/v1/users/{id}` and `PUT /api/v1/users/{id}/warehouses` require `[Authorize(Policy = "users:manage")]`.
3. **Immediate Revocation:** Any role or warehouse reassignment MUST rotate `User.SecurityStamp` in SQL Server and invalidate the security stamp store so stale JWTs are rejected immediately.
4. **Canonical Permission Allowlist:** `POST /api/v1/roles` MUST reject unknown/arbitrary permission strings (`400 Bad Request`).
5. **No Frontend Touch:** Keep all changes strictly within `apps/backend-api/` and `docs/`.

---

## 6. Acceptance Criteria (Definition of Done)

- [x] `GET /api/v1/users/{id}`, `PUT /api/v1/users/{id}/roles`, `PUT /api/v1/users/{id}/warehouses`, and `POST /api/v1/roles` are implemented with per-column FluentValidation, strict RBAC (`permissions:manage` for role creation/assignment), `SecurityStamp` rotation, and `[ops].[AuditLogs]` snapshots.
- [x] `dotnet build apps/backend-api/AlMomiaa.Erp.sln` passes with **0 warnings and 0 errors**.
- [x] `dotnet test apps/backend-api/AlMomiaa.Erp.sln` passes **100%** of tests including the new custom-role + user-permission enforcement test.
- [x] Local Development admin (`+966576171935` / `admin`) is bootstrapped in live SQL Server `AlMomiaa` via env vars (zero hardcoded secrets in source control) and verified via live `POST /api/v1/auth/login` against `http://localhost:5000`.
- [x] Live smoke test against `http://localhost:5000` + live SQL Server `AlMomiaa` passes across all 10 Postman categories.
- [x] `docs/api/AlMomiaa-ERP.postman_collection.json` (valid v2.1 JSON), `docs/api/AlMomiaa-Local.postman_environment.json` (valid JSON, no hardcoded secrets), `docs/api/BACKEND_API_GUIDE.md`, and `docs/architecture/06-API-Specification.md` are updated and synchronized.
- [x] `@QA & Security Auditor` returns `PASS`, and `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0006-user-permissions-and-live-local-api-verification.md` and updates `docs/agent-ops/PROJECT_LOG.md`.
