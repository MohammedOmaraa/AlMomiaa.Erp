# PROMPT-0011: Free HTTPS Cloud Demo Deployment Kit (Render.com Free Docker `.NET 9` API + Free External SQL Server + Vercel `web-erp` & Installable `rep-pwa`)

- **Status:** `COMPLETED — ARCHIVED (docs/agent-ops/prompt-history/0011-free-cloud-demo-deployment-kit.md)`
- **Prompt ID:** `PROMPT-0011`
- **Scope:** `apps/backend-api` (multi-stage production `Dockerfile`, `.dockerignore`, reverse-proxy `ForwardedHeaders`, comma-separated `Cors:AllowedOriginsCsv` support, opt-in `Database:AutoMigrateOnStartup`, opt-in `Redis:AllowInMemoryFallback` for single-instance demo containers, opt-in env-var-driven `DemoDataSeeder` + integration tests), `apps/web-erp` (`vercel.json`, `.env.example`, installable Web App Manifest & icons, clean `BACKEND_API_ORIGIN` / `NEXT_PUBLIC_SIGNALR_HUB_URL` wiring), `apps/rep-pwa` (`vercel.json`, serverless same-origin `/api/v1` & `/health` proxy for `HttpOnly; SameSite=Strict` cookies on Vercel, `.env.example`, iOS/Android PWA install meta verification), root `render.yaml` Blueprint, and `docs/deployment/FREE_CLOUD_DEMO_DEPLOYMENT_GUIDE.md`
- **Reference baseline:** Architecture v3.2 (`00-ADR`..`08-Development-Roadmap`); `PROMPT-0001`..`PROMPT-0010` completed (live `.NET 9` API + SignalR `/hubs/operations`, 45-table SQL Server `AlMomiaa` with 4 EF Core migrations, 29/29 backend tests, 27 `@almomiaa/ui` components + 27 Storybook stories, `@almomiaa/api-client` 9/9 tests, `apps/web-erp` 11 routes, `apps/rep-pwa` Workbox PWA).
- **Confirmed User Decisions:**
  1. **Free Hosting Stack:** **Vercel** (`apps/web-erp` Next.js 15 + `apps/rep-pwa` installable Vite Workbox PWA) + **Render.com Free Docker** (`.NET 9` API + SignalR `/hubs/operations`) — 100% free HTTPS URLs, no credit card required.
  2. **Free External SQL Server:** **Azure SQL Free Offer / MonsterASP.NET / Somee** — preserves 100% canonical Microsoft SQL Server (`AlMomiaaDbContext`, all 4 existing migrations, 45 tables, computed columns, check constraints, filtered indexes, `ROWVERSION`) with zero EF Core provider rewrite, plus opt-in `Database:AutoMigrateOnStartup=true` to auto-initialize the cloud database on first boot.
  3. **Opt-In Demo Seeder (Env-Var Controlled):** When `Demo:SeedEnabled=true` and `Demo:DefaultPassword` is provided via environment variable (never hardcoded in Git), idempotently seeds realistic demo warehouses, role accounts (`SUPER_ADMIN`, `SALES_MGR`, `SALES_REP`, `WH_KEEPER`), categories, brands, products with multi-warehouse stock, customers linked to the demo rep, financial accounts, open fiscal period, and ZATCA EGS device so every workflow can be tested immediately.
  4. **In-Memory Session/Rate-Limit Fallback (Env-Var Controlled):** Opt-in `Redis:AllowInMemoryFallback=true` (default `false`) allows single-instance free Render containers to run without an external Redis account while preserving strict fail-closed Redis enforcement whenever `false` (default Production behavior).

---

## 1. Purpose of Prompt

