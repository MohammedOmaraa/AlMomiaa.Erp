# PROMPT-0008: Build Installable `SALES_REP` Mobile PWA (`apps/rep-pwa`), Real-Time SignalR `OperationsHub`, and Enhanced `SALES_MGR` Pending Approvals Queue

- **Status:** `READY_FOR_ORCHESTRATOR`
- **Prompt ID:** `PROMPT-0008`
- **Scope:** `apps/backend-api` (SignalR `OperationsHub` + read-only `WarehousesRead` policy + tests), `packages/ui-kit` (2 new mobile/wizard components), `apps/design-system-studio` (2 new CSF3 stories), `packages/api-client` (typed SignalR `OperationsHub` client), `apps/rep-pwa` (new Vite + React 19 + Workbox installable PWA), `apps/web-erp` (live SignalR provider + dedicated `SALES_MGR` Pending Approvals queue on `/sales`), `docs/architecture/06-API-Specification.md`
- **Reference baseline:** Architecture v3.2 (`00-ADR-Architecture-Decisions.md`..`08-Development-Roadmap.md`); `PROMPT-0001`..`PROMPT-0007` completed (live `.NET 9` API with 73 endpoints on `http://localhost:5000`, live SQL Server `AlMomiaa`, 19/19 backend tests passing, 24 `@almomiaa/ui` components + 24 Storybook 8 stories, full 8-module `@almomiaa/api-client` with 6/6 tests passing, all 10 `apps/web-erp` routes on `http://localhost:3000`, `Cors:AllowedOrigins:0 = http://localhost:5173` and `:1 = http://localhost:3000` already configured in local `dotnet user-secrets`)
- **Confirmed User Decisions:**
  1. **PWA Architecture (`apps/rep-pwa`):** Online-first installable PWA using **Vite + React 19 + TypeScript + Tailwind + Workbox (`vite-plugin-pwa`) + Web App Manifest** — live API calls only (no Dexie.js IndexedDB offline queue).
  2. **Approval Workflow Scope:** Build `apps/rep-pwa` for `SALES_REP` to create and track sell requests (`PendingApproval`) **AND** enhance `apps/web-erp` `/sales` with a dedicated `SALES_MGR` **Pending Approvals** queue (plus grant `SALES_REP` / `SALES_MGR` read-only access to `GET /api/v1/warehouses` via a least-privilege policy so warehouse names render cleanly).
  3. **Real-Time Status Updates:** Implement `.NET 9` **SignalR `OperationsHub` (`/hubs/operations`)** broadcasting `OrderSubmitted`, `CreditCoApprovalRequired`, `OrderApproved`, `OrderRejected`, and `OrderUpdated` in real time, with live toast alerts and instant UI refresh in both `apps/web-erp` (`SALES_MGR`) and `apps/rep-pwa` (`SALES_REP`).
  4. **Focused Rep PWA Screens:** Mobile Login (`/login`), Rep Home (`/`, KPIs + live SignalR status + quick CTA), 3-Step Guided New Sell Request Wizard (`/new-request`: Customer & Credit Check -> Category/Products & Price/Stock -> Cart & Submit), and My Requests Tracker (`/requests`: filterable list + read-only detail drawer + live status updates).

---

## 1. Purpose of Prompt

Deliver the end-to-end **Field Sales Representative (`SALES_REP`) -> Sales Manager (`SALES_MGR`) Governed Sell-Request Approval Workflow**:
1. A mobile-first, bilingual (`ar-SA` RTL default + `en-US` LTR, Light/Dark AlMomiaa Gold `#CFA434` / Obsidian `#1C1B1A`), installable PWA at **`apps/rep-pwa`** (`http://localhost:5173`) where a `SALES_REP` signs in, selects an assigned customer (inspecting live credit status, grade, overdue debt, and payment terms), browses category products with resolved customer tier pricing and live warehouse `AvailableQty`, builds a cart, and submits an idempotent sell request (`POST /api/v1/orders` -> `Status = PendingApproval`).
2. Real-time **SignalR `OperationsHub` (`/hubs/operations`)** push notifications in `apps/backend-api` so that:
   - The moment `SALES_REP` submits an order, `SALES_MGR` (and `SUPER_ADMIN`) immediately receives an `OrderSubmitted` push notification in `apps/web-erp` (and `ACCOUNTANT`/`EXEC_MGR` receives `CreditCoApprovalRequired` if `MR-03` triggers).
   - The moment `SALES_MGR` approves, edits, or rejects the order in `apps/web-erp` `/sales`, `SALES_REP` immediately receives an `OrderApproved` / `OrderRejected` / `OrderUpdated` push notification in `apps/rep-pwa` and sees the updated status and rejection reason without reloading.
