# 03 - High-Level Architecture (HLD)
**Project Name:** AlMomiaa Enterprise ERP & DMS  
**Architecture Pattern:** Modular Monolith + Clean Architecture + Pragmatic CQRS  
**Technology Stack:** .NET 9, EF Core 9, MediatR, SQL Server 2022, Redis 7, SignalR, Hangfire, Next.js 15, React 19 PWA, Storybook 8, TypeScript, Tailwind CSS

---

## 1. Architectural Overview
The AlMomiaa ERP + DMS is architected as a **Domain-Driven Modular Monolith** in **.NET 9** paired with a **pnpm + Turborepo** TypeScript frontend monorepo. Rather than introducing the operational fragility and distributed-transaction latency of microservices, all business bounded contexts run inside a single stateless `.NET 9` API host while enforcing strict logical separation at both the C# assembly/module level and the SQL Server schema level (`id`, `cust`, `cat`, `inv`, `proc`, `sales`, `fin`, `sys`).

On the frontend, **three dedicated apps** consume **three shared workspace packages** (`@almomiaa/ui`, `@almomiaa/api-client`, `@almomiaa/shared-types`), guaranteeing pixel-consistent AlMomiaa Gold & Obsidian branding, RTL/LTR parity, and zero contract drift across Desktop ERP, Warehouse Tablets, Field Sales Mobile PWA, and Storybook.

---

## 2. System Topology Diagram

```text
+---------------------------------------------------------------------------------------------------------------+
|                                        PRESENTATION & CLIENT TIER (Turborepo)                                 |
|                                                                                                               |
|  +----------------------------------+  +----------------------------------+  +-----------------------------+  |
|  |   apps/web-erp (Next.js 15/TS)   |  |  apps/rep-pwa (Vite React 19)    |  | apps/design-system-studio   |  |
|  | - Exec, Sales, Fin, WH, PO Apps  |  | - Mobile-First RTL/LTR Field App |  | - Storybook 8 + Vite        |  |
|  | - Virtualized TanStack Grids     |  | - Dexie.js (IndexedDB) Offline   |  | - Visual QA for @almomiaa/ui|  |
|  | - Thermal/A4 ZATCA Print Engine  |  | - Workbox + Idempotent Sync Queue|  | - RTL & LTR Token Matrix    |  |
|  +-----------------+----------------+  +-----------------+----------------+  +--------------+--------------+  |
|                    |                                     |                                  |                 |
|  +-----------------+-------------------------------------+----------------------------------+--------------+  |
|  |  SHARED PACKAGES:  packages/ui-kit (@almomiaa/ui) | packages/api-client | packages/shared-types         |  |
|  +------------------------------------+------------------------------------+-------------------------------+  |
+---------------------------------------|------------------------------------|----------------------------------+
                                        | HTTPS REST (JSON) + WSS (SignalR)  |
+---------------------------------------v------------------------------------v----------------------------------+
|                             EDGE & GATEWAY TIER (Cloudflare WAF + NGINX Proxy)                                |
|                        TLS 1.3 Termination | Rate Limiting | Gzip/Brotli | Static CDN                         |
+------------------------------------------------------+--------------------------------------------------------+
                                                       |
+------------------------------------------------------v--------------------------------------------------------+
|                               APPLICATION TIER (.NET 9 MODULAR MONOLITH — AlMomiaa.Erp.sln)                   |
|                                                                                                               |
|  +---------------------------------------------------------------------------------------------------------+  |
|  | AlMomiaa.WebApi: /api/v1/* REST Endpoints | OpenAPI Swagger | OperationsHub (SR) | FieldTelemetryHub    |  |
|  +---------------------------------------------------------------------------------------------------------+  |
|  | AlMomiaa.Application (CQRS / MediatR):                                                                  |  |
|  |   [IdempotencyBehavior] -> [ValidationBehavior] -> [AuthorizationBehavior] -> [TransactionBehavior]     |  |
|  +---------------------------------------------------------------------------------------------------------+  |
|  | AlMomiaa.Domain (Isolated Bounded Contexts & Schemas):                                                  |  |
|  |   [Identity (id.)] [Customers (cust.)] [Catalog & PriceLists (cat.)] [Inventory & Vans (inv.)]          |  |
|  |   [Procurement & Containers (proc.)] [Sales, Dispatch & ZATCA (sales.)]                                 |  |
|  |   [Finance, Custody, Ledger & Periods (fin.)] [Audit, Outbox, Notifications & GPS (sys.)]               |  |
|  +---------------------------------------------------------------------------------------------------------+  |
|  | AlMomiaa.Infrastructure + Hangfire: Outbox Dispatcher | ZATCA Phase 2 Signer | Reservation TTL Cleaner  |  |
|  +-------------------+-----------------------------------+-----------------------------------+----------------+
                       |                                   |                                   |
+----------------------v--------+   +----------------------v--------+   +----------------------v----------------+
|    SQL SERVER 2022 CLUSTER    |   |       REDIS 7 ENTERPRISE      |   |       S3 / MINIO OBJECT STORAGE       |
| - Primary Read/Write Node     |   | - SignalR Scale-out Backplane |   | - Signed Customer Invoice POD Scans   |
| - Read Replica (Reporting)    |   | - Live Financial KPI Hashes   |   | - Bank Transfer Proof Images (SHA256) |
| - Monthly Table Partitioning  |   | - SecurityStamp Auth Cache    |   | - Signed ZATCA UBL 2.1 XML Archives   |
| - ZatcaDeviceSequences Lock   |   | - Hybrid Catalog Cache        |   | - Container Customs Documents         |
+-------------------------------+   +-------------------------------+   +---------------------------------------+
```

