"use client";

import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  AlertBanner,
  Badge,
  Button,
  DrawerModal,
  EmptyState,
  NotificationItem,
  PasswordField,
  ResponsiveAppShell,
  Tabs,
  type SidebarNavItemProps,
  type Theme,
} from "@almomiaa/ui";
import { useTheme } from "next-themes";
import { formatApiError } from "../lib/api-client";
import { formatDateTime } from "../lib/formatters";
import { hasAnyPermission } from "../lib/permissions";
import { ar } from "../locales/ar";
import { en } from "../locales/en";
import { useAuth } from "../providers/AuthProvider";
import { useLocale } from "../providers/LocaleProvider";
import { useRealtime } from "../providers/RealtimeProvider";

const GPS_ALLOWED_ROLES = ["SUPER_ADMIN", "EXEC_MGR", "SALES_MGR"] as const;

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
  gps: <NavIcon paths={["M12 21s-6-5.33-6-10a6 6 0 1 1 12 0c0 4.67-6 10-6 10z", "M12 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4z"]} />,
  reports: <NavIcon paths={["M18 20V10", "M12 20V4", "M6 20v-6"]} />,
};

export interface ErpAppLayoutProps {
  title: string;
  subtitle?: string;
  requiredPermissions?: string[];
  allowedRoles?: readonly string[];
  headerActions?: ReactNode;
  children: ReactNode;
}

