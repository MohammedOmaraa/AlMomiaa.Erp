# PROMPT-0012: Fix Stuck Session/Navigation & Error Handling in `web-erp` & `rep-pwa` + End-to-End Live Verification Suite

- **Status:** `COMPLETED — ARCHIVED (docs/agent-ops/prompt-history/0012-fix-stuck-session-error-handling-and-live-verification.md)`
- **Prompt ID:** `PROMPT-0012`
- **Scope:** `apps/web-erp` (fix `AuthProvider` React remount deadlock & bootstrap timeout/error recovery, guard all 9–10 ERP workspaces against firing API calls before `status === "authenticated"`, add Next.js `error.tsx` & `global-error.tsx` boundaries, add slow-connection/error recovery & fallback `/login` CTA on `ErpAppLayout` session gate, add `AbortSignal.timeout` & clean error diagnostics to `backend-proxy.ts`), `apps/rep-pwa` (add React `ErrorBoundary`, bootstrap timeout/retry/login recovery UI, and `AbortSignal.timeout` on `api/[...path].ts`), `scripts/verify-deployed-apps.mjs` (zero-secret end-to-end live verification script testing Backend, Web ERP, and Rep PWA end-to-end), and automated verification of both local builds and live deployed apps (`https://almomiaa-erp.onrender.com`, `https://almomiaa-erp.vercel.app`, `https://almomiaa-rep.vercel.app`).
- **Reference baseline:** Architecture v3.2 (`00-ADR`..`08-Development-Roadmap`); `PROMPT-0001`..`PROMPT-0011` completed (`PROJECT_LOG.md` Next Prompt Number: `0012`).

---

## 1. Purpose of Prompt (Root-Cause Analysis & Objective)

Resolve all production defects causing `https://almomiaa-erp.vercel.app/` to stay frozen on the root/session-check screen or fail to navigate/recover from backend errors, harden error handling across both `apps/web-erp` and `apps/rep-pwa`, and verify end-to-end that **both deployed apps work cleanly**:

### Root Causes Identified in Codebase Audit
1. **`AuthProvider` Remount Deadlock (`apps/web-erp/src/providers/AuthProvider.tsx`):**
   - `bootstrapStartedRef.current` is set to `true` on mount #1, cleanup sets `active = false`, and on React 19 Strict Mode / hydration remount #2 `if (bootstrapStartedRef.current) return;` exits early — so when `authService.refresh()` settles, `active` is `false` and `setStatus(...)` is **never called**, freezing the app permanently on `status === "loading"` (`جارٍ التحقق من الجلسة...`)!
2. **Unauthenticated API Race Across All ERP Workspaces (`apps/web-erp/src/features/*/*.tsx`):**
   - All 9 workspaces (`Dashboard`, `Users`, `Customers`, `Catalog`, `Inventory`, `Purchasing`, `Sales`, `Finance`, `Reports` — plus check `GpsTrackingWorkspace`) call `loadData()` in `useEffect` immediately on mount without checking `status === "authenticated"`, firing unauthenticated `GET` requests while `AuthProvider` is still bootstrapping -> triggering concurrent `401` refresh failures and unexpected redirects/errors!
3. **Missing Global/Route Error Boundaries & Unrecoverable Session-Check Screen:**
   - `apps/web-erp` has no `src/app/error.tsx` or `src/app/global-error.tsx`, and `apps/rep-pwa` has no top-level React `ErrorBoundary` — uncaught runtime or chunk errors freeze the UI without recovery options.
   - `ErpAppLayout.tsx` shows a bare `جارٍ التحقق من الجلسة...` card with no elapsed-time warning (Render free-tier cold start), no error message display, no **Retry** button, and no direct **Go to Login** fallback link when `status === "loading"` takes too long or `status === "unauthenticated"` stalls.
4. **Missing Upstream Timeouts in Vercel Proxy Handlers:**
   - `apps/web-erp/src/lib/backend-proxy.ts` and `apps/rep-pwa/api/[...path].ts` call `fetch(upstreamUrl, ...)` without `AbortSignal.timeout(25_000)`, risking silent Vercel 504 HTML timeouts instead of clean `504` RFC 7807 `ProblemDetails` JSON when the upstream Render container is cold-starting or unreachable.