3. Strict 3-tier security enforcement (`07-Security.md`): `SALES_REP` can ONLY see assigned customers, own orders, and masked product costs (never `WeightedAvgCost`), and is physically prevented from editing or approving any submitted order at UI, API, and Domain levels.

---

## 2. Target Execution Agents and Order

1. **`@Backend .NET`** —
   - Add `IOperationsRealtimeNotifier` and strongly typed realtime event records (`OrderSubmittedNotification`, `CreditCoApprovalRequiredNotification`, `OrderApprovedNotification`, `OrderRejectedNotification`, `OrderUpdatedNotification`) in `AlMomiaa.Application.Common.Abstractions` (preserving Clean Architecture: zero ASP.NET Core / SignalR references in `Domain` or `Application`).
   - Add a `NoOpOperationsRealtimeNotifier` fallback in `AlMomiaa.Infrastructure` and invoke `IOperationsRealtimeNotifier` **after** transaction commit (`SaveChangesAsync`) inside `ProcurementAndSalesServices.cs` (`CreateOrder`, `ManagerEdit`, `OverridePrice`, `CoApproveCredit`, `Approve`, `Reject`).
   - Implement `[Authorize] OperationsHub` (`/hubs/operations`) and `SignalROperationsRealtimeNotifier` in `AlMomiaa.WebApi/Hubs/`. On `OnConnectedAsync`, join each authenticated connection strictly to JWT-verified groups: `user:{userId}`, `role:{roleCode}` for each role claim, and `warehouse:{warehouseId}` for each assigned warehouse claim.
   - Update `Program.cs`:
     - Register `AddSignalR()`, map `app.MapHub<OperationsHub>("/hubs/operations").RequireCors("TrustedFrontend")` (enforcing strict configured origins with credentials).
     - Add `JwtBearerEvents.OnMessageReceived` extracting `?access_token=` ONLY for paths starting with `/hubs` so WebSocket & SSE transports authenticate through standard JWT + fail-closed `SecurityStampMiddleware`.
     - Add named policy `"WarehousesRead"` requiring ANY of `inventory:view`, `orders:create`, `orders:approve`, or `dashboards:inventory`, and apply it to `GET /api/v1/warehouses` in `WarehousesController.cs` (keeping `GET /api/v1/warehouses/{id}/stocks` on `inventory:view` and keeping `WH_KEEPER` warehouse scoping intact; for `SALES_REP` with non-empty `AssignedWarehouseIds`, scope `GET /api/v1/warehouses` to assigned warehouses, otherwise return active non-deleted warehouses so a newly created rep can still select a warehouse).
   - Add integration tests in `AlMomiaa.AuthenticationTests` verifying:
     1. `SALES_REP` can read `GET /api/v1/warehouses` and submit `POST /api/v1/orders` (`PendingApproval`), receives `403` on `PUT /orders/{id}/manager-edit` and `POST /orders/{id}/approve`, and cannot see `WeightedAvgCost`.
     2. `SALES_MGR` can view pending orders, edit submitted orders, override price, approve, or reject.
     3. SignalR `/hubs/operations` rejects unauthenticated connections (`401`), accepts authenticated JWT connections (via `access_token` query string / Bearer header), and delivers `OrderSubmitted` to `SALES_MGR` and `OrderApproved` / `OrderRejected` to the submitting `SALES_REP`!