Make **AlMomiaa ERP** (`apps/backend-api`, `apps/web-erp`, and installable `apps/rep-pwa`) 100% deployable to **free public HTTPS cloud infrastructure** for real-device demo and QA testing — **NOT development mode**, with:
1. **Production-Grade, Installable Apps over HTTPS:**
   - `apps/rep-pwa` deployed on Vercel HTTPS (`https://<rep-pwa>.vercel.app`) with Workbox Service Worker, Android/Chrome/Edge native **Install App** prompt, iOS **Add to Home Screen** meta/icons, and a same-origin Vercel API proxy (`/api/v1/*` -> `BACKEND_API_ORIGIN`) so `HttpOnly; Secure; SameSite=Strict` refresh cookies work without third-party cookie restrictions.
   - `apps/web-erp` deployed on Vercel HTTPS (`https://<web-erp>.vercel.app`) with Next.js 15 rewrites (`/api/v1/*` & `/health` -> `BACKEND_API_ORIGIN`), SignalR over WSS (`NEXT_PUBLIC_SIGNALR_HUB_URL`), and an installable Web App Manifest (`manifest.webmanifest`) for desktop/tablet app-like launch.
2. **Production `.NET 9` Docker Container for Render.com Free Tier:**
   - Multi-stage non-root `Dockerfile` (`sdk:9.0` -> `aspnet:9.0`), listening on Render's dynamic `$PORT`, with `UseForwardedHeaders` for cloud TLS termination, `/health` liveness probe, and a root `render.yaml` Blueprint.
   - Connects directly to any free external **Microsoft SQL Server** (`ConnectionStrings__SqlServer`) and optionally runs `Database.MigrateAsync()` + idempotent `DemoDataSeeder` on startup when explicitly opted in via environment variables.
3. **Zero-Friction Step-by-Step Deployment Runbook:**
   - Comprehensive Arabic + English guide (`docs/deployment/FREE_CLOUD_DEMO_DEPLOYMENT_GUIDE.md`) covering GitHub push, free SQL Server provisioning (MonsterASP.NET / Somee / Azure SQL Free), Render.com Docker setup, Vercel deployment of both monorepo apps, exact environment variable matrices, PWA installation steps on Android/iOS/Desktop, demo login credentials template, and Render cold-start notes.

---

## 2. Target Execution Agents and Order

