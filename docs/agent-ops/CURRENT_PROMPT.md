# PROMPT-0001: Scaffold Frontend and Backend App Structures

- **Status:** `READY_FOR_ORCHESTRATOR`
- **Prompt ID:** `PROMPT-0001`
- **Scope:** `apps/web-erp` and `apps/backend-api` only
- **Reference baseline:** Architecture v3.1 (`docs/architecture/00-ADR-Architecture-Decisions.md`)

## 1. Purpose of Prompt

Create minimal, buildable application scaffolds and clear, extensible folder structures for the primary web ERP frontend and the .NET backend API. This is an application-structure task, not a feature implementation.

Follow the canonical monorepo boundaries and technology choices:

- `apps/web-erp`: Next.js 15+ App Router, React 19, strict TypeScript, and Tailwind CSS.
- `apps/backend-api`: .NET 9 / C# 13 Clean Architecture Modular Monolith, with the `AlMomiaa.*` namespace.

Do not add business workflows, domain entities, database tables or migrations, API feature endpoints, authentication flows, or production integrations.

## 2. Scope and Target Execution Agents

1. `@Frontend Engineer` and `@Backend .NET` — scaffold their respective apps independently; the Orchestrator may run these tasks in parallel.
2. `@QA & Security Auditor` — verify the resulting structure, project references, and build/test results.
3. `@Orchestrator` — coordinate the work, resolve cross-app conflicts without expanding scope, and update the project log and prompt history after successful execution.

## 3. Target Files to Create or Modify

Only create or modify files beneath `apps/web-erp/` and `apps/backend-api/`. Inspect existing files first and preserve compatible work. Do not overwrite or delete existing user content.

### `apps/web-erp/`

Create a minimal, runnable Next.js app with:

- App-level `package.json`, TypeScript configuration, Next.js configuration, and lint/style configuration compatible with the versions and conventions already present in the repository.
- `src/app/layout.tsx`, `src/app/page.tsx`, and `src/app/globals.css` as the App Router entry structure.
- A concise, non-business landing page sufficient to verify the app starts.
- An extensible `src/` layout separating app routes, app-specific components, feature code, and app utilities. Keep the structure lean; do not add empty folders unless a short `README.md` is needed to explain a reserved boundary.
- A `public/` directory only if an actual scaffold asset requires it.

Set the initial document locale and direction to Arabic (`ar-SA`, RTL). Keep layout CSS direction-safe and use logical CSS properties/utilities. Do not implement locale switching.

### `apps/backend-api/`

Create a minimal, buildable .NET 9 solution and Clean Architecture project structure:

- `AlMomiaa.Erp.sln` and app-local build configuration as needed.
- `src/AlMomiaa.Domain/`
- `src/AlMomiaa.Application/`
- `src/AlMomiaa.Infrastructure/`
- `src/AlMomiaa.WebApi/`
- `tests/` with the smallest practical test project needed to verify the scaffold.

Use explicit project references that preserve dependency direction: Application may depend on Domain; Infrastructure may depend on Application and Domain; WebApi may compose Application and Infrastructure. Keep Domain independent of infrastructure and web frameworks.

Include only the minimal entrypoint, project files, and dependency-registration code needed to build and start the API. A basic health/readiness response is acceptable as a scaffold check; do not implement business endpoints. Use `AlMomiaa` namespaces, nullable reference types, and the repository's existing formatting conventions.

## 4. Frontend and Backend Data Schema

No business data schemas, DTOs, database entities, EF Core mappings, or migrations are in scope. Use framework defaults only where needed to make each application scaffold buildable. Do not introduce placeholder business contracts.

## 5. Security, Domain, RTL, and Offline Rules

- This prompt creates no authenticated or business API endpoints; do not invent permission claims or bypass authorization requirements for future endpoints.
- Keep authorization enforcement server-side when business endpoints are later added. Frontend route/UI structure must not be presented as an authorization boundary.
- Do not add credentials, secrets, environment-specific URLs, database connections, Redis, SignalR, Hangfire, or ZATCA configuration.
- Keep the web app RTL-first (`ar-SA`) and avoid directional left/right CSS conventions.
- `rep-pwa` and offline functionality are explicitly out of scope.

## 6. Design System and Shared Package Boundaries

- Do not create or modify `packages/*`, `apps/design-system-studio`, or root workspace configuration.
- Do not duplicate design tokens or introduce ad-hoc hex colors.
- If `@almomiaa/ui` already exists and is usable without changing out-of-scope files, reuse it; otherwise keep the landing scaffold dependency-light and do not create substitute shared components or tokens.
- Do not add API-client or shared-types packages as part of this prompt.

## 7. Acceptance Criteria (Definition of Done)

- [ ] Only files under `apps/web-erp/` and `apps/backend-api/` are added or changed.
- [ ] `apps/web-erp` has a coherent App Router structure, a minimal landing page, Arabic RTL document defaults, and app-local configuration.
- [ ] `apps/backend-api` contains a .NET 9 solution with the four canonical projects and a test project; project references follow Clean Architecture dependency direction.
- [ ] Both apps contain only scaffolding and no business features, database schema changes, or production integrations.
- [ ] Frontend lint/typecheck/build and backend build/tests pass using the available repository tooling; report any missing root prerequisites rather than modifying files outside the agreed scope.
- [ ] No secrets, hardcoded environment URLs, ad-hoc hex colors, or directional left/right layout utilities are introduced.
- [ ] `@QA & Security Auditor` reports the scope, structure, and validation results.
- [ ] After execution succeeds, `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0001-scaffold-frontend-backend-app-structures.md` and updates `docs/agent-ops/PROJECT_LOG.md`. Do not archive or mark the prompt completed before implementation and validation.
