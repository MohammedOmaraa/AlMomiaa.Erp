"use client";

import { useEffect, useState, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  AlertBanner,
  Badge,
  Button,
  DrawerModal,
  EmptyState,
  JsonResponseViewer,
  ResponsiveAppShell,
  type SidebarNavItemProps,
  type Theme,
} from "@almomiaa/ui";
import { useTheme } from "next-themes";
import { hasAnyPermission } from "../lib/permissions";
import { ar } from "../locales/ar";
import { en } from "../locales/en";
import { useApiActivity } from "../providers/ApiActivityProvider";
import { useAuth } from "../providers/AuthProvider";
import { useLocale } from "../providers/LocaleProvider";
import { useRealtime } from "../providers/RealtimeProvider";

function NavIcon({ paths }: { paths: string[] }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      className="size-5"
      aria-hidden="true"
    >
      {paths.map((d) => (
        <path key={d} d={d} />
      ))}
    </svg>
  );
}

const navIcons: Record<string, ReactNode> = {
  dashboard: <NavIcon paths={["M3 13h8V3H3v10z", "M13 21h8V11h-8v10z", "M13 3h8v6h-8V3z", "M3 21h8v-6H3v6z"]} />,
  users: <NavIcon paths={["M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2", "M9 7a4 4 0 1 0 0-8 4 4 0 0 0 0 8z", "M22 21v-2a4 4 0 0 0-3-3.87", "M16 3.13a4 4 0 0 1 0 7.75"]} />,
  customers: <NavIcon paths={["M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2", "M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z"]} />,
  catalog: <NavIcon paths={["M4 19.5A2.5 2.5 0 0 1 6.5 17H20", "M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"]} />,
  inventory: <NavIcon paths={["M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z", "M3.27 6.96L12 12.01l8.73-5.05", "M12 22.08V12"]} />,
  purchasing: <NavIcon paths={["M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z", "M3 6h18", "M16 10a4 4 0 0 1-8 0"]} />,
  sales: <NavIcon paths={["M3 17l6-6 4 4 8-8", "M17 7h4v4"]} />,
  finance: <NavIcon paths={["M2 7h20v12H2z", "M16 13h2", "M2 10h20"]} />,
  reports: <NavIcon paths={["M18 20V10", "M12 20V4", "M6 20v-6"]} />,
};

export interface ErpAppLayoutProps {
  title: string;
  subtitle?: string;
  requiredPermissions?: string[];
  headerActions?: ReactNode;
  children: ReactNode;
}

