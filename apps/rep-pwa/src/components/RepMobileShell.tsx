import type { ReactNode } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  AlertBanner,
  Badge,
  BottomNavBar,
  BrandLogo,
  Button,
  LocaleSwitcher,
  ThemeToggle,
} from "@almomiaa/ui";
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
  const { locale, isAr, setLocale, theme, setTheme, t, user, logout, isOnline, hubState, toasts, dismissToast } = useApp();
  const location = useLocation();
  const navigate = useNavigate();

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

  return (
    <div className="min-h-screen bg-surface-canvas pb-24 text-text-primary">
      <header className="sticky top-0 z-30 border-b border-surface-border bg-surface-obsidian text-text-inverse shadow-sm">
        <div className="mx-auto flex max-w-2xl items-center justify-between gap-2 px-3 py-2">
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

          <div className="flex shrink-0 items-center gap-1.5">
            <LocaleSwitcher value={locale} onChange={setLocale} />
            <ThemeToggle value={theme} onChange={setTheme} />
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

        {toasts.length > 0 ? (
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
    </div>
  );
}
