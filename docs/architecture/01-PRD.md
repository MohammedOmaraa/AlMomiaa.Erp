# 01 - Product Requirements Document (PRD)
**Project Name:** AlMomiaa Enterprise ERP & Distribution Management System (DMS)  
**Company & Location:** AlMomiaa Trading Co. (`شركة المومياء للتجارة`) — Riyadh, Kingdom of Saudi Arabia  
**Version:** 3.1 (Reconciled Architecture Baseline)

---

## 1. المشروع والرؤية (Project Overview & Business Vision)
**AlMomiaa (`شركة المومياء للتجارة`)** is an established wholesale and retail trading and distribution enterprise operating in Riyadh, Saudi Arabia. The company procures fast-moving consumer goods (FMCG), beverages, and food products from local and international manufacturers via sea containers and large purchase orders, stores inventory across multiple warehouses (including mobile retail vans), and sells to thousands of B2B customers (markets, supermarkets, mini-markets/Baqalas, restaurants, and wholesale traders).

### 1.1 Core Problems Solved by This System
1. **Multi-Supplier Catalog Unification:** Eliminates duplicate product records when the same item (e.g., *Pepsi Cola 1L*) is purchased from different suppliers at varying costs, while preserving true Weighted Average Cost (`WeightedAvgCost`) and container landed cost.
2. **Real-Time Stock & Reservation Accuracy:** Prevents multiple sales representatives from overselling limited stock by locking `ReservedQty` immediately upon order submission, with configurable reservation TTL expiry to prevent zombie reservations.
3. **Strict Order Governance & Pricing Control:** Prevents unauthorized tampering with orders after submission by locking orders for Sales Representatives and restricting post-submission edits **exclusively to the Sales Manager**, while enforcing tiered price lists, minimum price floors, and two-tier credit override controls.
4. **End-to-End Cash & Bank Custody Tracking:** Eliminates financial leakage by tracking cash inside each **Sales Representative's Custody Wallet** (with optional customer OTP confirmation) until physical handover to the main office **Treasury**, gating **Bank Transfers** behind mandatory **Accountant verification**, and locking closed accounting periods.
5. **Delivery Accountability (POD) & Van Sales:** Tracks prepared invoices from warehouse dispatch through signed Proof of Delivery (POD) and supports direct **Cash Van Sales** against mobile van virtual warehouses.
6. **Saudi ZATCA Phase 2 Compliance:** Ensures all B2B/B2C tax invoices and credit notes meet KSA Fatoora Phase 2 XML UBL 2.1 cryptographic hash-chaining and TLV QR standards.

---

## 2. User Roles & Operational Scope

| Role Code | Role Title | Primary Device | Core Operational Scope |
| :--- | :--- | :--- | :--- |
| `SUPER_ADMIN` | Super Admin (Owner) | Web Desktop & Mobile | Full system control, dynamic permission management, user lifecycle, fiscal/grade settings, all warehouses/inventory/financial balances, and workflow overrides. |
| `EXEC_MGR` | Executive Manager | Web Desktop & Mobile | Real-time executive & profit dashboards, sales/collection/inventory monitoring, cash & bank balances, credit co-approvals, and strategic approvals. |
| `SALES_MGR` | Sales Manager | Web Desktop & Tablet | Manage Wholesale & Retail reps, approve/reject orders, **exclusively edit submitted orders**, approve price floor overrides, monitor customer balances, collections, and sales KPIs. |
| `SALES_REP` | Sales Representative (`Wholesale` / `Retail` / `CashVan`) | Mobile PWA (`rep-pwa`) | Login via offline-capable mobile PWA, view assigned customers, check credit/history, create pre-sales orders (or direct van-sales invoices), collect cash/bank transfer proofs, upload signed invoice images. **Cannot edit orders after submission.** |
| `ACCOUNTANT` | Accountant | Web Desktop (`web-erp`) | Verify bank transfers, confirm cash handed over by reps to treasury, co-approve credit hold overrides, manage customer ledgers, close fiscal periods, and generate financial/VAT reports. |
| `WH_KEEPER` | Warehouse Keeper | Web Tablet & Desktop (`web-erp`) | Receive approved orders in real time, print picking lists & invoices, prepare shipments, create delivery dispatches, update stock movements, confirm delivery preparation. **Cannot approve or edit orders.** |
| `PURCH_MGR` | Purchasing Manager | Web Desktop (`web-erp`) | Manage suppliers, create purchase orders, track incoming sea containers, record landed costs, monitor incoming stock, and approve supplier purchases. |

---

## 3. Core Business Rules & Domain Policies