export function ErpAppLayout({
  title,
  subtitle,
  requiredPermissions = [],
  headerActions,
  children,
}: ErpAppLayoutProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { locale, setLocale } = useLocale();
  const { resolvedTheme, setTheme } = useTheme();
  const { status, user, logout } = useAuth();
  const { logs, inspectorOpen, setInspectorOpen, clearLogs } = useApiActivity();
  const { hubState, toasts, dismissToast } = useRealtime();
  const [mounted, setMounted] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (status === "unauthenticated") {
      router.replace("/login");
    }
  }, [status, router]);

  const t = locale === "ar-SA" ? ar : en;
  const theme: Theme = mounted && resolvedTheme === "dark" ? "dark" : "light";

  if (status !== "authenticated" || !user) {
    return (
      <div className="flex min-h-screen w-full items-center justify-center bg-surface-canvas p-6 text-text-primary">
        <div
          role="status"
          className="w-full max-w-sm rounded-xl border border-surface-border bg-surface-card p-6 text-center"
        >
          <div className="mx-auto mb-3 h-4 w-3/4 animate-pulse rounded bg-surface-skeleton" />
          <p className="text-sm text-text-secondary">{t.auth.checkingSession}</p>
        </div>
      </div>
    );
  }

  const displayName = locale === "ar-SA" ? user.fullNameAr : (user.fullNameEn ?? user.fullNameAr);

  const navDefinitions: Array<{
    id: string;
    label: string;
    href: string;
    permissions: string[];
    icon: ReactNode;
  }> = [
    { id: "dashboard", label: t.dashboard, href: "/", permissions: [], icon: navIcons.dashboard },
    { id: "users", label: t.users, href: "/users", permissions: ["users:manage", "permissions:manage"], icon: navIcons.users },
    { id: "customers", label: t.customers, href: "/customers", permissions: ["customers:view", "customers:manage"], icon: navIcons.customers },
    { id: "catalog", label: t.catalog, href: "/catalog", permissions: ["catalog:view", "catalog:manage"], icon: navIcons.catalog },
    { id: "inventory", label: t.inventory, href: "/inventory", permissions: ["inventory:view", "inventory:transfer", "inventory:adjust", "settings:manage"], icon: navIcons.inventory },
    { id: "purchasing", label: t.purchasing, href: "/purchasing", permissions: ["procurement:manage", "procurement:receive"], icon: navIcons.purchasing },
    { id: "sales", label: t.sales, href: "/sales", permissions: ["orders:create", "orders:approve", "warehouse:ship", "invoices:view", "sales:returns", "delivery:dispatch", "delivery:update_pod"], icon: navIcons.sales },
    { id: "finance", label: t.finance, href: "/finance", permissions: ["dashboards:finance", "collections:create", "finance:confirm_treasury", "finance:verify_bank", "finance:close_period"], icon: navIcons.finance },
    { id: "reports", label: t.reports, href: "/reports", permissions: ["audit:view", "gps:stream"], icon: navIcons.reports },
  ];

  const items: Array<SidebarNavItemProps & { id: string }> = navDefinitions
    .filter((item) => hasAnyPermission(user, item.permissions))
    .map((item) => ({
      id: item.id,
      label: item.label,
      href: item.href,
      active: item.href === "/" ? pathname === "/" : pathname.startsWith(item.href),
      icon: item.icon,
    }));

  const canAccessPage = hasAnyPermission(user, requiredPermissions);

  const handleLogout = async () => {
    setLoggingOut(true);
    try {
      await logout();
    } finally {
      setLoggingOut(false);
    }
  };

  const sidebarFooter = (
    <div className="space-y-2">
      <div className="truncate px-1 text-start">
        <p className="truncate text-xs text-text-inverse-muted">{t.auth.signedInAs}</p>
        <p className="truncate text-sm font-semibold text-text-inverse">{displayName}</p>
        <p className="truncate font-mono text-xs text-brand-gold" dir="ltr">
          {user.employeeCode} • {user.roleCodes.join(", ")}
        </p>
      </div>
      <Button
        variant="secondary"
        size="compact"
        loading={loggingOut}
        onClick={handleLogout}
        className="w-full"
      >
        {loggingOut ? t.auth.loggingOut : t.auth.logout}
      </Button>
    </div>
  );

  const lastLog = logs[0];

  return (
    <>
      <ResponsiveAppShell
        items={items}
        title={title}
        brandLabel={t.brand}
        locale={locale}
        onLocaleChange={setLocale}
        theme={theme}
        onThemeChange={(nextTheme) => setTheme(nextTheme)}
        sidebarFooter={sidebarFooter}
      >
        <div className="space-y-5">
          <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-surface-border bg-surface-card px-4 py-3.5">
            <div className="min-w-0 text-start">
              <h2 className="text-lg font-bold text-text-primary">{title}</h2>
              {subtitle && <p className="mt-0.5 text-sm text-text-secondary">{subtitle}</p>}
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <Badge
                tone={
                  hubState === "connected"
                    ? "success"
                    : hubState === "connecting" || hubState === "reconnecting"
                      ? "warning"
                      : "neutral"
                }
                size="sm"
              >
                {hubState === "connected"
                  ? locale === "ar-SA"
                    ? "إشعارات مباشرة"
                    : "Live SignalR"
                  : hubState === "reconnecting" || hubState === "connecting"
                    ? locale === "ar-SA"
                      ? "جاري الاتصال"
                      : "Connecting"
                    : locale === "ar-SA"
                      ? "غير متصل"
                      : "Offline"}
              </Badge>
              {headerActions}
              <Button
                variant="secondary"
                size="compact"
                onClick={() => setInspectorOpen(true)}
              >
                <span>{t.common.apiInspector}</span>
                <Badge
                  tone={
                    lastLog?.status && lastLog.status >= 400
                      ? "danger"
                      : logs.length > 0
                        ? "success"
                        : "neutral"
                  }
                  mono
                >
                  {logs.length}
                </Badge>
              </Button>
            </div>
          </div>

          {toasts.length > 0 && (
            <div className="space-y-2" aria-live="polite">
              {toasts.map((toast) => (
                <AlertBanner
                  key={toast.id}
                  tone={toast.tone}
                  title={toast.title}
                  message={toast.message}
                  onDismiss={() => dismissToast(toast.id)}
                />
              ))}
            </div>
          )}

          {!canAccessPage ? (
            <AlertBanner
              tone="danger"
              title={t.common.accessDeniedTitle}
              message={t.common.accessDeniedDesc}
            />
          ) : (
            children
          )}
        </div>
      </ResponsiveAppShell>

      <DrawerModal
        open={inspectorOpen}
        onClose={() => setInspectorOpen(false)}
        title={t.common.apiInspectorTitle}
        subtitle={t.common.apiInspectorSubtitle}
        size="xl"
        closeLabel={t.common.close}
        footer={
          <>
            <Button variant="secondary" size="compact" onClick={clearLogs} disabled={logs.length === 0}>
              {t.common.clearLogs}
            </Button>
            <Button size="compact" onClick={() => setInspectorOpen(false)}>
              {t.common.close}
            </Button>
          </>
        }
      >
        {logs.length === 0 ? (
          <EmptyState title={t.common.noApiLogs} description={t.common.noApiLogsDesc} />
        ) : (
          <div className="space-y-3">
            {logs.map((entry) => (
              <JsonResponseViewer
                key={entry.id}
                method={entry.method}
                endpoint={entry.url}
                status={entry.status}
                durationMs={entry.durationMs}
                timestamp={entry.timestamp}
                requestPayload={entry.requestBody}
                payload={entry.responseBody ?? { error: entry.errorMessage }}
                copyLabel={t.common.copyJson}
                copiedLabel={t.common.copied}
                requestLabel={t.common.requestBody}
                responseLabel={t.common.responseBody}
              />
            ))}
          </div>
        )}
      </DrawerModal>
    </>
  );
}