2. **`@Design System UI`** —
   - Add 2 pure, prop-driven, RTL/LTR logical-CSS components to `packages/ui-kit` (total **26** exported components):
     - `StepProgress` (`src/components/navigation/StepProgress.tsx`) — Accessible numbered wizard stepper (`steps: { id, label, description? }[]`, `activeStepIndex`, optional `onStepSelect` for completed steps).
     - `BottomNavBar` (`src/components/navigation/BottomNavBar.tsx`) — Mobile-first fixed bottom navigation bar (`items: { id, label, href?, icon, badgeCount?, active }[]`, `onSelect?`) with AlMomiaa Gold active indicator, safe-area inset bottom padding, and strict logical CSS.
   - Export both from `packages/ui-kit/src/index.ts`.
   - Author CSF3 Storybook 8 stories (`StepProgress.stories.tsx`, `BottomNavBar.stories.tsx`) in `apps/design-system-studio/src/stories/` and update `Introduction.mdx`.
3. **`@Frontend Engineer`** —
   - **Shared Realtime Client (`packages/api-client`):**
     - Add `@microsoft/signalr` dependency to `packages/api-client`.
     - Create `src/realtime/operations-hub.ts` exporting strongly typed event payloads (`OrderSubmittedEvent`, `CreditCoApprovalRequiredEvent`, `OrderApprovedEvent`, `OrderRejectedEvent`, `OrderUpdatedEvent`, `HubConnectionState`) and `createOperationsHubClient({ hubUrl, getAccessToken, onEvent, onStateChange })` with jittered exponential backoff (`RISK-04`: `[0, 2000, 5000, 10000, 30000]` + random jitter) and safe cleanup (`stop()`).
     - Export from `packages/api-client/src/index.ts` and add unit tests in `packages/api-client/tests/erp-client.test.mjs`.
   - **New Installable Mobile PWA (`apps/rep-pwa` — `@almomiaa/rep-pwa`):**
     - Scaffold Vite + React 19 + TypeScript + Tailwind CSS (reusing `@almomiaa/ui/tailwind-preset` and `@almomiaa/ui/styles.css`) + `react-router-dom` + `vite-plugin-pwa` (Workbox).
     - Configure `vite.config.ts` on port `5173` with:
       - Dev proxy: `/api/v1` -> `http://localhost:5000`, `/health` -> `http://localhost:5000`, `/hubs` -> `http://localhost:5000` (`ws: true`), so same-origin HttpOnly refresh cookies and SignalR WebSockets work cleanly out of the box!
       - `VitePWA` manifest (`name: "المومياء للمبيعات | AlMomiaa Rep"`, `short_name: "AlMomiaa Rep"`, `start_url: "/"`, `display: "standalone"`, `dir: "rtl"`, `lang: "ar"`, `theme_color: "#1C1B1A"`, `background_color: "#F8F7F4"`, PNG icons `pwa-192x192.png` & `pwa-512x512.png` generated from `packages/ui-kit/src/assets/almomiaa-logo.jpg` into `apps/rep-pwa/public/icons/`).
       - Workbox runtimeCaching: precache static shell assets ONLY; enforce `NetworkOnly` for `/api/` and `/hubs/` routes so live pricing, stock, auth, and approval states are never served stale.
     - Implement bilingual (`ar-SA` RTL default + `en-US` LTR) + Light/Dark theme + AuthProvider (cookie refresh + in-memory JWT via `@almomiaa/api-client`) + RealtimeProvider (live SignalR `OperationsHub` + toast notifications + online/offline `navigator.onLine` status banner).
     - Implement all 4 mobile-first screens using `@almomiaa/ui` components (`BrandLogo`, `Button`, `TextField`, `PasswordField`, `SelectField`, `SurfaceCard`, `SummaryCard`, `Badge`, `AlertBanner`, `EmptyState`, `KeyValueList`, `DrawerModal`, `Tabs`, `StepProgress`, `BottomNavBar`, `LocaleSwitcher`, `ThemeToggle`):
       1. `/login` — Employee Code / Phone + Password login, checks `orders:create` claim (shows clean localized access notice if user lacks `orders:create`).
       2. `/` (`RepHomePage`) — Live SignalR connection badge (`متصل مباشر` / `Live`), Rep KPI summary cards (`GET /dashboards/role/rep`), counts of `PendingApproval` / `Approved` / `Rejected` orders, recent 5 sell requests, install-PWA button (when browser emits `beforeinstallprompt`), and primary gold CTA to `/new-request`.
       3. `/new-request` (`NewSellRequestWizardPage`) — 3-Step Guided Wizard (`StepProgress`):
          - **Step 1 (Customer & Credit Check):** Searchable list of assigned customers (`GET /customers`), Warehouse selector (`GET /warehouses`, auto-preselected to `user.defaultWarehouseId`), Channel selector (`PreSales` default; `CashVan` enabled when `repType === "CashVan"` or user switches), and live `GET /customers/{id}/credit-status` card showing Credit Grade (`A`–`D`), Credit Limit, Current Balance, Overdue Debt, Effective Payment Days, and amber `MR-03` Credit Hold warning if limit/overdue is breached.
          - **Step 2 (Categories, Products & Cart):** Category chips/list (`GET /catalog/categories`), product cards from `GET /catalog/categories/{categoryId}/products?customerId=&warehouseId=` displaying Arabic/English name, monospace SKU, resolved customer tier price (`StandardTierPrice`), unit selector (`Carton`/`Box`/`Piece` with `conversionFactor`), real-time `AvailableQty` stock pill (never shows `WeightedAvgCost`), quantity stepper, optional negotiated unit price input (flags amber `MR-02` price override badge if `< StandardTierPrice`, and `RISK-03` shortage badge if `orderedQty > availableQty`), and sticky bottom cart summary bar (`itemCount`, estimated SAR total, "Review Request" button).
          - **Step 3 (Review & Submit):** Complete line-item table/cards with unit, qty, unit price, 15% VAT, line total (`tabular-nums`), subtotal/VAT/grand total in SAR, warning summary banners (`MR-02` / `MR-03` / `RISK-03`), submit button calling `POST /api/v1/orders` (auto-injects `X-Idempotency-Key` GUID, locked while submitting to prevent double-submit), followed by a crisp **Submitted — Pending Manager Approval** confirmation screen showing `OrderNumber`, `Status = PendingApproval`, 24h `ReservationExpiresAtUtc`, and buttons to "Track My Requests" (`/requests`) or "New Request".
       4. `/requests` (`MyRequestsPage`) — Filter tabs (`All`, `PendingApproval`, `Approved`, `Rejected`, `Shipped`), order cards/list (`GET /orders`), real-time updates when `OrderApproved` / `OrderRejected` / `OrderUpdated` arrives via SignalR, visible rejection reason banner on rejected orders, and a read-only **Request Details `DrawerModal`** (strictly NO edit/approve controls for `SALES_REP`, displaying an informational lock note: "Submitted requests are locked for Sales Manager review").
   - **Enhanced `SALES_MGR` Approval Queue in `apps/web-erp`:**
     - Add `RealtimeProvider` (`apps/web-erp/src/providers/RealtimeProvider.tsx`) connecting to `/hubs/operations` once authenticated (using `NEXT_PUBLIC_API_BASE_URL ?? http://localhost:5000`), showing a live SignalR indicator in `ErpAppLayout` and dismissible floating toast notifications (`AlertBanner`) whenever `OrderSubmitted` or `CreditCoApprovalRequired` arrives.
     - Enhance `apps/web-erp/src/features/sales/SalesWorkspace.tsx` by adding a prominent default **`Pending Approvals`** (`طلبات بانتظار الاعتماد`) tab with live pending count badge for `SALES_MGR` / `SUPER_ADMIN` / `EXEC_MGR` / `ACCOUNTANT`:
       - Dedicated queue table of `PendingApproval` orders displaying: Order Number (`sku-mono`), Channel, Customer / Rep IDs, Warehouse name (loaded via `GET /warehouses`), Total SAR (`tabular-nums`), Governance Flags (`MR-03 Credit Hold`, `MR-02 Price Override`, `RISK-03 Shortage`), Reservation Expiry (`24h`), and quick **Review & Decide** button.
       - Slide-over **Manager Review Drawer** allowing `SALES_MGR` to inspect every line item (`StandardTierPrice` vs `UnitPrice`, `OrderedQty` vs `ApprovedQty`), execute **Manager Edit** (`PUT /orders/{id}/manager-edit`), **Approve Price Override** (`POST /orders/{id}/override-price`), **Co-Approve Credit** (`POST /orders/{id}/co-approve-credit`, if permitted), **Approve Order** (`POST /orders/{id}/approve`), or **Reject Order** with mandatory reason (`POST /orders/{id}/reject`) — automatically refreshing the queue and pushing the live decision to the `SALES_REP`'s PWA via SignalR!
   - Update `docs/architecture/06-API-Specification.md` if any event payload detail is refined.
