# 02 - Software Requirements Specification (SRS)
**Project Name:** AlMomiaa Enterprise ERP & DMS  
**Version:** 3.1 (Synchronized with `00-ADR`, `05-Database`, `06-API`, `07-Security`)

---

## 1. Functional Requirements

### 1.1 Identity, Roles & Permissions (`FR-IAM`)
- **FR-IAM-01:** Super Admin shall create, edit, deactivate, and assign users to one or more roles (`SUPER_ADMIN`, `EXEC_MGR`, `SALES_MGR`, `SALES_REP`, `ACCOUNTANT`, `WH_KEEPER`, `PURCH_MGR`).
- **FR-IAM-02:** Sales Representatives shall be sub-classified as `Wholesale`, `Retail`, or `CashVan`, bound to specific customer portfolios and default warehouses (including `MobileVan` virtual warehouses for `CashVan` reps).
- **FR-IAM-03:** Super Admin shall be able to toggle granular permission claims per role via a dynamic Permission Matrix, with immediate session revocation via Redis `SecurityStamp`.

### 1.2 Customer & Credit Management (`FR-CUST`)
- **FR-CUST-01:** System shall maintain Customer Code, Arabic/English Name, Phone, Address, City, VAT Number, Category, Grade (`A, B, C, D`), Price List assignment, Credit Limit, Current Balance, Payment Terms (days), and Assigned Representative.
- **FR-CUST-02:** System shall allow Super Admin / Executive Manager to configure default payment days for Grades A, B, C, and D, and override payment days per customer.
- **FR-CUST-03:** System shall calculate invoice due dates automatically (`Invoice.IssueDateUtc + EffectivePaymentDays`) and compute real-time overdue debt buckets (`0-30`, `31-45`, `46-60`, `60+` days).

### 1.3 Product Catalog, Multi-Supplier Mapping & Price Lists (`FR-CAT`)
- **FR-CAT-01:** System shall support a multi-tier catalog hierarchy: `Category -> Subcategory -> Brand -> MasterProduct -> ProductUnit (UOM)`.
- **FR-CAT-02:** Each `MasterProduct` shall maintain SKU, Base Barcode, Category, Subcategory, Brand, Base Unit, Weighted Average Cost (`WeightedAvgCost`), Wholesale Selling Price, Retail Selling Price, Minimum Allowed Selling Price (`MinAllowedSellingPrice`), and VAT Rate (default `15.00%`).
- **FR-CAT-03:** Multiple `SupplierProducts` from different suppliers (e.g., Supplier A and Supplier B both selling *Pepsi 1L*) shall map to a single `MasterProductId`, tracking supplier-specific SKU, purchase price, and preferred flag.
- **FR-CAT-04 (`MR-02`):** System shall support dynamic **Price Lists** (`Wholesale`, `Retail`, `KeyAccount`, custom tiers) via `cat.PriceLists` and `cat.PriceListItems`. Order forms shall auto-fill the customer's resolved unit price and flag any price below the standard tier or `MinAllowedSellingPrice` for Sales Manager override approval.

### 1.4 Multi-Warehouse & Real-Time Inventory (`FR-INV`)
- **FR-INV-01 (`MR-01`):** System shall support multiple physical warehouses (`MainWarehouse`, `BranchWarehouse`) and mobile delivery van warehouses (`MobileVan`, linked to `AssignedRepId`).
- **FR-INV-02:** For every `(WarehouseId, MasterProductId)` pair, the system shall maintain:
  - `PhysicalQty`: Total stock physically on hand.
  - `ReservedQty`: Stock allocated to `PendingApproval`, `Approved`, or `PreparingShipment` orders.
  - `AvailableQty`: Persisted computed column (`PhysicalQty - ReservedQty`).
  - `IncomingQty`: Stock ordered in approved POs/Containers not yet received.
