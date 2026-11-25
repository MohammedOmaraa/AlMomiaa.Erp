import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { SalesOrderDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  EmptyState,
  SummaryCard,
  SurfaceCard,
} from "@almomiaa/ui";
import { erpApi, formatApiError } from "../lib/api-client";
import { formatDateTime, formatSar } from "../lib/formatters";
import { useApp } from "../providers/AppProviders";
import { RepMobileShell } from "../components/RepMobileShell";

function statusTone(status: string): "warning" | "success" | "danger" | "info" | "neutral" {
  if (status === "PendingApproval") return "warning";
  if (status === "Approved" || status === "Shipped") return "success";
  if (status === "Rejected" || status === "Cancelled") return "danger";
  if (status === "PartiallyShipped") return "info";
  return "neutral";
}

export function RepHomePage() {
  const { locale, isAr, t, user, logout, hasPermission, eventVersion, canInstallPwa, promptInstallPwa } = useApp();
  const navigate = useNavigate();

  const [orders, setOrders] = useState<SalesOrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [signingOut, setSigningOut] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSwitchAccount = async () => {
    setSigningOut(true);
    try {
      await logout();
      navigate("/login", { replace: true });
    } finally {
      setSigningOut(false);
    }
  };

  const loadHome = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [ordersRes] = await Promise.all([
        erpApi.sales.getOrders({ page: 1, pageSize: 50 }),
        erpApi.operations.getRoleDashboard("rep").catch(() => null),
      ]);
      setOrders(ordersRes.items);
    } catch (err) {
      setError(formatApiError(err, "Failed to load dashboard."));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadHome();
  }, [loadHome, eventVersion]);

  const pendingCount = orders.filter((o) => o.status === "PendingApproval").length;
  const approvedCount = orders.filter((o) => o.status === "Approved" || o.status === "Shipped").length;
  const rejectedCount = orders.filter((o) => o.status === "Rejected").length;
  const totalAmount = orders.reduce((acc, o) => acc + o.totalAmount, 0);

  return (
    <RepMobileShell pendingCount={pendingCount}>
      {!hasPermission("orders:create") ? (
        <AlertBanner tone="warning" message={t.auth.missingOrderCreatePermission} />
      ) : null}

      {error ? <AlertBanner tone="danger" message={error} /> : null}

      <div className="rounded-2xl bg-surface-obsidian p-4 text-text-inverse shadow-sm">
        <div className="flex items-start justify-between gap-3">
          <div>
            <p className="text-xs text-text-muted">
              {t.home.welcome} • <span className="font-mono" dir="ltr">{user?.employeeCode}</span>
            </p>
            <h1 className="mt-0.5 text-lg font-bold text-text-inverse">
              {isAr ? user?.fullNameAr : user?.fullNameEn}
            </h1>
            <p className="mt-1 text-xs text-text-muted">{t.home.newSellRequestHint}</p>
          </div>
          <div className="flex shrink-0 flex-wrap items-center gap-1.5">
            {canInstallPwa ? (
              <Button size="compact" variant="secondary" onClick={() => void promptInstallPwa()}>
                {t.home.installApp}
              </Button>
            ) : null}
            <Button
              size="compact"
              variant="ghost"
              loading={signingOut}
              onClick={() => void handleSwitchAccount()}
              className="text-xs text-text-inverse hover:bg-surface-charcoal"
            >
              {signingOut ? t.auth.signingOut : t.auth.signOut}
            </Button>
          </div>
        </div>

        <div className="mt-4">
          <Button
            className="w-full"
            disabled={!hasPermission("orders:create")}
            onClick={() => navigate("/new-request")}
          >
            {t.home.newSellRequestCta}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <SummaryCard
          label={t.home.kpiPending}
          value={String(pendingCount)}
          state={loading ? "loading" : "ready"}
        />
        <SummaryCard
          label={t.home.kpiApproved}
          value={String(approvedCount)}
          state={loading ? "loading" : "ready"}
        />
        <SummaryCard
          label={t.home.kpiRejected}
          value={String(rejectedCount)}
          state={loading ? "loading" : "ready"}
        />
        <SummaryCard
          label={t.home.kpiTotalSales}
          value={formatSar(totalAmount, locale)}
          state={loading ? "loading" : "ready"}
        />
      </div>

      <SurfaceCard
        title={t.home.recentRequests}
        description={
          orders.length > 0 ? `${orders.length} ${t.requests.itemsCount}` : undefined
        }
      >
        <div className="mb-3 flex items-center justify-between gap-2">
          <Button size="compact" variant="secondary" onClick={() => void loadHome()} loading={loading}>
            {t.home.refresh}
          </Button>
          <Button size="compact" variant="ghost" onClick={() => navigate("/requests")}>
            {t.home.viewAll}
          </Button>
        </div>

        {orders.length === 0 && !loading ? (
          <EmptyState
            title={t.home.emptyRequests}
            description={t.home.emptyRequestsDesc}
            action={
              <Button size="compact" onClick={() => navigate("/new-request")}>
                {t.home.newSellRequestCta}
              </Button>
            }
          />
        ) : (
          <ul className="divide-y divide-surface-border">
            {orders.slice(0, 5).map((order) => (
              <li key={order.id} className="py-3">
                <button
                  type="button"
                  onClick={() => navigate(`/requests?orderId=${encodeURIComponent(order.id)}`)}
                  className="flex w-full items-center justify-between gap-3 rounded-lg p-1 text-start transition-colors hover:bg-surface-hover"
                >
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-mono text-sm font-semibold text-text-primary" dir="ltr">
                        {order.orderNumber}
                      </span>
                      <Badge tone={statusTone(order.status)} size="sm">
                        {t.statusLabels[order.status] ?? order.status}
                      </Badge>
                    </div>
                    <p className="mt-1 text-xs text-text-secondary">
                      {formatDateTime(order.createdAtUtc, locale)} • {order.items.length} {t.requests.itemsCount}
                    </p>
                  </div>
                  <div className="shrink-0 text-end">
                    <div className="text-sm font-bold tabular-nums text-text-primary">
                      {formatSar(order.totalAmount, locale)}
                    </div>
                  </div>
                </button>
              </li>
            ))}
          </ul>
        )}
      </SurfaceCard>
    </RepMobileShell>
  );
}