### 3.1 Customer Segmentation & Configurable Credit Grades
Every customer is assigned a **Category** (*Supermarket, Store, Restaurant, Market, Wholesale*) and a **Credit Grade** governing their default payment period:
- **Grade A:** 60 Days payment term
- **Grade B:** 45 Days payment term
- **Grade C:** 30 Days payment term
- **Grade D:** Cash Only (0 Days — immediate settlement required)
*Note:* Payment periods and credit limits are configurable globally per grade and overridable per individual customer by authorized management.

### 3.2 Two-Tier Credit Hold & Override Policy (`MR-03`)
- When a customer exceeds their Credit Limit or has overdue invoices beyond their Grade payment term, the Sales Representative may still submit the order so no business demand is silently lost.
- The system flags `ExceededCreditWarning = true` and `RequiresFinanceCoApproval = true`.
- Approval requires explicit **Sales Manager** approval plus **Accountant / Executive Manager** co-approval (`orders:coapprove_credit`).

### 3.3 Product Hierarchy, Multi-Supplier Mapping & Tiered Price Lists (`MR-02`)
- **Hierarchy Example:** `Beverages (Main Category) -> Pepsi (Subcategory/Brand) -> [Pepsi Cola 1L, Pepsi Cola 1.5L, Mirinda, Seven Up]`.
- **Tiered Price Lists:** Support `Wholesale`, `Retail`, and `KeyAccount` price lists with a hard `MinAllowedSellingPrice` floor per product/unit.
- **Order Entry UX:** When a Sales Representative selects a category on `rep-pwa`, products load instantaneously from local `IndexedDB` cache, auto-populate the customer's applicable selling price, and flag any negotiated below-tier price for mandatory Sales Manager override approval.

### 3.4 Governed 10-Step Sales Order & Delivery Workflow (`MR-01`, `MR-04`)
1. Sales Representative initiates order on Mobile PWA (`rep-pwa`).
2. Representative selects an assigned Customer -> System displays **Current Balance, Overdue Debt, Credit Limit, Grade, and Previous Purchases**.
3. Representative selects category and products (selling price auto-populated; rep enters quantity).
4. Representative submits order (`ReservedQty` is soft-allocated immediately with a 24-hour TTL; offline orders with stock shortfalls are accepted with `HasStockShortage = true`).
5. Order enters status: **`PendingApproval`**. *(For `CashVan` retail reps selling directly from a `MobileVan` virtual warehouse, streamlined van-invoice issuance deducts van stock immediately).*
6. **Sales Manager** reviews and either **Approves** (with Accountant/Exec co-approval if credit-flagged), **Rejects**, or **Edits** the order.
7. Once **Approved**, the order appears **instantly (< 1.5 seconds via SignalR)** on the screens of both the **Warehouse Keeper** and **Purchasing Manager**.
8. Warehouse Keeper prepares shipment, confirms actual shipped quantities, and assigns the invoice to a **Delivery Dispatch / Manifest** (`PreparedAtWarehouse -> OutForDelivery -> Delivered`).
9. System generates the official ZATCA Phase 2 Tax Invoice, deducts physical stock, updates customer receivable balance, and archives the signed POD invoice photo upon delivery.
10. **Strict Rule:** Representative cannot edit the order at any point after Step 4; only the Sales Manager (or Super Admin) can edit submitted orders.

### 3.5 Collection, Custody Reconciliation & Fiscal Closing Rules (`MR-05`)
- **Cash Collection:** Rep collects cash -> Customer optionally confirms via OTP/SMS (`IsCustomerConfirmed`) -> Rep Custody Wallet balance increases -> Rep returns to office and hands cash to Treasury -> Accountant confirms receipt -> Main Treasury Safe balance increases and Executive Dashboard updates in real time.
- **Bank Transfer:** Customer transfers funds to company bank account -> Rep uploads transfer slip image (SHA-256 deduplicated) & bank reference -> Accountant verifies payment against bank statement -> Company Bank Account balance increases, Customer Balance decreases, and Executive Dashboard updates automatically.
- **Accounting Period Lock:** Closed months (`fin.FiscalPeriods.IsClosed = true`) cannot receive backdated postings; late verifications post to the current open period while retaining the original transaction value date.

---

## 4. Dashboards & Future Expandability
- **7 Dedicated Role Dashboards:** Executive, Sales, Inventory, Finance, Warehouse, Representative (Web), and Mobile PWA Dashboard.
- **Future GPS Tracking Architecture:** Pluggable feature-flagged telemetry engine (`FieldTelemetryHub` + `sys.GpsRouteHistory`) ready to stream live representative locations, route histories, and geofenced customer visit durations when enabled.
