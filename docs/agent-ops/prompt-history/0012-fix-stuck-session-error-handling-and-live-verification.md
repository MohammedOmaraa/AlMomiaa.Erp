# PROMPT-0012: Fix Stuck Session/Navigation & Error Handling in `web-erp` & `rep-pwa` + End-to-End Live Verification Suite

- **Status:** `COMPLETED — QA PASS`
- **Prompt ID:** `PROMPT-0012`
- **Archived At:** `2026-11-25`
- **Scope:** `apps/web-erp` (`AuthProvider` remount deadlock fix + slow-bootstrap/error recovery, `ErpAppLayout` Retry/Go-to-Login gate, Next.js `error.tsx` & `global-error.tsx`, auth-status guards across all 10 workspaces, `25s` timeout + `504`/`502` ProblemDetails on `backend-proxy.ts`), `apps/rep-pwa` (`AppErrorBoundary.tsx`, `slowBootstrap` & `retrySession` recovery UI, `25s` timeout + `504`/`502` ProblemDetails on `api/[...path].ts`), and `scripts/verify-deployed-apps.mjs` (`pnpm verify:live`).

---

## 1. Execution Summary

1. **Fixed `AuthProvider` Remount Deadlock & Added Session Recovery (`apps/web-erp`):**
   - Replaced the deadlocking `bootstrapStartedRef` + `active = false` pattern in `AuthProvider.tsx` with `mountedRef`, bounded `18s` bootstrap timeout, `5s` `slowBootstrap` cold-start indicator, structured `bootstrapError`, and `retrySession()`.
   - Upgraded `ErpAppLayout.tsx` session gate so slow/unauthenticated/error states show clear bilingual guidance, **Retry**, and **Go to Sign In** buttons (never trapping users on `/`).
   - Added Next.js App Router error boundaries `src/app/error.tsx` and `src/app/global-error.tsx`.
2. **Guarded All 10 `apps/web-erp` Workspaces Against Pre-Auth API Races:**
   - Updated `DashboardWorkspace`, `UsersWorkspace`, `CustomersWorkspace`, `CatalogWorkspace`, `InventoryWorkspace`, `PurchasingWorkspace`, `SalesWorkspace`, `FinanceWorkspace`, `GpsTrackingWorkspace`, and `ReportsWorkspace` so data-loading `useEffect` hooks wait for `status === "authenticated" && user` before issuing protected API requests.
3. **Hardened Both Vercel Proxies & `apps/rep-pwa` Error Handling:**
   - Added `AbortSignal.timeout(25_000)` with distinct `504 Gateway Timeout` vs `502 Bad Gateway` RFC 7807 `ProblemDetails` responses in `apps/web-erp/src/lib/backend-proxy.ts` and `apps/rep-pwa/api/[...path].ts`.
   - Added tokenized React `AppErrorBoundary.tsx` wrapping `apps/rep-pwa/src/main.tsx`, plus `slowBootstrap` / `retrySession()` / Sign In fallback in `AppProviders.tsx` and `App.tsx`.
4. **Live Cloud Deployment Verifier (`scripts/verify-deployed-apps.mjs`):**
   - Added zero-secret verification script (`pnpm verify:live`) testing Backend `/health`, Web ERP routes + `/manifest.webmanifest` + `/health` proxy, Rep PWA routes + `/manifest.webmanifest` + `/sw.js` + `/health` proxy, plus optional authenticated login + `Set-Cookie` + `/auth/refresh` round-trip when `DEMO_IDENTIFIER` and `DEMO_PASSWORD` are provided via env vars.

---

## 2. Verification & QA Gate (`PASS`)

- [x] `dotnet test apps\backend-api\AlMomiaa.Erp.sln -c Release`: **33/33 passed**.
- [x] `pnpm.cmd -r typecheck` & `pnpm.cmd -r lint`: **5/5 workspaces passed**.
- [x] `@almomiaa/api-client` unit tests: **9/9 passed**.
- [x] `apps/rep-pwa` production Vite + Workbox PWA build & proxy strict `tsc`: **passed** (`dist/sw.js` & `dist/manifest.webmanifest`).
- [x] `apps/web-erp` Next.js 15 production build: **passed** (all 13 routes including `/api/v1/[...path]` & `/health`).
- [x] Static QA & Security audit: **0** hardcoded hex colors, **0** directional Tailwind classes, **0** `any`/`@ts-ignore`, **0** committed secrets.
- [x] Live deployment verification (`node scripts/verify-deployed-apps.mjs`): verified all 6 `Web ERP` routes (`200 OK`) and all 4 `Rep PWA` routes/assets (`200 OK`, `sw.js` 1430 bytes), and pinpointed Render free-tier `/health` wake-up latency.