1. **`@Backend .NET`** —
   - **A. Cloud Reverse-Proxy, CORS, Redis Fallback & Auto-Migrate (`apps/backend-api`):**
     - Add `ForwardedHeadersOptions` (`XForwardedFor | XForwardedProto`, cleared `KnownNetworks`/`KnownProxies` when `Hosting:TrustForwardedHeaders=true` or in Production behind cloud proxy) early in `Program.cs` before `UseCors` / `UseAuthentication`.
     - Enhance CORS origin loading in `Program.cs` and `TrustedOriginValidator.cs` to merge BOTH `Cors:AllowedOrigins` (array) AND optional comma/semicolon-separated `Cors:AllowedOriginsCsv` (e.g. `https://almomiaa-erp.vercel.app,https://almomiaa-rep.vercel.app`) while STILL rejecting `"*"` wildcard origins with `InvalidOperationException`.
     - Update `DependencyInjection.cs`:
       - Support opt-in `Redis:AllowInMemoryFallback` (`bool`, defaults to `false`).
       - Use `InMemoryDevSecurityStampStore` + `InMemoryDevLoginAttemptLimiter` ONLY when `(isDevelopment || allowInMemoryFallback) && string.IsNullOrWhiteSpace(redisConnectionString)`. When `allowInMemoryFallback` is `false` in Production and `Redis:ConnectionString` is blank, continue throwing `InvalidOperationException` (preserving fail-closed security!).
       - Configure `SqlServer` with `EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)` for resilient cloud SQL Server connections over WAN.
     - Add opt-in startup migration & demo seeding in `Program.cs` (guarded by `Database:AutoMigrateOnStartup=true` and `Demo:SeedEnabled=true`):
       - Create `apps/backend-api/src/AlMomiaa.Infrastructure/Persistence/DemoDataSeeder.cs`:
         - Reads `Demo:DefaultPassword` (must be >= 8 chars; refuses to run if missing — ZERO hardcoded passwords in source code!).
         - Idempotently seeds (checking existence by unique code before inserting, safe across restarts):
           1. 3 Warehouses: `WH-RYD-MAIN` (`MainWarehouse`, Riyadh), `WH-JED-BR` (`BranchWarehouse`, Jeddah), `VAN-RYD-01` (`MobileVan`, Riyadh).
           2. 4 Demo Users (using `IPasswordHasher.HashPassword(defaultPassword)`):
              - `ADMIN-DEMO` (`+966500000001`, role `SUPER_ADMIN`)
              - `MGR-DEMO` (`+966500000002`, role `SALES_MGR`)
              - `REP-DEMO` (`+966500000003`, role `SALES_REP`, `RepType = "PreSales"`, assigned & default warehouse `WH-RYD-MAIN`)
              - `WHK-DEMO` (`+966500000004`, role `WH_KEEPER`, strictly 1 warehouse `WH-RYD-MAIN`)
           3. 2 Categories (`مشروبات / Beverages`, `مواد غذائية / Foodstuffs`), 2 Brands (`المومياء الذهبية / AlMomiaa Gold`, `واحة الرياض / Riyadh Oasis`), 1 Default Price List (`PL-WHOLESALE-DEMO`).
           4. 4 Master Products (with multi-unit `Piece` + `Carton` definitions) + multi-warehouse `WarehouseStock` allocations split across `WH-RYD-MAIN` and `WH-JED-BR` + `"OpeningBalance"` `StockMovements`.
           5. 3 Customers (`CUST-DEMO-001` Grade A, `CUST-DEMO-002` Grade B, `CUST-DEMO-003` Grade C) linked to `REP-DEMO`.
           6. Financial Accounts (`SAFE-MAIN` MainTreasurySafe, `BANK-SNB` BankAccount, `WALLET-REP-DEMO` RepCustodyWallet for `REP-DEMO`), an Open Fiscal Period for the current year/month, and 1 ZATCA EGS Device (`EGS-DEMO-01`) + 1 initial GPS ping for `REP-DEMO` in Riyadh so `/gps` renders a live pin immediately!
       - Disable Swagger UI in Production ONLY if `Swagger:Enabled` is `false` (default `true` for demo convenience, or configurable via `Swagger:Enabled`).
       - Enrich `GET /health` to return `{ status: "Healthy", environment, timestampUtc }` (anonymous, no sensitive info/connection strings!).
   - **B. Container & Cloud Blueprint Files:**
     - Create `apps/backend-api/Dockerfile` (multi-stage `mcr.microsoft.com/dotnet/sdk:9.0` build -> `mcr.microsoft.com/dotnet/aspnet:9.0` runtime, non-root `app` user, binds `http://+:${PORT:-10000}`, `DOTNET_EnableDiagnostics=0`).
     - Create `apps/backend-api/.dockerignore` (excludes `bin/`, `obj/`, `tests/`, `.git/`, `*.user`, `appsettings.Development.json`).
     - Create `apps/backend-api/.env.example` documenting all required Render environment variables with placeholder values (zero real secrets).
     - Create root `render.yaml` Blueprint pointing to `apps/backend-api/Dockerfile` (plan `free`, `healthCheckPath: /health`, all env vars declared with `sync: false` for secrets!).
   - **C. Backend Tests (`DemoDeploymentAndSeedTests.cs`):**
     - Verify `DemoDataSeeder.SeedAsync` is idempotent (running twice creates exact expected warehouses, 4 demo users including single-warehouse `WH_KEEPER`, 4 products with multi-warehouse stocks, 3 customers linked to `REP-DEMO`, financial accounts, fiscal period, EGS device, and GPS ping without duplicating rows).
     - Verify `DemoDataSeeder` rejects missing/weak `Demo:DefaultPassword`.
     - Verify comma-separated `Cors:AllowedOriginsCsv` parses multiple HTTPS origins cleanly and still rejects wildcard `"*"`.
     - Verify `Redis:AllowInMemoryFallback=false` still fails closed when Redis is missing in Production, while `true` succeeds with in-memory stores.

