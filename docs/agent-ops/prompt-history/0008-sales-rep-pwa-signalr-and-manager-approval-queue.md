# PROMPT-0008: Build Installable `SALES_REP` Mobile PWA (`apps/rep-pwa`), Real-Time SignalR `OperationsHub`, and Enhanced `SALES_MGR` Pending Approvals Queue

- **Status:** `COMPLETED`
- **Prompt ID:** `PROMPT-0008`
- **Archived At:** `2026-11-24`
- **QA & Security Auditor Verdict:** `PASS`
- **Scope:** `apps/backend-api` (SignalR `OperationsHub` + `WarehousesRead` policy + tests), `packages/ui-kit` (`StepProgress`, `BottomNavBar`), `apps/design-system-studio` (2 CSF3 stories), `packages/api-client` (typed SignalR client), `apps/rep-pwa` (installable Vite + React 19 + Workbox PWA), `apps/web-erp` (`RealtimeProvider` + `Pending Approvals` queue), `docs/architecture/06-API-Specification.md`

---

## 1. Execution Summary

Delivered the complete end-to-end **`SALES_REP` Mobile PWA -> Real-Time SignalR `OperationsHub` -> `SALES_MGR` Pending Approvals Queue** workflow:

1. **`.NET 9` Real-Time SignalR `OperationsHub` & Read-Only Warehouse Access (`apps/backend-api`):**
   - Added `IOperationsRealtimeNotifier` and strongly typed event records in `AlMomiaa.Application/Common/Abstractions/IOperationsRealtimeNotifier.cs` (zero ASP.NET Core / SignalR coupling in Domain/Application).
   - Added `NoOpOperationsRealtimeNotifier` in `AlMomiaa.Infrastructure` and post-commit dispatch calls inside `ProcurementAndSalesServices.cs` (`CreateOrder`, `ManagerEdit`, `OverridePrice`, `CoApproveCredit`, `Approve`, `Reject`).
   - Implemented `[Authorize] OperationsHub` (`/hubs/operations`) and `SignalROperationsRealtimeNotifier` in `AlMomiaa.WebApi/Hubs/` joining connections strictly via verified JWT claims (`user:{userId}`, `role:{roleCode}`, `warehouse:{warehouseId}`).
   - Added `JwtBearerEvents.OnMessageReceived` extracting `?access_token=` only for `/hubs/*` paths (preserving fail-closed `SecurityStampMiddleware` and strict `TrustedFrontend` CORS), plus least-privilege `"WarehousesRead"` policy on `GET /api/v1/warehouses` with rep warehouse scoping in `CatalogAndInventoryServices.cs`.
   - Added `SalesApprovalAndSignalRTests.cs` verifying anonymous `401` hub rejection, `?access_token=` negotiate `200`, hub group membership, `SALES_REP` warehouse read & cost masking, `SALES_REP` submit -> `PendingApproval`, `403` on rep edit/approve, and `SALES_MGR` approve/reject dispatching live notifications.
2. **2 New Reusable Standalone `@almomiaa/ui` Components + Storybook 8 Stories:**
   - Added `StepProgress` and `BottomNavBar` to `packages/ui-kit` (**26** total pure components) and CSF3 stories `StepProgress.stories.tsx` & `BottomNavBar.stories.tsx` in `apps/design-system-studio` (**26** total stories + updated `Introduction.mdx`).
3. **Shared Typed SignalR Client (`packages/api-client`):**
   - Added `@microsoft/signalr` and `src/realtime/operations-hub.ts` (`createOperationsHubClient`, `computeJitteredReconnectDelay`) with jittered exponential backoff (`RISK-04`) and in-memory JWT `accessTokenFactory`.
4. **Installable Online-First `SALES_REP` Mobile PWA (`apps/rep-pwa` on port `5173`):**
   - Scaffolded Vite 5 + React 19 + TypeScript + Tailwind + `vite-plugin-pwa` (Workbox) with Arabic RTL Web App Manifest, generated `192x192` & `512x512` PNG brand icons, Vite dev proxy (`/api`, `/health`, `/hubs` with `ws: true`), and Workbox `NetworkOnly` strictly enforced for `/api/`, `/hubs/`, and `/health`.
   - Built bilingual (`ar-SA` RTL default + `en-US` LTR, Light/Dark) screens: `LoginPage`, `RepHomePage` (KPIs + live SignalR status + PWA install button), `NewSellRequestPage` (3-Step Wizard: Customer & Credit Check -> Category/Products & Tier Price/Stock -> Cart Review & Idempotent `POST /api/v1/orders` submit + Confirmation), and `MyRequestsPage` (filter tabs + live SignalR updates + read-only Detail Drawer).
