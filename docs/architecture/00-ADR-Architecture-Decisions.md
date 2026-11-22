# 00 - Architecture Decision Records (ADRs) & Principal Architect Audit
**Project Name:** AlMomiaa Enterprise ERP & Distribution Management System (DMS)  
**Company:** AlMomiaa Trading Co. (`شركة المومياء للتجارة`) — Riyadh, Kingdom of Saudi Arabia  
**Baseline Version:** 3.1 (Reconciled Unified Monorepo & Principal Architect Final Audit)  
**Owner:** `@Architecture Builder`

---

## 1. Canonical Naming & Monorepo Topology (ADR-001)

### Decision
Standardize all solution namespaces, npm scopes, and documentation on **`AlMomiaa`** (double `a`, matching `AlMomiaa-app`), organized as a single **pnpm + Turborepo** workspace alongside the **.NET 9 Modular Monolith**:

```text
AlMomiaa-app/
├── .github/
│   ├── copilot-instructions.md
│   ├── agents/                              # 01..07 Custom VS Code Agents
│   └── skills/                              # 4 Domain & Engineering Skills
├── docs/
│   ├── architecture/                        # Synchronized 00-ADR .. 08-Development-Roadmap
│   └── agent-ops/                           # CURRENT_PROMPT.md, PROJECT_LOG.md, prompt-history/
├── apps/
│   ├── web-erp/                             # Next.js 15+ (App Router), React 19, TS, Tailwind (Desktop & Warehouse Tablet)
│   ├── rep-pwa/                             # Vite + React 19, TS, Tailwind, Workbox + Dexie.js Offline Mobile PWA
│   ├── design-system-studio/                # Standalone Storybook 8 + Vite showcase for @almomiaa/ui
│   └── backend-api/                         # .NET 9 Modular Monolith (AlMomiaa.Erp.sln)
│       ├── src/
│       │   ├── AlMomiaa.Domain/
│       │   ├── AlMomiaa.Application/
│       │   ├── AlMomiaa.Infrastructure/
│       │   └── AlMomiaa.WebApi/
│       └── tests/
├── packages/
│   ├── ui-kit/                              # @almomiaa/ui — shared tree-shaken RTL/LTR UI components & tokens
│   ├── api-client/                          # @almomiaa/api-client — Axios + OpenAPI types + Zod + TanStack Query v5
│   └── shared-types/                        # @almomiaa/shared-types — Enums, ZATCA constants, SAR/VAT helpers, Permissions
├── pnpm-workspace.yaml
└── turbo.json
```

### Rationale
- Splitting `apps/web-erp` (Next.js 15 SSR dashboards & heavy back-office/warehouse tablet workflows) from `apps/rep-pwa` (lightweight Vite/React 19 offline-first PWA with `Dexie.js` & Workbox) keeps the mobile field app ultra-fast on cellular connections without Next.js SSR overhead, while sharing 100% of UI primitives (`@almomiaa/ui`), API hooks (`@almomiaa/api-client`), and domain helpers (`@almomiaa/shared-types`).

---

## 2. Frontend & Integration Architectural Decisions (ADR-002 to ADR-006)

| ADR ID | Decision Area | Decision | Why It Matters for AlMomiaa ERP |
| :--- | :--- | :--- | :--- |
| **ADR-002** | **Design System Split** | Separate `packages/ui-kit` (`@almomiaa/ui` npm workspace package) + `apps/design-system-studio` (Storybook 8 + Vite standalone app). | Enables isolated visual component engineering in Storybook while providing zero-latency, tree-shaken imports across both `web-erp` and `rep-pwa`. |
| **ADR-003** | **HTTP & API Client** | Wrap **Axios** inside `packages/api-client` paired with **TanStack Query v5** + **Zod** runtime validation. | Axios cleanly handles JWT refresh interceptors, `Accept-Language: ar-SA`, `X-Idempotency-Key`, and request cancellation; TanStack Query deduplicates calls, invalidates caches, and supports optimistic updates. |
| **ADR-004** | **Table Virtualization** | Use **`@tanstack/react-table`** + **`@tanstack/react-virtual`** combined with **server-side keyset/cursor pagination** (or indexed offset) for datasets > 50,000 rows. | Renders 10,000+ rows at 60fps without DOM freezes while preventing multi-megabyte JSON transfers over mobile field networks. |
| **ADR-005** | **Backend Contract Sync** | Export OpenAPI (`swagger.json`) from `.NET 9` WebApi and auto-generate TypeScript DTOs into `packages/api-client`. | Eliminates frontend/backend schema drift when adding grid columns, enum states, or DTO properties. |
| **ADR-006** | **State & Offline PWA** | Use **Zustand** (UI/cart/session state) + **Dexie.js (IndexedDB)** for offline order/collection queuing in `apps/rep-pwa`. | Field sales reps in Riyadh warehouses or delivery routes frequently encounter dead zones; idempotent offline queuing guarantees zero lost orders or collections. |

---

## 3. Backend & Data Architecture Decisions (ADR-007 to ADR-010)

