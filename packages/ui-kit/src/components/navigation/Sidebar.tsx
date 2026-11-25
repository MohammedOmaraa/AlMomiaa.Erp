import type { ReactNode } from "react";
import { BrandLogo } from "../brand/BrandLogo";
import { SidebarNavItem } from "./SidebarNavItem";
import type { SidebarNavItemProps } from "./SidebarNavItem";

export type SidebarProps = {
  items: Array<SidebarNavItemProps & { id: string }>;
  brandLabel: string;
  compact?: boolean;
  headerAction?: ReactNode;
  footer?: ReactNode;
  onNavigate?: () => void;
};

export function Sidebar({
  items,
  brandLabel,
  compact = false,
  headerAction,
  footer,
  onNavigate,
}: SidebarProps) {
  return (
    <div className="flex h-full flex-col gap-4 border-e border-surface-charcoal bg-surface-obsidian p-3 text-text-inverse">
      <div className="flex flex-col gap-2">
        {headerAction && (
          <div className={`flex ${compact ? "justify-center" : "justify-end"}`}>
            {headerAction}
          </div>
        )}
        <BrandLogo label={brandLabel} compact={compact} />
      </div>
      <nav aria-label={brandLabel} className="min-h-0 flex-1 overflow-y-auto">
        <ul className="space-y-1">
          {items.map(({ id, onNavigate: itemOnNavigate, ...item }) => (
            <li key={id}>
              <SidebarNavItem
                {...item}
                collapsed={compact}
                onNavigate={(event) => {
                  itemOnNavigate?.(event);
                  onNavigate?.();
                }}
              />
            </li>
          ))}
        </ul>
      </nav>
      {footer && <div className="border-t border-surface-charcoal pt-3">{footer}</div>}
    </div>
  );
}