2. **`@Frontend Engineer`** —
   - **A. `apps/web-erp` Vercel Production Deployment & Installability:**
     - Add `apps/web-erp/vercel.json` configured for pnpm monorepo root build (`buildCommand`, `installCommand`, security headers `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`).
     - Add `apps/web-erp/.env.example` documenting `BACKEND_API_ORIGIN`, `NEXT_PUBLIC_SIGNALR_HUB_URL`, and optional `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY`.
     - Add `apps/web-erp/public/manifest.webmanifest` + copy PWA PNG icons (`apps/web-erp/public/icons/pwa-192x192.png`, `pwa-512x512.png` from `apps/rep-pwa/public/icons/`) and link `manifest` + `appleWebApp` metadata in `apps/web-erp/src/app/layout.tsx` so `web-erp` is installable as a standalone desktop/tablet web app.
     - Update `apps/web-erp/next.config.ts` to keep `/api/v1/:path*` and `/health` rewrites clean and resilient when `BACKEND_API_ORIGIN` is set on Vercel.
   - **B. `apps/rep-pwa` Vercel Production Deployment & Same-Origin Cookie Proxy:**
     - Add `apps/rep-pwa/api/[...path].ts` (Vercel Node/Web-standard serverless proxy function) that forwards `/api/v1/*` and `/health` to `process.env.BACKEND_API_ORIGIN`, preserving HTTP method, request body, `Authorization`, `Cookie`, `Accept-Language`, `X-Idempotency-Key`, and forwarding `Set-Cookie` headers back to the browser same-origin (so `HttpOnly; Secure; SameSite=Strict` refresh cookies work seamlessly on Android/iOS/Desktop PWAs!). Rejects non-`/api/` & non-`/health` paths to prevent open-proxy SSRF!
     - Add `apps/rep-pwa/vercel.json` routing `/api/(.*)` -> `/api/$1`, `/health` -> `/api/health`, and SPA fallback `/(.*)` -> `/index.html` (excluding static assets, `manifest.webmanifest`, `sw.js`, `workbox-*.js`, `icons/*`) plus security headers.
     - Add `apps/rep-pwa/.env.example` documenting `BACKEND_API_ORIGIN` and `VITE_SIGNALR_HUB_URL`.
     - Verify `apps/rep-pwa/index.html` includes `apple-touch-icon`, `apple-mobile-web-app-capable`, `apple-mobile-web-app-status-bar-style`, and `theme-color` meta tags for full iOS & Android PWA installation.
   - **C. Deployment Guide (`docs/deployment/FREE_CLOUD_DEMO_DEPLOYMENT_GUIDE.md`):**
     - Clear bilingual (Arabic + English) step-by-step instructions covering:
       1. Free external SQL Server setup (MonsterASP.NET free tier & Azure SQL Free Offer connection string format).
       2. Deploying `apps/backend-api` on Render.com Free Docker (using `render.yaml` or manual Docker Web Service) + exact env vars table.
       3. Deploying `apps/web-erp` on Vercel (Root Directory: `apps/web-erp`) + exact env vars.
       4. Deploying `apps/rep-pwa` on Vercel (Root Directory: `apps/rep-pwa`) + exact env vars.
       5. Updating Render `Cors__AllowedOriginsCsv` with both live `https://*.vercel.app` URLs.
       6. Installing `AlMomiaa Rep` PWA on Android Chrome, iPhone Safari, and Windows/Mac Chrome/Edge.
       7. Demo accounts table (`ADMIN-DEMO`, `MGR-DEMO`, `REP-DEMO`, `WHK-DEMO`) + Render free-tier cold-start / optional free UptimeRobot keep-alive tip.

3. **`@QA & Security Auditor`** —
   - Verify `dotnet build` (0 warnings, 0 errors) & `dotnet test` (all 29 existing + new `DemoDeploymentAndSeedTests` passing).
   - Verify `pnpm -r typecheck`, `pnpm -r lint`, `apps/rep-pwa` production Vite + Workbox PWA build (confirming `dist/manifest.webmanifest` & `dist/sw.js` generated), and `apps/web-erp` Next.js 15 production build.
   - Verify security invariants:
     - ZERO hardcoded passwords, JWT signing keys, SQL Server passwords, or API keys committed in `render.yaml`, `Dockerfile`, `.env.example`, `DemoDataSeeder.cs`, or `vercel.json`.
     - `apps/rep-pwa/api/[...path].ts` strictly allows ONLY `/api/v1/*` and `/health` paths against `BACKEND_API_ORIGIN` (no SSRF / arbitrary URL forwarding) and never logs `Authorization` or `Cookie` headers.
     - `Cors:AllowedOriginsCsv` still forbids `*` wildcard.
     - `Redis:AllowInMemoryFallback` defaults to `false`.
     - `Demo:SeedEnabled` and `Database:AutoMigrateOnStartup` default to `false`.

