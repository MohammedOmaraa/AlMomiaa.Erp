import type { ReactNode } from "react";
import type { SurfaceState } from "./SurfaceCard";

export type SummaryCardProps = {
  label: string;
  value?: string;
  supportingText?: string;
  icon?: ReactNode;
  state?: SurfaceState;
  loadingLabel?: string;
  emptyMessage?: string;
  errorMessage?: string;
  className?: string;
};

export function SummaryCard({
  label,
  value,
  supportingText,
  icon,
  state = "ready",
  loadingLabel = "Loading",
  emptyMessage = "No value available",
  errorMessage = "Summary unavailable",
  className = "",
}: SummaryCardProps) {
  return (
    <article
      aria-busy={state === "loading" || undefined}
      className={`min-h-32 rounded-xl border border-surface-border bg-surface-card p-4 text-text-primary ${className}`}
    >
      <div className="flex items-start justify-between gap-3">
        <h2 className="text-start text-sm font-medium text-text-secondary">{label}</h2>
        {icon && (
          <span aria-hidden="true" className="text-brand-gold">
            {icon}
          </span>
        )}
      </div>
      {state === "loading" && (
        <div role="status" aria-label={loadingLabel} className="mt-4">
          <span className="sr-only">{loadingLabel}</span>
          <div className="h-7 w-2/3 animate-pulse rounded bg-surface-skeleton" />
        </div>
      )}
      {state === "empty" && <p className="mt-4 text-sm text-text-secondary">{emptyMessage}</p>}
      {state === "error" && (
        <p role="alert" className="mt-4 text-sm text-status-danger">
          {errorMessage}
        </p>
      )}
      {state === "ready" && (
        <>
          <p className="mt-3 text-start text-2xl font-bold tabular-nums">{value}</p>
          {supportingText && (
            <p className="mt-1 text-start text-xs text-text-secondary">{supportingText}</p>
          )}
        </>
      )}
    </article>
  );
}
