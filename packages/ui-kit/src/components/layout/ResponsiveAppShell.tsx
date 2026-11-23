import { useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import { AppHeader } from "./AppHeader";
import type { Locale } from "../preferences/LocaleSwitcher";
import type { Theme } from "../preferences/ThemeToggle";
import { Sidebar } from "../navigation/Sidebar";
import type { SidebarNavItemProps } from "../navigation/SidebarNavItem";
import { Button } from "../actions/Button";

export type ResponsiveAppShellProps = {
  children?: ReactNode;
  items: Array<SidebarNavItemProps & { id: string }>;
  brandLabel: string;
  title: string;
  locale: Locale;
  theme: Theme;
  onLocaleChange: (locale: Locale) => void;
  onThemeChange: (theme: Theme) => void;
  contentState?: "ready" | "loading" | "empty" | "error";
  stateMessage?: string;
  retryLabel?: string;
  onRetry?: () => void;
  initialCompact?: boolean;
  initialDrawerOpen?: boolean;
};

export function ResponsiveAppShell({
  children,
  items,
  brandLabel,
  title,
  locale,
  theme,
  onLocaleChange,
  onThemeChange,
  contentState = "ready",
  stateMessage,
  retryLabel,
  onRetry,
  initialCompact = true,
  initialDrawerOpen = false,
}: ResponsiveAppShellProps) {
  const [compact, setCompact] = useState(initialCompact);
  const [drawerOpen, setDrawerOpen] = useState(initialDrawerOpen);
  const [mobileViewport, setMobileViewport] = useState(false);
  const [mediumViewport, setMediumViewport] = useState(false);
  const navRef = useRef<HTMLElement>(null);
  const menuRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const mobileMedia = window.matchMedia("(max-width: 767px)");
    const mediumMedia = window.matchMedia("(min-width: 768px) and (max-width: 1279px)");
    const updateViewport = () => {
      setMobileViewport(mobileMedia.matches);
      setMediumViewport(mediumMedia.matches);
      if (!mobileMedia.matches) {
        setDrawerOpen(false);
      }
    };
    updateViewport();
    mobileMedia.addEventListener("change", updateViewport);
    mediumMedia.addEventListener("change", updateViewport);
    return () => {
      mobileMedia.removeEventListener("change", updateViewport);
      mediumMedia.removeEventListener("change", updateViewport);
    };
  }, []);

  useEffect(() => {
    if (!drawerOpen || !mobileViewport) return;

    const getFocusable = () =>
      Array.from(
        navRef.current?.querySelectorAll<HTMLElement>(
          "a[href], button:not([disabled]):not([aria-disabled='true'])"
        ) ?? []
      ).filter((element) => element.getClientRects().length > 0);

    const initialFocusable = getFocusable()[0];
    initialFocusable?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setDrawerOpen(false);
        menuRef.current?.focus();
        return;
      }
      if (event.key === "Tab") {
        const elements = getFocusable();
        if (elements.length === 0) return;
        const first = elements[0];
        const last = elements[elements.length - 1];
        if (event.shiftKey && document.activeElement === first) {
          event.preventDefault();
          last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault();
          first.focus();
        }
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [drawerOpen, mobileViewport]);

  const closeDrawer = () => {
    setDrawerOpen(false);
    menuRef.current?.focus();
  };

  const handleNavigate = () => {
    if (drawerOpen) {
      closeDrawer();
    }
  };

  const isArabic = locale === "ar-SA";
  const resolvedRetryLabel = retryLabel ?? (isArabic ? "إعادة المحاولة" : "Retry");
  const stateLabel =
    stateMessage ??
    (contentState === "loading"
      ? isArabic
        ? "جارٍ تحميل مساحة العمل"
        : "Loading workspace"
      : contentState === "empty"
        ? isArabic
          ? "لا يوجد محتوى لعرضه"
          : "No content to display"
        : isArabic
          ? "تعذر تحميل مساحة العمل"
          : "Workspace content unavailable");

  const closeNavigationLabel = isArabic ? "إغلاق قائمة التنقل" : "Close navigation";
  const openNavigationLabel = isArabic ? "فتح قائمة التنقل" : "Open navigation";
  const expandNavigationLabel = isArabic ? "توسيع قائمة التنقل" : "Expand navigation";
  const collapseNavigationLabel = isArabic ? "طي قائمة التنقل" : "Collapse navigation";

  const isCompactRail = mediumViewport && compact;

  const sidebarHeaderAction = (
    <>
      <button
        type="button"
        className="inline-flex size-11 items-center justify-center rounded-lg bg-surface-charcoal text-lg text-text-inverse transition-colors hover:text-brand-gold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-gold focus-visible:ring-offset-2 focus-visible:ring-offset-surface-obsidian md:hidden"
        aria-label={closeNavigationLabel}
        onClick={closeDrawer}
      >
        <span aria-hidden="true">{"\u2715"}</span>
      </button>
      <button
        type="button"
        className="hidden size-11 items-center justify-center rounded-lg bg-surface-charcoal text-lg text-text-inverse transition-colors hover:text-brand-gold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-gold focus-visible:ring-offset-2 focus-visible:ring-offset-surface-obsidian md:max-xl:inline-flex"
        aria-label={compact ? expandNavigationLabel : collapseNavigationLabel}
        aria-expanded={!compact}
        onClick={() => setCompact((value) => !value)}
      >
        <span aria-hidden="true" className="inline-block rtl:rotate-180">
          {compact ? "\u203a" : "\u2039"}
        </span>
      </button>
    </>
  );

  const mobileTransformClass = drawerOpen
    ? "translate-x-0"
    : "-translate-x-full rtl:translate-x-full";

  return (
    <div className="flex min-h-screen w-full overflow-x-clip bg-surface-canvas text-text-primary">
      {drawerOpen && (
        <button
          type="button"
          aria-label={closeNavigationLabel}
          className="fixed inset-0 z-20 bg-surface-obsidian/60 md:hidden"
          onClick={closeDrawer}
        />
      )}
      <aside
        id="almomiaa-primary-navigation"
        ref={navRef}
        aria-label={brandLabel}
        role={drawerOpen && mobileViewport ? "dialog" : undefined}
        aria-modal={drawerOpen && mobileViewport ? true : undefined}
        aria-hidden={mobileViewport && !drawerOpen ? true : undefined}
        inert={mobileViewport && !drawerOpen}
        className={`fixed inset-y-0 start-0 z-30 h-dvh w-72 shrink-0 transition-[width,transform] duration-200 ${mobileTransformClass} md:sticky md:top-0 md:z-auto md:h-screen md:translate-x-0 md:rtl:translate-x-0 ${
          compact ? "md:max-xl:w-20" : "md:max-xl:w-72"
        } xl:w-72`}
      >
        <Sidebar
          items={items}
          brandLabel={brandLabel}
          compact={isCompactRail}
          headerAction={sidebarHeaderAction}
          onNavigate={handleNavigate}
        />
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <AppHeader
          title={title}
          locale={locale}
          theme={theme}
          onLocaleChange={onLocaleChange}
          onThemeChange={onThemeChange}
          onMenuClick={() => setDrawerOpen((value) => !value)}
          menuExpanded={drawerOpen}
          menuLabel={drawerOpen ? closeNavigationLabel : openNavigationLabel}
          menuButtonRef={menuRef}
        />
        <main className="min-w-0 flex-1 overflow-x-hidden p-4 sm:p-6">
          {contentState === "loading" && (
            <div role="status" className="space-y-4" aria-label={stateLabel}>
              <span className="sr-only">{stateLabel}</span>
              <div className="h-8 animate-pulse rounded-lg bg-surface-skeleton" />
              <div className="h-40 animate-pulse rounded-xl bg-surface-skeleton" />
            </div>
          )}
          {contentState === "empty" && (
            <div className="rounded-xl border border-surface-border bg-surface-card p-8 text-center text-text-secondary">
              {stateLabel}
            </div>
          )}
          {contentState === "error" && (
            <div
              role="alert"
              className="flex flex-wrap items-center justify-between gap-4 rounded-xl bg-status-danger-bg p-5 text-status-danger"
            >
              <p>{stateLabel}</p>
              {onRetry && (
                <Button variant="secondary" onClick={onRetry}>
                  {resolvedRetryLabel}
                </Button>
              )}
            </div>
          )}
          {contentState === "ready" && children}
        </main>
      </div>
    </div>
  );
}
