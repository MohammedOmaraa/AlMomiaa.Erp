import type { ReactNode } from "react";

export type SidebarNavItemProps = {
  label: string;
  href?: string;
  icon?: ReactNode;
  active?: boolean;
  collapsed?: boolean;
  placeholder?: boolean;
  placeholderLabel?: string;
  onNavigate?: () => void;
};

export function SidebarNavItem({
  label,
  href,
  icon,
  active = false,
  collapsed = false,
  placeholder = false,
  placeholderLabel,
  onNavigate,
}: SidebarNavItemProps) {
  const layoutClass = collapsed ? "justify-center px-2" : "justify-between px-3";
  const stateClass = active
    ? "bg-surface-charcoal font-semibold text-brand-gold"
    : placeholder
      ? "cursor-default text-text-inverse-muted opacity-80"
      : "text-text-inverse hover:bg-surface-charcoal";

  const className = `flex min-h-11 w-full items-center gap-3 rounded-lg text-start text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-gold focus-visible:ring-offset-2 focus-visible:ring-offset-surface-obsidian ${layoutClass} ${stateClass}`;

  const content = (
    <>
      <span className={`flex min-w-0 items-center gap-3 ${collapsed ? "justify-center" : ""}`}>
        {icon && (
          <span aria-hidden="true" className="flex size-5 shrink-0 items-center justify-center">
            {icon}
          </span>
        )}
        {!collapsed && <span className="truncate">{label}</span>}
      </span>
      {!collapsed && placeholder && placeholderLabel && (
        <span className="ms-2 shrink-0 rounded bg-surface-charcoal px-1.5 py-0.5 text-xs font-medium text-text-inverse-muted">
          {placeholderLabel}
        </span>
      )}
      {collapsed && <span className="sr-only">{label}</span>}
    </>
  );

  if (href && !placeholder) {
    return (
      <a
        href={href}
        aria-current={active ? "page" : undefined}
        title={collapsed ? label : undefined}
        onClick={onNavigate}
        className={className}
      >
        {content}
      </a>
    );
  }

  return (
    <button
      type="button"
      aria-current={active ? "page" : undefined}
      aria-disabled={placeholder || undefined}
      title={collapsed ? label : undefined}
      onClick={placeholder ? undefined : onNavigate}
      className={className}
    >
      {content}
    </button>
  );
}
