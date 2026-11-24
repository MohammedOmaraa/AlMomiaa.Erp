"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { PurchaseOrderDto, SupplierDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
  SelectField,
  SurfaceCard,
  Tabs,
  TextField,
  type DataTableColumn,
} from "@almomiaa/ui";
import { ErpAppLayout } from "../../components/ErpAppLayout";
import { erpApi, formatApiError } from "../../lib/api-client";
import { formatDateTime, formatSar } from "../../lib/formatters";
import { hasPermission } from "../../lib/permissions";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";

export function PurchasingWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const isAr = locale === "ar-SA";

  const [tab, setTab] = useState("pos");
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [pos, setPos] = useState<PurchaseOrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{ tone: "success" | "danger"; title: string; message: string; details?: string[] } | null>(null);

  // Create Supplier drawer
  const [supOpen, setSupOpen] = useState(false);
  const [supCode, setSupCode] = useState("");
  const [supNameAr, setSupNameAr] = useState("");
  const [supNameEn, setSupNameEn] = useState("");
  const [supCountry, setSupCountry] = useState("China");

  // Create PO drawer
  const [poOpen, setPoOpen] = useState(false);
  const [poSupId, setPoSupId] = useState("");
  const [poWhId, setPoWhId] = useState("1");
  const [poType, setPoType] = useState("ContainerImport");
  const [poProdId, setPoProdId] = useState("");
  const [poQty, setPoQty] = useState("50");
  const [poCost, setPoCost] = useState("12");

  // Container form
  const [cntPoId, setCntPoId] = useState("");
  const [cntNumber, setCntNumber] = useState("");
  const [cntPort, setCntPort] = useState("Jeddah Islamic Port");
  const [cntEta, setCntEta] = useState("2026-12-15");
  const [cntStatus, setCntStatus] = useState("InTransitRiyadh");
  const [cntGoodsCost, setCntGoodsCost] = useState("600");
  const [cntFreight, setCntFreight] = useState("150");

  // GRN form
  const [grnPoId, setGrnPoId] = useState("");
  const [grnContId, setGrnContId] = useState("");
  const [grnWhId, setGrnWhId] = useState("1");
  const [grnProdId, setGrnProdId] = useState("");
  const [grnQty, setGrnQty] = useState("50");
  const [grnLandedCost, setGrnLandedCost] = useState("15");

  const canManage = hasPermission(user, "procurement:manage");
  const canReceive = hasPermission(user, "procurement:receive");

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [supRes, poRes] = await Promise.all([
        erpApi.procurement.getSuppliers({ page: 1, pageSize: 50 }),
        erpApi.procurement.getPurchaseOrders({ page: 1, pageSize: 50 }),
      ]);
      setSuppliers(supRes.items);
      setPos(poRes.items);
      if (!poSupId && supRes.items[0]) setPoSupId(supRes.items[0].id);
      if (!cntPoId && poRes.items[0]) setCntPoId(poRes.items[0].id);
      if (!grnPoId && poRes.items[0]) {
        setGrnPoId(poRes.items[0].id);
        if (poRes.items[0].items[0]) setGrnProdId(poRes.items[0].items[0].masterProductId);
      }
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [cntPoId, grnPoId, poSupId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const handleCreateSupplier = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const s = await erpApi.procurement.createSupplier({
        supplierCode: supCode,
        nameAr: supNameAr,
        nameEn: supNameEn || null,
        country: supCountry,
      });
      setSupOpen(false);
      setSupCode("");
      setSupNameAr("");
      setSupNameEn("");
      setBanner({ tone: "success", title: isAr ? "تم إضافة المورد" : "Supplier Created", message: `${s.supplierCode} — ${s.nameAr}` });
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreatePo = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const po = await erpApi.procurement.createPurchaseOrder({
        supplierId: poSupId,
        targetWarehouseId: Number(poWhId),
        orderType: poType,
        items: [{ masterProductId: poProdId, orderedQty: Number(poQty), unitCostSAR: Number(poCost) }],
      });
      setPoOpen(false);
      setCntPoId(po.id);
      setGrnPoId(po.id);
      setGrnProdId(poProdId);
      setBanner({ tone: "success", title: isAr ? "تم إنشاء أمر الشراء" : "Purchase Order Created", message: po.poNumber });
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateContainer = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const c = await erpApi.procurement.createContainer({
        purchaseOrderId: cntPoId,
        containerNumber: cntNumber,
        portOfArrival: cntPort,
        estimatedArrivalDate: cntEta,
        status: cntStatus,
        supplierGoodsCostSAR: Number(cntGoodsCost),
        freightAndCustomsCostSAR: Number(cntFreight),
      });
      setGrnContId(c.id);
      setBanner({ tone: "success", title: isAr ? "تم تسجيل الحاوية" : "Container Registered", message: `${c.containerNumber} — Landed: ${formatSar(c.totalLandedCostSAR, locale)}` });
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateGrn = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const grn = await erpApi.procurement.createGoodsReceipt({
        purchaseOrderId: grnPoId,
        containerId: grnContId || null,
        warehouseId: Number(grnWhId),
        items: [{ masterProductId: grnProdId, receivedQty: Number(grnQty), allocatedLandedUnitCostSAR: Number(grnLandedCost) }],
      });
      setBanner({ tone: "success", title: isAr ? "تم تسجيل سند الاستلام وتحديث WAC" : "GRN Recorded & WAC Updated", message: grn.grnNumber });
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const poColumns: DataTableColumn<PurchaseOrderDto>[] = [
    { key: "po", header: isAr ? "رقم الأمر" : "PO #", mono: true, render: (p) => p.poNumber },
    { key: "type", header: isAr ? "النوع" : "Type", render: (p) => <Badge tone="neutral">{p.orderType}</Badge> },
    { key: "st", header: isAr ? "الحالة" : "Status", render: (p) => <Badge tone={p.status === "Completed" ? "success" : "gold"}>{p.status}</Badge> },
    { key: "wh", header: isAr ? "المستودع" : "WH", mono: true, render: (p) => `#${p.targetWarehouseId}` },
    { key: "tot", header: isAr ? "الإجمالي" : "Total", align: "end", numeric: true, render: (p) => formatSar(p.totalAmountSAR, locale) },
    { key: "dt", header: isAr ? "التاريخ" : "Created", render: (p) => formatDateTime(p.createdAtUtc, locale) },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "المشتريات والموردون والحاويات وسندات الاستلام" : "Procurement, Suppliers, Containers & GRN"}
      subtitle={isAr ? "أوامر الشراء الدولية والمحلية وتتبع الحاويات وحساب التكلفة المرجحة (WAC)" : "Suppliers, POs, container tracking, and landed-cost GRN"}
      requiredPermissions={["procurement:manage", "procurement:receive"]}
      headerActions={
        <div className="flex flex-wrap gap-2">
          {canManage && <Button size="compact" onClick={() => setPoOpen(true)}>{isAr ? "+ أمر شراء" : "+ New PO"}</Button>}
          {canManage && <Button variant="secondary" size="compact" onClick={() => setSupOpen(true)}>{isAr ? "+ مورد" : "+ Supplier"}</Button>}
          <Button variant="ghost" size="compact" onClick={() => void loadData()}>{isAr ? "تحديث" : "Refresh"}</Button>
        </div>
      }
    >
      {banner && <AlertBanner tone={banner.tone} title={banner.title} message={banner.message} details={banner.details} onDismiss={() => setBanner(null)} />}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "pos", label: isAr ? "أوامر الشراء" : "Purchase Orders", badge: pos.length },
          { id: "suppliers", label: isAr ? "الموردون" : "Suppliers", badge: suppliers.length },
          { id: "containers", label: isAr ? "الحاويات" : "Containers", disabled: !canManage },
          { id: "grn", label: isAr ? "سندات الاستلام (GRN)" : "Goods Receipts (GRN)", disabled: !canReceive },
        ]}
      />

      {tab === "pos" && (
        <SurfaceCard title={isAr ? "أوامر الشراء" : "Purchase Orders"}>
          <DataTable columns={poColumns} rows={pos} rowKey={(p) => p.id} loading={loading} />
        </SurfaceCard>
      )}

      {tab === "suppliers" && (
        <SurfaceCard title={isAr ? "الموردون" : "Suppliers"}>
          <DataTable
            rows={suppliers}
            rowKey={(s) => s.id}
            loading={loading}
            columns={[
              { key: "code", header: isAr ? "الرمز" : "Code", mono: true, render: (s) => s.supplierCode },
              { key: "name", header: isAr ? "المورد" : "Supplier", render: (s) => (isAr ? s.nameAr : (s.nameEn ?? s.nameAr)) },
              { key: "country", header: isAr ? "الدولة" : "Country", render: (s) => s.country },
              { key: "pay", header: isAr ? "المستحق" : "Payable", align: "end", numeric: true, render: (s) => formatSar(s.currentPayableBalance, locale) },
              { key: "id", header: "GUID", mono: true, render: (s) => s.id },
            ]}
          />
        </SurfaceCard>
      )}

      {tab === "containers" && (
        <SurfaceCard title={isAr ? "تسجيل حاوية شحن دولي" : "Register Import Container"}>
          <form onSubmit={handleCreateContainer} className="max-w-xl space-y-3">
            <TextField label={isAr ? "معرف أمر الشراء (PO GUID)" : "Purchase Order ID"} value={cntPoId} onChange={(e) => setCntPoId(e.target.value)} required />
            <TextField label={isAr ? "رقم الحاوية" : "Container Number"} value={cntNumber} onChange={(e) => setCntNumber(e.target.value)} required placeholder="MSCU-2026-9001" />
            <TextField label={isAr ? "ميناء الوصول" : "Port of Arrival"} value={cntPort} onChange={(e) => setCntPort(e.target.value)} required />
            <TextField label={isAr ? "تاريخ الوصول المتوقع (YYYY-MM-DD)" : "ETA (YYYY-MM-DD)"} value={cntEta} onChange={(e) => setCntEta(e.target.value)} required />
            <SelectField
              label={isAr ? "الحالة" : "Status"}
              value={cntStatus}
              onChange={(e) => setCntStatus(e.target.value)}
              options={[
                { value: "AtSea", label: "AtSea" },
                { value: "ArrivedPort", label: "ArrivedPort" },
                { value: "CustomsClearance", label: "CustomsClearance" },
                { value: "InTransitRiyadh", label: "InTransitRiyadh" },
                { value: "ReceivedAtWarehouse", label: "ReceivedAtWarehouse" },
              ]}
            />
            <div className="grid grid-cols-2 gap-2">
              <TextField label={isAr ? "قيمة البضاعة (ر.س)" : "Goods Cost (SAR)"} value={cntGoodsCost} onChange={(e) => setCntGoodsCost(e.target.value)} required />
              <TextField label={isAr ? "الشحن والجمارك (ر.س)" : "Freight & Customs (SAR)"} value={cntFreight} onChange={(e) => setCntFreight(e.target.value)} required />
            </div>
            <Button type="submit" loading={submitting}>{isAr ? "تسجيل الحاوية" : "Register Container"}</Button>
          </form>
        </SurfaceCard>
      )}

      {tab === "grn" && (
        <SurfaceCard title={isAr ? "تسجيل سند استلام بضاعة (GRN + WAC)" : "Record Goods Receipt Note (GRN)"}>
          <form onSubmit={handleCreateGrn} className="max-w-xl space-y-3">
            <TextField label={isAr ? "معرف أمر الشراء (PO GUID)" : "Purchase Order ID"} value={grnPoId} onChange={(e) => setGrnPoId(e.target.value)} required />
            <TextField label={isAr ? "معرف الحاوية (اختياري)" : "Container ID (Optional)"} value={grnContId} onChange={(e) => setGrnContId(e.target.value)} />
            <TextField label={isAr ? "المستودع (ID)" : "Warehouse ID"} value={grnWhId} onChange={(e) => setGrnWhId(e.target.value)} required />
            <TextField label={isAr ? "معرف المنتج (MasterProduct GUID)" : "Product ID (GUID)"} value={grnProdId} onChange={(e) => setGrnProdId(e.target.value)} required />
            <div className="grid grid-cols-2 gap-2">
              <TextField label={isAr ? "الكمية المستلمة" : "Received Qty"} value={grnQty} onChange={(e) => setGrnQty(e.target.value)} required />
              <TextField label={isAr ? "تكلفة الوحدة الواصلة (ر.س)" : "Landed Unit Cost (SAR)"} value={grnLandedCost} onChange={(e) => setGrnLandedCost(e.target.value)} required />
            </div>
            <Button type="submit" loading={submitting}>{isAr ? "تأكيد الاستلام" : "Record GRN"}</Button>
          </form>
        </SurfaceCard>
      )}

      <DrawerModal
        open={supOpen}
        onClose={() => setSupOpen(false)}
        title={isAr ? "إضافة مورد جديد" : "Create Supplier"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setSupOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-sup-form" loading={submitting}>{isAr ? "حفظ" : "Save"}</Button>
          </>
        }
      >
        <form id="create-sup-form" onSubmit={handleCreateSupplier} className="space-y-3">
          <TextField label={isAr ? "رمز المورد" : "Supplier Code"} value={supCode} onChange={(e) => setSupCode(e.target.value)} required placeholder="SUP-CN-010" />
          <TextField label={isAr ? "الاسم بالعربية" : "Name (AR)"} value={supNameAr} onChange={(e) => setSupNameAr(e.target.value)} required />
          <TextField label={isAr ? "الاسم بالإنجليزية" : "Name (EN)"} value={supNameEn} onChange={(e) => setSupNameEn(e.target.value)} />
          <TextField label={isAr ? "الدولة" : "Country"} value={supCountry} onChange={(e) => setSupCountry(e.target.value)} />
        </form>
      </DrawerModal>

      <DrawerModal
        open={poOpen}
        onClose={() => setPoOpen(false)}
        title={isAr ? "إنشاء أمر شراء" : "Create Purchase Order"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setPoOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-po-form" loading={submitting}>{isAr ? "إنشاء" : "Create"}</Button>
          </>
        }
      >
        <form id="create-po-form" onSubmit={handleCreatePo} className="space-y-3">
          <SelectField label={isAr ? "المورد" : "Supplier"} value={poSupId} onChange={(e) => setPoSupId(e.target.value)} options={suppliers.map((s) => ({ value: s.id, label: `${s.supplierCode} — ${isAr ? s.nameAr : (s.nameEn ?? s.nameAr)}` }))} />
          <TextField label={isAr ? "المستودع المستهدف (ID)" : "Target Warehouse ID"} value={poWhId} onChange={(e) => setPoWhId(e.target.value)} required />
          <SelectField label={isAr ? "نوع الطلب" : "Order Type"} value={poType} onChange={(e) => setPoType(e.target.value)} options={[{ value: "ContainerImport", label: "ContainerImport" }, { value: "LocalSupplierOrder", label: "LocalSupplierOrder" }]} />
          <TextField label={isAr ? "معرف المنتج (MasterProduct GUID)" : "Product ID (GUID)"} value={poProdId} onChange={(e) => setPoProdId(e.target.value)} required />
          <div className="grid grid-cols-2 gap-2">
            <TextField label={isAr ? "الكمية" : "Qty"} value={poQty} onChange={(e) => setPoQty(e.target.value)} required />
            <TextField label={isAr ? "تكلفة الوحدة (ر.س)" : "Unit Cost (SAR)"} value={poCost} onChange={(e) => setPoCost(e.target.value)} required />
          </div>
        </form>
      </DrawerModal>
    </ErpAppLayout>
  );
}
