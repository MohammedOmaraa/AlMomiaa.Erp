import type { ReactNode } from "react";
import { Badge } from "../data/Badge";

export type TabItem = {
  id: string;
  label: string;
  badge?: string | number;
  icon?: ReactNode;
  disabled?: boolean;
};

export type TabsProps = {
  items: TabItem[];
  activeId: string;
  onChange: (id: string) => void;
  ariaLabel?: string;
  className?: string;
};

export function Tabs({ items, activeId, onChange, ariaLabel = "Section tabs", className = "" }: TabsProps) {
  return (
    <div
      role="tablist"
      aria-label={ariaLabel}
      className={`flex flex-wrap items-center gap-1.5 border-b border-surface-border pb-2 ${className}`}
    >
      {items.map((item) => {
        const isActive = item.id === activeId;
        return (
          <button
            key={item.id}
            type="button"
            role="tab"
            aria-selected={isActive}
            disabled={item.disabled}
            onClick={() => onChange(item.id)}
            className={`inline-flex min-h-10 items-center gap-2 rounded-lg px-3.5 py-2 text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-50 ${
              isActive
                ? "bg-brand-gold text-surface-obsidian shadow-sm"
                : "bg-surface-card text-text-secondary hover:bg-surface-hover hover:text-text-primary"
            }`}
          >
            {item.icon}
            <span>{item.label}</span>
            {item.badge !== undefined && (
              <Badge tone={isActive ? "neutral" : "gold"} size="sm" mono>
                {item.badge}
              </Badge>
            )}
          </button>
        );
      })}
    </div>
  );
}