export function ErpAppLayout({
  title,
  subtitle,
  requiredPermissions = [],
  allowedRoles,
  headerActions,
  children,
}: ErpAppLayoutProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { locale, setLocale } = useLocale();
  const { resolvedTheme, setTheme } = useTheme();
  const { status, user, slowBootstrap, bootstrapError, retrySession, logout, changePassword } = useAuth();
  const {
    hubState,
    toasts,
    dismissToast,
    notifications,
    unreadCount,
    loadingNotifications,
    refreshNotifications,
    markRead,
    markAllRead,
    mutedPopups,
    setMutedPopups,
  } = useRealtime();

  const [mounted, setMounted] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const [notifTab, setNotifTab] = useState("unread");
  const [markingAll, setMarkingAll] = useState(false);

  const [pwdOpen, setPwdOpen] = useState(false);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [pwdError, setPwdError] = useState<string | null>(null);
  const [pwdSubmitting, setPwdSubmitting] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (status === "unauthenticated") {
      router.replace("/login");
    }
  }, [status, router]);

  const isAr = locale === "ar-SA";
  const t = isAr ? ar : en;
  const theme: Theme = mounted && resolvedTheme === "dark" ? "dark" : "light";

  if (status !== "authenticated" || !user) {
    const isUnauthenticated = status === "unauthenticated";
    return (
      <div className="flex min-h-screen w-full items-center justify-center bg-surface-canvas p-6 text-text-primary">
        <div
          role="status"
          className="w-full max-w-md space-y-4 rounded-xl border border-surface-border bg-surface-card p-6 text-center"
        >
          {!isUnauthenticated && (
            <div className="mx-auto h-4 w-3/4 animate-pulse rounded bg-surface-skeleton" />
          )}
          <p className="text-sm font-medium text-text-secondary">
            {isUnauthenticated
              ? isAr
                ? "يرجى تسجيل الدخول للمتابعة..."
                : "Redirecting to Sign In..."
              : t.auth.checkingSession}
          </p>

          {bootstrapError && (
            <AlertBanner
              tone="warning"
              title={isAr ? "تنبيه الاتصال بالخادم" : "Server Connection Notice"}
              message={bootstrapError}
            />
          )}

          {!isUnauthenticated && slowBootstrap && (
            <p className="text-xs text-text-muted">
              {isAr
                ? "قد يستغرق الخادم السحابي بضع ثوانٍ للاستيقاظ لأول مرة. يمكنك الانتظار قليلاً أو المحاولة الآن."
                : "The cloud server may take a few seconds to wake up on the first request. You can wait or retry now."}
            </p>
          )}

          {(slowBootstrap || isUnauthenticated || Boolean(bootstrapError)) && (
            <div className="flex flex-wrap items-center justify-center gap-2 pt-1">
              <Button variant="secondary" size="compact" onClick={() => void retrySession()}>
                {isAr ? "إعادة المحاولة" : "Retry"}
              </Button>
              <Button size="compact" onClick={() => router.replace("/login")}>
                {isAr ? "تسجيل الدخول" : "Go to Sign In"}
              </Button>
            </div>
          )}
        </div>
      </div>
    );
  }

  const displayName = isAr ? user.fullNameAr : (user.fullNameEn ?? user.fullNameAr);
  const canTrackGps = user.roleCodes.some((role) =>
    (GPS_ALLOWED_ROLES as readonly string[]).includes(role),
  );

  const navDefinitions: Array<{
    id: string;
    label: string;
    href: string;
    permissions: string[];
    roles?: readonly string[];
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
    { id: "gps", label: t.gps, href: "/gps", permissions: [], roles: GPS_ALLOWED_ROLES, icon: navIcons.gps },
    { id: "reports", label: t.reports, href: "/reports", permissions: ["audit:view", "gps:stream"], icon: navIcons.reports },
  ];

  const items: Array<SidebarNavItemProps & { id: string }> = navDefinitions
    .filter((item) => {
      if (item.roles && !item.roles.some((r) => user.roleCodes.includes(r))) {
        return false;
      }
      return hasAnyPermission(user, item.permissions);
    })
    .map((item) => ({
      id: item.id,
      label: item.label,
      href: item.href,
      active: item.href === "/" ? pathname === "/" : pathname.startsWith(item.href),
      icon: item.icon,
      onNavigate: (event) => {
        if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) {
          return;
        }
        event.preventDefault();
        if (pathname !== item.href) {
          router.push(item.href);
        }
      },
    }));

  const hasRoleAccess =
    !allowedRoles || allowedRoles.length === 0 || allowedRoles.some((r) => user.roleCodes.includes(r));
  const canAccessPage = hasRoleAccess && hasAnyPermission(user, requiredPermissions);

  const visibleNotifications =
    notifTab === "unread" ? notifications.filter((n) => !n.isRead) : notifications;

  const handleLogout = async () => {
    setLoggingOut(true);
    try {
      await logout();
    } finally {
      setLoggingOut(false);
    }
  };

  const handleChangePassword = async (e: FormEvent) => {
    e.preventDefault();
    setPwdError(null);

    if (newPassword.length < 8) {
      setPwdError(
        isAr ? "يجب أن تكون كلمة المرور الجديدة 8 أحرف على الأقل." : "New password must be at least 8 characters.",
      );
      return;
    }
    if (newPassword !== confirmNewPassword) {
      setPwdError(
        isAr ? "تأكيد كلمة المرور غير مطابق." : "Password confirmation does not match.",
      );
      return;
    }

    setPwdSubmitting(true);
    try {
      await changePassword({
        currentPassword,
        newPassword,
        confirmNewPassword,
      });
      setPwdOpen(false);
    } catch (err) {
      const formatted = formatApiError(err);
      setPwdError(formatted.details.length > 0 ? formatted.details.join(" | ") : formatted.message);
    } finally {
      setPwdSubmitting(false);
    }
  };

  const handleMarkAllRead = async () => {
    setMarkingAll(true);
    try {
      await markAllRead();
    } finally {
      setMarkingAll(false);
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
        onClick={() => {
          setPwdError(null);
          setCurrentPassword("");
          setNewPassword("");
          setConfirmNewPassword("");
          setPwdOpen(true);
        }}
        className="w-full"
      >
        {t.auth.changePassword}
      </Button>
      <Button
        variant="ghost"
        size="compact"
        loading={loggingOut}
        onClick={handleLogout}
        className="w-full text-text-inverse hover:bg-surface-charcoal"
      >
        {loggingOut ? t.auth.loggingOut : t.auth.logout}
      </Button>
    </div>
  );

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
                  ? isAr
                    ? "متصل مباشر"
                    : "Live"
                  : hubState === "reconnecting" || hubState === "connecting"
                    ? isAr
                      ? "جاري الاتصال"
                      : "Connecting"
                    : isAr
                      ? "غير متصل"
                      : "Offline"}
              </Badge>

              {canTrackGps && pathname !== "/gps" ? (
                <Button variant="ghost" size="compact" onClick={() => router.push("/gps")}>
                  {t.gps}
                </Button>
              ) : null}

              <Button
                variant="secondary"
                size="compact"
                onClick={() => {
                  setNotifOpen(true);
                  void refreshNotifications();
                }}
              >
                <span>{t.notifications.buttonLabel}</span>
                <Badge tone={unreadCount > 0 ? "danger" : "neutral"} mono>
                  {unreadCount}
                </Badge>
              </Button>

              {headerActions}
            </div>
          </div>

          {!mutedPopups && toasts.length > 0 && (
            <div className="space-y-2" aria-live="polite">
              {toasts.map((toast) => (
                <AlertBanner
                  key={toast.id}
                  tone={toast.tone}
                  title={toast.title}
                  message={toast.message}
                  onDismiss={() => {
                    dismissToast(toast.id);
                    if (toast.notificationId) {
                      void markRead(toast.notificationId);
                    }
                  }}
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
        open={notifOpen}
        onClose={() => setNotifOpen(false)}
        title={t.notifications.title}
        subtitle={t.notifications.subtitle}
        size="lg"
        closeLabel={t.common.close}
        footer={
          <>
            <Button
              variant="secondary"
              size="compact"
              onClick={() => setMutedPopups(!mutedPopups)}
            >
              {mutedPopups ? t.notifications.unmutePopups : t.notifications.mutePopups}
            </Button>
            <Button
              size="compact"
              loading={markingAll}
              disabled={unreadCount === 0}
              onClick={() => void handleMarkAllRead()}
            >
              {t.notifications.markAllRead}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          {mutedPopups ? (
            <AlertBanner tone="info" message={t.notifications.mutedHint} />
          ) : null}

          <Tabs
            activeId={notifTab}
            onChange={setNotifTab}
            items={[
              { id: "unread", label: t.notifications.tabUnread, badge: unreadCount },
              { id: "all", label: t.notifications.tabAll, badge: notifications.length },
            ]}
          />

          {loadingNotifications && visibleNotifications.length === 0 ? (
            <p className="py-6 text-center text-sm text-text-secondary">{t.common.loading}</p>
          ) : visibleNotifications.length === 0 ? (
            <EmptyState title={t.notifications.emptyTitle} description={t.notifications.emptyDesc} />
          ) : (
            <div className="space-y-2.5">
              {visibleNotifications.map((item) => (
                <NotificationItem
                  key={item.id}
                  title={isAr ? item.titleAr : item.titleEn}
                  message={isAr ? item.messageAr : item.messageEn}
                  severity={item.severity}
                  timestamp={formatDateTime(item.createdAtUtc, locale)}
                  isRead={item.isRead}
                  markReadLabel={t.notifications.markRead}
                  unreadLabel={t.notifications.unreadBadge}
                  onMarkRead={() => void markRead(item.id)}
                  onClick={() => {
                    if (!item.isRead) {
                      void markRead(item.id);
                    }
                    if (item.entityName === "SalesOrder") {
                      setNotifOpen(false);
                      router.push("/sales");
                    }
                  }}
                />
              ))}
            </div>
          )}
        </div>
      </DrawerModal>

      <DrawerModal
        open={pwdOpen}
        onClose={() => setPwdOpen(false)}
        title={t.auth.changePasswordTitle}
        subtitle={t.auth.changePasswordSubtitle}
        closeLabel={t.common.close}
        footer={
          <>
            <Button variant="secondary" size="compact" onClick={() => setPwdOpen(false)}>
              {t.common.cancel}
            </Button>
            <Button type="submit" form="erp-change-password-form" size="compact" loading={pwdSubmitting}>
              {t.auth.changePasswordSubmit}
            </Button>
          </>
        }
      >
        <form id="erp-change-password-form" onSubmit={handleChangePassword} className="space-y-4">
          {pwdError ? <AlertBanner tone="danger" message={pwdError} /> : null}
          <PasswordField
            id="erp-current-password"
            label={t.auth.currentPassword}
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.currentTarget.value)}
            autoComplete="current-password"
            required
          />
          <PasswordField
            id="erp-new-password"
            label={t.auth.newPassword}
            value={newPassword}
            onChange={(e) => setNewPassword(e.currentTarget.value)}
            autoComplete="new-password"
            required
          />
          <PasswordField
            id="erp-confirm-password"
            label={t.auth.confirmNewPassword}
            value={confirmNewPassword}
            onChange={(e) => setConfirmNewPassword(e.currentTarget.value)}
            autoComplete="new-password"
            required
          />
        </form>
      </DrawerModal>
    </>
  );
}
