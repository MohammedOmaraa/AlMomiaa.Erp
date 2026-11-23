import type { ReactNode } from "react";

export type SurfaceState = "ready" | "loading" | "empty" | "error";

export type SurfaceCardProps = {
  title?: string;
  description?: string;
  state?: SurfaceState;
  loadingLabel?: string;
  emptyMessage?: string;
  errorMessage?: string;
  retryLabel?: string;
  onRetry?: () => void;
  children?: ReactNode;
  className?: string;
};

export function SurfaceCard({
  title,
  description,
  state = "ready",
  loadingLabel = "Loading",
  emptyMessage = "Nothing to display yet.",
  errorMessage = "This section could not be loaded.",
  retryLabel = "Retry",
  onRetry,
  children,
  className = "",
}: SurfaceCardProps) {
  return (
    <section
      aria-busy={state === "loading" || undefined}
      className={`rounded-xl border border-surface-border bg-surface-card p-4 text-text-primary shadow-sm sm:p-5 ${className}`}
    >
      {(title || description) && (
        <header className="mb-4">
          {title && <h2 className="text-start text-base font-semibold text-text-primary">{title}</h2>}
          {description && (
            <p className="mt-1 text-start text-sm text-text-secondary">{description}</p>
          )}
        </header>
      )}
      {state === "loading" && (
        <div role="status" aria-label={loadingLabel} className="space-y-3">
          <span className="sr-only">{loadingLabel}</span>
          <div className="h-4 animate-pulse rounded bg-surface-skeleton" />
          <div className="h-4 w-3/4 animate-pulse rounded bg-surface-skeleton" />
        </div>
      )}
      {state === "empty" && (
        <p className="py-6 text-center text-sm text-text-secondary">{emptyMessage}</p>
      )}
      {state === "error" && (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg bg-status-danger-bg p-3 text-sm text-status-danger"
        >
          <p>{errorMessage}</p>
          {onRetry && (
            <button
              type="button"
              onClick={onRetry}
              className="min-h-11 rounded-md px-3 font-semibold underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
            >
              {retryLabel}
            </button>
          )}
        </div>
      )}
      {state === "ready" && children}
    </section>
  );
}