4. **`@QA & Security Auditor`** —
   - Verify `.NET 9` build (0 warnings, 0 errors) and all `dotnet test` tests passing (including new SignalR + `SALES_REP`/`SALES_MGR` workflow integration tests).
   - Verify `pnpm -r typecheck`, `pnpm -r lint`, `@almomiaa/ui` build, `@almomiaa/api-client` build & tests, `apps/design-system-studio` `build-storybook` (26 stories), `apps/rep-pwa` production Vite + Workbox PWA build (`dist/manifest.webmanifest` + `dist/sw.js` generated), and `apps/web-erp` Next.js 15 production build.
   - Verify static security & design-system gates: zero forbidden imports in `packages/ui-kit`, zero hardcoded hex colors in TSX/TS (outside `tokens.css` & manifest/config), zero directional Tailwind classes (`ml-`/`mr-`/`pl-`/`pr-`/`left-`/`right-`/`text-left`/`text-right`), zero `any`/`@ts-ignore`, zero JWT/refresh-token storage in `localStorage`/`sessionStorage`/IndexedDB, Workbox `NetworkOnly` for `/api/` & `/hubs/`, and zero committed secrets.
5. **`@Orchestrator`** — Archive as `docs/agent-ops/prompt-history/0008-sales-rep-pwa-signalr-and-manager-approval-queue.md`, update `docs/agent-ops/PROJECT_LOG.md`, and mark `CURRENT_PROMPT.md` completed only after QA returns `PASS`.

