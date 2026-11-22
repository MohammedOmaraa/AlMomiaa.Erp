# PROMPT-0000: Bootstrap Reconciled Architecture Suite & VS Code Agentic Engineering System

- **Executed On:** 2026-11-22
- **Scope:** Documentation & Agentic Workspace Scaffolding (Zero Application Code)
- **Agents / Roles:** Architecture Builder, Orchestrator
- **Status:** Completed

---

## 1. Key Architectural Decisions Locked In
1. **Canonical Naming:** Standardized exclusively on `AlMomiaa` (`AlMomiaa.Erp.sln`, `AlMomiaa.Domain`, `@almomiaa/ui`, `@almomiaa/api-client`, `@almomiaa/shared-types`).
2. **Unified 3-App Frontend + .NET 9 Modular Monolith Topology:**
   - `apps/web-erp` — Next.js 15+ (App Router), React 19, TypeScript, Tailwind CSS (Desktop & Warehouse Tablet)
   - `apps/rep-pwa` — Vite + React 19 Offline-First Mobile PWA (`Workbox` + `Dexie.js`)
   - `apps/design-system-studio` — Standalone Storybook 8 + Vite showcase for `@almomiaa/ui`
   - `apps/backend-api` — `.NET 9` Clean Architecture Modular Monolith + CQRS/MediatR + EF Core 9 + SQL Server 2022 + Redis 7 + SignalR + Hangfire
   - `packages/ui-kit`, `packages/api-client`, `packages/shared-types`
3. **Database & API Completeness (`05-Database.md`, `06-API-Specification.md`, `07-Security.md`):**
   - Added complete DDL tables and matching API/permission entries for all Principal Architect audit findings:
     - `MR-01`: `inv.Warehouses` (`WarehouseType = 'MobileVan'`, `AssignedRepId`) + `sales.SalesOrders.OrderChannel`
     - `MR-02`: `cat.PriceLists`, `cat.PriceListItems`, `MinAllowedPrice` floor, `PriceOverridePending`, `orders:override_price`
     - `MR-03`: Two-tier credit gate (`RequiresFinanceCoApproval`, `FinanceCoApprovedByUserId`, `orders:coapprove_credit`)
     - `MR-04`: `sales.DeliveryDispatches`, `sales.DeliveryDispatchItems` (Driver Manifest & POD tracking)
     - `MR-05`: `fin.FiscalPeriods` (Accounting Period Lock & Fiscal Closing, `finance:close_period`)
     - `RISK-01`: `ReservationExpiresAtUtc` & `ReservationState` on `sales.SalesOrders`
     - `RISK-02`: `sales.ZatcaDeviceSequences` (`UPDLOCK, HOLDLOCK` single-concurrency ICV/PIH lock)
     - `RISK-03`: `HasStockShortage` graceful offline order acceptance
     - Completed double-entry ledger & operational tables: `fin.JournalEntries`, `fin.JournalLines`, `fin.DailyFinancialSnapshots`, `sales.SalesReturns`, `sales.SalesReturnItems`, `proc.PurchaseOrderItems`, `proc.GoodsReceiptItems`, `inv.StockTransferItems`, `id.UserWarehouses`, `sys.OutboxMessages`.

---

## 2. Files Created
- `.github/copilot-instructions.md`
- `.github/agents/01-architecture-builder.agent.md`
- `.github/agents/02-prompt-constructor.agent.md`
- `.github/agents/03-orchestrator.agent.md`
- `.github/agents/04-design-system-ui.agent.md`
- `.github/agents/05-frontend-engineer.agent.md`
- `.github/agents/06-backend-dotnet.agent.md`
- `.github/agents/07-qa-security-auditor.agent.md`
- `.github/skills/tanstack-virtual-grid/SKILL.md`
- `.github/skills/zatca-phase2-compliance/SKILL.md`
- `.github/skills/cqrs-efcore-patterns/SKILL.md`
- `.github/skills/design-token-governance/SKILL.md`
- `docs/architecture/00-ADR-Architecture-Decisions.md`
- `docs/architecture/01-PRD.md`
- `docs/architecture/02-SRS.md`
- `docs/architecture/03-HLD.md`
- `docs/architecture/04-LLD.md`
- `docs/architecture/05-Database.md`
- `docs/architecture/06-API-Specification.md`
- `docs/architecture/07-Security.md`
- `docs/architecture/08-Development-Roadmap.md`
- `docs/agent-ops/PROJECT_LOG.md`
- `docs/agent-ops/CURRENT_PROMPT.md`
- `docs/agent-ops/prompt-history/0000-bootstrap-architecture-and-agentic-system.md`
