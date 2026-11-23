# PROMPT-0003: Implement User Authentication, Session Persistence, and Login UI

- **Status:** `COMPLETED`
- **Prompt ID:** `PROMPT-0003`
- **Executed On:** 2026-11-23
- **Scope:** `apps/backend-api`, `packages/api-client`, `packages/ui-kit` (auth form controls and stories), `apps/web-erp`, and synchronized auth documentation (`05-Database.md`, `06-API-Specification.md`, `07-Security.md`)
- **Reference baseline:** Architecture v3.1; `PROMPT-0001` and `PROMPT-0002` completed

## 1. Purpose

Implement a secure, end-to-end authentication foundation for AlMomiaa ERP:

- Persist canonical users, roles, permissions, warehouse assignments, and rotating refresh-token sessions in SQL Server via EF Core 9.
- Implement `POST /api/v1/auth/login`, `/refresh`, and `/logout` in the .NET 9 Web API with 10-minute Bearer JWTs, 14-day rotating host-only `HttpOnly; Secure; SameSite=Strict` cookies, Redis `SecurityStamp` validation (fail-closed), login throttling, and trusted-origin protection.
- Share OpenAPI-aligned TypeScript contracts, Zod schemas, in-memory access token storage, and a single-flight Axios refresh interceptor via `@almomiaa/api-client`.
- Add reusable accessible `TextField` and `PasswordField` controls (and CSF3 Storybook stories) in `@almomiaa/ui` / `apps/design-system-studio`.
- Build the bilingual Arabic RTL / English LTR login experience (`/login`) and session bootstrap/logout in `apps/web-erp`.

## 2. Agents Executed

1. `@Backend .NET`
2. `@Design System UI`
3. `@Frontend Engineer`
4. `@QA & Security Auditor` — **Verdict: `PASS`**
5. `@Orchestrator`

## 3. Key Files Created / Modified

- **Backend Domain (`AlMomiaa.Domain`):** `Identity/User.cs`, `Identity/Role.cs`, `Identity/RolePermission.cs`, `Identity/UserRole.cs`, `Identity/UserWarehouse.cs`, `Identity/RefreshTokenSession.cs`, `Identity/CanonicalRolesAndPermissions.cs`
- **Backend Application (`AlMomiaa.Application`):** `Authentication/Contracts/{LoginRequest,RefreshRequest,AuthenticatedUserDto,AuthenticationResponse}.cs`, `Authentication/Abstractions/AuthenticationAbstractions.cs`, `Authentication/Commands/{Login,RefreshSession,Logout}/*` (including `LoginCommandValidator.cs`)
- **Backend Infrastructure (`AlMomiaa.Infrastructure`):** `Persistence/AlMomiaaDbContext.cs`, `Persistence/AlMomiaaDbContextFactory.cs`, `Persistence/IdentitySeedData.cs`, `Persistence/PipelineServices.cs`, `Persistence/Configurations/Identity/IdentityConfigurations.cs`, `Persistence/Migrations/20261123063607_InitialIdentityAndRefreshSessions.*`, `Authentication/{AuthenticationService,JwtTokenService,SecurityServices}.cs`, `DependencyInjection.cs`
- **Backend WebApi (`AlMomiaa.WebApi`):** `Controllers/AuthController.cs`, `Middleware/SecurityStampMiddleware.cs`, `Security/TrustedOriginValidator.cs`, `Program.cs`, `appsettings.json`
- **Backend Tests:** `tests/AlMomiaa.AuthenticationTests/{AlMomiaa.AuthenticationTests.csproj,TestHarness.cs,AuthenticationEndToEndTests.cs}`, `AlMomiaa.Erp.sln`
- **Shared API Client (`packages/api-client`):** `package.json`, `tsconfig.json`, `src/index.ts`, `src/auth/{auth.types,auth.schemas,access-token-store,auth.service}.ts`, `src/http/axios.ts`, `tests/auth-client.test.mjs`
- **Design System (`packages/ui-kit` & `apps/design-system-studio`):** `src/components/forms/{TextField,PasswordField}.tsx`, `src/components/layout/ResponsiveAppShell.tsx` (`sidebarFooter`), `src/index.ts`, `src/stories/{TextField,PasswordField}.stories.tsx`
- **Web ERP (`apps/web-erp`):** `package.json`, `next.config.ts`, `.env.example`, `src/lib/api-client.ts`, `src/providers/AuthProvider.tsx`, `src/features/auth/LoginForm.tsx`, `src/app/login/{page,loading}.tsx`, `src/app/{layout,page}.tsx`, `src/locales/{ar,en}.ts`
- **Architecture Documentation:** `docs/architecture/05-Database.md`, `docs/architecture/06-API-Specification.md`, `docs/architecture/07-Security.md`

