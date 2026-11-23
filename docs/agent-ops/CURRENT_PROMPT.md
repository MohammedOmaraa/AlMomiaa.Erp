# PROMPT-0003: Implement User Authentication, Session Persistence, and Login UI

- **Status:** `COMPLETED (Archived as 0003-user-authentication-and-login.md)`
- **Prompt ID:** `PROMPT-0003`
- **Scope:** `apps/backend-api`, `packages/api-client`, `packages/ui-kit` (auth form controls and stories), `apps/web-erp`, and synchronized auth documentation
- **Reference baseline:** Architecture v3.1; `PROMPT-0001` and `PROMPT-0002` completed
- **Confirmed decisions:** Login + refresh + logout; login by phone or employee code; no public registration; map the canonical identity schema and add refresh-session persistence; implement API + web login design + shared typed client/Zod contracts.

## 1. Purpose

Implement a secure, end-to-end authentication foundation for the existing AlMomiaa ERP:

- Persist canonical users, roles, permissions, and warehouse assignments in SQL Server through EF Core.
- Implement login, refresh, and logout endpoints in the .NET 9 Web API.
- Define precise backend request/response contracts and share their TypeScript equivalents with the web frontend through `@almomiaa/api-client`, using OpenAPI-aligned types and Zod validation.
- Build a responsive, bilingual Arabic/English login experience consistent with the existing AlMomiaa UI design system.
- Bootstrap the existing dashboard shell from the authenticated session without treating frontend navigation as an authorization boundary.

This prompt does not include public registration, forgot/reset password, MFA/OTP, user administration screens/endpoints, or any unrelated ERP workflow.

## 2. Architectural References and Existing Schema

Before implementation, read and obey:

- `docs/architecture/00-ADR-Architecture-Decisions.md`
- `docs/architecture/05-Database.md`
- `docs/architecture/06-API-Specification.md`
- `docs/architecture/07-Security.md`
- `.github/skills/cqrs-efcore-patterns/SKILL.md`
- `.github/skills/zatca-phase2-compliance/SKILL.md` only where shared backend conventions apply
- `.github/skills/design-token-governance/SKILL.md`

`05-Database.md` already defines `[id].[Users]`, `[id].[Roles]`, `[id].[RolePermissions]`, `[id].[UserRoles]`, and `[id].[UserWarehouses]`. Map these existing contracts instead of inventing a parallel user schema. Add only the session persistence needed for secure rotating refresh tokens, and update the database/API/security docs to match the implemented authentication contract.

Preserve the mandated security semantics:

- Access JWT lifetime: 10 minutes.
- Refresh-session maximum lifetime: 14 days; rotate the refresh token on every successful refresh.
- Web refresh token: opaque, host-only `HttpOnly`, `Secure`, `SameSite=Strict` cookie; never return it in JSON or expose it to JavaScript.
- JWT context: `UserId`, `RoleCodes`, `RepType`, `DefaultWarehouseId`, `AssignedWarehouseIds`, `SecurityStamp`, and effective permission claims required by explicit API policies. Include the same effective role/permission/warehouse context in the authenticated session response.
- Redis security-stamp validation and immediate revocation per `07-Security.md`.
- Every future protected API endpoint continues to enforce server-side policies and row-level scoping.

## 3. Target Execution Agents and Order

1. **`@Backend .NET`** — implement canonical identity persistence, refresh-session persistence, authentication application services/handlers, JWT and refresh-cookie behavior, `AuthController`, and backend tests.
2. **`@Design System UI`** — add only the reusable form controls required by login (for example `TextField` and `PasswordField`) to `packages/ui-kit`, with accessible props, tokens, and CSF3 stories.
3. **`@Frontend Engineer`** — add request/response contracts and authenticated Axios session handling in `packages/api-client`, then implement the bilingual login page and session bootstrap in `apps/web-erp`.
4. **`@QA & Security Auditor`** — audit API authorization/cookie/session security, schema alignment, contract consistency, accessibility, and all acceptance criteria. Fixes must be routed to the owning specialist and re-audited.
5. **`@Orchestrator`** — coordinate handoffs and archive/log only after QA returns `PASS`.

