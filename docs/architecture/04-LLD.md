# 04 - Low-Level Design & Component Specifications (LLD)
**Project Name:** AlMomiaa Enterprise ERP & DMS  
**Version:** 3.1 (Reconciled Solution & Workflows)

---

## 1. Backend Solution Structure (`.NET 9` Clean Architecture — `apps/backend-api/`)

```text
apps/backend-api/
├── AlMomiaa.Erp.sln
├── src/
│   ├── AlMomiaa.Domain/                         # Zero external NuGet dependencies
│   │   ├── Common/                              # EntityBase, AggregateRoot, IDomainEvent, BusinessRuleException
│   │   ├── Identity/                            # User, Role, Permission, RepType
│   │   ├── Customers/                           # Customer, CustomerGrade, CreditEvaluationResult
│   │   ├── Catalog/                             # Category, Brand, MasterProduct, ProductUnit, SupplierProduct, PriceList
│   │   ├── Inventory/                           # Warehouse (Main/Branch/MobileVan), WarehouseStock, StockMovement, StockTransfer
│   │   ├── Procurement/                         # Supplier, PurchaseOrder, Container, GoodsReceipt
│   │   ├── Sales/                               # SalesOrder, SalesOrderItem, Invoice, SalesReturn, DeliveryDispatch, ZatcaDeviceSequence
│   │   ├── Finance/                             # FinancialAccount, Collection, CollectionAllocation, JournalEntry, JournalLine, FiscalPeriod
│   │   └── System/                              # AuditLog, OutboxMessage, GpsRouteHistory
│   │
│   ├── AlMomiaa.Application/                    # CQRS Commands/Queries, Validators, Pipeline Behaviors, Ports
│   │   ├── Common/Behaviors/                    # IdempotencyBehavior, ValidationBehavior, AuthorizationBehavior, TransactionBehavior
│   │   ├── Features/Identity/
│   │   ├── Features/Customers/
│   │   ├── Features/Catalog/                    # PriceLists, Products Paged Queries, Supplier Mapping
│   │   ├── Features/Inventory/                  # Multi-Warehouse Stock, Transfers, Van Loading
│   │   ├── Features/Procurement/                # POs, Containers, GRN Landed Cost & WAC Engine
│   │   ├── Features/Orders/                     # CreateSalesOrder, EditSubmittedOrderByManager, ApproveOrder, CoApproveCredit, RejectOrder
│   │   ├── Features/Warehouse/                  # ConfirmShipmentPreparation, DeliveryDispatches (POD)
│   │   ├── Features/Returns/                    # CreateSalesReturn (ZATCA Credit Note 381)
│   │   ├── Features/Collections/                # RecordCashCollection, ConfirmByCustomer, HandoverToTreasury, VerifyBankTransfer
│   │   ├── Features/Finance/                    # CloseFiscalPeriod, ReconcileLedger
│   │   └── Features/Dashboards/                 # Executive, Sales, Inventory, Finance, Warehouse, Rep Queries
│   │
│   ├── AlMomiaa.Infrastructure/                 # SQL Server 2022, Redis 7, Hangfire, S3/MinIO, ZATCA
│   │   ├── Persistence/                         # AlMomiaaDbContext, AuditSaveChangesInterceptor, Entity Configurations, Migrations
│   │   ├── Caching/                             # RedisKpiSnapshotService, HybridCatalogCache, SecurityStampStore
│   │   ├── Zatca/                               # ZatcaXmlGenerator, ZatcaSequenceLockService, ZatcaCryptographicSigner, ZatcaQrBuilder
│   │   ├── BackgroundJobs/                      # OutboxDispatcherJob, ZatcaReportingJob, ReservationExpiryJob
│   │   └── Storage/                             # S3BlobStorageService, ImageHashDeduplicator
│   │
│   └── AlMomiaa.WebApi/                         # Entry Point
│       ├── Endpoints/                           # Thin /api/v1/* Minimal APIs or Controllers
│       ├── Hubs/                                # OperationsHub.cs, FieldTelemetryHub.cs
│       └── Middleware/                          # GlobalExceptionHandler (RFC 7807), SecurityStampMiddleware
└── tests/
    ├── AlMomiaa.ArchitectureTests/              # ArchUnitNET module & dependency boundary enforcement
    ├── AlMomiaa.Domain.UnitTests/               # Order lock, WAC, credit gate, VAT & ZATCA TLV unit tests
    └── AlMomiaa.IntegrationTests/               # Testcontainers SQL Server + Redis end-to-end API tests
```

---

## 2. Detailed Component Workflows & State Machines

### 2.1 The 10-Step Sales Order, Stock Reservation, ZATCA & Delivery Engine

