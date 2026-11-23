import type { Ref } from "react";
import { LocaleSwitcher } from "../preferences/LocaleSwitcher";
import type { Locale } from "../preferences/LocaleSwitcher";
import { ThemeToggle } from "../preferences/ThemeToggle";
import type { Theme } from "../preferences/ThemeToggle";

export type AppHeaderProps = {
  title: string;
  locale: Locale;
  theme: Theme;
  onLocaleChange: (locale: Locale) => void;
  onThemeChange: (theme: Theme) => void;
  onMenuClick?: () => void;
  menuExpanded?: boolean;
  menuLabel?: string;
  className?: string;
  menuButtonRef?: Ref<HTMLButtonElement>;
};

export function AppHeader({
  title,
  locale,
  theme,
  onLocaleChange,
  onThemeChange,
  onMenuClick,
  menuExpanded = false,
  menuLabel,
  className = "",
  menuButtonRef,
}: AppHeaderProps) {
  const isArabic = locale === "ar-SA";
  const resolvedMenuLabel =
    menuLabel ??
    (isArabic
      ? menuExpanded
        ? "إغلاق قائمة التنقل"
        : "فتح قائمة التنقل"
      : menuExpanded
        ? "Close navigation"
        : "Open navigation");

  return (
    <header
      className={`flex min-h-16 items-center justify-between gap-3 border-b border-surface-border bg-surface-card px-3 py-2 sm:px-5 ${className}`}
    >
      <div className="flex min-w-0 items-center gap-2">
        {onMenuClick && (
          <button
            ref={menuButtonRef}
            type="button"
            aria-label={resolvedMenuLabel}
            aria-expanded={menuExpanded}
            aria-controls="almomiaa-primary-navigation"
            className="inline-flex size-11 shrink-0 items-center justify-center rounded-lg text-lg text-text-primary transition-colors hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface-card md:hidden"
            onClick={onMenuClick}
          >
            <span aria-hidden="true">{"\u2630"}</span>
          </button>
        )}
        <h1 className="truncate text-start text-base font-semibold text-text-primary sm:text-lg">
          {title}
        </h1>
      </div>
      <div className="flex shrink-0 items-center gap-1 sm:gap-2">
        <LocaleSwitcher
          value={locale}
          onChange={onLocaleChange}
          label={isArabic ? "اللغة" : "Language"}
        />
        <ThemeToggle
          value={theme}
          onChange={onThemeChange}
          labels={
            isArabic
              ? {
                  light: "التبديل إلى الوضع الداكن",
                  dark: "التبديل إلى الوضع الفاتح",
                }
              : undefined
          }
        />
      </div>
    </header>
  );
}
