---
name: tanstack-virtual-grid
description: Standardizes high-performance ERP data grids using @tanstack/react-table and @tanstack/react-virtual with server-side pagination, RTL pinning, sticky headers, keyboard navigation, and skeleton loading states.
---

# TanStack Virtual Grid Skill — AlMomiaa ERP

Use this skill whenever building or modifying a data table, ledger grid, product catalog list, customer list, or picking queue in `packages/ui-kit`, `apps/web-erp`, or `apps/rep-pwa`.

---

## 1. Mandatory Architecture Rules
1. **Virtualization Threshold:** Any table or scrollable list expected to hold **> 100 rows** MUST combine `@tanstack/react-table` v8+ and `@tanstack/react-virtual` v3+.
2. **Server-Side Pagination for Large Datasets (> 50,000 rows):**
   - Never fetch unbounded tables into browser memory.
   - Pair virtualization with server-side **keyset/cursor pagination** (preferred for ledgers, audit logs, stock movements) or indexed **offset pagination** (acceptable for admin lookup tables with page size 50–250).
   - Use TanStack Query `placeholderData: keepPreviousData` (or `useInfiniteQuery` for seamless virtual scroll-to-load).
3. **Zero Unnecessary Re-Renders:**
   - Memoize `columns` with `useMemo` (or define outside component).
   - Memoize stable row callbacks (`onRowClick`, `onAction`).
   - Use `getRowId: (row) => row.id` so selection and virtual measurements remain stable across refetches.

---

## 2. Virtualization & Row Height Standards
- **Scroll Container:** Fixed or flex-bounded height container (`overflow-auto relative`) with `ref={parentRef}`.
- **Row Height Estimation:**
  - Compact ERP row: `estimateSize: () => 44`
  - Standard ERP row (default): `estimateSize: () => 52`
  - Multi-line mobile PWA card row: `estimateSize: () => 88` with `measureElement` enabled for dynamic heights.
- **Overscan:** Default `overscan: 12` rows for smooth 60fps wheel/touch scrolling without blank flashes.
- **Transform Positioning:** Render virtualized rows with `transform: translateY(${virtualRow.start}px)` inside a spacer container whose height equals `rowVirtualizer.getTotalSize()`.

---

## 3. RTL / LTR Column Pinning & Sticky Headers
1. **Sticky Header:**
   - `thead` uses `sticky top-0 z-20 bg-[var(--surface-obsidian)] text-white` (or warm canvas header variant defined in `@almomiaa/ui`).
2. **Logical Pinning (RTL Safe):**
   - Never hardcode `left: 0` or `right: 0` for pinned columns.
   - Use CSS logical offsets (`inset-inline-start` / `inset-inline-end`, Tailwind `start-0` / `end-0`) driven by `column.getIsPinned()` (`'left'` maps to inline-start in LTR and inline-end in RTL, or map semantic `'start'`/`'end'` positions through the `@almomiaa/ui` `DataGridVirtual` wrapper).
   - Pin primary identifier columns (`SKU`, `InvoiceNumber`, `OrderNumber`) at inline-start and `actions` at inline-end.
3. **Typography & Cell Alignment:**
   - Arabic text (`NameAr`, `CustomerName`): `font-[var(--font-arabic)] text-start`.
   - SKUs, Barcodes, Invoice Numbers: `dir="ltr" font-mono text-start inline-block` (with one-click copy).
   - Currency (`SAR`) & Quantities: `dir="ltr" tabular-nums font-medium text-end` formatted via `@almomiaa/shared-types` `formatSar()`.

---

## 4. Interaction, Keyboard & Loading UX
- **Keyboard Navigation:** Support `ArrowUp` / `ArrowDown` row focus, `Enter` to open detail drawer, `Space` to toggle bulk selection checkbox, and `/` to focus the grid search bar.
- **Active Row Highlight:** Use `--brand-gold-light` (`#F5EED8`) with an inline-start `2px` accent border in `--brand-gold-primary` (`#CFA434`).
- **Skeleton Loading:** Render 10–15 shimmering skeleton rows matching exact column widths during initial load (`isLoading`); show a subtle top progress bar during background refetch (`isFetching && !isLoading`).
- **Empty & Error States:** Always provide bilingual Arabic/English empty-state messaging with a clear CTA (e.g., reset filters) and retry action on error.