---

## 3. Target Files to Create / Modify

### `apps/backend-api/`
- `src/AlMomiaa.Application/Common/Abstractions/IOperationsRealtimeNotifier.cs` (new) — Interface + typed realtime notification records (`OrderSubmittedNotification`, `CreditCoApprovalRequiredNotification`, `OrderApprovedNotification`, `OrderRejectedNotification`, `OrderUpdatedNotification`).
- `src/AlMomiaa.Infrastructure/Realtime/NoOpOperationsRealtimeNotifier.cs` (new) + register in `DependencyInjection.cs`.
- `src/AlMomiaa.Infrastructure/Persistence/Services/ProcurementAndSalesServices.cs` (modify) — Dispatch realtime notifications via `IOperationsRealtimeNotifier` strictly **after** `SaveChangesAsync` succeeds on `CreateOrder`, `ManagerEdit`, `OverridePrice`, `CoApproveCredit`, `Approve`, and `Reject`.
- `src/AlMomiaa.Infrastructure/Persistence/Services/CatalogAndInventoryServices.cs` (modify) — Ensure `GetWarehousesAsync` safely scopes `SALES_REP` to assigned warehouses (if assigned) or active warehouses (read-only metadata only, no stock quantities).
- `src/AlMomiaa.WebApi/Hubs/OperationsHub.cs` (new) — `[Authorize]` SignalR hub mapping authenticated connections into `user:{userId}`, `role:{roleCode}`, and `warehouse:{warehouseId}` groups.
- `src/AlMomiaa.WebApi/Hubs/SignalROperationsRealtimeNotifier.cs` (new) — `IHubContext<OperationsHub>` implementation pushing events to canonical role/user/warehouse groups.
- `src/AlMomiaa.WebApi/Controllers/WarehousesController.cs` (modify) — Apply `[Authorize(Policy = "WarehousesRead")]` to `GET /api/v1/warehouses`.
- `src/AlMomiaa.WebApi/Program.cs` (modify) — Add `AddSignalR()`, `SignalROperationsRealtimeNotifier`, `JwtBearerEvents.OnMessageReceived` for `/hubs`, `"WarehousesRead"` policy, and `MapHub<OperationsHub>("/hubs/operations").RequireCors("TrustedFrontend")`.
- `tests/AlMomiaa.AuthenticationTests/SalesApprovalAndSignalRTests.cs` (new) — End-to-end integration tests for `SALES_REP` warehouse read, order submit, edit-lock `403`, `SALES_MGR` approve/reject, and live SignalR `/hubs/operations` push delivery + `401` unauthenticated rejection.