- **FR-INV-03:** System shall record an immutable append-only ledger entry in `inv.StockMovements` for every incoming receipt, outgoing shipment, inter-warehouse transfer, van loading/unloading, sales return, and inventory count adjustment.
- **FR-INV-04:** System shall trigger real-time `LowStockAlert` notifications when `AvailableQty <= MinReorderQty`.
- **FR-INV-05 (`RISK-01`):** Hangfire shall monitor unapproved orders past `ReservationExpiresAtUtc` (default 24h), releasing expired soft reservations to `SoftDemandWarning` or escalating to `EXEC_MGR`.

### 1.5 Purchasing & Container Management (`FR-PROC`)
- **FR-PROC-01:** Purchasing Manager shall manage Suppliers, Purchase Orders (`PO` + `PurchaseOrderItems`), and Container Shipments (`ContainerNumber`, `BillOfLading`, `ETA`, `CustomsStatus`).
- **FR-PROC-02:** Upon Goods Receipt (`GRN` + `GoodsReceiptItems`), the system shall distribute container landed costs (`SupplierGoodsCostSAR + FreightAndCustomsCostSAR`) across received items, recalculate `MasterProducts.WeightedAvgCost`, increment `PhysicalQty`, and decrement `IncomingQty`.

### 1.6 Sales Order, Delivery Dispatch & ZATCA Invoicing Workflow (`FR-SALES`)
- **FR-SALES-01:** Sales Rep order creation screen shall display Customer Balance, Overdue Debt, Credit Limit, Grade, and Previous Purchases upon customer selection.
- **FR-SALES-02:** Submitting an order (`POST /api/v1/orders` with `X-Idempotency-Key`) shall transition status to `PendingApproval`, increment `ReservedQty`, and **permanently lock the order against edits by the Sales Representative**.
- **FR-SALES-03 (`RISK-03`):** Offline-synced orders whose requested quantity exceeds current `AvailableQty` shall never be dropped; they enter `PendingApproval` with `HasStockShortage = true` for Sales Manager resolution.
- **FR-SALES-04 (`MR-02`, `MR-03`):** Orders breaching Credit Limit/Overdue terms shall set `ExceededCreditWarning = true` and `RequiresFinanceCoApproval = true`. Line items priced below tier/floor shall set `PriceOverridePending = true`.
- **FR-SALES-05:** Only the **Sales Manager** (or Super Admin) with `orders:edit_submitted` may invoke `PUT /api/v1/orders/{id}/manager-edit` to adjust quantities, bonus items, or prices prior to shipment.
- **FR-SALES-06:** Approving an order shall transition status to `Approved` and broadcast a real-time SignalR notification to the assigned **Warehouse Keeper** and **Purchasing Manager**.
- **FR-SALES-07:** Rejecting an order shall transition status to `Rejected`, release `ReservedQty`, and notify the Sales Representative with the rejection reason.
- **FR-SALES-08 (`RISK-02`):** Warehouse Keeper confirms shipment (`POST /api/v1/orders/{id}/confirm-shipment`), deducting `PhysicalQty` and `ReservedQty`, acquiring a single-concurrency lock on `sales.ZatcaDeviceSequences` to allocate sequential `ICV`/`PIH`, generating the signed ZATCA Phase 2 `Invoice` + TLV QR code, and debiting the Customer Receivable balance.
- **FR-SALES-09 (`MR-04`):** Warehouse Keeper assigns invoiced orders to a **Delivery Dispatch / Manifest** (`sales.DeliveryDispatches`) tracking `PreparedAtWarehouse -> OutForDelivery -> Delivered`, with mandatory or optional upload of the signed customer invoice photo (POD).
- **FR-SALES-10:** System shall support **Sales Returns & ZATCA Credit Notes** (`sales.SalesReturns`, ZATCA type `381`) linked to the original invoice, restoring sellable stock and crediting the customer ledger.

### 1.7 Collections, Treasury Reconciliation & Fiscal Closing (`FR-FIN`)
- **FR-FIN-01 (Cash Collection):**
  - Sales Rep records cash collected (`Status = InRepCustody`), debiting `RepCustodyWallet` and crediting Customer Receivable via balanced double-entry (`fin.JournalEntries` + `fin.JournalLines`).
  - Optional customer confirmation via SMS/OTP link sets `IsCustomerConfirmed = true`.
  - Accountant executes `ConfirmTreasuryHandover` when physical cash arrives at headquarters, debiting `MainTreasurySafe` and crediting `RepCustodyWallet`, updating Redis Live KPIs and Executive Dashboard in real time.