The Backend agent must publish the endpoint and OpenAPI contract before the Frontend agent finalizes client contracts. The Design System task may run in parallel with backend implementation.

## 4. Database and Backend Implementation

### 4.1 Identity persistence

Create EF Core entities/configurations and migrations aligned to the existing `[id]` DDL:

- `id.Users`: UUIDv7 primary key; unique employee code and phone; Arabic/English names; optional email; password hash; rep type; default warehouse; security stamp; active flag; created UTC timestamp.
- `id.Roles` and `id.RolePermissions`: canonical role codes and permission claim names.
- `id.UserRoles`: user-role join.
- `id.UserWarehouses`: assigned warehouse join, maintaining the existing warehouse FK relationship when the inventory schema is present.
- Seed the canonical role codes and permission claims from `07-Security.md` idempotently; never seed a production user or password.

Do not silently rename columns, remove constraints, or duplicate role/permission data in a new schema. Use EF Core configurations and `.AsNoTracking()` for read projections. Keep the Domain independent of EF Core and ASP.NET Core.

### 4.2 Refresh-session persistence

Add a narrowly scoped table/entity such as `[id].[RefreshTokenSessions]` for rotation and revocation. It must:

- Store only a cryptographic hash of each opaque random refresh token, never the raw token.
- Associate sessions with a user and a session/family identifier; record issued, expiry, revoked timestamps and replacement/revocation relationships.
- Enforce uniqueness for token hashes and add indexes supporting active-session lookup and user revocation.
- Include optimistic concurrency protection for rotation/revocation.
- Make rotation atomic. Detect reuse of a rotated/revoked token and revoke the affected session family; never mint a second valid token from the same refresh token.
- Enforce a maximum 14-day session lifetime and bind each cookie token to its server-side session.

Use secure cryptographic randomness and a constant-time hash comparison. Keep secrets/keys out of source control and migrations.

### 4.3 Backend files to create/modify

At minimum, place code within these existing Clean Architecture projects:

- `apps/backend-api/src/AlMomiaa.Domain/Identity/User.cs`
- `apps/backend-api/src/AlMomiaa.Domain/Identity/RefreshTokenSession.cs`
- `apps/backend-api/src/AlMomiaa.Application/Authentication/Contracts/LoginRequest.cs`
- `apps/backend-api/src/AlMomiaa.Application/Authentication/Contracts/AuthenticationResponse.cs`
- `apps/backend-api/src/AlMomiaa.Application/Authentication/Contracts/AuthenticatedUserDto.cs`
- `apps/backend-api/src/AlMomiaa.Application/Authentication/Contracts/RefreshRequest.cs` (empty body or no-body contract; refresh credential comes only from cookie)
- `apps/backend-api/src/AlMomiaa.Application/Authentication/Commands/Login/*`
- `apps/backend-api/src/AlMomiaa.Application/Authentication/Commands/RefreshSession/*`
- `apps/backend-api/src/AlMomiaa.Application/Authentication/Commands/Logout/*`
- `apps/backend-api/src/AlMomiaa.Application/Authentication/Abstractions/`
- `apps/backend-api/src/AlMomiaa.Infrastructure/Persistence/AlMomiaaDbContext.cs`
- `apps/backend-api/src/AlMomiaa.Infrastructure/Persistence/Configurations/Identity/*`
- `apps/backend-api/src/AlMomiaa.Infrastructure/Persistence/Migrations/*`
- `apps/backend-api/src/AlMomiaa.Infrastructure/Authentication/`
- `apps/backend-api/src/AlMomiaa.Infrastructure/DependencyInjection.cs`
- `apps/backend-api/src/AlMomiaa.WebApi/Controllers/AuthController.cs`
- `apps/backend-api/src/AlMomiaa.WebApi/Program.cs`
- `apps/backend-api/src/AlMomiaa.WebApi/appsettings.json` only for non-secret safe defaults; secrets and environment values come from configuration/environment.
- `apps/backend-api/tests/` authentication unit/architecture/integration tests in the existing test project or a narrowly scoped new test project.

