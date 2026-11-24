"use client";

import { useCallback, useEffect, useState } from "react";
import type { ExecutiveDashboardDto, HealthDto, RoleDashboardDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  KeyValueList,
  SummaryCard,
  SurfaceCard,
  Tabs,
} from "@almomiaa/ui";
import { ErpAppLayout } from "../../components/ErpAppLayout";
import { erpApi, formatApiError } from "../../lib/api-client";
import { formatDateTime, formatNumber, formatSar } from "../../lib/formatters";
import { hasPermission } from "../../lib/permissions";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";

type RoleView = "sales" | "inventory" | "warehouse" | "finance" | "rep";

export function DashboardWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const isAr = locale === "ar-SA";

  const [health, setHealth] = useState<HealthDto | null>(null);
  const [exec, setExec] = useState<ExecutiveDashboardDto | null>(null);
  const [roleView, setRoleView] = useState<RoleView>("sales");
  const [roleData, setRoleData] = useState<RoleDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<{ title: string; message: string; details: string[] } | null>(null);

  const canExec = hasPermission(user, "dashboards:executive");

  const loadAll = useCallback(
    async (view: RoleView) => {
      setLoading(true);
      setError(null);
      try {
        const healthRes = await erpApi.operations.getHealth();
        setHealth(healthRes);

        if (canExec) {
          const execRes = await erpApi.operations.getExecutiveDashboard();
          setExec(execRes);
        }

        const roleRes = await erpApi.operations.getRoleDashboard(view);
        setRoleData(roleRes);
      } catch (err) {
        setError(formatApiError(err));
      } finally {
        setLoading(false);
      }
    },
    [canExec]
  );

  useEffect(() => {
    void loadAll(roleView);
  }, [loadAll, roleView]);

  const roleTabs = [
    { id: "sales", label: isAr ? "المبيعات" : "Sales", disabled: !hasPermission(user, "dashboards:sales") },
    { id: "inventory", label: isAr ? "المخزون" : "Inventory", disabled: !hasPermission(user, "dashboards:inventory") },
    { id: "warehouse", label: isAr ? "المستودعات" : "Warehouse", disabled: !hasPermission(user, "dashboards:inventory") },
    { id: "finance", label: isAr ? "المالية" : "Finance", disabled: !hasPermission(user, "dashboards:finance") },
    { id: "rep", label: isAr ? "المندوب" : "Rep", disabled: !hasPermission(user, "dashboards:sales") },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "لوحة التحكم التنفيذية" : "Executive & Role Dashboards"}
      subtitle={isAr ? "عرض حي لمؤشرات الخزينة والمبيعات والمخزون من خادم .NET 9" : "Live KPIs streamed from AlMomiaa .NET 9 backend"}
      headerActions={
        <div className="flex items-center gap-2">
          <Badge tone={health?.status === "Healthy" ? "success" : "warning"} mono>
            {health?.status ?? "..."}
          </Badge>
          <Button size="compact" loading={loading} onClick={() => void loadAll(roleView)}>
            {isAr ? "تحديث" : "Refresh"}
          </Button>
        </div>
      }
    >
      {error && (
        <AlertBanner
          tone="danger"
          title={error.title}
          message={error.message}
          details={error.details}
          onDismiss={() => setError(null)}
        />
      )}

      {exec && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label={isAr ? "الخزينة الرئيسية" : "Main Safe Cash"} value={formatSar(exec.totalMainSafeCash, locale)} />
          <SummaryCard label={isAr ? "عهدة المناديب" : "Rep Custody Cash"} value={formatSar(exec.totalRepCustodyCash, locale)} />
          <SummaryCard label={isAr ? "أرصدة البنوك" : "Bank Balances"} value={formatSar(exec.totalBankBalance, locale)} />
          <SummaryCard label={isAr ? "ذمم العملاء" : "Receivables"} value={formatSar(exec.totalReceivables, locale)} />
          <SummaryCard label={isAr ? "مستحقات الموردين" : "Payables"} value={formatSar(exec.totalPayables, locale)} />
          <SummaryCard label={isAr ? "قيمة المخزون" : "Inventory Valuation"} value={formatSar(exec.totalInventoryValuation, locale)} />
          <SummaryCard label={isAr ? "طلبات بانتظار الاعتماد" : "Pending Orders"} value={formatNumber(exec.pendingOrdersCount, locale)} />
          <SummaryCard label={isAr ? "أصناف منخفضة المخزون" : "Low Stock Items"} value={formatNumber(exec.lowStockItemsCount, locale)} />
        </div>
      )}

      <SurfaceCard
        title={isAr ? "مؤشرات حسب الدور الوظيفي" : "Role-Scoped Operational Metrics"}
        description={roleData ? `${isAr ? "آخر تحديث" : "Generated"}: ${formatDateTime(roleData.generatedAtUtc, locale)}` : undefined}
        state={loading && !roleData ? "loading" : "ready"}
      >
        <div className="space-y-4">
          <Tabs
            items={roleTabs}
            activeId={roleView}
            onChange={(id) => setRoleView(id as RoleView)}
          />
          {roleData && (
            <KeyValueList
              columns={3}
              items={Object.entries(roleData.metrics).map(([key, val]) => ({
                id: key,
                label: key,
                value: formatNumber(val, locale, Number.isInteger(val) ? 0 : 2),
                mono: true,
              }))}
            />
          )}
        </div>
      </SurfaceCard>
    </ErpAppLayout>
  );
}