- **FR-FIN-02 (Bank Transfer Collection):**
  - Sales Rep uploads bank transfer proof image (SHA-256 deduplicated) and reference (`Status = PendingBankVerification`). No bank/receivable balance changes until verified.
  - Accountant executes `VerifyBankTransfer` (`Status = VerifiedInBank`) or `RejectBankTransfer`. Verification posts the double-entry journal (`Debit BankAccount / Credit Customer Receivable`), allocates invoices, and broadcasts real-time KPI updates.
- **FR-FIN-03 (Invoice Allocation):** Confirmed collections allocate against open invoices via `fin.CollectionAllocations` (automatic FIFO or manual selection).
- **FR-FIN-04 (`MR-05`):** Accountant / Super Admin shall manage `fin.FiscalPeriods` (Open/Closed). Journal postings into a closed month are rejected or routed to the current open period while recording `OriginalValueDateUtc`.

### 1.8 Audit, Notifications & Future GPS (`FR-SYS`)
- **FR-SYS-01:** System shall log every mutation, approval, override, and security event in `sys.AuditLogs` with `UserId`, `UserRole`, `ActionName`, `EntityName`, `EntityId`, `OldValuesJson`, `NewValuesJson`, `IpAddress`, and `TimestampUtc` inside the same ACID transaction.
- **FR-SYS-02:** System shall deliver real-time SignalR notifications via `sys.OutboxMessages` for: New Orders, Approved Orders, Rejected Orders, Credit Co-Approval Requests, New Collections, Cash Deposits, Bank Verifications, Low Stock, and Container/PO updates.
- **FR-SYS-03:** `apps/rep-pwa` and Backend shall include a feature-flagged GPS module (`sys.GpsRouteHistory` & `FieldTelemetryHub`) to stream live representative coordinates, route history, and customer visit timestamps when enabled.

---

## 2. Non-Functional Requirements (NFR)

| Category | Requirement ID | Target Metric / Specification |
| :--- | :--- | :--- |
| **Performance** | `NFR-01` | API p95 latency `< 180ms` for write commands; `< 80ms` for indexed paged catalog queries; `< 15ms` for Redis-backed dashboard KPI queries. |
| **Grid Rendering** | `NFR-02` | `@tanstack/react-table` + `@tanstack/react-virtual` renders `10,000+` loaded rows at 60fps with zero frame drops in RTL & LTR. |
| **Real-Time Push** | `NFR-03` | SignalR event delivery `< 1.5 seconds` across `web-erp` and `rep-pwa` clients with jittered exponential backoff reconnection. |
| **Offline Capability** | `NFR-04` | `apps/rep-pwa` operates 100% offline for catalog browsing, customer credit review, and order/collection queuing via `Dexie.js` (`IndexedDB`), with automatic idempotent sync. |
| **Scalability** | `NFR-05` | Support `50M+` historical records via SQL Server 2022 monthly partitioning, UUIDv7 clustered indexing, and `fin.DailyFinancialSnapshots` + Redis read caches. |
| **Availability** | `NFR-06` | `99.9%` uptime during Saudi business hours (Saturday–Thursday) with zero-downtime deployments and 15-minute RPO backups. |
| **Security & Compliance** | `NFR-07` | ZATCA Phase 2 (Fatoora) compliant UBL 2.1 XML/QR invoicing, TLS 1.3 in transit, AES-256 at rest, SHA-256 upload deduplication, and Saudi data residency (Riyadh/Dammam). |
| **Bilingual & Brand UX** | `NFR-08` | RTL-first Arabic (`ar-SA`) + English (`en-US`) using logical CSS properties and AlMomiaa Gold (`#CFA434`) & Obsidian (`#1C1B1A`) tokens from `@almomiaa/ui`. |