## 4. Acceptance Criteria (Definition of Done)

- [x] EF Core identity mappings and migration align with `[id].[Users]`, `[id].[Roles]`, `[id].[RolePermissions]`, `[id].[UserRoles]`, and `[id].[UserWarehouses]` in `05-Database.md`.
- [x] Refresh-session persistence stores only token hashes, supports atomic rotation and replay detection, is concurrency-safe, and has the documented maximum lifetime.
- [x] `POST /api/v1/auth/login`, `/refresh`, and `/logout` match the documented contracts, OpenAPI output, cookie semantics, and public authorization metadata.
- [x] JWT has the required 10-minute lifetime and identity/role/effective-permission/warehouse/security-stamp context; permissions returned to the frontend are based on assigned roles, not client input.
- [x] Canonical role and permission seed data is idempotent and creates no default production user or password.
- [x] Redis security-stamp validation and invalidation are implemented; no default signing key, production credential, or wildcard credentialed CORS is committed.
- [x] Wrong password, unknown user, and inactive user do not reveal account existence; login throttling/lockout is covered by tests.
- [x] Backend unit/integration tests cover valid and invalid login, phone and employee-code lookup, inactive accounts, token claims, cookie flags, refresh rotation, replay, expiry, logout revocation, and security-stamp invalidation.
- [x] OpenAPI, C# DTOs, TypeScript interfaces, and Zod schemas agree; refresh credentials never appear in JSON.
- [x] Web login page is usable in Arabic RTL and English LTR, light/dark, keyboard-only, and narrow/mobile viewport; validation, pending, and generic error states are translated and accessible.
- [x] Access tokens exist only in memory in the frontend; refresh is cookie-based and refresh interceptor is single-flight with no retry loop.
- [x] Existing ERP shell is unavailable to unauthenticated users through UX session bootstrap, while backend remains the actual authorization boundary.
- [x] `@almomiaa/ui` form controls have individual files and corresponding RTL/LTR Storybook stories; no app-local duplicate visual controls or hardcoded app colors are introduced.
- [x] Backend build/tests, API-client typecheck/lint/tests, web-erp typecheck/lint/build, relevant Storybook build, and `@QA & Security Auditor` all pass.
- [x] `05-Database.md`, `06-API-Specification.md`, and `07-Security.md` are updated only for the authentication behavior implemented.
- [x] No public registration, password reset/MFA, user administration UI, or unrelated ERP workflow is added.
- [x] Archived as `docs/agent-ops/prompt-history/0003-user-authentication-and-login.md`, `PROJECT_LOG.md` updated, and `CURRENT_PROMPT.md` marked completed.

## 5. Verification Summary

- **Backend:** `dotnet build AlMomiaa.Erp.sln` passed with **0 warnings, 0 errors**; `dotnet test` (with `DOTNET_ROLL_FORWARD=Major`) passed **11/11 tests** (`AlMomiaa.ArchitectureTests`: 1/1; `AlMomiaa.AuthenticationTests`: 10/10 covering login by employee code/phone, generic `401` anti-enumeration, `429` throttling, 10m JWT claims, hardened `Set-Cookie` attributes & hash-only storage, atomic refresh rotation & replay family revocation, 14-day expiry, idempotent `204` logout, Redis `SecurityStamp` invalidation & fail-closed `503`, untrusted cross-site `Origin` `403`, canonical RBAC seed idempotency with 0 users, and `/openapi/v1.json`).
- **API Client (`@almomiaa/api-client`):** `build`, `typecheck`, `lint`, and `test` (**4/4 tests passing**: Zod strict schema validation + refreshToken rejection, in-memory token store, single-flight concurrent 401 refresh coalescing, terminal failure cleanup with no retry loops) all passed.
- **Design System & Storybook:** `@almomiaa/ui` build/typecheck/lint and Storybook 8 static build passed with 13/13 components paired with RTL/LTR CSF3 stories (including `TextField` and `PasswordField`).
- **Web ERP (`@almomiaa/web-erp`):** `typecheck`, `lint`, and Next.js 15 production build (`/` and `/login`) passed with 0 warnings/errors.
- **QA & Security Gate (`PASS`):** Zero hardcoded hex colors in `web-erp`, zero forbidden directional classes, zero browser-storage token persistence, zero forbidden imports in `ui-kit`, zero `any`/`@ts-ignore`, zero committed secrets/default signing keys/wildcard credentialed CORS, and zero out-of-scope auth flows.