| ADR ID | Decision Area | Decision | Why It Matters for AlMomiaa ERP |
| :--- | :--- | :--- | :--- |
| **ADR-007** | **Modular Monolith vs. Microservices** | **Pragmatic Modular Monolith + Clean Architecture** in `.NET 9` (`AlMomiaa.Erp.sln`) with isolated SQL schemas (`id`, `cust`, `cat`, `inv`, `proc`, `sales`, `fin`, `sys`) and `ArchUnitNET` boundary enforcement. | Guarantees single-transaction ACID consistency across Order + Credit + Stock Reservation + Double-Entry Ledger without Saga/Kafka operational overhead, while remaining extraction-ready. |
| **ADR-008** | **ORM Strategy** | Use **EF Core 9** for all Commands and compiled `.AsNoTracking()` DTO projections for 95% of Queries; reserve SQL Views strictly for complex financial aging matrices. | Eliminates dual-ORM boilerplate while retaining SQL Server Temporal Tables, Global Query Filters (Rep/Warehouse isolation), and `ROWVERSION` concurrency. |
| **ADR-009** | **50M+ Record Scalability** | Use `.NET 9` sequential **UUIDv7** (`Guid.CreateVersion7()`) PKs, **Monthly Table Partitioning** on append-only tables (`inv.StockMovements`, `fin.JournalLines`, `sys.AuditLogs`, `sys.GpsRouteHistory`), and **Redis KPI Hashes + `fin.DailyFinancialSnapshots`**. | Eliminates clustered index page-split fragmentation and delivers `< 15ms` Executive/Finance dashboard queries regardless of historical row count. |
| **ADR-010** | **Background Jobs & Reliability** | Use **Hangfire** (SQL Server / Redis) paired with **`sys.OutboxMessages`** transactional outbox. | Ensures SignalR broadcasts, ZATCA Phase 2 reporting/clearance, SMS OTPs, and 24-hour stock reservation expiry run reliably with zero lost events. |

---

## 4. Brand & Design System Tokens (ADR-011)

Enforced as the single source of truth in `packages/ui-kit`:

| Token Name | Value | Usage |
| :--- | :--- | :--- |
| `--brand-gold-primary` | `#CFA434` | Primary CTAs, active navigation states, key data badges |
| `--brand-gold-hover` | `#B88E24` | Button hover states, focus rings |
| `--brand-gold-light` | `#F5EED8` | Soft alert backgrounds, active table row highlights |
| `--brand-gold-metallic` | `#D4AF37` | Metallic brand hero accents |
| `--surface-obsidian` | `#1C1B1A` | Primary sidebar, dark-mode headers, brand hero sections |
| `--surface-charcoal` | `#262523` | Elevated dark cards, dropdown menus |
| `--surface-canvas` | `#F8F7F4` | Light-mode workspace background (warm off-white complementing gold) |
| `--font-arabic` | `'IBM Plex Sans Arabic', 'Cairo', sans-serif` | Primary Arabic typography (RTL default) |
| `--font-latin` | `'Inter', 'Plus Jakarta Sans', sans-serif` | SKUs, invoice numbers, English labels (LTR) |

---

## 5. Principal Architect Deep-Dive Audit: Missing Requirements Resolved (MR-01 to MR-05)

| ID | Missing Requirement | Real-World Impact at AlMomiaa | Architectural Resolution (Synced Across Docs `01`–`08`) |
| :--- | :--- | :--- | :--- |
| **MR-01** | **Van Sales (Cash Van) vs. Pre-Sales Split** | Retail reps servicing Baqalas carry stock directly in vans and invoice on the spot, while Wholesale reps take pre-orders for next-day delivery. | Treat each delivery van as a Mobile Virtual Warehouse (`inv.Warehouses.WarehouseType = 'MobileVan'`, `AssignedRepId`). Van reps invoice directly against van stock; Pre-Sales reps follow the 10-step approval workflow. |
| **MR-02** | **Dynamic Price Lists, Tiered Pricing & Minimum Price Floor** | Hypermarkets buying 500 cartons get different pricing than small restaurants buying 5 cartons; reps negotiate on-site discounts. | Add `cat.PriceLists` + `cat.PriceListItems` plus hard `MinAllowedSellingPrice` on `MasterProducts`. Below-tier or below-floor proposals flag `PriceOverridePending = true` requiring `orders:override_price` approval by `SALES_MGR`. |
| **MR-03** | **Customer Credit Hold vs. Credit Limit Override** | A Grade A customer (60d, 100k SAR) may have an invoice 62 days overdue. Hard-blocking loses business; silent acceptance creates bad debt. | Implement a **Two-Tier Credit Gate**: orders breaching credit limit or aging submit into `PendingApproval` with `ExceededCreditWarning = true` and `RequiresFinanceCoApproval = true`, requiring explicit `SALES_MGR` + `ACCOUNTANT`/`EXEC_MGR` co-approval (`orders:coapprove_credit`). |
| **MR-04** | **Driver / Delivery Manifest & POD (Proof of Delivery)** | After warehouse preparation, operations must track which driver/van took which invoices and confirm signed delivery. | Add `sales.DeliveryDispatches` & `sales.DeliveryDispatchItems` tracking `PreparedAtWarehouse -> OutForDelivery -> Delivered` with uploaded signed invoice POD. |
| **MR-05** | **Accounting Period Lock & Fiscal Closing** | Verifying an October bank transfer on November 5 after October's ZATCA VAT return is filed causes ledger-to-tax drift. | Add `fin.FiscalPeriods` with `IsClosed` lock. Financial journal entries cannot post into closed months; late verifications post to the current open period while storing `OriginalValueDateUtc`. |