4. **`@Orchestrator`** — Archive as `docs/agent-ops/prompt-history/0011-free-cloud-demo-deployment-kit.md`, update `docs/agent-ops/PROJECT_LOG.md`, and mark `CURRENT_PROMPT.md` completed only after QA returns `PASS`.

---

## 3. Target Files to Create / Modify

### `apps/backend-api/`
- `Dockerfile` (new — multi-stage `.NET 9` production image for Render/Docker)
- `.dockerignore` (new)
- `.env.example` (new — Render env var template, zero secrets)
- `src/AlMomiaa.Infrastructure/DependencyInjection.cs` (modify — SQL retry resiliency + opt-in `Redis:AllowInMemoryFallback`)
- `src/AlMomiaa.Infrastructure/Persistence/DemoDataSeeder.cs` (new — idempotent opt-in demo data & role accounts seeder)
- `src/AlMomiaa.WebApi/Security/TrustedOriginValidator.cs` (modify — support `AllowedOriginsCsv` alongside `AllowedOrigins`)
- `src/AlMomiaa.WebApi/Program.cs` (modify — `ForwardedHeaders`, `Cors:AllowedOriginsCsv`, opt-in `Database:AutoMigrateOnStartup`, opt-in `Demo:SeedEnabled`, enriched `/health`)
- `tests/AlMomiaa.AuthenticationTests/DemoDeploymentAndSeedTests.cs` (new)

### `apps/web-erp/`
- `vercel.json` (new)
- `.env.example` (new)
- `public/manifest.webmanifest` (new)
- `public/icons/pwa-192x192.png` & `public/icons/pwa-512x512.png` (new — copied from `apps/rep-pwa/public/icons/`)
- `src/app/layout.tsx` (modify — register Web App Manifest & `appleWebApp` metadata)

### `apps/rep-pwa/`
- `vercel.json` (new)
- `api/[...path].ts` (new — secure same-origin proxy to `BACKEND_API_ORIGIN` for `/api/v1/*` & `/health`)
- `.env.example` (new)
- `index.html` (verify/enhance iOS & Android PWA install meta tags)

### Root & Docs
- `render.yaml` (new — Render.com Free Docker Blueprint)
- `docs/deployment/FREE_CLOUD_DEMO_DEPLOYMENT_GUIDE.md` (new — complete bilingual deployment & PWA installation runbook)

---

## 4. Environment Variable Contract (Zero Secrets in Git)

### Render.com Free Docker Service (`apps/backend-api`)
| Variable | Example / Format | Required | Notes |
| :--- | :--- | :---: | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Yes | Production mode (non-dev) |
| `ConnectionStrings__SqlServer` | `Server=...;Database=...;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=True;` | Yes | From MonsterASP.NET / Azure SQL Free / Somee |
| `Auth__Jwt__Issuer` | `https://almomiaa-api.onrender.com` | Yes | Matches Render URL |
| `Auth__Jwt__Audience` | `almomiaa-erp-clients` | Yes | Canonical JWT audience |
| `Auth__Jwt__SigningKey` | `<random 48+ char secret>` | Yes | Auto-generated or random secret (`>= 32` UTF-8 bytes) |
| `Cors__AllowedOriginsCsv` | `https://almomiaa-erp.vercel.app,https://almomiaa-rep.vercel.app` | Yes | Exact HTTPS origins of both Vercel apps (no trailing slash, no `*`) |
| `Hosting__TrustForwardedHeaders` | `true` | Yes | Required behind Render HTTPS reverse proxy |
| `Redis__AllowInMemoryFallback` | `true` | Yes (Demo) | Enables single-instance in-memory session & rate-limit stores (default `false`) |
| `Database__AutoMigrateOnStartup` | `true` | Yes (Demo) | Runs EF Core migrations on boot to create all 45 tables (default `false`) |
| `Demo__SeedEnabled` | `true` | Yes (Demo) | Idempotently seeds demo warehouses, products, customers & 4 demo users (default `false`) |
| `Demo__DefaultPassword` | `<your chosen demo password >= 8 chars>` | If `Demo__SeedEnabled=true` | Password applied to `ADMIN-DEMO`, `MGR-DEMO`, `REP-DEMO`, `WHK-DEMO` |