### `packages/ui-kit/` & `apps/design-system-studio/`
- `packages/ui-kit/src/components/navigation/StepProgress.tsx` (new)
- `packages/ui-kit/src/components/navigation/BottomNavBar.tsx` (new)
- `packages/ui-kit/src/index.ts` (modify — export `StepProgress`, `BottomNavBar` and prop types)
- `apps/design-system-studio/src/stories/StepProgress.stories.tsx` (new)
- `apps/design-system-studio/src/stories/BottomNavBar.stories.tsx` (new)
- `apps/design-system-studio/src/stories/Introduction.mdx` (modify — list all 26 components)

### `packages/api-client/`
- `package.json` (modify — add `@microsoft/signalr`)
- `src/realtime/operations-hub.ts` (new — typed SignalR hub client with jittered exponential reconnect backoff)
- `src/index.ts` (modify — export realtime client & types)
- `tests/erp-client.test.mjs` (modify — add unit test verifying realtime event types / backoff config)

### `apps/rep-pwa/` (New Workspace Package: `@almomiaa/rep-pwa`)
- `package.json`, `tsconfig.json`, `vite.config.ts`, `tailwind.config.ts`, `postcss.config.js`, `eslint.config.mjs`, `index.html`, `.env.local.example`
- `public/icons/pwa-192x192.png` & `public/icons/pwa-512x512.png` (generated from `packages/ui-kit/src/assets/almomiaa-logo.jpg`)
- `src/main.tsx`, `src/App.tsx`, `src/styles/globals.css`
- `src/lib/api-client.ts`, `src/lib/formatters.ts`
- `src/locales/ar.ts`, `src/locales/en.ts`
- `src/providers/AppProviders.tsx` (Theme + Locale + AuthProvider + RealtimeProvider + Online/Offline detector + PWA Install Prompt)
- `src/components/RepMobileShell.tsx` (Mobile header with `BrandLogo`, live SignalR indicator, `LocaleSwitcher`, `ThemeToggle`, logout + `BottomNavBar` + floating live SignalR toast banner)
- `src/pages/LoginPage.tsx`
- `src/pages/RepHomePage.tsx`
- `src/pages/NewSellRequestPage.tsx` (3-Step Wizard: Customer & Credit Check -> Category/Products & Price/Stock -> Cart Review & Submit)
- `src/pages/MyRequestsPage.tsx` (Status filter tabs + live SignalR refresh + read-only Detail Drawer)

### `apps/web-erp/`
- `src/providers/RealtimeProvider.tsx` (new — SignalR `/hubs/operations` connection, live toast notifications, event subscription hook)
- `src/app/layout.tsx` (modify — wrap with `RealtimeProvider`)
- `src/components/ErpAppLayout.tsx` (modify — show live SignalR status pill + toast alerts on `OrderSubmitted` / `CreditCoApprovalRequired`)
- `src/features/sales/SalesWorkspace.tsx` (modify — add dedicated `Pending Approvals` tab with live count badge, warehouse name resolution, instant SignalR refresh, and streamlined Manager Review & Decide drawer)
- `src/locales/ar.ts` & `src/locales/en.ts` (modify — add bilingual keys for Pending Approvals queue & live SignalR notifications)

