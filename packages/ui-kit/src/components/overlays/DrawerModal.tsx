import { useEffect, type ReactNode } from "react";

export type DrawerModalProps = {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  size?: "md" | "lg" | "xl";
  closeLabel?: string;
  footer?: ReactNode;
  children: ReactNode;
};

const widthStyles = {
  md: "max-w-lg",
  lg: "max-w-2xl",
  xl: "max-w-4xl",
};

export function DrawerModal({
  open,
  onClose,
  title,
  subtitle,
  size = "lg",
  closeLabel = "Close",
  footer,
  children,
}: DrawerModalProps) {
  useEffect(() => {
    if (!open) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-surface-obsidian/60 backdrop-blur-xs">
      <div
        className="fixed inset-0"
        aria-hidden="true"
        onClick={onClose}
      />
      <section
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={`relative z-10 flex h-full w-full flex-col border-s border-surface-border bg-surface-card shadow-2xl ${widthStyles[size]}`}
      >
        <header className="flex items-start justify-between gap-4 border-b border-surface-border px-5 py-4 text-start">
          <div className="min-w-0">
            <h2 className="text-lg font-bold text-text-primary">{title}</h2>
            {subtitle && <p className="mt-0.5 text-sm text-text-secondary">{subtitle}</p>}
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label={closeLabel}
            className="inline-flex size-10 shrink-0 items-center justify-center rounded-lg border border-surface-border bg-surface-canvas text-sm font-bold text-text-primary hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            ✕
          </button>
        </header>

        <div className="flex-1 overflow-y-auto px-5 py-4 text-start">{children}</div>

        {footer && (
          <footer className="flex flex-wrap items-center justify-end gap-2 border-t border-surface-border bg-surface-canvas/70 px-5 py-3.5">
            {footer}
          </footer>
        )}
      </section>
    </div>
  );
}