---

## 6. Architectural Risks & Mitigation Matrix (RISK-01 to RISK-04)

| Risk ID | Architectural Risk | Severity | Failure Scenario | Mitigation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **RISK-01** | **Stock Reservation Starvation ("Zombie Reservations")** | High | Reps submit 80 orders Thursday afternoon; unreviewed until Sunday, locking 4,000 cartons away from cash buyers. | Add `ReservationExpiresAtUtc` (configurable default 24h) processed by Hangfire. Unapproved reservations expire to `SoftDemandWarning` or escalate via push notification to `EXEC_MGR`. |
| **RISK-02** | **ZATCA Cryptographic Hash-Chain Contention** | High | Concurrent invoice generation across multiple warehouses forks the sequential `ICV` and previous invoice hash (`PIH`). | Funnel final XML signing and `ICV`/`PIH` allocation through a single-concurrency SQL row lock (`WITH (UPDLOCK, HOLDLOCK)`) on `sales.ZatcaDeviceSequences` per EGS unit. |
| **RISK-03** | **Offline PWA Price/Stock Drift** | Medium | Rep stays offline 6 hours, queues 12 orders with morning stock/price snapshots, syncs at 4 PM when 2 items are out of stock. | `POST /api/v1/orders` NEVER drops an offline order. If `AvailableQty < OrderedQty`, it accepts into `PendingApproval` with `HasStockShortage = true` so `SALES_MGR` can adjust quantities or trigger an inter-warehouse transfer. |
| **RISK-04** | **SignalR Hub Reconnection Storm** | Medium | Mobile network drop in Riyadh causes 200 PWAs + 30 warehouse tablets to reconnect simultaneously, hammering SQL Server. | Apply jittered exponential backoff on SignalR clients and serve reconnection state + live KPI snapshots from Redis Hashes rather than SQL Server. |

---

## 7. Comparison of Alternative Architectural Approaches

| Evaluation Criterion | Approach A: Traditional N-Tier Monolith | Approach B: Distributed Microservices | Approach C: Pragmatic Modular Monolith + Clean Architecture (**FINAL CHOICE**) |
| :--- | :--- | :--- | :--- |
| **Transactional Consistency (Order + Credit + Stock + Ledger)** | High (Single DB Transaction) | Very Difficult (Eventual consistency, Sagas, compensating rollbacks) | **High** (Single ACID DB Transaction across schema boundaries) |
| **Module Isolation & Maintainability** | Poor (High coupling over time) | High (Separate processes) | **High** (Isolated `.NET` modules, SQL schemas `sales.*`, `inv.*`, `fin.*`, `ArchUnitNET`) |
| **Operational & Hosting Cost in KSA** | Low | Very High (K8s, service mesh, Kafka brokers) | **Low-to-Medium** (2 stateless API nodes + Managed SQL Server + Redis) |
| **Real-Time SignalR Latency** | Fast | Complex (Cross-service event bus bridging) | **Ultra-Fast** (In-process domain event -> Outbox -> Redis SignalR backplane) |
| **Future Split Readiness** | Requires total rewrite | Already split | **High** (Any bounded context can be extracted with minimal refactor) |

---

## 8. Agent Roster & Skill Matrix (Handoff to Orchestrator)

| Agent File | Role | Primary Ownership | Mandatory Skills |
| :--- | :--- | :--- | :--- |
| `01-architecture-builder.agent.md` | Principal Architect | `docs/architecture/*`, ADRs, monorepo governance | All architecture docs |
| `02-prompt-constructor.agent.md` | Technical PM / Prompt Engineer | `docs/agent-ops/CURRENT_PROMPT.md` | `00-ADR`, `05-Database`, `06-API`, `07-Security` |
| `03-orchestrator.agent.md` | Engineering Coordinator | Execution routing, `PROJECT_LOG.md`, `prompt-history/*` | All agents & quality gates |
| `04-design-system-ui.agent.md` | Design System Engineer | `packages/ui-kit`, `apps/design-system-studio` | `design-token-governance`, `tanstack-virtual-grid` |
| `05-frontend-engineer.agent.md` | Senior Frontend Engineer | `apps/web-erp`, `apps/rep-pwa`, `packages/api-client`, `packages/shared-types` | `tanstack-virtual-grid`, `design-token-governance` |
| `06-backend-dotnet.agent.md` | Principal .NET 9 Architect | `apps/backend-api` (`AlMomiaa.*`) | `cqrs-efcore-patterns`, `zatca-phase2-compliance` |
| `07-qa-security-auditor.agent.md` | QA & Security Gate | Acceptance criteria, RBAC, RTL, ZATCA & build verification | All 4 skills + `07-Security` |