The exact implementation may add small, directly related files (for example entity configurations, validators, options, or migration designer files) under these same projects.

### 4.4 Security behavior

- Hash passwords with a framework-maintained secure password hasher; never store or log plaintext passwords.
- Validate the login identifier and password; normalize phone and employee-code lookups consistently with uniqueness constraints.
- Return the same generic unauthorized response for unknown identifier, incorrect password, and inactive account; do not reveal which condition failed.
- Apply bounded rate limiting/lockout to login attempts, keyed by appropriate client and identifier signals. Avoid permanent lockout denial-of-service; document the configured policy.
- Configure and validate JWT issuer, audience, signing key, and expiry from configuration; fail safely when required production security configuration is absent. Never ship a default signing secret.
- Implement Redis-backed `SecurityStamp` validation using `user:stamp:{userId}` and ensure user deactivation or stamp rotation invalidates active access tokens promptly.
- Use explicit authorization metadata: login, refresh, and logout are intentionally public endpoints; all new non-auth endpoints remain protected by explicit permission policies.
- Return RFC 7807 `ProblemDetails` errors without leaking passwords, token values, hashes, or user existence.
- Configure credentialed CORS only for explicitly configured trusted frontend origins; never combine credentials with wildcard origins.
- For the refresh cookie, set `HttpOnly`, `Secure`, `SameSite=Strict`, omit the `Domain` attribute, use the narrow `/api/v1/auth` path, and set the 14-day maximum expiry. Clear it on logout and failed/replayed refresh. Do not weaken production cookie policy for local HTTP.
- Protect cookie-authenticated refresh/logout from cross-site requests with strict configured-origin validation (and an explicit anti-CSRF mechanism if the deployment topology requires it); never allow arbitrary origins with credentials.
- Fail closed for protected requests if Redis security-stamp validation is unavailable; return an explicit service error rather than accepting a token without revocation verification.

## 5. Canonical HTTP Contracts

All routes use the existing `/api/v1` prefix and match `06-API-Specification.md`.

### `POST /api/v1/auth/login` — public

Request JSON:

```json
{
  "identifier": "employee-code-or-phone",
  "password": "user-supplied-password"
}
```

Response JSON (refresh token is set only in `Set-Cookie`):

```json
{
  "accessToken": "<short-lived-jwt>",
  "tokenType": "Bearer",
  "expiresIn": 600,
  "user": {
    "id": "01933c90-9999-7000-8000-abcdef123456",
    "employeeCode": "EMP-0001",
    "fullNameAr": "اسم المستخدم",
    "fullNameEn": "User Name",
    "roleCodes": ["SALES_MGR"],
    "permissions": ["orders:approve"],
    "repType": null,
    "defaultWarehouseId": null,
    "assignedWarehouseIds": []
  }
}
```

### `POST /api/v1/auth/refresh` — public, cookie-authenticated

- Request body is empty; authenticate only from the refresh cookie.
- Atomically rotate the refresh session and set a new cookie.
- Return the same access-session response contract as login.
- On invalid, expired, revoked, replayed, inactive-user, or security-stamp-invalid sessions: clear the cookie and return a safe RFC 7807 response.

### `POST /api/v1/auth/logout` — public, cookie-authenticated and idempotent

- Revoke the current refresh session/family when a valid cookie is present.
- Always clear the refresh cookie and return `204 No Content`; do not disclose whether the cookie was valid.

Use typed C# request/response DTOs in Application and keep the Controller thin. Publish the exact request, response, cookie, and error contracts through the Web API OpenAPI document.

