"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { InvoicePrintDto, SalesOrderDto, WarehouseDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
  EmptyState,
  KeyValueList,
  PaginationBar,
  SelectField,
  SurfaceCard,
  Tabs,
  TextareaField,
  TextField,
  type DataTableColumn,
} from "@almomiaa/ui";
import { ErpAppLayout } from "../../components/ErpAppLayout";
import { erpApi, formatApiError } from "../../lib/api-client";
import { formatDateTime, formatSar } from "../../lib/formatters";
import { hasPermission } from "../../lib/permissions";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";
import { useRealtime } from "../../providers/RealtimeProvider";

export function SalesWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const { eventVersion } = useRealtime();
  const isAr = locale === "ar-SA";

  const canReviewApprovals =
    hasPermission(user, "orders:approve") ||
    hasPermission(user, "orders:edit_submitted") ||
    hasPermission(user, "orders:coapprove_credit");

  const [tab, setTab] = useState(canReviewApprovals ? "pending" : "orders");
  const [orders, setOrders] = useState<SalesOrderDto[]>([]);
  const [pendingOrders, setPendingOrders] = useState<SalesOrderDto[]>([]);
  const [warehousesById, setWarehousesById] = useState<Record<number, WarehouseDto>>({});
  const [reviewOrder, setReviewOrder] = useState<SalesOrderDto | null>(null);
  const [totalOrders, setTotalOrders] = useState(0);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState("");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{ tone: "success" | "danger"; title: string; message: string; details?: string[] } | null>(null);

  // Create Order Drawer
  const [createOpen, setCreateOpen] = useState(false);
  const [channel, setChannel] = useState("PreSales");
  const [custId, setCustId] = useState("");
  const [whId, setWhId] = useState("1");
  const [prodId, setProdId] = useState("");
  const [unitId, setUnitId] = useState("");
  const [qty, setQty] = useState("5");
  const [unitPrice, setUnitPrice] = useState("20");

  // Manager Edit & Reject & Shipment Drawers
  const [editOrder, setEditOrder] = useState<SalesOrderDto | null>(null);
  const [editQty, setEditQty] = useState("4");
  const [editPrice, setEditPrice] = useState("20");
  const [rejectOrderTarget, setRejectOrderTarget] = useState<SalesOrderDto | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [shipOrderTarget, setShipOrderTarget] = useState<SalesOrderDto | null>(null);
  const [shipEgsId, setShipEgsId] = useState("EGS-RYD-WH01");
  const [shipInvType, setShipInvType] = useState("StandardB2B");

  // Invoices / EGS / Print / Copy
  const [egsDeviceId, setEgsDeviceId] = useState("EGS-RYD-WH01");
  const [egsWhId, setEgsWhId] = useState("1");
  const [egsCert, setEgsCert] = useState("ZATCA-CERT-2026-01");
  const [printInvId, setPrintInvId] = useState("");
  const [printedInvoice, setPrintedInvoice] = useState<InvoicePrintDto | null>(null);
  const [copyUrl, setCopyUrl] = useState("https://storage.almomiaa.sa/invoices/signed-copy.pdf");

  // Returns
  const [retInvId, setRetInvId] = useState("");
  const [retWhId, setRetWhId] = useState("1");
  const [retReason, setRetReason] = useState("Damaged item");
  const [retEgsId, setRetEgsId] = useState("EGS-RYD-WH01");
  const [retProdId, setRetProdId] = useState("");
  const [retUnitId, setRetUnitId] = useState("");
  const [retQty, setRetQty] = useState("1");
  const [retPrice, setRetPrice] = useState("20");

  // Delivery Dispatch & POD
  const [dspWhId, setDspWhId] = useState("1");
  const [dspDriverId, setDspDriverId] = useState(user?.id ?? "");
  const [dspPlate, setDspPlate] = useState("RYD-2026");
  const [dspInvoiceId, setDspInvoiceId] = useState("");
  const [podDispatchId, setPodDispatchId] = useState("");
  const [podInvoiceId, setPodInvoiceId] = useState("");
  const [podStatus, setPodStatus] = useState("Delivered");
  const [podImgUrl, setPodImgUrl] = useState("https://storage.almomiaa.sa/pod/signed.jpg");

  const loadOrders = useCallback(async () => {
    setLoading(true);
    try {
      const [res, pendingRes, whList] = await Promise.all([
        erpApi.sales.getOrders({ status: statusFilter || undefined, page, pageSize: 25 }),
        erpApi.sales.getOrders({ status: "PendingApproval", page: 1, pageSize: 50 }),
        erpApi.inventory.getWarehouses().catch(() => [] as WarehouseDto[]),
      ]);
      setOrders(res.items);
      setTotalOrders(res.totalCount);
      setPendingOrders(pendingRes.items);
      const map: Record<number, WarehouseDto> = {};
      for (const w of whList) {
        map[w.id] = w;
      }
      setWarehousesById(map);
      setReviewOrder((prev) =>
        prev
          ? (pendingRes.items.find((o) => o.id === prev.id) ??
              res.items.find((o) => o.id === prev.id) ??
              null)
          : null,
      );
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [page, statusFilter]);

  useEffect(() => {
    void loadOrders();
  }, [loadOrders, eventVersion]);

  useEffect(() => {
    if (user && !dspDriverId) setDspDriverId(user.id);
  }, [dspDriverId, user]);

  const runOrderAction = async (label: string, fn: () => Promise<unknown>) => {
    setSubmitting(true);
    setBanner(null);
    try {
      await fn();
      setBanner({ tone: "success", title: label, message: isAr ? "تمت العملية بنجاح" : "Action completed" });
      await loadOrders();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateOrder = async (e: FormEvent) => {
    e.preventDefault();
    await runOrderAction(isAr ? "تم تقديم طلب المبيعات" : "Sales Order Submitted", async () => {
      await erpApi.sales.createOrder({
        orderChannel: channel,
        customerId: custId,
        warehouseId: Number(whId),
        items: [{ masterProductId: prodId, productUnitId: unitId, orderedQty: Number(qty), unitPrice: Number(unitPrice) }],
      });
      setCreateOpen(false);
    });
  };

  const handleManagerEdit = async (e: FormEvent) => {
    e.preventDefault();
    if (!editOrder || !editOrder.items[0]) return;
    await runOrderAction(isAr ? "تم تعديل الطلب بواسطة المدير" : "Order Edited by Manager", async () => {
      await erpApi.sales.managerEditOrder(editOrder.id, [
        { salesOrderItemId: editOrder.items[0].id, approvedQty: Number(editQty), unitPrice: Number(editPrice) },
      ]);
      setEditOrder(null);
    });
  };

  const handleRejectOrder = async (e: FormEvent) => {
    e.preventDefault();
    if (!rejectOrderTarget) return;
    await runOrderAction(isAr ? "تم رفض الطلب" : "Order Rejected", async () => {
      await erpApi.sales.rejectOrder(rejectOrderTarget.id, rejectReason);
      setRejectOrderTarget(null);
      setRejectReason("");
    });
  };

  const handleConfirmShipment = async (e: FormEvent) => {
    e.preventDefault();
    if (!shipOrderTarget) return;
    setSubmitting(true);
    setBanner(null);
    try {
      const inv = await erpApi.sales.confirmShipment(shipOrderTarget.id, {
        egsDeviceId: shipEgsId,
        invoiceType: shipInvType,
      });
      setPrintInvId(inv.id);
      setRetInvId(inv.id);
      setDspInvoiceId(inv.id);
      setPodInvoiceId(inv.id);
      setShipOrderTarget(null);
      setBanner({ tone: "success", title: isAr ? "تم الشحن وإصدار فاتورة ZATCA" : "Shipped & ZATCA Invoice Issued", message: `${inv.invoiceNumber} (ICV #${inv.zatcaIcv})` });
      await loadOrders();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const orderColumns: DataTableColumn<SalesOrderDto>[] = [
    { key: "no", header: isAr ? "رقم الطلب" : "Order #", mono: true, render: (o) => o.orderNumber },
    { key: "st", header: isAr ? "الحالة" : "Status", render: (o) => <Badge tone={o.status === "ShippedAndInvoiced" ? "success" : o.status === "Approved" ? "info" : o.status === "Rejected" ? "danger" : "warning"}>{o.status}</Badge> },
    {
      key: "flags",
      header: isAr ? "التنبيهات (MR)" : "Flags",
      render: (o) => (
        <div className="flex flex-wrap gap-1">
          {o.requiresFinanceCoApproval && <Badge tone={o.financeCoApprovedByUserId ? "success" : "danger"}>MR-03 Credit</Badge>}
          {o.hasPriceOverridePending && <Badge tone="warning">MR-02 Price</Badge>}
          {o.hasStockShortage && <Badge tone="danger">RISK-03 Shortage</Badge>}
        </div>
      ),
    },
    { key: "tot", header: isAr ? "الإجمالي شامل الضريبة" : "Total (incl. VAT)", align: "end", numeric: true, render: (o) => formatSar(o.totalAmount, locale) },
    { key: "dt", header: isAr ? "التاريخ" : "Created", render: (o) => formatDateTime(o.createdAtUtc, locale) },
    {
      key: "act",
      header: isAr ? "سير العمل" : "Workflow Actions",
      render: (o) => (
        <div className="flex flex-wrap gap-1">
          {o.status === "PendingApproval" && hasPermission(user, "orders:edit_submitted") && (
            <Button
              variant="secondary"
              size="compact"
              onClick={() => {
                setEditOrder(o);
                if (o.items[0]) {
                  setEditQty(String(o.items[0].approvedQty));
                  setEditPrice(String(o.items[0].unitPrice));
                }
              }}
            >
              {isAr ? "تعديل" : "Edit"}
            </Button>
          )}
          {o.hasPriceOverridePending && hasPermission(user, "orders:override_price") && (
            <Button variant="secondary" size="compact" onClick={() => void runOrderAction("Override Price", () => erpApi.sales.overrideOrderPrice(o.id))}>
              {isAr ? "استثناء السعر" : "Override"}
            </Button>
          )}
          {o.requiresFinanceCoApproval && !o.financeCoApprovedByUserId && hasPermission(user, "orders:coapprove_credit") && (
            <Button variant="secondary" size="compact" onClick={() => void runOrderAction("Credit Co-Approved", () => erpApi.sales.coApproveOrderCredit(o.id))}>
              {isAr ? "موافقة مالية" : "Co-Approve"}
            </Button>
          )}
          {o.status === "PendingApproval" && hasPermission(user, "orders:approve") && (
            <>
              <Button size="compact" onClick={() => void runOrderAction("Order Approved", () => erpApi.sales.approveOrder(o.id))}>
                {isAr ? "اعتماد" : "Approve"}
              </Button>
              <Button variant="danger" size="compact" onClick={() => setRejectOrderTarget(o)}>
                {isAr ? "رفض" : "Reject"}
              </Button>
            </>
          )}
          {o.status === "Approved" && hasPermission(user, "warehouse:ship") && (
            <Button size="compact" onClick={() => setShipOrderTarget(o)}>
              {isAr ? "شحن وفوترة ZATCA" : "Ship & Invoice"}
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "طلبات المبيعات وفواتير ZATCA والمرتجعات والتوصيل" : "Sales Orders, ZATCA Invoices, Returns & Delivery POD"}
      subtitle={isAr ? "دورة البيع الكاملة من تقديم الطلب وحتى الشحن وإصدار فاتورة ZATCA Phase 2 وإثبات التسليم" : "Complete governed order workflow, ZATCA invoicing, credit notes, and POD"}
      requiredPermissions={["orders:create", "orders:approve", "warehouse:ship", "invoices:view", "sales:returns", "delivery:dispatch", "delivery:update_pod"]}
      headerActions={
        <div className="flex gap-2">
          {hasPermission(user, "orders:create") && <Button size="compact" onClick={() => setCreateOpen(true)}>{isAr ? "+ طلب مبيعات" : "+ New Order"}</Button>}
          <Button variant="ghost" size="compact" onClick={() => void loadOrders()}>{isAr ? "تحديث" : "Refresh"}</Button>
        </div>
      }
    >
      {banner && <AlertBanner tone={banner.tone} title={banner.title} message={banner.message} details={banner.details} onDismiss={() => setBanner(null)} />}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "pending", label: isAr ? "طلبات بانتظار الاعتماد" : "Pending Approvals", badge: pendingOrders.length },
          { id: "orders", label: isAr ? "كل طلبات المبيعات" : "All Sales Orders", badge: totalOrders },
          { id: "invoices", label: isAr ? "فواتير ZATCA و EGS" : "ZATCA Invoices & EGS" },
          { id: "returns", label: isAr ? "المرتجعات (Credit Note 381)" : "Sales Returns" },
          { id: "delivery", label: isAr ? "التوصيل و POD" : "Delivery & POD" },
        ]}
      />

      {tab === "pending" && (
        <SurfaceCard
          title={isAr ? "طلبات مناديب المبيعات بانتظار الاعتماد" : "Incoming Field Rep Sell Requests (Pending Approval)"}
          description={
            isAr
              ? "يتم التحديث تلقائياً عبر SignalR فور إرسال المندوب طلباً جديداً من تطبيق PWA"
              : "Updates live via SignalR the moment a Sales Rep submits a request from the Rep PWA"
          }
        >
          {pendingOrders.length === 0 && !loading ? (
            <EmptyState
              title={isAr ? "لا توجد طلبات معلّقة حالياً" : "No pending requests"}
              description={
                isAr
                  ? "ستظهر هنا طلبات البيع الجديدة المرسلة من مناديب المبيعات تلقائياً."
                  : "New sell requests submitted by Sales Reps will appear here automatically."
              }
            />
          ) : (
            <DataTable
              columns={[
                { key: "no", header: isAr ? "رقم الطلب" : "Order #", mono: true, render: (o) => o.orderNumber },
                {
                  key: "wh",
                  header: isAr ? "المستودع / القناة" : "Warehouse / Channel",
                  render: (o) => {
                    const wh = warehousesById[o.warehouseId];
                    return (
                      <div className="space-y-0.5">
                        <div className="text-sm font-medium text-text-primary">
                          {wh ? (isAr ? wh.nameAr : (wh.nameEn ?? wh.nameAr)) : `#${o.warehouseId}`}
                        </div>
                        <Badge tone="info" size="sm">{o.orderChannel}</Badge>
                      </div>
                    );
                  },
                },
                {
                  key: "flags",
                  header: isAr ? "تنبيهات الحوكمة" : "Governance Flags",
                  render: (o) => (
                    <div className="flex flex-wrap gap-1">
                      {o.requiresFinanceCoApproval && (
                        <Badge tone={o.financeCoApprovedByUserId ? "success" : "danger"} size="sm">
                          MR-03 Credit
                        </Badge>
                      )}
                      {o.hasPriceOverridePending && <Badge tone="warning" size="sm">MR-02 Price</Badge>}
                      {o.hasStockShortage && <Badge tone="danger" size="sm">RISK-03 Shortage</Badge>}
                      {!o.requiresFinanceCoApproval && !o.hasPriceOverridePending && !o.hasStockShortage && (
                        <Badge tone="success" size="sm">{isAr ? "جاهز للاعتماد" : "Ready"}</Badge>
                      )}
                    </div>
                  ),
                },
                {
                  key: "tot",
                  header: isAr ? "الإجمالي شامل الضريبة" : "Total (incl. VAT)",
                  align: "end",
                  numeric: true,
                  render: (o) => formatSar(o.totalAmount, locale),
                },
                {
                  key: "exp",
                  header: isAr ? "انتهاء الحجز (24س)" : "Reservation TTL",
                  render: (o) => formatDateTime(o.reservationExpiresAtUtc, locale),
                },
                {
                  key: "decide",
                  header: isAr ? "المراجعة والقرار" : "Review & Decide",
                  render: (o) => (
                    <div className="flex flex-wrap gap-1">
                      <Button size="compact" variant="secondary" onClick={() => setReviewOrder(o)}>
                        {isAr ? "مراجعة" : "Review"}
                      </Button>
                      {hasPermission(user, "orders:approve") && (
                        <>
                          <Button
                            size="compact"
                            disabled={submitting}
                            onClick={() => void runOrderAction(isAr ? "تم اعتماد الطلب" : "Order Approved", () => erpApi.sales.approveOrder(o.id))}
                          >
                            {isAr ? "اعتماد" : "Approve"}
                          </Button>
                          <Button
                            size="compact"
                            variant="danger"
                            disabled={submitting}
                            onClick={() => setRejectOrderTarget(o)}
                          >
                            {isAr ? "رفض" : "Reject"}
                          </Button>
                        </>
                      )}
                    </div>
                  ),
                },
              ]}
              rows={pendingOrders}
              rowKey={(o) => o.id}
              loading={loading}
            />
          )}
        </SurfaceCard>
      )}

      {tab === "orders" && (
        <SurfaceCard title={isAr ? "سجل طلبات المبيعات" : "Sales Orders Queue"}>
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-2">
              <div className="w-56">
                <SelectField
                  label={isAr ? "تصفية حسب الحالة" : "Filter Status"}
                  value={statusFilter}
                  onChange={(e) => {
                    setStatusFilter(e.target.value);
                    setPage(1);
                  }}
                  placeholderOption={isAr ? "الكل" : "All"}
                  options={[
                    { value: "PendingApproval", label: "PendingApproval" },
                    { value: "Approved", label: "Approved" },
                    { value: "ShippedAndInvoiced", label: "ShippedAndInvoiced" },
                    { value: "Rejected", label: "Rejected" },
                  ]}
                />
              </div>
            </div>
            <DataTable columns={orderColumns} rows={orders} rowKey={(o) => o.id} loading={loading} />
            <PaginationBar page={page} pageSize={25} totalCount={totalOrders} onPageChange={setPage} />
          </div>
        </SurfaceCard>
      )}

      {tab === "invoices" && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <SurfaceCard title={isAr ? "تسجيل جهاز ZATCA EGS" : "Register ZATCA EGS Device"}>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                void runOrderAction("EGS Registered", () =>
                  erpApi.sales.registerEgsDevice({ egsDeviceId, warehouseId: egsWhId ? Number(egsWhId) : null, certificateSerial: egsCert })
                );
              }}
              className="space-y-3"
            >
              <TextField label="EGS Device ID" value={egsDeviceId} onChange={(e) => setEgsDeviceId(e.target.value)} required />
              <TextField label={isAr ? "المستودع (ID)" : "Warehouse ID"} value={egsWhId} onChange={(e) => setEgsWhId(e.target.value)} />
              <TextField label="Certificate Serial" value={egsCert} onChange={(e) => setEgsCert(e.target.value)} required />
              <Button type="submit" loading={submitting}>{isAr ? "تسجيل الجهاز" : "Register EGS"}</Button>
            </form>
          </SurfaceCard>

          <SurfaceCard title={isAr ? "عرض فاتورة ZATCA ورفع النسخة الموقعة" : "Invoice Print Preview & Upload Signed Copy"}>
            <div className="space-y-3">
              <TextField label={isAr ? "معرف الفاتورة (Invoice GUID)" : "Invoice ID (GUID)"} value={printInvId} onChange={(e) => setPrintInvId(e.target.value)} />
              <div className="flex flex-wrap gap-2">
                <Button
                  loading={submitting}
                  onClick={async () => {
                    try {
                      setPrintedInvoice(await erpApi.sales.getInvoicePrint(printInvId));
                    } catch (err) {
                      const f = formatApiError(err);
                      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
                    }
                  }}
                >
                  {isAr ? "عرض بيانات الطباعة" : "Load Print Payload"}
                </Button>
              </div>
              <TextField label={isAr ? "رابط نسخة الفاتورة الموقعة" : "Signed Copy URL"} value={copyUrl} onChange={(e) => setCopyUrl(e.target.value)} />
              <Button variant="secondary" loading={submitting} onClick={() => void runOrderAction("Copy Attached", () => erpApi.sales.uploadInvoiceCopy(printInvId, copyUrl))}>
                {isAr ? "رفع رابط النسخة" : "Attach Copy URL"}
              </Button>
            </div>
            {printedInvoice && (
              <div className="mt-4">
                <KeyValueList
                  items={[
                    { id: "no", label: isAr ? "رقم الفاتورة" : "Invoice #", value: printedInvoice.invoice.invoiceNumber, mono: true },
                    { id: "icv", label: "ZATCA ICV", value: printedInvoice.invoice.zatcaIcv, mono: true },
                    { id: "tot", label: isAr ? "الإجمالي" : "Total", value: formatSar(printedInvoice.invoice.totalAmount, locale), mono: true },
                    { id: "hash", label: "XML Hash", value: printedInvoice.invoice.zatcaXmlHash, mono: true, dir: "ltr" },
                    { id: "qr", label: "TLV QR (Base64)", value: printedInvoice.invoice.zatcaQrCodeBase64, mono: true, dir: "ltr" },
                  ]}
                />
              </div>
            )}
          </SurfaceCard>
        </div>
      )}

      {tab === "returns" && (
        <SurfaceCard title={isAr ? "إنشاء إشعار دائن / مرتجع مبيعات (ZATCA 381)" : "Create Sales Return (ZATCA Credit Note 381)"}>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void runOrderAction("Sales Return Created", () =>
                erpApi.sales.createSalesReturn({
                  originalInvoiceId: retInvId,
                  warehouseId: Number(retWhId),
                  returnReason: retReason,
                  egsDeviceId: retEgsId,
                  items: [{ masterProductId: retProdId, productUnitId: retUnitId, returnedQty: Number(retQty), isSellableRestock: true, unitPrice: Number(retPrice) }],
                })
              );
            }}
            className="max-w-xl space-y-3"
          >
            <TextField label={isAr ? "معرف الفاتورة الأصلية" : "Original Invoice ID"} value={retInvId} onChange={(e) => setRetInvId(e.target.value)} required />
            <div className="grid grid-cols-2 gap-2">
              <TextField label={isAr ? "المستودع (ID)" : "Warehouse ID"} value={retWhId} onChange={(e) => setRetWhId(e.target.value)} required />
              <TextField label="EGS Device ID" value={retEgsId} onChange={(e) => setRetEgsId(e.target.value)} required />
            </div>
            <TextareaField label={isAr ? "سبب المرتجع" : "Return Reason"} value={retReason} onChange={(e) => setRetReason(e.target.value)} required />
            <TextField label={isAr ? "معرف المنتج (GUID)" : "Product ID"} value={retProdId} onChange={(e) => setRetProdId(e.target.value)} required />
            <TextField label={isAr ? "معرف الوحدة (GUID)" : "Product Unit ID"} value={retUnitId} onChange={(e) => setRetUnitId(e.target.value)} required />
            <div className="grid grid-cols-2 gap-2">
              <TextField label={isAr ? "الكمية المرتجعة" : "Returned Qty"} value={retQty} onChange={(e) => setRetQty(e.target.value)} required />
              <TextField label={isAr ? "سعر الوحدة" : "Unit Price"} value={retPrice} onChange={(e) => setRetPrice(e.target.value)} required />
            </div>
            <Button type="submit" loading={submitting}>{isAr ? "إصدار إشعار دائن" : "Issue Credit Note"}</Button>
          </form>
        </SurfaceCard>
      )}

      {tab === "delivery" && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <SurfaceCard title={isAr ? "إنشاء بوليصة توصيل (Dispatch)" : "Create Delivery Dispatch"}>
            <form
              onSubmit={async (e) => {
                e.preventDefault();
                setSubmitting(true);
                try {
                  const d = await erpApi.sales.createDeliveryDispatch({
                    warehouseId: Number(dspWhId),
                    driverOrRepId: dspDriverId,
                    vehiclePlateNumber: dspPlate,
                    invoiceIds: [dspInvoiceId],
                  });
                  setPodDispatchId(d.id);
                  setPodInvoiceId(dspInvoiceId);
                  setBanner({ tone: "success", title: isAr ? "تم إنشاء بوليصة التوصيل" : "Dispatch Created", message: d.dispatchNumber });
                } catch (err) {
                  const f = formatApiError(err);
                  setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
                } finally {
                  setSubmitting(false);
                }
              }}
              className="space-y-3"
            >
              <TextField label={isAr ? "المستودع (ID)" : "Warehouse ID"} value={dspWhId} onChange={(e) => setDspWhId(e.target.value)} required />
              <TextField label={isAr ? "معرف السائق/المندوب (GUID)" : "Driver/Rep User ID"} value={dspDriverId} onChange={(e) => setDspDriverId(e.target.value)} required />
              <TextField label={isAr ? "لوحة المركبة" : "Vehicle Plate"} value={dspPlate} onChange={(e) => setDspPlate(e.target.value)} />
              <TextField label={isAr ? "معرف الفاتورة (GUID)" : "Invoice ID"} value={dspInvoiceId} onChange={(e) => setDspInvoiceId(e.target.value)} required />
              <Button type="submit" loading={submitting}>{isAr ? "إنشاء بوليصة" : "Create Dispatch"}</Button>
            </form>
          </SurfaceCard>

          <SurfaceCard title={isAr ? "تحديث إثبات التسليم (POD)" : "Update Proof of Delivery (POD)"}>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                void runOrderAction("POD Updated", () =>
                  erpApi.sales.updateDeliveryPod(podDispatchId, podInvoiceId, {
                    deliveryStatus: podStatus,
                    signedPodImageUrl: podImgUrl,
                  })
                );
              }}
              className="space-y-3"
            >
              <TextField label={isAr ? "معرف البوليصة (Dispatch GUID)" : "Dispatch ID"} value={podDispatchId} onChange={(e) => setPodDispatchId(e.target.value)} required />
              <TextField label={isAr ? "معرف الفاتورة (Invoice GUID)" : "Invoice ID"} value={podInvoiceId} onChange={(e) => setPodInvoiceId(e.target.value)} required />
              <SelectField
                label={isAr ? "حالة التسليم" : "Delivery Status"}
                value={podStatus}
                onChange={(e) => setPodStatus(e.target.value)}
                options={[
                  { value: "PreparedAtWarehouse", label: "PreparedAtWarehouse" },
                  { value: "OutForDelivery", label: "OutForDelivery" },
                  { value: "Delivered", label: "Delivered" },
                  { value: "FailedAttempt", label: "FailedAttempt" },
                ]}
              />
              <TextField label={isAr ? "رابط صورة POD الموقعة" : "Signed POD URL"} value={podImgUrl} onChange={(e) => setPodImgUrl(e.target.value)} />
              <Button type="submit" loading={submitting}>{isAr ? "تحديث الحالة" : "Update POD"}</Button>
            </form>
          </SurfaceCard>
        </div>
      )}

      <DrawerModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title={isAr ? "تقديم طلب مبيعات جديد (Idempotent)" : "Submit Sales Order"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setCreateOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-order-form" loading={submitting}>{isAr ? "تقديم الطلب" : "Submit Order"}</Button>
          </>
        }
      >
        <form id="create-order-form" onSubmit={handleCreateOrder} className="space-y-3">
          <SelectField label={isAr ? "القناة" : "Channel"} value={channel} onChange={(e) => setChannel(e.target.value)} options={[{ value: "PreSales", label: "PreSales" }, { value: "CashVan", label: "CashVan" }]} />
          <TextField label={isAr ? "معرف العميل (Customer GUID)" : "Customer ID"} value={custId} onChange={(e) => setCustId(e.target.value)} required />
          <TextField label={isAr ? "المستودع (ID)" : "Warehouse ID"} value={whId} onChange={(e) => setWhId(e.target.value)} required />
          <TextField label={isAr ? "معرف المنتج (MasterProduct GUID)" : "Product ID"} value={prodId} onChange={(e) => setProdId(e.target.value)} required />
          <TextField label={isAr ? "معرف الوحدة (ProductUnit GUID)" : "Product Unit ID"} value={unitId} onChange={(e) => setUnitId(e.target.value)} required />
          <div className="grid grid-cols-2 gap-2">
            <TextField label={isAr ? "الكمية" : "Qty"} value={qty} onChange={(e) => setQty(e.target.value)} required />
            <TextField label={isAr ? "سعر الوحدة (ر.س)" : "Unit Price (SAR)"} value={unitPrice} onChange={(e) => setUnitPrice(e.target.value)} required />
          </div>
        </form>
      </DrawerModal>

      <DrawerModal
        open={editOrder !== null}
        onClose={() => setEditOrder(null)}
        title={isAr ? "تعديل الطلب بواسطة المدير" : "Manager Edit Order"}
        subtitle={editOrder?.orderNumber}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditOrder(null)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="manager-edit-form" loading={submitting}>{isAr ? "حفظ التعديل" : "Save Edit"}</Button>
          </>
        }
      >
        <form id="manager-edit-form" onSubmit={handleManagerEdit} className="space-y-3">
          <TextField label={isAr ? "الكمية المعتمدة" : "Approved Qty"} value={editQty} onChange={(e) => setEditQty(e.target.value)} required />
          <TextField label={isAr ? "سعر الوحدة (ر.س)" : "Unit Price (SAR)"} value={editPrice} onChange={(e) => setEditPrice(e.target.value)} required />
        </form>
      </DrawerModal>

      <DrawerModal
        open={rejectOrderTarget !== null}
        onClose={() => setRejectOrderTarget(null)}
        title={isAr ? "رفض طلب المبيعات" : "Reject Sales Order"}
        subtitle={rejectOrderTarget?.orderNumber}
        footer={
          <>
            <Button variant="secondary" onClick={() => setRejectOrderTarget(null)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button variant="danger" type="submit" form="reject-order-form" loading={submitting}>{isAr ? "تأكيد الرفض" : "Confirm Reject"}</Button>
          </>
        }
      >
        <form id="reject-order-form" onSubmit={handleRejectOrder} className="space-y-3">
          <TextareaField label={isAr ? "سبب الرفض" : "Rejection Reason"} value={rejectReason} onChange={(e) => setRejectReason(e.target.value)} required />
        </form>
      </DrawerModal>

      <DrawerModal
        open={reviewOrder !== null}
        onClose={() => setReviewOrder(null)}
        title={isAr ? "مراجعة طلب المندوب واتخاذ القرار" : "Review Field Rep Request & Decide"}
        subtitle={reviewOrder?.orderNumber}
        size="lg"
        footer={
          reviewOrder ? (
            <div className="flex flex-wrap items-center gap-2">
              {reviewOrder.status === "PendingApproval" && hasPermission(user, "orders:edit_submitted") && (
                <Button
                  variant="secondary"
                  size="compact"
                  onClick={() => {
                    setEditOrder(reviewOrder);
                    if (reviewOrder.items[0]) {
                      setEditQty(String(reviewOrder.items[0].approvedQty));
                      setEditPrice(String(reviewOrder.items[0].unitPrice));
                    }
                  }}
                >
                  {isAr ? "تعديل المدير" : "Manager Edit"}
                </Button>
              )}
              {reviewOrder.hasPriceOverridePending && hasPermission(user, "orders:override_price") && (
                <Button
                  variant="secondary"
                  size="compact"
                  loading={submitting}
                  onClick={() => void runOrderAction(isAr ? "تم اعتماد استثناء السعر" : "Price Override Approved", () => erpApi.sales.overrideOrderPrice(reviewOrder.id))}
                >
                  {isAr ? "اعتماد السعر (MR-02)" : "Override Price"}
                </Button>
              )}
              {reviewOrder.requiresFinanceCoApproval && !reviewOrder.financeCoApprovedByUserId && hasPermission(user, "orders:coapprove_credit") && (
                <Button
                  variant="secondary"
                  size="compact"
                  loading={submitting}
                  onClick={() => void runOrderAction(isAr ? "تمت الموافقة المالية" : "Credit Co-Approved", () => erpApi.sales.coApproveOrderCredit(reviewOrder.id))}
                >
                  {isAr ? "موافقة مالية (MR-03)" : "Co-Approve Credit"}
                </Button>
              )}
              {reviewOrder.status === "PendingApproval" && hasPermission(user, "orders:approve") && (
                <>
                  <Button
                    size="compact"
                    loading={submitting}
                    onClick={() => void runOrderAction(isAr ? "تم اعتماد الطلب" : "Order Approved", () => erpApi.sales.approveOrder(reviewOrder.id))}
                  >
                    {isAr ? "اعتماد الطلب" : "Approve Order"}
                  </Button>
                  <Button
                    variant="danger"
                    size="compact"
                    onClick={() => setRejectOrderTarget(reviewOrder)}
                  >
                    {isAr ? "رفض الطلب" : "Reject Order"}
                  </Button>
                </>
              )}
            </div>
          ) : undefined
        }
      >
        {reviewOrder && (
          <div className="space-y-4">
            <KeyValueList
              columns={2}
              items={[
                { id: "rev-status", label: isAr ? "الحالة" : "Status", value: <Badge tone="warning">{reviewOrder.status}</Badge> },
                { id: "rev-channel", label: isAr ? "القناة" : "Channel", value: <Badge tone="info">{reviewOrder.orderChannel}</Badge> },
                { id: "rev-wh", label: isAr ? "المستودع" : "Warehouse", value: warehousesById[reviewOrder.warehouseId]?.nameAr ?? `#${reviewOrder.warehouseId}` },
                { id: "rev-total", label: isAr ? "الإجمالي شامل الضريبة" : "Total (incl. VAT)", value: <span className="font-bold tabular-nums">{formatSar(reviewOrder.totalAmount, locale)}</span> },
                { id: "rev-bal", label: isAr ? "رصيد العميل عند الطلب" : "Customer Balance", value: <span className="tabular-nums">{formatSar(reviewOrder.customerBalanceAtOrder, locale)}</span> },
                { id: "rev-lim", label: isAr ? "الحد الائتماني" : "Credit Limit", value: <span className="tabular-nums">{formatSar(reviewOrder.customerCreditLimitAtOrder, locale)}</span> },
              ]}
            />

            {reviewOrder.requiresFinanceCoApproval && !reviewOrder.financeCoApprovedByUserId && (
              <AlertBanner
                tone="warning"
                title="MR-03"
                message={isAr ? "يتطلب هذا الطلب موافقة مالية مشتركة قبل الاعتماد النهائي." : "Requires Finance/Executive credit co-approval before final approval."}
              />
            )}

            {reviewOrder.hasPriceOverridePending && (
              <AlertBanner
                tone="warning"
                title="MR-02"
                message={isAr ? "توجد أصناف بسعر أقل من قائمة الأسعار وتحتاج استثناء سعر." : "One or more lines are below tier price and require price override."}
              />
            )}

            <div className="space-y-2">
              {reviewOrder.items.map((item) => (
                <div key={item.id} className="space-y-1 rounded-xl border border-surface-border bg-surface-canvas p-3 text-xs">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-mono text-text-secondary" dir="ltr">{item.masterProductId}</span>
                    <span className="font-bold tabular-nums text-text-primary">{formatSar(item.lineTotal, locale)}</span>
                  </div>
                  <div className="flex flex-wrap items-center justify-between gap-2 tabular-nums text-text-secondary">
                    <span>{isAr ? "الكمية المطلوبة / المعتمدة" : "Ordered / Approved"}: {item.orderedQty} / {item.approvedQty}</span>
                    <span>{isAr ? "سعر القائمة" : "Tier"}: {formatSar(item.standardTierPrice, locale)} • {isAr ? "المقترح" : "Unit"}: {formatSar(item.unitPrice, locale)}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </DrawerModal>

      <DrawerModal
        open={shipOrderTarget !== null}
        onClose={() => setShipOrderTarget(null)}
        title={isAr ? "تأكيد الشحن وإصدار فاتورة ZATCA" : "Confirm Shipment & Issue ZATCA Invoice"}
        subtitle={shipOrderTarget?.orderNumber}
        footer={
          <>
            <Button variant="secondary" onClick={() => setShipOrderTarget(null)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="ship-order-form" loading={submitting}>{isAr ? "تأكيد الشحن" : "Confirm Shipment"}</Button>
          </>
        }
      >
        <form id="ship-order-form" onSubmit={handleConfirmShipment} className="space-y-3">
          <TextField label="ZATCA EGS Device ID" value={shipEgsId} onChange={(e) => setShipEgsId(e.target.value)} required />
          <SelectField label={isAr ? "نوع الفاتورة" : "Invoice Type"} value={shipInvType} onChange={(e) => setShipInvType(e.target.value)} options={[{ value: "StandardB2B", label: "StandardB2B" }, { value: "SimplifiedB2C", label: "SimplifiedB2C" }]} />
        </form>
      </DrawerModal>
    </ErpAppLayout>
  );
}
