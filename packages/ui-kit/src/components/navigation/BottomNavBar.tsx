import type { ReactNode } from "react";

export interface BottomNavItem {
  id: string;
  label: string;
  icon: ReactNode;
  active?: boolean;
  badgeCount?: number;
  disabled?: boolean;
}

export interface BottomNavBarProps {
  items: readonly BottomNavItem[];
  onSelect: (id: string) => void;
  ariaLabel?: string;
  className?: string;
}

export function BottomNavBar({
  items,
  onSelect,
  ariaLabel = "Primary mobile navigation",
  className = ""
}: BottomNavBarProps): ReactNode {
  return (
    <nav
      aria-label={ariaLabel}
      className={`fixed inset-x-0 bottom-0 z-40 border-t border-surface-border bg-surface-card/95 backdrop-blur-md ${className}`}
    >
      <ul className="mx-auto flex max-w-2xl items-stretch justify-around px-2 py-1.5">
        {items.map((item) => {
          const isActive = Boolean(item.active);
          return (
            <li key={item.id} className="flex flex-1">
              <button
                type="button"
                disabled={item.disabled}
                onClick={() => onSelect(item.id)}
                aria-current={isActive ? "page" : undefined}
                className={`relative flex w-full flex-col items-center justify-center gap-1 rounded-xl px-2 py-2 text-xs font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-gold disabled:opacity-45 ${
                  isActive
                    ? "bg-brand-gold/15 text-text-primary"
                    : "text-text-secondary hover:bg-surface-hover hover:text-text-primary"
                }`}
              >
                {isActive ? (
                  <span
                    aria-hidden="true"
                    className="absolute inset-x-4 top-0 h-0.5 rounded-full bg-brand-gold"
                  />
                ) : null}
                <span className="relative flex h-5 w-5 items-center justify-center">
                  {item.icon}
                  {typeof item.badgeCount === "number" && item.badgeCount > 0 ? (
                    <span className="absolute -end-3 -top-1.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-brand-gold px-1 text-[10px] font-bold text-surface-obsidian tabular-nums">
                      {item.badgeCount > 99 ? "99+" : item.badgeCount}
                    </span>
                  ) : null}
                </span>
                <span className="truncate">{item.label}</span>
              </button>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