5. **No One-Command Verification Script for Deployed Apps:**
   - Need an automated, secret-free verification script (`scripts/verify-deployed-apps.mjs`) that validates the live Backend (`/health`), Web ERP (`/` HTML, `/login`, `/users`, `/catalog`, `/customers`, `/manifest.webmanifest`, same-origin `/health` proxy, optional login+refresh+cookie smoke check via CLI env vars), and Rep PWA (`/` HTML, `/manifest.webmanifest`, `/sw.js`, same-origin `/health` proxy) so both apps are provably working end-to-end.

---

## 2. Target Execution Agents and Order

1. **`@Frontend Engineer`** —
   - **A. Fix `AuthProvider` Deadlock & Expose Session Recovery API (`apps/web-erp/src/providers/AuthProvider.tsx`):**
     - Remove the broken `bootstrapStartedRef` + `active = false` deadlock pattern: always track `mountedRef` (reset to `true` on every mount, `false` on unmount), share a single in-flight bootstrap promise or call `authService.refresh()` safely so `setStatus("authenticated" | "unauthenticated")` ALWAYS executes on the active mounted instance!
     - Track `bootstrapError: string | null` and `slowBootstrap: boolean` (flips `true` after 5 seconds while still `"loading"`, informing the user that the server may be waking up) and expose `retrySession: () => Promise<void>` on `AuthContextValue`.
     - Use a bounded bootstrap timeout (e.g., 15 seconds on initial session check; if `/auth/refresh` times out or returns `502`/`503`/`504`, surface a clean bilingual message + Retry + Go to Login instead of silently looping or freezing!).
   - **B. Harden `ErpAppLayout` Session Gate & Navigation (`apps/web-erp/src/components/ErpAppLayout.tsx`):**
     - When `status === "loading"`: show `جارٍ التحقق من الجلسة...`; if `slowBootstrap` is `true` (after 5s), show a helpful Render cold-start hint + **إعادة المحاولة / Retry** button + **الذهاب لتسجيل الدخول / Go to Login** `<Link href="/login">`.
     - When `status === "unauthenticated"`: trigger `router.replace("/login")` AND render a clean fallback card with an explicit **تسجيل الدخول / Sign In** button (so even if a router transition stalls, the user is never trapped on `/`).
     - Ensure every sidebar navigation item navigates reliably via Next.js `<Link>` / `router.push` without freezing or dropping auth state, and verify `pathname` active state matching works for all 10 routes (`/`, `/users`, `/customers`, `/catalog`, `/inventory`, `/purchasing`, `/sales`, `/finance`, `/gps`, `/reports`).
   - **C. Guard All Workspaces Against Pre-Auth API Calls (`apps/web-erp/src/features/*/*.tsx`):**
     - Inspect all 10 workspaces (`DashboardWorkspace`, `UsersWorkspace`, `CustomersWorkspace`, `CatalogWorkspace`, `InventoryWorkspace`, `PurchasingWorkspace`, `SalesWorkspace`, `FinanceWorkspace`, `GpsTrackingWorkspace`, `ReportsWorkspace`).
     - In every workspace's `useEffect` data loader, check `if (status !== "authenticated" || !user) return;` so ZERO protected API requests fire before `AuthProvider` establishes the in-memory Bearer token!
   - **D. Add Next.js Error Boundaries (`apps/web-erp/src/app/error.tsx` & `apps/web-erp/src/app/global-error.tsx`):**
     - Bilingual RTL/LTR tokenized error recovery UI (zero hardcoded hex colors, zero directional Tailwind classes) with **إعادة المحاولة / Try Again** (`reset()`), **العودة للرئيسية / Dashboard**, and **تسجيل الدخول / Sign In** buttons.
   - **E. Harden Vercel Same-Origin Proxies (`apps/web-erp/src/lib/backend-proxy.ts` & `apps/rep-pwa/api/[...path].ts`):**
     - Add `signal: AbortSignal.timeout(25_000)` to upstream `fetch` calls.
     - Differentiate `TimeoutError`/`AbortError` (`504 Gateway Timeout` ProblemDetails: *"Backend is waking up — please retry in a few seconds"*) from network errors (`502 Bad Gateway` ProblemDetails).
     - Forward `x-forwarded-for` / `x-forwarded-proto` safely while never logging `authorization` or `cookie`.
   - **F. Harden `apps/rep-pwa` Error Handling & Session Recovery (`apps/rep-pwa/src/`):**
     - Add a clean tokenized React `ErrorBoundary` component (`apps/rep-pwa/src/components/AppErrorBoundary.tsx`) wrapping `<AppProviders>` in `apps/rep-pwa/src/main.tsx`.
     - In `apps/rep-pwa/src/providers/AppProviders.tsx` and `apps/rep-pwa/src/App.tsx`: add a 5s slow-connection hint + **Retry** + **Go to Login** fallback during initial session check so `rep-pwa` never freezes on cold starts either!
   - **G. Add Live Verification Script (`scripts/verify-deployed-apps.mjs`):**
     - Node 20+ zero-dependency script accepting URLs via env/CLI args (defaulting to `https://almomiaa-erp.onrender.com`, `https://almomiaa-erp.vercel.app`, `https://almomiaa-rep.vercel.app`) that checks:
       1. Backend `GET /health` -> `200 Healthy`
       2. Web ERP `GET /`, `/login`, `/users`, `/catalog`, `/customers`, `/manifest.webmanifest`, and same-origin proxy `GET /health` -> `200`
       3. Rep PWA `GET /`, `/login`, `/manifest.webmanifest`, `/sw.js`, and same-origin proxy `GET /health` -> `200`
       4. Optional authenticated end-to-end cookie & API check (when `DEMO_IDENTIFIER` & `DEMO_PASSWORD` env vars are passed — NEVER hardcoded!): `POST /api/v1/auth/login` through BOTH Vercel proxies -> verifies `Set-Cookie: almomiaa_refresh` -> `POST /api/v1/auth/refresh` with cookie -> `GET /api/v1/users` & `GET /api/v1/customers` with Bearer token!
     - Add root `package.json` script `"verify:live": "node scripts/verify-deployed-apps.mjs"`.

