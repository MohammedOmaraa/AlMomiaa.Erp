# PROMPT-0007: Build Reusable `@almomiaa/ui` ERP Component Kit, Storybook 8 Stories, Full 8-Module `@almomiaa/api-client`, and Live Bilingual Web ERP Pages Covering All 73 API Endpoints

- **Status:** `COMPLETED`
- **Prompt ID:** `PROMPT-0007`
- **Archived At:** `2026-11-23`
- **QA & Security Auditor Verdict:** `PASS`
- **Scope:** `packages/ui-kit`, `apps/design-system-studio`, `packages/api-client`, `apps/web-erp`, local Dev CORS user-secret (`http://localhost:3000`)
- **Reference baseline:** Architecture v3.2 (`00-ADR`..`08-Development-Roadmap`); `PROMPT-0001`..`PROMPT-0006` completed (live `.NET 9` API with 73 endpoints across 8 modules on `http://localhost:5000`, live SQL Server `AlMomiaa`, 19/19 backend tests passing, `@almomiaa/ui` v1 with 13 components, Storybook 8, `@almomiaa/api-client` auth module, `apps/web-erp` `/login` + authenticated shell)

---

## 1. Execution Summary

Delivered the complete reusable ERP design system, interactive Storybook 8 catalog, strongly typed 8-module API client covering all **73 backend endpoints**, and all **9 live bilingual (`ar-SA` RTL default + `en-US` LTR) Web ERP pages** with RBAC permission gating and a collapsible live **API Response Inspector** drawer. Zero `.NET 9` C# source files under `apps/backend-api/src/**` were modified.

### Delivered Artifacts
1. **`packages/ui-kit` (`@almomiaa/ui` — 24 Total Pure Standalone Components):**
   - Added 11 new tokenized, logical RTL/LTR-safe components: `Badge`, `DataTable`, `EmptyState`, `KeyValueList`, `PaginationBar`, `JsonResponseViewer`, `Tabs`, `DrawerModal`, `SelectField`, `TextareaField`, and `AlertBanner`.
   - Exported all 24 components and TypeScript interfaces from `packages/ui-kit/src/index.ts`.
   - Verified zero forbidden imports (`next/`, `axios`, `@almomiaa/api-client`), zero hardcoded hex colors, zero directional Tailwind utilities, and zero `any`/`@ts-ignore`.
2. **`apps/design-system-studio` (Storybook 8 — 24 CSF3 Stories):**
   - Added interactive CSF3 stories for all 11 new components (Default, RTL/LTR, Light/Dark, Loading, Error, Empty states) and updated `Introduction.mdx`.
   - Verified `pnpm --filter @almomiaa/design-system-studio build-storybook` produces a clean static build (`storybook-static`, 163 modules transformed).
3. **`packages/api-client` (`@almomiaa/api-client` — Full 8 ERP Modules + Auth):**
   - Added `src/common/types.ts` (`PagedResult<T>`, `ApiCallLog`, `createIdempotencyKey`, `redactSensitiveData`) and extended `src/http/axios.ts` with a secret-redacting `onApiCall` telemetry hook.
   - Added 8 typed module services (`users`, `customers`, `catalog`, `inventory`, `procurement`, `sales`, `finance`, `operations`) plus `createErpApi(http)` covering all 73 backend endpoints with automatic `X-Idempotency-Key` UUID injection on `POST /orders`, `POST /orders/{id}/confirm-shipment`, `POST /collections/cash`, and `POST /collections/bank-transfer`.
   - Verified `tests/erp-client.test.mjs` (**6/6 tests passing**).