---

## 4. Frontend & Backend Data Schema

### SignalR Event Payloads (`OperationsHub` `/hubs/operations`)
- **`OrderSubmitted`** (sent to `role:SALES_MGR`, `role:SUPER_ADMIN`):
  ```ts
  interface OrderSubmittedEvent {
    orderId: string;
    orderNumber: string;
    customerId: string;
    repId: string;
    repName: string;
    warehouseId: number;
    totalAmount: number;
    exceededCreditWarning: boolean;
    requiresFinanceCoApproval: boolean;
    hasStockShortage: boolean;
    hasPriceOverridePending: boolean;
    createdAtUtc: string;
  }
  ```
- **`CreditCoApprovalRequired`** (sent to `role:ACCOUNTANT`, `role:EXEC_MGR`, `role:SUPER_ADMIN`):
  ```ts
  interface CreditCoApprovalRequiredEvent {
    orderId: string;
    orderNumber: string;
    customerId: string;
    customerBalanceAtOrder: number;
    customerCreditLimitAtOrder: number;
    totalAmount: number;
  }
  ```
- **`OrderApproved`** (sent to `user:{repId}`, `warehouse:{warehouseId}`, `role:PURCH_MGR`, `role:SALES_MGR`, `role:SUPER_ADMIN`):
  ```ts
  interface OrderApprovedEvent {
    orderId: string;
    orderNumber: string;
    repId: string;
    warehouseId: number;
    totalAmount: number;
    approvedAtUtc: string;
  }
  ```
- **`OrderRejected`** (sent to `user:{repId}`, `role:SALES_MGR`, `role:SUPER_ADMIN`):
  ```ts
  interface OrderRejectedEvent {
    orderId: string;
    orderNumber: string;
    repId: string;
    rejectionReason: string;
    rejectedAtUtc: string;
  }
  ```
- **`OrderUpdated`** (sent to `user:{repId}`, `role:SALES_MGR`, `role:SUPER_ADMIN` after Manager Edit / Price Override / Credit Co-Approval):
  ```ts
  interface OrderUpdatedEvent {
    orderId: string;
    orderNumber: string;
    repId: string;
    status: string;
    totalAmount: number;
    changeType: "ManagerEdit" | "PriceOverrideApproved" | "CreditCoApproved";
  }
  ```

### New Sell Request Cart Line Schema (`apps/rep-pwa`, validated with Zod before submit)
```ts
interface SellRequestCartLine {
  masterProductId: string; // UUID
  productUnitId: string;   // UUID
  sku: string;
  productNameAr: string;
  productNameEn: string;
  unitNameAr: string;
  unitNameEn: string;
  conversionFactor: number;
  standardTierPrice: number;
  unitPrice: number;       // > 0
  orderedQty: number;      // > 0
  availableQty: number;    // live snapshot for RISK-03 warning
}
```
- Maps 1:1 to `POST /api/v1/orders` (`CreateSalesOrderRequest`) with automatic `X-Idempotency-Key: <UUID>` generated per checkout attempt (regenerated only after success or cart modification to prevent double submission).

---

## 5. Security, Design System & Operational Rules

1. **Three-Tier Authorization (`07-Security.md`):**
   - `SALES_REP` is strictly blocked from `PUT /orders/{id}/manager-edit`, `POST /orders/{id}/override-price`, `POST /orders/{id}/co-approve-credit`, `POST /orders/{id}/approve`, and `POST /orders/{id}/reject` at UI, API, and Domain levels.
   - `WeightedAvgCost` is NEVER displayed in `apps/rep-pwa` (and remains masked `null` server-side for `SALES_REP`).
   - SignalR group membership is derived **exclusively** from server-verified JWT claims (`sub`, `role`, `warehouse_id`) inside `OperationsHub.OnConnectedAsync` — clients can NEVER self-join arbitrary role or user groups.
