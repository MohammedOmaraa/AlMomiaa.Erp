"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { StockTransferDto, WarehouseDto, WarehouseStockDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
  PaginationBar,
  SelectField,
  SurfaceCard,
  Tabs,
  TextField,
  type DataTableColumn,
} from "@almomiaa/ui";
import { ErpAppLayout } from "../../components/ErpAppLayout";
import { erpApi, formatApiError } from "../../lib/api-client";
import { formatNumber } from "../../lib/formatters";
import { hasPermission } from "../../lib/permissions";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";

export function InventoryWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const isAr = locale === "ar-SA";

  const [tab, setTab] = useState("stocks");
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [selectedWhId, setSelectedWhId] = useState("");
  const [stocks, setStocks] = useState<WarehouseStockDto[]>([]);
  const [totalStocks, setTotalStocks] = useState(0);
  const [page, setPage] = useState(1);
  const [lastTransfer, setLastTransfer] = useState<StockTransferDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{ tone: "success" | "danger"; title: string; message: string; details?: string[] } | null>(null);

  // Create Warehouse drawer
  const [whOpen, setWhOpen] = useState(false);
  const [whCode, setWhCode] = useState("");
  const [whNameAr, setWhNameAr] = useState("");
  const [whNameEn, setWhNameEn] = useState("");
  const [whType, setWhType] = useState("MainWarehouse");
  const [whCity, setWhCity] = useState("Riyadh");

  // Transfer form
  const [srcWhId, setSrcWhId] = useState("");
  const [dstWhId, setDstWhId] = useState("");
  const [trfProdId, setTrfProdId] = useState("");
  const [trfQty, setTrfQty] = useState("5");
  const [rcvTransferId, setRcvTransferId] = useState("");
  const [rcvItemId, setRcvItemId] = useState("");
  const [rcvQty, setRcvQty] = useState("5");

  // Physical count form
  const [cntProdId, setCntProdId] = useState("");
  const [cntQty, setCntQty] = useState("50");

  const canCreateWh = hasPermission(user, "settings:manage");
  const canTransfer = hasPermission(user, "inventory:transfer");
  const canAdjust = hasPermission(user, "inventory:adjust");
  const isScopedWhKeeper = Boolean(
    user?.roleCodes.includes("WH_KEEPER") &&
      !user.roleCodes.includes("SUPER_ADMIN") &&
      !user.roleCodes.includes("EXEC_MGR") &&
      !user.roleCodes.includes("PURCH_MGR"),
  );

  const loadWarehouses = useCallback(async () => {
    setLoading(true);
    try {
      const list = await erpApi.inventory.getWarehouses();
      setWarehouses(list);
      if (!selectedWhId && list[0]) setSelectedWhId(String(list[0].id));
      if (!srcWhId && list[0]) setSrcWhId(String(list[0].id));
      if (!dstWhId && list[1]) setDstWhId(String(list[1].id));
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [dstWhId, selectedWhId, srcWhId]);

  const loadStocks = useCallback(async () => {
    if (!selectedWhId) return;
    setLoading(true);
    try {
      const res = await erpApi.inventory.getWarehouseStocks(Number(selectedWhId), { page, pageSize: 25 });
      setStocks(res.items);
      setTotalStocks(res.totalCount);
      if (!trfProdId && res.items[0]) setTrfProdId(res.items[0].masterProductId);
      if (!cntProdId && res.items[0]) setCntProdId(res.items[0].masterProductId);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [cntProdId, page, selectedWhId, trfProdId]);

  useEffect(() => {
    void loadWarehouses();
  }, [loadWarehouses]);

  useEffect(() => {
    void loadStocks();
  }, [loadStocks]);

  const handleCreateWarehouse = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const created = await erpApi.inventory.createWarehouse({
        warehouseCode: whCode,
        nameAr: whNameAr,
        nameEn: whNameEn || null,
        warehouseType: whType,
        city: whCity || "Riyadh",
      });
      setBanner({ tone: "success", title: isAr ? "تم إنشاء المستودع" : "Warehouse Created", message: `${created.warehouseCode} (#${created.id})` });
      setWhOpen(false);
      setWhCode("");
      setWhNameAr("");
      setWhNameEn("");
      await loadWarehouses();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleDispatchTransfer = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const trf = await erpApi.inventory.createTransfer({
        sourceWarehouseId: Number(srcWhId),
        destinationWarehouseId: Number(dstWhId),
        items: [{ masterProductId: trfProdId, requestedQty: Number(trfQty), dispatchedQty: Number(trfQty) }],
      });
      setLastTransfer(trf);
      setRcvTransferId(trf.id);
      if (trf.items[0]) setRcvItemId(trf.items[0].id);
      setBanner({ tone: "success", title: isAr ? "تم إرسال التحويل المخزني" : "Transfer Dispatched", message: `${trf.transferNumber} (${trf.status})` });
      await loadStocks();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleReceiveTransfer = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const updated = await erpApi.inventory.receiveTransfer(rcvTransferId, [
        { stockTransferItemId: rcvItemId, receivedQty: Number(rcvQty) },
      ]);
      setLastTransfer(updated);
      setBanner({ tone: "success", title: isAr ? "تم استلام التحويل" : "Transfer Received", message: `${updated.transferNumber} (${updated.status})` });
      await loadStocks();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleInventoryCount = async (e: FormEvent) => {
    e.preventDefault();
    if (!selectedWhId) return;
    setSubmitting(true);
    try {
      await erpApi.inventory.submitInventoryCount(Number(selectedWhId), [
        { masterProductId: cntProdId, countedPhysicalQty: Number(cntQty) },
      ]);
      setBanner({ tone: "success", title: isAr ? "تم تسجيل الجرد الفعلي" : "Inventory Count Saved", message: `Warehouse #${selectedWhId}` });
      await loadStocks();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const stockColumns: DataTableColumn<WarehouseStockDto>[] = [
    { key: "sku", header: "SKU", mono: true, render: (s) => s.sku },
    { key: "name", header: isAr ? "المنتج" : "Product", render: (s) => s.productNameAr },
    { key: "phys", header: isAr ? "الفعلي" : "Physical", align: "end", numeric: true, render: (s) => formatNumber(s.physicalQty, locale, 2) },
    { key: "res", header: isAr ? "المحجوز" : "Reserved", align: "end", numeric: true, render: (s) => formatNumber(s.reservedQty, locale, 2) },
    { key: "inc", header: isAr ? "الوارد" : "Incoming", align: "end", numeric: true, render: (s) => formatNumber(s.incomingQty, locale, 2) },
    {
      key: "avail",
      header: isAr ? "المتاح" : "Available",
      align: "end",
      numeric: true,
      render: (s) => (
        <Badge tone={s.availableQty <= s.minReorderQty ? "danger" : "success"} mono>
          {formatNumber(s.availableQty, locale, 2)}
        </Badge>
      ),
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "المستودعات وأرصدة المخزون والتحويلات" : "Warehouses, Live Stocks & Transfers"}
      subtitle={isAr ? "متابعة الكميات الفعلية والمحجوزة والواردة، والتحويلات بين المستودعات، والجرد الفعلي" : "Monitor physical/reserved/incoming balances, transfers, and physical counts"}
      requiredPermissions={["inventory:view", "inventory:transfer", "inventory:adjust", "settings:manage"]}
      headerActions={
        <div className="flex gap-2">
          {canCreateWh && <Button size="compact" onClick={() => setWhOpen(true)}>{isAr ? "+ مستودع جديد" : "+ New Warehouse"}</Button>}
          <Button variant="ghost" size="compact" onClick={() => { void loadWarehouses(); void loadStocks(); }}>{isAr ? "تحديث" : "Refresh"}</Button>
        </div>
      }
    >
      {banner && <AlertBanner tone={banner.tone} title={banner.title} message={banner.message} details={banner.details} onDismiss={() => setBanner(null)} />}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "stocks", label: isAr ? "المستودعات والأرصدة" : "Warehouses & Stocks", badge: warehouses.length },
          { id: "transfers", label: isAr ? "تحويلات المخزون" : "Stock Transfers", disabled: !canTransfer },
          { id: "count", label: isAr ? "الجرد الفعلي" : "Inventory Count", disabled: !canAdjust },
        ]}
      />

      {tab === "stocks" && (
        <div className="space-y-4">
          <SurfaceCard title={isAr ? "المستودعات المسجلة" : "Registered Warehouses"}>
            <DataTable
              rows={warehouses}
              rowKey={(w) => String(w.id)}
              loading={loading}
              columns={[
                { key: "id", header: "ID", mono: true, render: (w) => w.id },
                { key: "code", header: isAr ? "الرمز" : "Code", mono: true, render: (w) => w.warehouseCode },
                { key: "name", header: isAr ? "الاسم" : "Name", render: (w) => (isAr ? w.nameAr : (w.nameEn ?? w.nameAr)) },
                { key: "type", header: isAr ? "النوع" : "Type", render: (w) => <Badge tone="gold">{w.warehouseType}</Badge> },
                { key: "city", header: isAr ? "المدينة" : "City", render: (w) => w.city },
              ]}
            />
          </SurfaceCard>

          <SurfaceCard title={isAr ? "أرصدة المستودع" : "Warehouse Stock Balances"}>
            <div className="space-y-4">
              {isScopedWhKeeper ? (
                <div className="flex flex-wrap items-center gap-2 rounded-lg border border-surface-border bg-surface-canvas px-3 py-2 text-sm">
                  <Badge tone="gold">{isAr ? "مستودعك المخصص (WH_KEEPER)" : "Your Assigned Warehouse (WH_KEEPER)"}</Badge>
                  <span className="font-semibold text-text-primary">
                    {warehouses[0]
                      ? `#${warehouses[0].id} — ${warehouses[0].warehouseCode} (${isAr ? warehouses[0].nameAr : (warehouses[0].nameEn ?? warehouses[0].nameAr)})`
                      : `#${selectedWhId}`}
                  </span>
                </div>
              ) : (
                <div className="w-full sm:w-80">
                  <SelectField
                    label={isAr ? "اختر المستودع" : "Select Warehouse"}
                    value={selectedWhId}
                    onChange={(e) => {
                      setSelectedWhId(e.target.value);
                      setPage(1);
                    }}
                    options={warehouses.map((w) => ({ value: String(w.id), label: `#${w.id} — ${w.warehouseCode} (${isAr ? w.nameAr : (w.nameEn ?? w.nameAr)})` }))}
                  />
                </div>
              )}
              <DataTable columns={stockColumns} rows={stocks} rowKey={(s) => s.id} loading={loading} />
              <PaginationBar page={page} pageSize={25} totalCount={totalStocks} onPageChange={setPage} />
            </div>
          </SurfaceCard>
        </div>
      )}

      {tab === "transfers" && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <SurfaceCard title={isAr ? "إرسال تحويل مخزني" : "Dispatch Stock Transfer"}>
            <form onSubmit={handleDispatchTransfer} className="space-y-3">
              <SelectField label={isAr ? "المستودع المصدر" : "Source Warehouse"} value={srcWhId} onChange={(e) => setSrcWhId(e.target.value)} options={warehouses.map((w) => ({ value: String(w.id), label: `${w.warehouseCode} (#${w.id})` }))} />
              <SelectField label={isAr ? "المستودع الوجهة" : "Destination Warehouse"} value={dstWhId} onChange={(e) => setDstWhId(e.target.value)} options={warehouses.map((w) => ({ value: String(w.id), label: `${w.warehouseCode} (#${w.id})` }))} />
              <TextField label={isAr ? "معرف المنتج (MasterProduct GUID)" : "Product ID (GUID)"} value={trfProdId} onChange={(e) => setTrfProdId(e.target.value)} required />
              <TextField label={isAr ? "الكمية" : "Quantity"} value={trfQty} onChange={(e) => setTrfQty(e.target.value)} required />
              <Button type="submit" loading={submitting}>{isAr ? "إرسال التحويل" : "Dispatch Transfer"}</Button>
            </form>
          </SurfaceCard>

          <SurfaceCard title={isAr ? "استلام تحويل مخزني" : "Receive Stock Transfer"}>
            <form onSubmit={handleReceiveTransfer} className="space-y-3">
              <TextField label={isAr ? "معرف التحويل (Transfer GUID)" : "Transfer ID (GUID)"} value={rcvTransferId} onChange={(e) => setRcvTransferId(e.target.value)} required />
              <TextField label={isAr ? "معرف بند التحويل (Item GUID)" : "Transfer Item ID (GUID)"} value={rcvItemId} onChange={(e) => setRcvItemId(e.target.value)} required />
              <TextField label={isAr ? "الكمية المستلمة" : "Received Quantity"} value={rcvQty} onChange={(e) => setRcvQty(e.target.value)} required />
              <Button type="submit" loading={submitting}>{isAr ? "تأكيد الاستلام" : "Confirm Receipt"}</Button>
            </form>
            {lastTransfer && (
              <div className="mt-4 rounded-lg border border-surface-border bg-surface-canvas p-3 text-xs">
                <p className="font-mono font-semibold text-text-primary">{lastTransfer.transferNumber} — {lastTransfer.status}</p>
              </div>
            )}
          </SurfaceCard>
        </div>
      )}

      {tab === "count" && (
        <SurfaceCard title={isAr ? "تسوية الجرد الفعلي للمستودع" : "Submit Physical Inventory Count"}>
          <form onSubmit={handleInventoryCount} className="max-w-lg space-y-3">
            <SelectField label={isAr ? "المستودع" : "Warehouse"} value={selectedWhId} onChange={(e) => setSelectedWhId(e.target.value)} options={warehouses.map((w) => ({ value: String(w.id), label: `${w.warehouseCode} (#${w.id})` }))} />
            <TextField label={isAr ? "معرف المنتج (MasterProduct GUID)" : "Product ID (GUID)"} value={cntProdId} onChange={(e) => setCntProdId(e.target.value)} required />
            <TextField label={isAr ? "الكمية الفعلية المعدودة" : "Counted Physical Qty"} value={cntQty} onChange={(e) => setCntQty(e.target.value)} required />
            <Button type="submit" loading={submitting}>{isAr ? "اعتماد الجرد" : "Submit Count"}</Button>
          </form>
        </SurfaceCard>
      )}

      <DrawerModal
        open={whOpen}
        onClose={() => setWhOpen(false)}
        title={isAr ? "إنشاء مستودع جديد" : "Create Warehouse"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setWhOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-wh-form" loading={submitting}>{isAr ? "إنشاء" : "Create"}</Button>
          </>
        }
      >
        <form id="create-wh-form" onSubmit={handleCreateWarehouse} className="space-y-3">
          <TextField label={isAr ? "رمز المستودع" : "Warehouse Code"} value={whCode} onChange={(e) => setWhCode(e.target.value)} required placeholder="WH-RYD-02" />
          <TextField label={isAr ? "الاسم بالعربية" : "Name (AR)"} value={whNameAr} onChange={(e) => setWhNameAr(e.target.value)} required />
          <TextField label={isAr ? "الاسم بالإنجليزية" : "Name (EN)"} value={whNameEn} onChange={(e) => setWhNameEn(e.target.value)} />
          <SelectField
            label={isAr ? "نوع المستودع" : "Warehouse Type"}
            value={whType}
            onChange={(e) => setWhType(e.target.value)}
            options={[
              { value: "MainWarehouse", label: "MainWarehouse" },
              { value: "BranchWarehouse", label: "BranchWarehouse" },
              { value: "MobileVan", label: "MobileVan" },
            ]}
          />
          <TextField label={isAr ? "المدينة" : "City"} value={whCity} onChange={(e) => setWhCity(e.target.value)} />
        </form>
      </DrawerModal>
    </ErpAppLayout>
  );
}