---

## 3. Technology Stack Decisions & Rationale

| Layer | Technology | Architectural Justification |
| :--- | :--- | :--- |
| **Backend Runtime** | **.NET 9 Web API (C# 13)** | Enterprise throughput, native `Guid.CreateVersion7()`, `HybridCache`, and first-class OpenAPI generation. |
| **Architecture & CQRS** | **Clean Architecture + MediatR + FluentValidation** | Isolates domain guards (Order Edit Lock, Credit Gate, Fiscal Period Lock) from HTTP/DB concerns; centralizes idempotency, validation, auth, and audit. |
| **ORM & Data Access** | **Entity Framework Core 9** | Global Query Filters (Rep/Warehouse row-level security), `ROWVERSION` optimistic concurrency, compiled `.AsNoTracking()` DTO projections. |
| **Primary Database** | **SQL Server 2022** | ACID transactions across all 8 schemas, persisted computed columns (`AvailableQty`, `RemainingBalance`, `TotalLandedCostSAR`), monthly partitioning, and `UPDLOCK/HOLDLOCK` ZATCA sequencing. |
| **Caching & Real-Time** | **Redis 7 + ASP.NET Core SignalR** | Sub-second push notifications (`OrderApproved`, `TreasuryUpdated`), instant `SecurityStamp` revocation, live KPI hashes, and future `GEOADD` GPS index. |
| **Web ERP (`apps/web-erp`)** | **Next.js 15+ (App Router) + React 19 + TS + Tailwind** | Desktop & Warehouse Tablet ERP with `@tanstack/react-table` + `@tanstack/react-virtual` for 50,000+ SKU/ledger grids. |
| **Mobile Field App (`apps/rep-pwa`)** | **Vite + React 19 + Workbox + Dexie.js** | Installable Android/iOS PWA with zero app-store friction, instant offline catalog/customer reads from `IndexedDB`, and idempotent background order/collection sync. |
| **Design System (`packages/ui-kit` + `apps/design-system-studio`)** | **React 19 + Tailwind Preset + Storybook 8** | Enforces AlMomiaa Gold (`#CFA434`) & Obsidian (`#1C1B1A`) tokens and RTL/LTR logical CSS across every screen. |
| **API Client (`packages/api-client`)** | **Axios + OpenAPI Types + Zod + TanStack Query v5** | Single typed HTTP & caching layer shared by both `web-erp` and `rep-pwa` with automatic JWT refresh and runtime validation. |

---

## 4. Scalability Strategy for 50M+ Records
1. **Sequential UUIDv7 Primary Keys:** Every transactional entity uses `.NET 9` `Guid.CreateVersion7()` so inserts append sequentially to SQL Server clustered B-Trees without page-split fragmentation.
2. **Monthly Horizontal Table Partitioning:** High-growth append tables (`inv.StockMovements`, `fin.JournalLines`, `sys.AuditLogs`, `sys.GpsRouteHistory`) are partitioned monthly on `CreatedAtUtc` / `TimestampUtc` / `RecordedAtUtc`.
3. **Materialized Financial & Inventory KPIs:** Dashboards never aggregate raw transactional tables on page load; transactional handlers atomically update `fin.DailyFinancialSnapshots` and Redis Live KPI Hashes (`< 15ms` reads).
4. **Resilient SignalR Reconnection (`RISK-04`):** Clients reconnect with jittered exponential backoff and hydrate KPI snapshots from Redis, preventing SQL Server thundering herds during Riyadh mobile network drops.
