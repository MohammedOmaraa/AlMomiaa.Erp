import { useCallback, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import type { SalesOrderDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DrawerModal,
  EmptyState,
  KeyValueList,
  SurfaceCard,
  Tabs,
} from "@almomiaa/ui";
import { erpApi, formatApiError } from "../lib/api-client";
import { formatDateTime, formatNumber, formatSar } from "../lib/formatters";
import { useApp } from "../providers/AppProviders";
import { RepMobileShell } from "../components/RepMobileShell";

function statusTone(status: string): "warning" | "success" | "danger" | "info" | "neutral" {
  if (status === "PendingApproval") return "warning";
  if (status === "Approved" || status === "Shipped") return "success";
  if (status === "Rejected" || status === "Cancelled") return "danger";
  if (status === "PartiallyShipped") return "info";
  return "neutral";
}

export function MyRequestsPage() {
  const { locale, t, eventVersion } = useApp();
  const [searchParams, setSearchParams] = useSearchParams();

  const [orders, setOrders] = useState<SalesOrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [selectedOrder, setSelectedOrder] = useState<SalesOrderDto | null>(null);

  const loadOrders = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await erpApi.sales.getOrders({ page: 1, pageSize: 100 });
      setOrders(res.items);

      const requestedId = searchParams.get("orderId");
      if (requestedId) {
        const match = res.items.find((o) => o.id === requestedId) ?? null;
        if (match) setSelectedOrder(match);
      } else {
        setSelectedOrder((prev) =>
          prev ? (res.items.find((o) => o.id === prev.id) ?? prev) : null,
        );
      }
    } catch (err) {
      setError(formatApiError(err, "Failed to load sell requests."));
    } finally {
      setLoading(false);
    }
  }, [searchParams]);

  useEffect(() => {
    void loadOrders();
  }, [loadOrders, eventVersion]);

  const counts = useMemo(
    () => ({
      all: orders.length,
      PendingApproval: orders.filter((o) => o.status === "PendingApproval").length,
      Approved: orders.filter((o) => o.status === "Approved").length,
      Rejected: orders.filter((o) => o.status === "Rejected").length,
      Shipped: orders.filter((o) => o.status === "Shipped" || o.status === "PartiallyShipped").length,
    }),
    [orders],
  );

  const visibleOrders = useMemo(() => {
    if (statusFilter === "all") return orders;
    if (statusFilter === "Shipped") {
      return orders.filter((o) => o.status === "Shipped" || o.status === "PartiallyShipped");
    }
    return orders.filter((o) => o.status === statusFilter);
  }, [orders, statusFilter]);

  return (
    <RepMobileShell pendingCount={counts.PendingApproval}>
      <div className="flex items-start justify-between gap-2">
        <div>
          <h1 className="text-lg font-bold text-text-primary">{t.requests.title}</h1>
          <p className="text-xs text-text-secondary">{t.requests.subtitle}</p>
        </div>
        <Button size="compact" variant="secondary" loading={loading} onClick={() => void loadOrders()}>
          {t.home.refresh}
        </Button>
      </div>

      {error ? <AlertBanner tone="danger" message={error} /> : null}

      <Tabs
        activeId={statusFilter}
        onChange={setStatusFilter}
        items={[
          { id: "all", label: t.requests.all, badge: counts.all },
          { id: "PendingApproval", label: t.requests.pending, badge: counts.PendingApproval },
          { id: "Approved", label: t.requests.approved, badge: counts.Approved },
          { id: "Rejected", label: t.requests.rejected, badge: counts.Rejected },
          { id: "Shipped", label: t.requests.shipped, badge: counts.Shipped },
        ]}
      />

      <SurfaceCard title={t.requests.title} state={loading ? "loading" : "ready"}>
        {visibleOrders.length === 0 && !loading ? (
          <EmptyState title={t.home.emptyRequests} description={t.home.emptyRequestsDesc} />
        ) : (
          <div className="space-y-3">
            {visibleOrders.map((order) => (
              <div
                key={order.id}
                className="space-y-2 rounded-xl border border-surface-border bg-surface-canvas p-3"
              >
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-mono text-sm font-bold text-text-primary" dir="ltr">
                        {order.orderNumber}
                      </span>
                      <Badge tone={statusTone(order.status)} size="sm">
                        {t.statusLabels[order.status] ?? order.status}
                      </Badge>
                    </div>
                    <div className="mt-1 text-xs text-text-secondary">
                      {formatDateTime(order.createdAtUtc, locale)} • {order.items.length}{" "}
                      {t.requests.itemsCount}
                    </div>
                  </div>
                  <div className="text-end">
                    <div className="text-sm font-bold tabular-nums text-text-primary">
                      {formatSar(order.totalAmount, locale)}
                    </div>
                    <Button
                      size="compact"
                      variant="ghost"
                      onClick={() => setSelectedOrder(order)}
                    >
                      {t.requests.viewDetails}
                    </Button>
                  </div>
                </div>

                <div className="flex flex-wrap gap-1.5">
                  {order.requiresFinanceCoApproval ? (
                    <Badge tone={order.financeCoApprovedByUserId ? "success" : "danger"} size="sm">
                      {t.requests.creditHoldFlag}
                    </Badge>
                  ) : null}
                  {order.hasPriceOverridePending ? (
                    <Badge tone="warning" size="sm">
                      {t.requests.priceOverrideFlag}
                    </Badge>
                  ) : null}
                  {order.hasStockShortage ? (
                    <Badge tone="danger" size="sm">
                      {t.requests.shortageFlag}
                    </Badge>
                  ) : null}
                </div>

                {order.rejectionReason ? (
                  <AlertBanner
                    tone="danger"
                    title={t.requests.rejectionReason}
                    message={order.rejectionReason}
                  />
                ) : null}
              </div>
            ))}
          </div>
        )}
      </SurfaceCard>

      <DrawerModal
        open={Boolean(selectedOrder)}
        onClose={() => {
          setSelectedOrder(null);
          if (searchParams.has("orderId")) {
            searchParams.delete("orderId");
            setSearchParams(searchParams, { replace: true });
          }
        }}
        title={t.requests.detailsTitle}
        subtitle={selectedOrder?.orderNumber}
      >
        {selectedOrder ? (
          <div className="space-y-4">
            <AlertBanner tone="info" message={t.requests.lockedNotice} />

            {selectedOrder.rejectionReason ? (
              <AlertBanner
                tone="danger"
                title={t.requests.rejectionReason}
                message={selectedOrder.rejectionReason}
              />
            ) : null}

            <KeyValueList
              columns={2}
              items={[
                {
                  id: "detail-status",
                  label: t.wizard.status,
                  value: (
                    <Badge tone={statusTone(selectedOrder.status)}>
                      {t.statusLabels[selectedOrder.status] ?? selectedOrder.status}
                    </Badge>
                  ),
                },
                {
                  id: "detail-channel",
                  label: t.wizard.orderChannel,
                  value: <Badge tone="info">{selectedOrder.orderChannel}</Badge>,
                },
                {
                  id: "detail-created",
                  label: t.requests.createdAt,
                  value: formatDateTime(selectedOrder.createdAtUtc, locale),
                },
                {
                  id: "detail-approved",
                  label: t.requests.approvedAt,
                  value: formatDateTime(selectedOrder.approvedAtUtc, locale),
                },
                {
                  id: "detail-subtotal",
                  label: t.wizard.subtotal,
                  value: (
                    <span className="tabular-nums">
                      {formatSar(selectedOrder.subTotal, locale)}
                    </span>
                  ),
                },
                {
                  id: "detail-total",
                  label: t.wizard.grandTotal,
                  value: (
                    <span className="font-bold tabular-nums">
                      {formatSar(selectedOrder.totalAmount, locale)}
                    </span>
                  ),
                },
              ]}
            />

            <div className="space-y-2">
              {selectedOrder.items.map((item) => (
                <div
                  key={item.id}
                  className="space-y-1 rounded-xl border border-surface-border bg-surface-canvas p-3 text-xs"
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-mono text-text-secondary" dir="ltr">
                      {item.masterProductId.slice(0, 8)}
                    </span>
                    <span className="font-bold tabular-nums text-text-primary">
                      {formatSar(item.lineTotal, locale)}
                    </span>
                  </div>
                  <div className="flex flex-wrap items-center justify-between gap-2 tabular-nums text-text-secondary">
                    <span>
                      {t.requests.lineQty}: {formatNumber(item.orderedQty, locale)} /{" "}
                      {formatNumber(item.approvedQty, locale)}
                    </span>
                    <span>
                      {t.requests.linePrice}: {formatSar(item.unitPrice, locale)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : null}
      </DrawerModal>
    </RepMobileShell>
  );
}