```text
[Sales Rep PWA (rep-pwa)]              [.NET 9 API & Domain]                     [Warehouse & Purchasing (web-erp)]
      │                                         │                                                │
      ├── 1. Select Customer ──────────────────>│                                                │
      │<── Balance, Overdue Debt, Credit Limit, │                                                │
      │    Grade (A/B/C/D), PriceList & History─┤                                                │
      │                                         │                                                │
      ├── 2. Pick Category -> Product ─────────>│ (Served from IndexedDB / Redis cache)          │
      │    (Auto-fills PriceList price;         │                                                │
      │     Rep enters OrderedQty)              │                                                │
      │                                         │                                                │
      ├── 3. Submit Order (POST /orders) ──────>│                                                │
      │    [Header: X-Idempotency-Key]          ├── Validate Customer Assignment                 │
      │                                         ├── Evaluate Credit Limit & Aging (MR-03)        │
      │                                         ├── Evaluate MinAllowedSellingPrice (MR-02)      │
      │                                         ├── Soft-Lock Stock: ReservedQty += Qty          │
      │                                         │   (If offline shortfall: HasStockShortage=true)│
      │                                         ├── Set ReservationExpiresAtUtc = Now + 24h      │
      │                                         ├── Set Status = PendingApproval                 │
      │                                         ├── LOCK Rep Edit Capability permanently         │
      │                                         ├── Persist Outbox -> SignalR to SALES_MGR       │
      │                                         │                                                │
      │   [SALES MANAGER / CO-APPROVAL ACTIONS] │                                                │
      │   - Option A: Edit Order ──────────────>│ PUT /orders/{id}/manager-edit                  │
      │     (Exclusive: orders:edit_submitted)  ├── Reconciles ReservedQty delta                 │
      │                                         ├── Logs Old/New JSON in sys.AuditLogs           │
      │   - Option B: Credit Co-Approval ──────>│ POST /orders/{id}/coapprove-credit             │
      │     (ACCOUNTANT / EXEC_MGR if flagged)  ├── Clears Finance Co-Approval hold              │
      │   - Option C: Approve Order ───────────>│ POST /orders/{id}/approve                      │
      │                                         ├── Set Status = Approved                        │
      │                                         ├── Instant SignalR Broadcast ──────────────────>│ Appears < 1.5s on:
      │                                         │                                                │ 1. WH_KEEPER Tablet
      │                                         │                                                │ 2. PURCH_MGR Screen
      │                                         │                                                │
      │                                         │<── 4. Warehouse Confirms Shipment ─────────────┤
      │                                         │    POST /orders/{id}/confirm-shipment          │
      │                                         ├── PhysicalQty -= ShippedQty; ReservedQty -= Qty│
      │                                         ├── Lock sales.ZatcaDeviceSequences (UPDLOCK)    │
      │                                         ├── Allocate ICV/PIH, Sign UBL XML + TLV QR      │
      │                                         ├── Create sales.Invoices + Double-Entry Journal │
      │                                         ├── Customer.CurrentBalance += TotalAmount       │
      │                                         ├── Status = ShippedAndInvoiced                  │
      │                                         │                                                │
      │                                         │<── 5. Delivery Dispatch & POD (MR-04) ─────────┤
      │                                         │    POST /delivery/dispatches                   │
      │                                         └── PreparedAtWarehouse -> OutForDelivery        │
      │                                             -> Delivered (Signed Invoice POD uploaded)   │
```

### 2.2 Collection, Double-Entry Custody & Fiscal Lock Engine
`AlMomiaa.Domain.Finance` unifies cash/bank custody inside `fin.FinancialAccounts`:
- `RepCustodyWallet` (One custody wallet automatically initialized per `SALES_REP`)
- `MainTreasurySafe` (Physical cash vaults at AlMomiaa Riyadh headquarters)
- `BankAccount` (Corporate bank accounts: Al Rajhi, SNB, Riyad Bank, etc.)

**Fiscal Period Lock Check (`MR-05`):**
Every journal posting checks `fin.FiscalPeriods`. If `PostingDateUtc` falls in a period where `IsClosed = true`, the engine posts to the current open period and preserves `OriginalValueDateUtc` for full ZATCA VAT auditability.

