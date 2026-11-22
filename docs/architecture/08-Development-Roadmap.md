# 08 - Phased Implementation & Delivery Roadmap (مراحل التنفيذ)
**Project Name:** AlMomiaa Enterprise ERP & DMS  
**Delivery Methodology:** 4 Production Phases (16 Weeks / 8 Two-Week Agile Sprints)  
**Version:** 3.1 (Aligned with Unified 3-App Monorepo & Design System Split)

---

## 1. Execution Strategy
Development follows a **Vertical Slice Milestone Strategy** governed by our VS Code Agentic Engineering workflow (`@Prompt Constructor` -> `@Orchestrator` -> Specialist Agents -> `@QA & Security Auditor` -> `PROJECT_LOG.md`):
- **Phase 1 (Weeks 1–4):** Monorepo & Design System Studio, Core `.NET 9` Foundation, IAM, Master Catalog, Price Lists & Customer Credit Engine.
- **Phase 2 (Weeks 5–8):** Multi-Warehouse & Mobile Van Inventory, Purchasing & Container Landed Cost Engine.
- **Phase 3 (Weeks 9–12):** Offline Mobile Rep PWA (`rep-pwa`), 10-Step Sales Order Workflow, Real-Time SignalR, ZATCA Phase 2 Invoicing, Returns & Delivery POD.
- **Phase 4 (Weeks 13–16):** Cash/Bank Collection Custody, Treasury Reconciliation, Fiscal Period Locking, 7 Role Dashboards, GPS Hook & Riyadh Production Cutover.

---

## 2. Detailed Sprint-by-Sprint Breakdown

### Phase 1: Monorepo, Design System, Security, Catalog & Customers (Weeks 1–4)

#### Sprint 1: Monorepo Scaffolding, Design System Studio, IAM & Audit Engine (Weeks 1–2)
- Initialize `pnpm` + `Turborepo` workspace (`apps/web-erp`, `apps/rep-pwa`, `apps/design-system-studio`, `packages/ui-kit`, `packages/api-client`, `packages/shared-types`).
- Build AlMomiaa Brand Tokens (`#CFA434` Gold / `#1C1B1A` Obsidian / `#F8F7F4` Canvas) and core RTL/LTR primitives in `packages/ui-kit` with Storybook 8 stories in `apps/design-system-studio` (including `DataGridVirtual` built on `@tanstack/react-table` + `@tanstack/react-virtual`).
- Scaffold `.NET 9` Clean Architecture solution (`apps/backend-api/AlMomiaa.Erp.sln`: `Domain`, `Application`, `Infrastructure`, `WebApi`, `ArchitectureTests`) and deploy SQL Server 2022 schemas (`id`, `cust`, `cat`, `inv`, `proc`, `sales`, `fin`, `sys`) + Redis 7.
- Implement JWT + Refresh Token rotation, Redis `SecurityStampMiddleware`, Dynamic Role-Permission Matrix (`07-Security.md`), OpenAPI TypeScript codegen pipeline, and `AuditSaveChangesInterceptor`.
- **Deliverable:** Branded Design System Studio live; Super Admin logs in to `web-erp`, manages roles/permissions/users, and inspects live audit logs.

#### Sprint 2: Multi-Supplier Product Catalog, Tiered Price Lists & Customer Credit Grades (Weeks 3–4)
- Build Hierarchical Category Tree (`Beverages -> Pepsi -> ...`), Brands, `MasterProducts`, `ProductUnits` (UOM conversions), `SupplierProducts` (multi-supplier mapping), and `PriceLists` / `PriceListItems` (`MR-02`).
- Build high-performance virtualized Products Master Grid in `web-erp` (`< 80ms` paged `.NET 9` queries + 60fps virtual scrolling).
- Build Customer Management with configurable Grades (`A=60d, B=45d, C=30d, D=Cash`), Price List assignment, Credit Limits, and Representative portfolio scoping.
- Build Excel/CSV Migration Tool to import AlMomiaa's existing catalog, suppliers, and customers.
- **Deliverable:** Complete master data catalog, tiered pricing, and customer credit policies operational.

---

### Phase 2: Multi-Warehouse Inventory & Container Procurement (Weeks 5–8)

#### Sprint 3: Multi-Warehouse & Mobile Van Real-Time Stock & Transfers (Weeks 5–6)
- Implement `Warehouses` (`MainWarehouse`, `BranchWarehouse`, `MobileVan` bound to `CashVan` reps — `MR-01`) and `WarehouseStocks` (`PhysicalQty`, `ReservedQty`, `IncomingQty`, `AvailableQty`).
- Build Two-Step Inter-Warehouse & Van-Loading Stock Transfers (`StockTransfers` + `StockTransferItems`), Physical Inventory Count Adjustments, and append-only `inv.StockMovements` ledger.
- **Deliverable:** Real-time multi-warehouse and mobile van stock tracking with low-stock SignalR alerts.

