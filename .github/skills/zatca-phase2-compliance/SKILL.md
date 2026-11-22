---
name: zatca-phase2-compliance
description: Enforces Saudi ZATCA Phase 2 (Fatoora) e-invoicing compliance across .NET 9 backend services, SQL Server schemas, and printable invoices (15% VAT precision, UBL 2.1 XML, cryptographic hash chaining, single-concurrency ICV/PIH lock, and TLV QR Tags 1-9).
---

# Saudi ZATCA Phase 2 (Fatoora) Compliance Skill — AlMomiaa ERP

Use this skill whenever implementing or modifying invoices (`sales.Invoices`), credit/debit notes (`sales.SalesReturns`), VAT calculation, invoice printing, or ZATCA background submission jobs.

---

## 1. Tax & Monetary Precision Rules
- **Standard KSA VAT Rate:** `15.00%` (`0.15m` in C#, configurable per `MasterProduct.VatRate` for zero-rated/exempt items).
- **Precision Storage:**
  - Unit costs & conversion factors: `DECIMAL(18,4)`.
  - Unit selling prices, line subtotals, VAT amounts, invoice totals: `DECIMAL(18,2)`.
- **Rounding Policy:** Calculate line `LineSubTotal = Math.Round(Qty * UnitPrice, 2, MidpointRounding.AwayFromZero)` and `LineVat = Math.Round(LineSubTotal * (VatRate / 100m), 2, MidpointRounding.AwayFromZero)`. Never use `float` or `double` for financial math in C# or unrounded IEEE-754 floats without rounding helpers in TypeScript.

---

## 2. Invoice Types & Workflows (B2B Clearance vs. B2C Reporting)
| Invoice Type | ZATCA Code | Buyer Criteria | ZATCA Phase 2 Workflow |
| :--- | :--- | :--- | :--- |
| Standard Tax Invoice (B2B) | `388` (`0100000`) | Registered business with 15-digit `VatNumber` | **Clearance** (Real-time API clearance before sharing final PDF/XML) |
| Simplified Tax Invoice (B2C / Cash Van) | `388` (`0200000`) | Unregistered buyer / retail cash sale | **Reporting** (Generate signed XML + QR immediately; report to Fatoora within **24 hours** via Hangfire) |
| Credit Note (Sales Return) | `381` | References original `InvoiceNumber` + `ZatcaUuid` + return reason | Clearance (B2B) or Reporting (B2C); must link `OriginalInvoiceId` |
| Debit Note | `383` | Price/quantity upward adjustment referencing original invoice | Clearance (B2B) or Reporting (B2C) |

---

## 3. Single-Concurrency Cryptographic Hash-Chain Lock (`RISK-02`)
ZATCA Phase 2 mandates strictly sequential **Invoice Counter Value (`ICV`)** and **Previous Invoice Hash (`PIH`)** per EGS (E-Invoice Generation Solution) unit. Concurrent invoice confirmations across warehouses will fork the hash chain if unlocked.

**Mandatory SQL Server Lock Pattern:**
1. Every EGS device has a row in `[sales].[ZatcaDeviceSequences]` (`EgsDeviceId`, `CurrentIcv`, `LastInvoiceHash`, `CertificateSerial`).
2. Inside the invoice generation transaction, acquire an exclusive row lock:
   ```sql
   SELECT CurrentIcv, LastInvoiceHash
   FROM [sales].[ZatcaDeviceSequences] WITH (UPDLOCK, HOLDLOCK)
   WHERE EgsDeviceId = @EgsDeviceId;
   ```
3. Increment `NextIcv = CurrentIcv + 1`, set `PreviousInvoiceHash = LastInvoiceHash`, canonicalize & hash the UBL 2.1 XML (`SHA-256` Base64), sign with ECDSA `secp256k1` private key, update `ZatcaDeviceSequences`, and persist `sales.Invoices` inside the **same ACID transaction**.
4. Keep the critical section minimal (no external HTTP calls inside the SQL lock); dispatch ZATCA Fatoora API submission via `sys.OutboxMessages` + Hangfire (or synchronous clearance step after local signing for B2B).

---

## 4. Mandatory QR Code TLV Encoding (Tags 1–9)
Encode QR payload as Base64 of concatenated Tag-Length-Value (TLV) bytes (`Tag: 1 byte`, `Length: 1 byte`, `Value: UTF-8 / raw bytes`):
1. **Tag 1:** Seller Name (`شركة المومياء للتجارة` / `AlMomiaa Trading Co.`)
2. **Tag 2:** Seller 15-digit VAT Registration Number
3. **Tag 3:** Invoice Timestamp in ISO-8601 UTC (`yyyy-MM-ddTHH:mm:ssZ`)
4. **Tag 4:** Invoice Total (including VAT, e.g. `1150.00`)
5. **Tag 5:** VAT Total (e.g. `150.00`)
6. **Tag 6:** SHA-256 Hash of UBL 2.1 XML Invoice (Base64)
7. **Tag 7:** ECDSA Digital Signature of XML Hash
8. **Tag 8:** ECDSA Public Key of EGS CSID Certificate
9. **Tag 9:** ZATCA Cryptographic Stamp Signature (for Simplified / B2C Phase 2 invoices)

---

## 5. Immutability & Audit Rules
- Once a ZATCA invoice is generated (`ShippedAndInvoiced`), **never** mutate its line items, totals, `ZatcaUuid`, `ZatcaXmlHash`, or `ZatcaIcv`. Any post-invoice correction MUST be issued as a linked Credit Note (`sales.SalesReturns`) or Debit Note.
- Archive the signed UBL 2.1 XML and signed customer PDF/photo copy in private S3/MinIO object storage.