## 6. Shared Frontend Contract and API Client

Create `packages/api-client` as the frontend’s only authentication HTTP boundary:

- `packages/api-client/package.json`, strict `tsconfig.json`, and public `src/index.ts`.
- `packages/api-client/src/auth/auth.types.ts` — TypeScript interfaces/types matching backend DTOs.
- `packages/api-client/src/auth/auth.schemas.ts` — Zod request/response validation schemas.
- `packages/api-client/src/auth/auth.service.ts` — login/refresh/logout API operations.
- `packages/api-client/src/http/axios.ts` — configured Axios instance, credentials support, cancellation, and base URL from environment/configuration (no hard-coded deployment URL).
- `packages/api-client/src/auth/access-token-store.ts` — in-memory-only access token storage.
- Refresh interceptor must single-flight concurrent refresh requests, prevent refresh loops, clear in-memory access state on terminal failure, and propagate errors explicitly.

Prefer OpenAPI-generated request/response types when compatible with the existing API codegen path; ensure Zod runtime schemas validate the same shapes. Do not return or store refresh tokens in frontend contracts.

### Shared interface shape

The frontend contract must expose an equivalent typed shape:

```ts
export interface LoginRequest {
  identifier: string;
  password: string;
}

export interface AuthenticatedUser {
  id: string;
  employeeCode: string;
  fullNameAr: string;
  fullNameEn: string | null;
  roleCodes: string[];
  permissions: string[];
  repType: string | null;
  defaultWarehouseId: number | null;
  assignedWarehouseIds: number[];
}

export interface AuthenticationResponse {
  accessToken: string;
  tokenType: "Bearer";
  expiresIn: number;
  user: AuthenticatedUser;
}
```

Keep C# DTO JSON property names and TypeScript/Zod fields synchronized and verified against OpenAPI. Refresh and logout have no token-bearing request DTO fields.

## 7. Web Login and Session UX

### Files to create/modify

- `apps/web-erp/src/app/login/page.tsx`
- `apps/web-erp/src/app/login/loading.tsx` if required for a stable route transition
- `apps/web-erp/src/features/auth/LoginForm.tsx`
- `apps/web-erp/src/providers/AuthProvider.tsx`
- `apps/web-erp/src/app/layout.tsx` and existing providers only as needed
- `apps/web-erp/src/locales/ar.ts`
- `apps/web-erp/src/locales/en.ts`
- `apps/web-erp/src/app/globals.css` only if token-based styling requires an update
- `apps/web-erp/.env.example` with a non-secret API base URL placeholder only if required by the client configuration
- `packages/ui-kit/src/components/forms/TextField.tsx`
- `packages/ui-kit/src/components/forms/PasswordField.tsx`
- exports from `packages/ui-kit/src/index.ts`
- corresponding Storybook CSF3 stories for each new public form component

### Login design requirements

- Follow the existing AlMomiaa shell tokens, typography, theme, and locale providers.
- Default to `ar-SA` / RTL; support `en-US` / LTR, light and dark themes.
- Display the supplied AlMomiaa identity and the exact Arabic brand name `المومياء`; do not modify/crop the logo asset.
- Provide accessible identifier and password inputs, show/hide password control, submit button, pending state, field validation, and generic authentication failure message.
- Login identifier label/help text must clearly support phone number or employee code.
- Use semantic form controls, visible keyboard focus, associated labels, `aria-invalid`/error descriptions, and minimum 44px interactive targets.
- Do not add a public registration link or forgot-password UI because those flows are out of scope.
- On successful authentication, store the access token only in memory and enter the existing ERP shell. On initial app boot, attempt cookie-based refresh before showing protected shell content. On refresh failure, clear auth state and route to `/login`.
- Logout invokes the API, clears in-memory access state, and returns to `/login`. Frontend route protection is UX only; every protected API request must be authorized by the backend.
- Translate all UI labels and authentication errors. Do not display raw backend exception details.
- No real user names or business records in sample UI; use the authenticated user DTO or neutral placeholders only.