#### Sprint 4: Supplier Purchasing, Container Tracking & Landed Cost Engine (Weeks 7–8)
- Build Supplier Management, Purchase Orders (`PurchaseOrders` + `PurchaseOrderItems`), and Sea Container Tracking (`AtSea -> ArrivedPort -> CustomsClearance -> InTransitRiyadh -> ReceivedAtWarehouse`).
- Implement Goods Receipt (`GoodsReceipts` + `GoodsReceiptItems`) workflow with automated **Container Landed Cost distribution** and **Weighted Average Cost (`WeightedAvgCost`)** recalculation.
- **Deliverable:** Purchasing Manager tracks containers from port to Riyadh warehouse with automatic stock & cost ledger updates.

---

### Phase 3: Offline Mobile Rep PWA, 10-Step Order Workflow, ZATCA & Delivery POD (Weeks 9–12)

#### Sprint 5: Offline-First `apps/rep-pwa` & Governed 10-Step Sales Order Engine (Weeks 9–10)
- Build `apps/rep-pwa` (Vite + React 19 + Workbox + `Dexie.js` IndexedDB) consuming `@almomiaa/ui` and `@almomiaa/api-client`.
- Implement the 10-Step Sales Order workflow:
  - Customer credit/debt/overdue snapshot on selection.
  - Category-driven product picker with auto-populated Price List prices and `MinAllowedSellingPrice` floor flagging (`MR-02`).
  - Idempotent offline queuing (`X-Idempotency-Key`), soft stock reservation (`ReservedQty` + 24h TTL Hangfire job — `RISK-01`), graceful offline stock-shortage flagging (`RISK-03`), and permanent Rep edit lock upon submission.
  - **Sales Manager exclusive order edit (`PUT /orders/{id}/manager-edit`)**, Price Override approval, Two-Tier Credit Co-Approval (`MR-03`), Approve, and Reject flows.
  - Real-time SignalR push (`OperationsHub` via `sys.OutboxMessages`) to **Warehouse Keeper** and **Purchasing Manager**.
- **Deliverable:** Online & offline mobile order capture, CashVan invoicing, and Sales Manager approval/override queue live.

#### Sprint 6: Warehouse Fulfillment, ZATCA Phase 2, Returns & Delivery Manifest POD (Weeks 11–12)
- Build Warehouse Keeper tablet picking queue, shipment confirmation (`ShippedQty`), and physical stock deduction.
- Implement single-concurrency ZATCA Phase 2 sequence locking (`sales.ZatcaDeviceSequences` `UPDLOCK, HOLDLOCK` — `RISK-02`), UBL 2.1 XML signing, TLV QR generation (Tags 1–9), and B2B Clearance / B2C 24h Reporting Hangfire jobs.
- Build A4 / Thermal invoice printing, Sales Returns & ZATCA Credit Notes (`381`), and Driver **Delivery Dispatches & Signed POD Upload** (`MR-04`).
- **Deliverable:** End-to-end order-to-ZATCA-invoice-to-signed-POD fulfillment pipeline validated in warehouse staging.

---

### Phase 4: Collections, Treasury Custody, Fiscal Closing, 7 Dashboards & Go-Live (Weeks 13–16)

#### Sprint 7: Cash Custody Wallet, Bank Verification, Double-Entry Ledger & Fiscal Locks (Weeks 13–14)
- Implement Cash Collection into `RepCustodyWallet`, optional Customer SMS/OTP confirmation (`IsCustomerConfirmed`), and Accountant HQ Treasury Handover (`MainTreasurySafe`).
- Implement Bank Transfer proof upload (EXIF-stripped + `SHA-256` duplicate prevention) and Accountant Bank Verification workflow.
- Implement `CollectionAllocations` (FIFO & manual) and balanced `fin.JournalEntries` / `fin.JournalLines` guarded by `fin.FiscalPeriods` period locks (`MR-05`).
- **Deliverable:** Complete double-entry financial loop from field collection to HQ safe, corporate bank accounts, and closed VAT periods.

#### Sprint 8: 7 Role Dashboards, Future GPS Hook, Load Testing & Riyadh Cutover (Weeks 15–16)
- Finalize all **7 Role Dashboards** (Executive, Sales, Inventory, Finance, Warehouse, Rep Web, Mobile PWA) powered by `fin.DailyFinancialSnapshots`, Redis Live KPI Hashes (`< 15ms`), and SignalR (`RISK-04` jittered backoff).
- Build and verify feature-flagged GPS Telemetry (`FieldTelemetryHub` + `sys.GpsRouteHistory`).
- Execute stress testing (`10M+` rows simulation + concurrent ZATCA lock contention test), conduct staff training for AlMomiaa teams in Riyadh, and execute zero-downtime production cutover.
