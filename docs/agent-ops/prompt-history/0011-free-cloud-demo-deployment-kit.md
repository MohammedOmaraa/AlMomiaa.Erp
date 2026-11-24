# PROMPT-0011: Free HTTPS Cloud Demo Deployment Kit (Render.com Free Docker `.NET 9` API + Free External SQL Server + Vercel `web-erp` & Installable `rep-pwa`)

- **Status:** `COMPLETED — QA PASS`
- **Prompt ID:** `PROMPT-0011`
- **Archived At:** `2026-11-24`
- **Scope:** `apps/backend-api` (multi-stage non-root `.NET 9` `Dockerfile`, `.dockerignore`, `.env.example`, `ForwardedHeaders`, `Cors:AllowedOriginsCsv`, SQL Server transient retries, opt-in `Redis:AllowInMemoryFallback`, opt-in `Database:AutoMigrateOnStartup`, opt-in idempotent `DemoDataSeeder` + 4 integration tests), `apps/web-erp` (`vercel.json`, `.env.example`, installable `manifest.webmanifest` + app icons), `apps/rep-pwa` (`vercel.json`, SSRF-safe same-origin cookie proxy `api/[...path].ts`, `.env.example`), root `render.yaml` Blueprint, and `docs/deployment/FREE_CLOUD_DEMO_DEPLOYMENT_GUIDE.md`.

---

## 1. Execution Summary

1. **Backend `.NET 9` Cloud Runtime & Containerization (`apps/backend-api` + `render.yaml`):**
   - Added `ForwardedHeaders` (`XForwardedFor | XForwardedProto`) in `Program.cs` for cloud HTTPS reverse-proxy TLS termination.
   - Added comma/semicolon-separated `Cors:AllowedOriginsCsv` parsing in `TrustedOriginValidator.cs` and `Program.cs` (while continuing to reject `*` wildcards).
   - Updated `DependencyInjection.cs` with SQL Server transient failure retries (`EnableRetryOnFailure`) and opt-in `Redis:AllowInMemoryFallback` (default `false` — strict fail-closed Redis preserved by default).
   - Created idempotent `DemoDataSeeder.cs` gated behind `Demo:SeedEnabled=true` + env-var `Demo:DefaultPassword` (>= 8 chars, zero hardcoded passwords), seeding 3 warehouses, 4 demo role accounts (`ADMIN-DEMO`, `MGR-DEMO`, `REP-DEMO`, and single-warehouse `WHK-DEMO`), 2 categories, 2 brands, 1 default price list, 4 multi-unit products with multi-warehouse opening stock & `"OpeningBalance"` movements, 3 customers linked to `REP-DEMO`, 3 financial accounts, open fiscal period, ZATCA EGS device, and initial Riyadh GPS ping.
   - Added opt-in `Database:AutoMigrateOnStartup=true` (default `false`), configurable `Swagger:Enabled`, and enriched anonymous `GET /health`.
   - Created production multi-stage non-root `apps/backend-api/Dockerfile`, `.dockerignore`, `.env.example`, and root `render.yaml` Blueprint (`sync: false` for all secrets).
   - Added `DemoDeploymentAndSeedTests.cs` (4 tests covering idempotent demo seeding + single-warehouse `WH_KEEPER` invariant, weak/missing demo password guard, `AllowedOriginsCsv` parsing, and fail-closed vs opt-in Redis fallback).
2. **Web ERP (`apps/web-erp`) Vercel & Installability:**
   - Added `apps/web-erp/vercel.json` (monorepo pnpm build + security headers), `.env.example`, `public/manifest.webmanifest`, app PNG icons (`pwa-192x192.png`, `pwa-512x512.png`), and manifest/Apple Web App metadata in `src/app/layout.tsx`.
3. **Installable Sales Rep PWA (`apps/rep-pwa`) Vercel & Same-Origin Cookie Proxy:**
   - Created SSRF-safe Vercel Serverless proxy `apps/rep-pwa/api/[...path].ts` forwarding strictly `/api/v1/*` and `/health` to `BACKEND_API_ORIGIN`, preserving `HttpOnly; Secure; SameSite=Strict` `Set-Cookie` headers same-origin across Android, iOS, and Desktop PWAs.
   - Added `apps/rep-pwa/vercel.json` (monorepo pnpm build, `/health` & SPA fallback rewrites, security & `no-cache` SW/manifest headers) and `.env.example`.
4. **Bilingual Deployment Guide:**
   - Authored `docs/deployment/FREE_CLOUD_DEMO_DEPLOYMENT_GUIDE.md` covering GitHub push, free external SQL Server (MonsterASP.NET / Somee / Azure SQL Free), Render.com Free Docker setup, Vercel deployment for both apps, CORS linking, Android/iOS/Desktop PWA installation, and demo credentials table.

---

## 2. Verification & QA Gate (`PASS`)

- [x] `dotnet build apps\backend-api\AlMomiaa.Erp.sln -c Release`: **0 warnings, 0 errors**.
- [x] `dotnet test apps\backend-api\AlMomiaa.Erp.sln -c Release`: **33/33 passed** (`1/1` ArchitectureTests + `32/32` Authentication & Integration Tests including 4 new `DemoDeploymentAndSeedTests`).
- [x] `pnpm.cmd -r typecheck` & `pnpm.cmd -r lint`: **5/5 workspaces passed**.
- [x] `apps/rep-pwa/api/[...path].ts` strict TypeScript check: **0 errors**.
- [x] `apps/rep-pwa` production Vite + Workbox PWA build: **passed** (`dist/sw.js` & `dist/manifest.webmanifest` generated).
- [x] `apps/web-erp` Next.js 15 production build: **passed** (all 11 routes).
- [x] Config JSON validation & security secret audit: **valid JSON**, **0 committed secrets**, **0 `any`**, fail-closed defaults preserved (`AutoMigrateOnStartup=false`, `SeedEnabled=false`, `AllowInMemoryFallback=false`).