2. **Token & PWA Cache Hygiene:**
   - Access JWTs remain strictly in memory (`createAccessTokenStore`); refresh tokens remain in `HttpOnly; SameSite=Strict` cookies via same-origin `/api/v1/auth` proxy. Never store tokens in `localStorage`, `sessionStorage`, or Service Worker caches.
   - Workbox Service Worker MUST use `NetworkOnly` for `/api/` and `/hubs/` so authenticated business data is never persisted in browser Cache Storage.
3. **Strict Design Tokens & Logical RTL/LTR CSS:**
   - Zero hardcoded hex colors in TSX/TS component code (semantic Tailwind tokens only; hex colors allowed strictly in `tokens.css` and PWA Web Manifest config).
   - Zero directional Tailwind utilities (`ml-`, `mr-`, `pl-`, `pr-`, `left-`, `right-`, `text-left`, `text-right`); use logical `ms-`, `me-`, `ps-`, `pe-`, `start-`, `end-`, `text-start`, `text-end`.
   - Use `tabular-nums` for all SAR totals & quantities and `font-mono dir="ltr"` for SKUs and order numbers.
   - Zero `any`, zero `@ts-ignore`, zero committed secrets.

---

## 6. Acceptance Criteria (Definition of Done)

- [ ] `.NET 9` backend implements `[Authorize] OperationsHub` (`/hubs/operations`), `IOperationsRealtimeNotifier`, `SignalROperationsRealtimeNotifier` (dispatched post-commit on order submit, manager edit, price override, credit co-approval, approve, reject), `JwtBearerEvents.OnMessageReceived` for `/hubs`, and `"WarehousesRead"` policy on `GET /api/v1/warehouses` — preserving Clean Architecture (`AlMomiaa.ArchitectureTests` passes).
- [ ] Backend `dotnet build` (0 warnings, 0 errors) and `dotnet test` pass all existing 19 tests **plus** new `SalesApprovalAndSignalRTests` proving `SALES_REP` submit -> `PendingApproval`, `SALES_REP` `403` on edit/approve, `SALES_MGR` approve/reject, and live SignalR event delivery (`OrderSubmitted`, `OrderApproved`, `OrderRejected`) + `401` for anonymous hub clients.
- [ ] `@almomiaa/ui` exports `StepProgress` and `BottomNavBar` (26 total pure components, zero forbidden imports, zero hardcoded hex colors, zero directional CSS), and `apps/design-system-studio` builds cleanly with **26** CSF3 Storybook 8 stories.
- [ ] `@almomiaa/api-client` exports `createOperationsHubClient` with jittered exponential backoff reconnect and typed SignalR event handlers, and all `@almomiaa/api-client` tests pass.
- [ ] `apps/rep-pwa` builds cleanly (`pnpm --filter @almomiaa/rep-pwa build`), emits valid `manifest.webmanifest` + Workbox `sw.js` (`NetworkOnly` for `/api/` & `/hubs/`), and implements bilingual RTL/LTR Login, Rep Home, 3-Step New Sell Request Wizard (Customer & Credit Check -> Category/Products & Price/Stock -> Cart & Submit), and My Requests Tracker with live SignalR status updates and read-only post-submission lock.
- [ ] `apps/web-erp` builds cleanly (`pnpm --filter @almomiaa/web-erp build`), connects to `/hubs/operations` via `RealtimeProvider`, displays live SignalR toasts on `OrderSubmitted` / `CreditCoApprovalRequired`, and provides a dedicated `Pending Approvals` tab & Manager Review Drawer on `/sales` for `SALES_MGR`.
- [ ] Full workspace verification passes (`pnpm -r typecheck`, `pnpm -r lint`, all package/app builds, `dotnet test`) and `@QA & Security Auditor` returns `PASS`, after which `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0008-sales-rep-pwa-signalr-and-manager-approval-queue.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks `CURRENT_PROMPT.md` completed.