4. **`apps/web-erp` (Next.js 15 Bilingual ERP Web Application — 10 Routes):**
   - Added Next.js `rewrites()` in `next.config.ts` proxying `/api/v1/:path*` and `/health` to `BACKEND_API_ORIGIN ?? http://localhost:5000` so same-origin HttpOnly `SameSite=Strict` refresh cookies work seamlessly on `http://localhost:3000`.
   - Added `ErpAppLayout`, `ApiActivityProvider` (capturing last 25 calls with secret redaction for the collapsible **API Response Inspector** drawer), `permissions.ts`, `formatters.ts`, bilingual `ar.ts` + `en.ts` locales, `.env.local.example`, and all 9 live ERP module routes:
     - `/` (`DashboardWorkspace`)
     - `/users` (`UsersWorkspace`)
     - `/customers` (`CustomersWorkspace`)
     - `/catalog` (`CatalogWorkspace`)
     - `/inventory` (`InventoryWorkspace`)
     - `/purchasing` (`PurchasingWorkspace`)
     - `/sales` (`SalesWorkspace`)
     - `/finance` (`FinanceWorkspace`)
     - `/reports` (`ReportsWorkspace`)
   - Configured local Dev CORS `Cors:AllowedOrigins:1 = http://localhost:3000` via `dotnet user-secrets` (zero secrets committed in Git).

---

## 2. Verification & QA Results

- `pnpm --filter @almomiaa/ui build`: **PASS**
- `pnpm --filter @almomiaa/api-client build` & `test`: **PASS (6/6 passed)**
- `pnpm -r typecheck`: **PASS (4/4 packages, 0 errors)**
- `pnpm -r lint`: **PASS (4/4 packages, 0 errors)**
- `pnpm --filter @almomiaa/design-system-studio build-storybook`: **PASS (24 stories, 163 modules, 0 errors)**
- `pnpm --filter @almomiaa/web-erp build`: **PASS (all 10 app routes compiled & built cleanly)**
- `dotnet test apps/backend-api/AlMomiaa.Erp.sln --no-build` (`DOTNET_ROLL_FORWARD=Major`): **PASS (19/19 passed: 1 Architecture + 18 Auth/E2E/Schema)**
- Static QA & Security Audit (97 scanned TS/TSX files):
  - Forbidden imports in `packages/ui-kit`: **0**
  - Hardcoded hex colors in UI-Kit & Web-ERP TSX/TS: **0**
  - Directional Tailwind utilities (`ml-`/`mr-`/`pl-`/`pr-`/`left-`/`right-`/`text-left`/`text-right`): **0**
  - `any` / `@ts-ignore` / `@ts-expect-error`: **0**
  - Committed secrets in `appsettings.json` / `.env.local.example`: **0**
  - `.NET 9` C# source files modified: **0**

---

## 3. Acceptance Criteria (Definition of Done)

- [x] All **11 new reusable components** (`Badge`, `DataTable`, `EmptyState`, `KeyValueList`, `PaginationBar`, `JsonResponseViewer`, `Tabs`, `DrawerModal`, `SelectField`, `TextareaField`, `AlertBanner`) are implemented in `packages/ui-kit` and exported from `@almomiaa/ui` (24 total components) with zero forbidden imports, zero hardcoded hex colors, and zero directional CSS utilities.
- [x] Every new component has an interactive **CSF3 Storybook 8 story** in `apps/design-system-studio/src/stories/` and `pnpm --filter @almomiaa/design-system-studio build-storybook` succeeds cleanly (24 stories).
- [x] `packages/api-client` exposes strongly typed services for all **8 ERP modules** covering all **73 backend endpoints**, auto-attaches `X-Idempotency-Key` on idempotent writes, redacts secrets from telemetry logs, and passes all unit tests (`6/6`).
- [x] `apps/web-erp` implements all **9 live bilingual RTL/LTR pages** (`/`, `/users`, `/customers`, `/catalog`, `/inventory`, `/purchasing`, `/sales`, `/finance`, `/reports`) + `/login`, RBAC permission-gated sidebar & actions, and the collapsible **API Response Inspector** drawer displaying real responses from `http://localhost:5000`.
- [x] Local Development CORS includes `http://localhost:3000` via `dotnet user-secrets` (zero secrets committed in Git).
- [x] Workspace verification passes across all JS packages/apps (`@almomiaa/ui`, `@almomiaa/api-client`, `apps/design-system-studio`, `apps/web-erp`), AND backend `dotnet test` remains **19/19 passing**.
- [x] `@QA & Security Auditor` returned `PASS`, archived as `docs/agent-ops/prompt-history/0007-erp-ui-kit-storybook-api-client-and-all-pages.md`, updated `docs/agent-ops/PROJECT_LOG.md`, and marked `CURRENT_PROMPT.md` completed.
