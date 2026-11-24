import { useState, type FormEvent, type ReactNode } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  AlertBanner,
  Badge,
  BottomNavBar,
  BrandLogo,
  Button,
  DrawerModal,
  EmptyState,
  LocaleSwitcher,
  NotificationItem,
  PasswordField,
  Tabs,
  ThemeToggle,
} from "@almomiaa/ui";
import { formatApiError } from "../lib/api-client";
import { formatDateTime } from "../lib/formatters";
import { useApp } from "../providers/AppProviders";

interface RepMobileShellProps {
  children: ReactNode;
  pendingCount?: number;
}

const HomeIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="h-5 w-5">
    <path strokeLinecap="round" strokeLinejoin="round" d="M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1v-9.5Z" />
  </svg>
);

const PlusCircleIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="h-5 w-5">
    <circle cx="12" cy="12" r="9" />
    <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v8m-4-4h8" />
  </svg>
);

const ClipboardIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="h-5 w-5">
    <path strokeLinecap="round" strokeLinejoin="round" d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2" />
  </svg>
);

export function RepMobileShell({ children, pendingCount = 0 }: RepMobileShellProps) {
  const {
    locale,
    isAr,
    setLocale,
    theme,
    setTheme,
    t,
    user,
    logout,
    changePassword,
    isOnline,
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
  } = useApp();
  const location = useLocation();
  const navigate = useNavigate();

  const [notifOpen, setNotifOpen] = useState(false);
  const [notifTab, setNotifTab] = useState("unread");
  const [markingAll, setMarkingAll] = useState(false);

  const [pwdOpen, setPwdOpen] = useState(false);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [pwdError, setPwdError] = useState<string | null>(null);
  const [pwdSubmitting, setPwdSubmitting] = useState(false);

  const hubTone = !isOnline
    ? "danger"
    : hubState === "connected"
      ? "success"
      : hubState === "connecting" || hubState === "reconnecting"
        ? "warning"
        : "neutral";

  const hubLabel = !isOnline
    ? t.connection.offline
    : hubState === "connected"
      ? t.connection.live
      : hubState === "reconnecting"
        ? t.connection.reconnecting
        : hubState === "connecting"
          ? t.connection.connecting
          : t.connection.offline;

  const activeNav =
    location.pathname === "/new-request"
      ? "new-request"
      : location.pathname === "/requests"
        ? "requests"
        : "home";

  const visibleNotifications =
    notifTab === "unread" ? notifications.filter((n) => !n.isRead) : notifications;

  async function handleChangePassword(e: FormEvent) {
    e.preventDefault();
    setPwdError(null);
    if (newPassword.length < 8) {
      setPwdError(isAr ? "كلمة المرور الجديدة 8 أحرف على الأقل." : "New password must be at least 8 characters.");
      return;
    }
    if (newPassword !== confirmNewPassword) {
      setPwdError(isAr ? "تأكيد كلمة المرور غير مطابق." : "Password confirmation does not match.");
      return;
    }
    setPwdSubmitting(true);
    try {
      await changePassword({ currentPassword, newPassword, confirmNewPassword });
      setPwdOpen(false);
      navigate("/login?passwordChanged=1", { replace: true });
    } catch (err) {
      setPwdError(formatApiError(err, "Failed to change password."));
    } finally {
      setPwdSubmitting(false);
    }
  }

  async function handleMarkAllRead() {
    setMarkingAll(true);
    try {
      await markAllRead();
    } finally {
      setMarkingAll(false);
    }
  }

  return (
    <div className="min-h-screen bg-surface-canvas pb-24 text-text-primary">
      <header className="sticky top-0 z-30 border-b border-surface-border bg-surface-obsidian text-text-inverse shadow-sm">
        <div className="mx-auto flex max-w-2xl flex-wrap items-center justify-between gap-2 px-3 py-2">
          <div className="flex min-w-0 items-center gap-2">
            <BrandLogo label={t.appName} compact />
            <div className="min-w-0">
              <div className="truncate text-xs text-text-muted">
                {isAr ? user?.fullNameAr : user?.fullNameEn}
              </div>
              <Badge tone={hubTone} size="sm">
                {hubLabel}
              </Badge>
            </div>
          </div>

          <div className="flex shrink-0 flex-wrap items-center gap-1.5">
            <Button
              size="compact"
              variant="secondary"
              onClick={() => {
                setNotifOpen(true);
                void refreshNotifications();
              }}
            >
              <span>{t.notifications.buttonLabel}</span>
              <Badge tone={unreadCount > 0 ? "danger" : "neutral"} size="sm" mono>
                {unreadCount}
              </Badge>
            </Button>
            <LocaleSwitcher value={locale} onChange={setLocale} />
            <ThemeToggle value={theme} onChange={setTheme} />
            <Button
              size="compact"
              variant="ghost"
              onClick={() => {
                setPwdError(null);
                setCurrentPassword("");
                setNewPassword("");
                setConfirmNewPassword("");
                setPwdOpen(true);
              }}
              className="text-xs text-text-inverse hover:bg-surface-charcoal"
            >
              {t.auth.changePassword}
            </Button>
            <Button
              size="compact"
              variant="ghost"
              onClick={() => void logout()}
              className="text-xs text-text-inverse hover:bg-surface-charcoal"
            >
              {t.auth.signOut}
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-2xl space-y-4 px-3 py-4">
        {!isOnline ? (
          <AlertBanner
            tone="warning"
            title={t.connection.offline}
            message={t.connection.offlineBanner}
          />
        ) : null}

        {!mutedPopups && toasts.length > 0 ? (
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
        ) : null}

        {children}
      </main>

      <BottomNavBar
        items={[
          { id: "home", label: t.nav.home, icon: HomeIcon, active: activeNav === "home" },
          { id: "new-request", label: t.nav.newRequest, icon: PlusCircleIcon, active: activeNav === "new-request" },
          {
            id: "requests",
            label: t.nav.requests,
            icon: ClipboardIcon,
            active: activeNav === "requests",
            badgeCount: pendingCount,
          },
        ]}
        onSelect={(id) => {
          if (id === "home") navigate("/");
          else if (id === "new-request") navigate("/new-request");
          else if (id === "requests") navigate("/requests");
        }}
      />

      <DrawerModal
        open={notifOpen}
        onClose={() => setNotifOpen(false)}
        title={t.notifications.title}
        subtitle={t.notifications.subtitle}
        footer={
          <>
            <Button
              size="compact"
              variant="secondary"
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
        <div className="space-y-3">
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
            <p className="py-6 text-center text-sm text-text-secondary">...</p>
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
                    if (item.entityName === "SalesOrder" && item.entityId) {
                      setNotifOpen(false);
                      navigate(`/requests?orderId=${encodeURIComponent(item.entityId)}`);
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
        footer={
          <>
            <Button size="compact" variant="secondary" onClick={() => setPwdOpen(false)}>
              {t.wizard.back}
            </Button>
            <Button
              type="submit"
              form="rep-change-password-form"
              size="compact"
              loading={pwdSubmitting}
            >
              {t.auth.updatePassword}
            </Button>
          </>
        }
      >
        <form id="rep-change-password-form" onSubmit={handleChangePassword} className="space-y-4">
          {pwdError ? <AlertBanner tone="danger" message={pwdError} /> : null}
          <PasswordField
            id="rep-current-password"
            label={t.auth.currentPassword}
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            autoComplete="current-password"
            required
          />
          <PasswordField
            id="rep-new-password"
            label={t.auth.newPassword}
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            autoComplete="new-password"
            required
          />
          <PasswordField
            id="rep-confirm-password"
            label={t.auth.confirmNewPassword}
            value={confirmNewPassword}
            onChange={(e) => setConfirmNewPassword(e.target.value)}
            autoComplete="new-password"
            required
          />
        </form>
      </DrawerModal>
    </div>
  );
}