2. **`@QA & Security Auditor`** —
   - Verify `pnpm.cmd --filter @almomiaa/ui build`, `pnpm.cmd -r typecheck`, `pnpm.cmd -r lint`, `@almomiaa/api-client` tests (`9/9`), `apps/rep-pwa` Vite + Workbox PWA build, `apps/web-erp` Next.js 15 production build, and `dotnet test -c Release` (`33/33`).
   - Run static QA & security audit across all modified files: zero hardcoded hex colors, zero directional Tailwind classes, zero `any`/`@ts-ignore`, zero hardcoded credentials in `scripts/verify-deployed-apps.mjs`, SSRF allowlist intact on both proxies.
   - Run `node scripts/verify-deployed-apps.mjs` (or inspect live endpoints) to report exact live health of `https://almomiaa-erp.onrender.com`, `https://almomiaa-erp.vercel.app`, and `https://almomiaa-rep.vercel.app` (noting clearly if Vercel still needs `git push` to deploy the new commits!).

3. **`@Orchestrator`** — Archive as `docs/agent-ops/prompt-history/0012-fix-stuck-session-error-handling-and-live-verification.md`, update `docs/agent-ops/PROJECT_LOG.md`, and mark `CURRENT_PROMPT.md` completed only after QA returns `PASS`.

---

## 3. Target Files to Create / Modify