### Vercel Project 1 (`apps/web-erp`)
| Variable | Example / Format | Required |
| :--- | :--- | :---: |
| `BACKEND_API_ORIGIN` | `https://almomiaa-api.onrender.com` | Yes |
| `NEXT_PUBLIC_SIGNALR_HUB_URL` | `https://almomiaa-api.onrender.com/hubs/operations` | Yes |
| `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY` | `<optional Google Maps JS key>` | Optional (Interactive Satellite/Hybrid map on `/gps`; works without it using embedded fallback) |

### Vercel Project 2 (`apps/rep-pwa`)
| Variable | Example / Format | Required |
| :--- | :--- | :---: |
| `BACKEND_API_ORIGIN` | `https://almomiaa-api.onrender.com` | Yes |
| `VITE_SIGNALR_HUB_URL` | `https://almomiaa-api.onrender.com/hubs/operations` | Yes |

---

## 5. Security & Domain Rules

1. **Zero Committed Secrets:** Never hardcode database passwords, JWT signing keys, demo passwords, or Google Maps keys in any source/config/YAML file. All secrets use `sync: false` in `render.yaml` and placeholder text in `.env.example`.
2. **Fail-Closed Defaults Preserved:** `Database:AutoMigrateOnStartup`, `Demo:SeedEnabled`, and `Redis:AllowInMemoryFallback` MUST all default to `false`. Wildcard `*` CORS origins MUST remain rejected.
3. **SSRF-Safe Same-Origin PWA Proxy:** `apps/rep-pwa/api/[...path].ts` MUST validate that `BACKEND_API_ORIGIN` is a valid `http(s)` URL and allow forwarding ONLY to `/api/v1/*` and `/health` paths, preserving `Set-Cookie` headers while stripping hop-by-hop headers.
4. **Canonical Security Preserved in Demo Seed:** `WHK-DEMO` (`WH_KEEPER`) MUST be seeded with strictly **1** warehouse (`WH-RYD-MAIN`), matching `PROMPT-0009` invariant.

---

## 6. Acceptance Criteria (Definition of Done)

- [ ] `apps/backend-api/Dockerfile` & `render.yaml` build cleanly and configure Render dynamic `$PORT`, non-root runtime, `ForwardedHeaders`, and `/health` probe.
- [ ] `Program.cs`, `TrustedOriginValidator.cs`, and `DependencyInjection.cs` support `Cors:AllowedOriginsCsv` (rejecting `*`), SQL Server transient retry, opt-in `Redis:AllowInMemoryFallback`, opt-in `Database:AutoMigrateOnStartup`, and opt-in idempotent `DemoDataSeeder` (requiring env-var `Demo:DefaultPassword`).
- [ ] `apps/web-erp` includes `vercel.json`, `.env.example`, and installable `manifest.webmanifest` + icons wired in `layout.tsx`.
- [ ] `apps/rep-pwa` includes `vercel.json`, SSRF-safe same-origin proxy `api/[...path].ts`, `.env.example`, and complete Android/iOS installable PWA metadata.
- [ ] `docs/deployment/FREE_CLOUD_DEMO_DEPLOYMENT_GUIDE.md` provides complete step-by-step Arabic + English deployment and phone/desktop PWA installation instructions.
- [ ] All backend builds (`0` warnings/errors), backend tests (`29+` including new `DemoDeploymentAndSeedTests`), `pnpm -r typecheck`, `pnpm -r lint`, `apps/rep-pwa` build (generating `dist/sw.js` & `dist/manifest.webmanifest`), `apps/web-erp` Next.js 15 build, and `@QA & Security Auditor` gate return **`PASS`**, after which `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0011-free-cloud-demo-deployment-kit.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks `CURRENT_PROMPT.md` completed.
