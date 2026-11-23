"use client";

import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import { useRouter } from "next/navigation";
import { Button, ResponsiveAppShell, SummaryCard, SurfaceCard } from "@almomiaa/ui";
import type { SidebarNavItemProps, Theme } from "@almomiaa/ui";
import { useTheme } from "next-themes";
import { ar } from "../locales/ar";
import { en } from "../locales/en";
import { useAuth } from "../providers/AuthProvider";
import { useLocale } from "../providers/LocaleProvider";

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
  sales: <NavIcon paths={["M3 17l6-6 4 4 8-8", "M17 7h4v4"]} />,
  customers: <NavIcon paths={["M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2", "M9 7a4 4 0 1 0 0-8 4 4 0 0 0 0 8z", "M23 21v-2a4 4 0 0 0-3-3.87", "M16 3.13a4 4 0 0 1 0 7.75"]} />,
  inventory: <NavIcon paths={["M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z", "M3.27 6.96L12 12.01l8.73-5.05", "M12 22.08V12"]} />,
  purchasing: <NavIcon paths={["M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z", "M3 6h18", "M16 10a4 4 0 0 1-8 0"]} />,
  finance: <NavIcon paths={["M2 7h20v12H2z", "M16 13h2", "M2 10h20"]} />,
  reports: <NavIcon paths={["M18 20V10", "M12 20V4", "M6 20v-6"]} />,
  settings: <NavIcon paths={["M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z", "M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"]} />,
};

export default function HomePage() {
  const router = useRouter();
  const { locale, setLocale } = useLocale();
  const { resolvedTheme, setTheme } = useTheme();
  const { status, user, logout } = useAuth();
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
  const onThemeChange = (nextTheme: Theme) => setTheme(nextTheme);

  if (status !== "authenticated" || !user) {
    return (
      <div className="flex min-h-screen w-full items-center justify-center bg-surface-canvas p-6 text-text-primary">
        <div role="status" className="w-full max-w-sm rounded-xl border border-surface-border bg-surface-card p-6 text-center">
          <div className="mx-auto mb-3 h-4 w-3/4 animate-pulse rounded bg-surface-skeleton" />
          <p className="text-sm text-text-secondary">{t.auth.checkingSession}</p>
        </div>
      </div>
    );
  }

  const displayName = locale === "ar-SA" ? user.fullNameAr : (user.fullNameEn ?? user.fullNameAr);

  const handleLogout = async () => {
    setLoggingOut(true);
    try {
      await logout();
    } finally {
      setLoggingOut(false);
    }
  };

  const items: Array<SidebarNavItemProps & { id: string }> = [
    { id: "dashboard", label: t.dashboard, href: "/", active: true, icon: navIcons.dashboard },
    { id: "sales", label: t.sales, placeholder: true, placeholderLabel: t.soon, icon: navIcons.sales },
    { id: "customers", label: t.customers, placeholder: true, placeholderLabel: t.soon, icon: navIcons.customers },
    { id: "inventory", label: t.inventory, placeholder: true, placeholderLabel: t.soon, icon: navIcons.inventory },
    { id: "purchasing", label: t.purchasing, placeholder: true, placeholderLabel: t.soon, icon: navIcons.purchasing },
    { id: "finance", label: t.finance, placeholder: true, placeholderLabel: t.soon, icon: navIcons.finance },
    { id: "reports", label: t.reports, placeholder: true, placeholderLabel: t.soon, icon: navIcons.reports },
    { id: "settings", label: t.settings, placeholder: true, placeholderLabel: t.soon, icon: navIcons.settings },
  ];

  const sidebarFooter = (
    <div className="space-y-2">
      <div className="truncate px-1 text-start">
        <p className="truncate text-xs text-text-inverse-muted">{t.auth.signedInAs}</p>
        <p className="truncate text-sm font-semibold text-text-inverse">{displayName}</p>
        <p className="truncate font-mono text-xs text-brand-gold">{user.employeeCode}</p>
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

  return (
    <ResponsiveAppShell
      items={items}
      brandLabel={t.brand}
      title={t.dashboard}
      locale={locale}
      theme={theme}
      onLocaleChange={setLocale}
      onThemeChange={onThemeChange}
      sidebarFooter={sidebarFooter}
    >
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-6">
        <SurfaceCard>
          <p className="text-start text-sm font-medium text-text-secondary">{t.preview}</p>
          <h2 className="mt-2 text-start text-2xl font-bold text-text-primary sm:text-3xl">
            {t.welcome}
          </h2>
          <p className="mt-2 text-start text-sm text-text-secondary">
            {displayName} ({user.employeeCode}) — {t.illustrative}
          </p>
        </SurfaceCard>

        <div className="grid min-w-0 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          <SummaryCard label={t.sales} value="—" supportingText={t.placeholder} icon={navIcons.sales} />
          <SummaryCard label={t.customers} value="—" supportingText={t.placeholder} icon={navIcons.customers} />
          <SummaryCard label={t.inventory} value="—" supportingText={t.placeholder} icon={navIcons.inventory} />
        </div>

        <SurfaceCard title={t.sampleActivity} description={t.placeholder}>
          <p className="text-start text-sm text-text-secondary">{t.activityValue}</p>
        </SurfaceCard>
      </div>
    </ResponsiveAppShell>
  );
}