### `apps/web-erp/`
- `src/providers/AuthProvider.tsx` (modify — fix remount deadlock, add `slowBootstrap`, `bootstrapError`, `retrySession`)
- `src/components/ErpAppLayout.tsx` (modify — slow-connection/error recovery + Retry + Go to Login fallback on session gate)
- `src/app/error.tsx` (new — Next.js App Router segment error boundary)
- `src/app/global-error.tsx` (new — Next.js App Router global error boundary)
- `src/lib/backend-proxy.ts` (modify — `AbortSignal.timeout(25_000)` + `504` vs `502` ProblemDetails)
- `src/features/dashboard/DashboardWorkspace.tsx` (modify — guard `loadAll` on `status === "authenticated"`)
- `src/features/users/UsersWorkspace.tsx` (modify — guard `loadData` on `status === "authenticated"`)
- `src/features/customers/CustomersWorkspace.tsx` (modify — guard `loadData` on `status === "authenticated"`)
- `src/features/catalog/CatalogWorkspace.tsx` (modify — guard `loadData` on `status === "authenticated"`)
- `src/features/inventory/InventoryWorkspace.tsx` (modify — guard `loadData` on `status === "authenticated"`)
- `src/features/purchasing/PurchasingWorkspace.tsx` (modify — guard `loadData` on `status === "authenticated"`)
- `src/features/sales/SalesWorkspace.tsx` (modify — guard `loadData` on `status === "authenticated"`)
- `src/features/finance/FinanceWorkspace.tsx` (modify — guard `loadData` on `status === "authenticated"`)
- `src/features/gps/GpsTrackingWorkspace.tsx` (verify/modify — guard on `status === "authenticated"`)
- `src/features/reports/ReportsWorkspace.tsx` (modify — guard `loadData` on `status === "authenticated"`)

### `apps/rep-pwa/`
- `src/components/AppErrorBoundary.tsx` (new — tokenized bilingual React ErrorBoundary)
- `src/main.tsx` (modify — wrap app in `AppErrorBoundary`)
- `src/providers/AppProviders.tsx` & `src/App.tsx` (modify — add `slowBootstrap` / `retrySession` / Go to Login recovery UI during initial session check)
- `api/[...path].ts` (modify — `AbortSignal.timeout(25_000)` + `504` vs `502` ProblemDetails)

### Root Scripts
- `scripts/verify-deployed-apps.mjs` (new — zero-secret live deployment smoke verifier for Backend, Web ERP, and Rep PWA)
- `package.json` (modify — add `"verify:live"` script)

---

## 4. Security & Domain Rules

1. **Zero Committed Credentials:** `scripts/verify-deployed-apps.mjs` must NEVER hardcode passwords or tokens; optional login checks must read `DEMO_IDENTIFIER` and `DEMO_PASSWORD` strictly from environment variables.
2. **Same-Origin Cookie & SSRF Integrity:** Both proxies (`apps/web-erp/src/lib/backend-proxy.ts` and `apps/rep-pwa/api/[...path].ts`) MUST retain their strict `/api/v1/*` and `/health` allowlist and preserve `HttpOnly; Secure; SameSite=Strict` `Set-Cookie` headers.
3. **Design System & RTL/LTR Hygiene:** All new error boundaries and recovery UIs must use semantic design tokens (`bg-surface-*`, `text-text-*`, `border-surface-border`, `@almomiaa/ui` `Button`/`AlertBanner`), zero hardcoded hex colors, zero directional Tailwind classes, and zero `any`/`@ts-ignore`.

---

## 5. Acceptance Criteria (Definition of Done)

- [ ] `AuthProvider` never deadlocks on React 19 Strict Mode / hydration remounts, surfaces slow-connection / error recovery UI after 5s, and exposes `retrySession()`.
- [ ] All 10 `apps/web-erp` workspaces wait for `status === "authenticated"` before calling protected API endpoints (zero spurious pre-auth `401` races on `/`).
- [ ] `apps/web-erp` includes `src/app/error.tsx` and `src/app/global-error.tsx`, and `apps/rep-pwa` includes `AppErrorBoundary.tsx`, plus recovery UI on session bootstrap.
- [ ] Both Vercel proxies (`apps/web-erp/src/lib/backend-proxy.ts` and `apps/rep-pwa/api/[...path].ts`) enforce `AbortSignal.timeout(25_000)` and return structured `504`/`502` ProblemDetails JSON.
- [ ] `scripts/verify-deployed-apps.mjs` verifies Backend, Web ERP, and Rep PWA live endpoints cleanly and reports clear pass/fail diagnostics.
- [ ] All monorepo checks (`typecheck`, `lint`, `@almomiaa/api-client` tests, `rep-pwa` build, `web-erp` build, `dotnet test`) and `@QA & Security Auditor` gate return **`PASS`**, after which `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0012-fix-stuck-session-error-handling-and-live-verification.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks `CURRENT_PROMPT.md` completed.