5. **Enhanced `SALES_MGR` Pending Approvals Queue (`apps/web-erp` on port `3000`):**
   - Added `RealtimeProvider` connecting to `/hubs/operations` with live SignalR status pill and toast notifications in `ErpAppLayout`.
   - Enhanced `/sales` (`SalesWorkspace.tsx`) with a default `Pending Approvals` tab (live count badge, warehouse name lookup, MR-02/MR-03/RISK-03 governance badges, 24h reservation TTL, instant SignalR auto-refresh) and a Manager Review & Decide drawer (Manager Edit, Override Price, Co-Approve Credit, Approve, Reject).

---

## 2. Verification & QA Results

- `dotnet build apps/backend-api/AlMomiaa.Erp.sln --no-restore`: **PASS (0 warnings, 0 errors)**
- `dotnet test apps/backend-api/AlMomiaa.Erp.sln --no-build` (`DOTNET_ROLL_FORWARD=Major`): **PASS (22/22 passed: 1 Architecture + 21 Auth/Schema/E2E/SignalR)**
- `pnpm -r typecheck`: **PASS (5/5 packages/apps, 0 errors)**
- `pnpm -r lint`: **PASS (5/5 packages/apps, 0 errors)**
- `@almomiaa/ui` build: **PASS (26 components)**
- `@almomiaa/api-client` build & tests: **PASS (7/7 tests passed)**
- `apps/design-system-studio` `build-storybook`: **PASS (26 stories, 167 modules)**
- `apps/rep-pwa` build: **PASS (`dist/manifest.webmanifest`, `dist/sw.js` with `NetworkOnly` for `/api/` & `/hubs/`, `dist/icons/pwa-{192,512}.png`)**
- `apps/web-erp` Next.js 15 production build: **PASS (all 10 app routes)**
- Static QA & Security Audit (115 scanned TS/TSX files): **0 forbidden imports, 0 hardcoded hex colors, 0 directional Tailwind utilities, 0 `any`/`@ts-ignore`, 0 client-side token storage, 0 committed secrets** (**QA PASS**).

---

## 3. Acceptance Criteria (Definition of Done)

- [x] `.NET 9` backend implements `[Authorize] OperationsHub` (`/hubs/operations`), `IOperationsRealtimeNotifier`, `SignalROperationsRealtimeNotifier`, `JwtBearerEvents.OnMessageReceived` for `/hubs`, and `"WarehousesRead"` policy on `GET /api/v1/warehouses` — preserving Clean Architecture.
- [x] Backend `dotnet build` (0 warnings, 0 errors) and `dotnet test` pass all 22 tests including `SalesApprovalAndSignalRTests`.
- [x] `@almomiaa/ui` exports `StepProgress` and `BottomNavBar` (26 total pure components) and `apps/design-system-studio` builds cleanly with **26** CSF3 Storybook 8 stories.
- [x] `@almomiaa/api-client` exports `createOperationsHubClient` with jittered exponential backoff reconnect and passes **7/7** unit tests.
- [x] `apps/rep-pwa` builds cleanly, emits valid `manifest.webmanifest` + Workbox `sw.js` (`NetworkOnly` for `/api/` & `/hubs/`), and implements Login, Rep Home, 3-Step New Sell Request Wizard, and My Requests Tracker with live SignalR updates and read-only lock.
- [x] `apps/web-erp` builds cleanly, connects to `/hubs/operations` via `RealtimeProvider`, displays live SignalR toasts, and provides a dedicated `Pending Approvals` tab & Manager Review Drawer on `/sales`.
- [x] Full workspace verification and `@QA & Security Auditor` returned `PASS`; archived as `docs/agent-ops/prompt-history/0008-sales-rep-pwa-signalr-and-manager-approval-queue.md`, updated `docs/agent-ops/PROJECT_LOG.md`, and marked `CURRENT_PROMPT.md` completed.