### Session handling rules

- Never put access or refresh tokens in `localStorage`, `sessionStorage`, IndexedDB, URLs, analytics, console output, or error logs.
- Use Axios `withCredentials` for refresh/logout so the browser sends the HttpOnly cookie; configure backend CORS accordingly.
- Keep access JWT in memory only. Re-establish it after reload through refresh cookie.
- A `401` may trigger at most one coordinated refresh/retry; logout or clear session on terminal failure.
- Do not claim that a frontend route guard provides authorization.

## 8. Documentation Changes

Update the architecture documentation in the same change:

- `docs/architecture/05-Database.md`: add the refresh-session table and indexes, preserving the existing canonical identity DDL.
- `docs/architecture/06-API-Specification.md`: document exact login/refresh/logout request/response shapes, cookie attributes, status/error behavior, and OpenAPI contracts.
- `docs/architecture/07-Security.md`: document password hashing, generic errors, rate limits, refresh rotation/replay revocation, cookie policy, access-token storage, and CORS requirements.

Do not change unrelated domain schemas, endpoints, or architectural decisions.

## 9. Acceptance Criteria (Definition of Done)

- [ ] EF Core identity mappings and migration align with `[id].[Users]`, `[id].[Roles]`, `[id].[RolePermissions]`, `[id].[UserRoles]`, and `[id].[UserWarehouses]` in `05-Database.md`.
- [ ] Refresh-session persistence stores only token hashes, supports atomic rotation and replay detection, is concurrency-safe, and has the documented maximum lifetime.
- [ ] `POST /api/v1/auth/login`, `/refresh`, and `/logout` match the documented contracts, OpenAPI output, cookie semantics, and public authorization metadata.
- [ ] JWT has the required 10-minute lifetime and identity/role/effective-permission/warehouse/security-stamp context; permissions returned to the frontend are based on assigned roles, not client input.
- [ ] Canonical role and permission seed data is idempotent and creates no default production user or password.
- [ ] Redis security-stamp validation and invalidation are implemented; no default signing key, production credential, or wildcard credentialed CORS is committed.
- [ ] Wrong password, unknown user, and inactive user do not reveal account existence; login throttling/lockout is covered by tests.
- [ ] Backend unit/integration tests cover valid and invalid login, phone and employee-code lookup, inactive accounts, token claims, cookie flags, refresh rotation, replay, expiry, logout revocation, and security-stamp invalidation.
- [ ] OpenAPI, C# DTOs, TypeScript interfaces, and Zod schemas agree; refresh credentials never appear in JSON.
- [ ] Web login page is usable in Arabic RTL and English LTR, light/dark, keyboard-only, and narrow/mobile viewport; validation, pending, and generic error states are translated and accessible.
- [ ] Access tokens exist only in memory in the frontend; refresh is cookie-based and refresh interceptor is single-flight with no retry loop.
- [ ] Existing ERP shell is unavailable to unauthenticated users through UX session bootstrap, while backend remains the actual authorization boundary.
- [ ] `@almomiaa/ui` form controls have individual files and corresponding RTL/LTR Storybook stories; no app-local duplicate visual controls or hardcoded app colors are introduced.
- [ ] Backend build/tests, API-client typecheck/lint/tests, web-erp typecheck/lint/build, relevant Storybook build, and `@QA & Security Auditor` all pass.
- [ ] `05-Database.md`, `06-API-Specification.md`, and `07-Security.md` are updated only for the authentication behavior implemented.
- [ ] No public registration, password reset/MFA, user administration UI, or unrelated ERP workflow is added.
- [ ] After QA returns `PASS`, archive as `docs/agent-ops/prompt-history/0003-user-authentication-and-login.md`, update `docs/agent-ops/PROJECT_LOG.md`, and mark this prompt completed. Do not archive before validation.