**Cash Collection & Treasury Handover Sequence:**
1. **Field Collection (`POST /api/v1/collections/cash`):**
   - Validates `X-Idempotency-Key`.
   - Creates `fin.Collections` with `Status = InRepCustody`.
   - Allocates amount to unpaid invoices via `fin.CollectionAllocations` (FIFO or manual).
   - Posts balanced `fin.JournalEntries` + `fin.JournalLines`:
     - **Debit:** `RepCustodyWallet` (Rep #X)
     - **Credit:** `Customer Accounts Receivable` (Customer #Y)
   - Optional SMS/WhatsApp OTP confirmation (`POST /api/v1/collections/{id}/customer-confirm`) sets `IsCustomerConfirmed = true`.
2. **Office Treasury Handover (`POST /api/v1/collections/treasury-handover`):**
   - Executed by `ACCOUNTANT` (`finance:confirm_treasury`).
   - Posts balanced journal entry:
     - **Debit:** `MainTreasurySafe` (HQ Safe #1)
     - **Credit:** `RepCustodyWallet` (Rep #X)
   - Updates `Collection.Status = DepositedToTreasury`, updates `fin.DailyFinancialSnapshots` & Redis Live KPIs (`total_cash_safe` up, `total_rep_custody_cash` down), and pushes `FinancialKpiUpdated` via SignalR.

**Bank Transfer Verification Sequence:**
1. **Field Upload (`POST /api/v1/collections/bank-transfer`):**
   - Rep uploads bank transfer slip + bank reference + amount.
   - Backend hashes file with `SHA-256` (`PaymentProofImageHash`) to reject duplicate receipt fraud.
   - Creates `fin.Collections` with `Status = PendingBankVerification` (no balance impact yet).
2. **Accountant Verification (`POST /api/v1/collections/bank-transfer/{id}/verify`):**
   - Executed strictly by `ACCOUNTANT` / `SUPER_ADMIN` (`finance:verify_bank`).
   - Checks `fin.FiscalPeriods` open period rule.
   - Posts balanced journal entry:
     - **Debit:** `BankAccount` (Company Bank)
     - **Credit:** `Customer Accounts Receivable` (Customer #Y)
   - Allocates payment to open invoices in `fin.CollectionAllocations`, marks `VerifiedInBank`, updates Redis Live KPIs, and pushes instant SignalR update.

---

## 3. Role-Specific Dashboard Components (7 Dashboards)

| # | Dashboard Name | Target App | Target Role(s) | Real-Time Widgets & Components (`@almomiaa/ui`) |
| :---: | :--- | :--- | :--- | :--- |
| 1 | **Executive Dashboard** | `web-erp` | `SUPER_ADMIN`, `EXEC_MGR` | Live KPI Cards (Main Safe Cash, Rep Custody Cash, Bank Balance, Receivables, Payables, Inventory Valuation, Daily/Monthly Sales, Gross Profit %); Sales vs. Collections Chart; Top Overdue Customers; Pending Credit Co-Approvals. |
| 2 | **Sales Dashboard** | `web-erp` | `SALES_MGR`, `SUPER_ADMIN` | Live `PendingApproval` Order Queue (Credit Warning, Stock Shortage & Price Override badges + inline Edit / Approve / Reject); Wholesale vs. Retail vs. CashVan Rep Leaderboard; Collection Efficiency by Rep. |
| 3 | **Inventory Dashboard** | `web-erp` | `PURCH_MGR`, `EXEC_MGR`, `SUPER_ADMIN` | Multi-Warehouse & MobileVan Stock Matrix (`Physical`, `Reserved`, `Available`, `Incoming`); Low Stock Alert Ticker; Container Arrival Pipeline (`AtSea -> Port -> Customs -> Warehouse`); Slow/Fast Moving SKUs. |
| 4 | **Finance Dashboard** | `web-erp` | `ACCOUNTANT`, `SUPER_ADMIN` | Rep Cash Custody Monitor (+ "Receive to Treasury" action); Pending Bank Transfer Verification Queue (Split-screen receipt viewer); AR/AP Aging Matrix by Grade (`A, B, C, D`); Fiscal Period Closing Panel. |
| 5 | **Warehouse Dashboard** | `web-erp` | `WH_KEEPER` | Live Approved Order Picking Queue (audible alert on new approved order); One-Click ZATCA Invoice & Packing Slip Printer; Delivery Dispatch / POD Board; Inter-Warehouse & Van Load Transfers. |
| 6 | **Representative Dashboard (Web)** | `web-erp` | `SALES_REP`, `SALES_MGR` | Personal Daily Sales & Collection Progress; My Custody Wallet Balance; Assigned Customer Portfolio & Debt Aging; Order Status Tracker (`Pending -> Approved -> Preparing -> Invoiced -> Delivered`). |
| 7 | **Mobile PWA Dashboard** | `rep-pwa` | `SALES_REP` | Offline Sync Status Bar (`IndexedDB` queue count); Quick Action Tiles (New Order / Van Sale, Collect Cash, Upload Bank Transfer, Upload Signed POD); My Cash-in-Hand Counter; Today's Customer Route. |
