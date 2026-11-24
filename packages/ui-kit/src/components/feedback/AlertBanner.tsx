import type { ReactNode } from "react";

export type AlertTone = "info" | "success" | "warning" | "danger";

export type AlertBannerProps = {
  tone?: AlertTone;
  title?: string;
  message: ReactNode;
  details?: string[];
  onDismiss?: () => void;
  dismissLabel?: string;
  className?: string;
};

const toneStyles: Record<AlertTone, string> = {
  info: "border-status-info/30 bg-status-info-bg text-status-info",
  success: "border-status-success/30 bg-status-success-bg text-status-success",
  warning: "border-status-warning/30 bg-status-warning-bg text-status-warning",
  danger: "border-status-danger/30 bg-status-danger-bg text-status-danger",
};

export function AlertBanner({
  tone = "info",
  title,
  message,
  details,
  onDismiss,
  dismissLabel = "Dismiss",
  className = "",
}: AlertBannerProps) {
  return (
    <div
      role={tone === "danger" ? "alert" : "status"}
      className={`flex items-start justify-between gap-3 rounded-xl border px-4 py-3 text-start text-sm ${toneStyles[tone]} ${className}`}
    >
      <div className="min-w-0 flex-1 space-y-1">
        {title && <p className="font-semibold">{title}</p>}
        <div className="text-sm">{message}</div>
        {details && details.length > 0 && (
          <ul className="mt-1 list-disc space-y-0.5 ps-5 text-xs">
            {details.map((item, idx) => (
              <li key={`${idx}-${item}`}>{item}</li>
            ))}
          </ul>
        )}
      </div>

      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          aria-label={dismissLabel}
          className="inline-flex size-8 shrink-0 items-center justify-center rounded-lg border border-current/20 text-xs font-bold hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          ✕
        </button>
      )}
    </div>
  );
}
