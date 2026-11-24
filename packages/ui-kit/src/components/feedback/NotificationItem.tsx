import type { ReactNode } from "react";
import { Badge, type BadgeTone } from "../data/Badge";

export type NotificationSeverity = "info" | "success" | "warning" | "danger";

export interface NotificationItemProps {
  title: string;
  message: string;
  severity?: NotificationSeverity;
  timestamp?: string;
  isRead?: boolean;
  onClick?: () => void;
  onMarkRead?: () => void;
  markReadLabel?: string;
  unreadLabel?: string;
  className?: string;
}

const severityToTone: Record<NotificationSeverity, BadgeTone> = {
  info: "info",
  success: "success",
  warning: "warning",
  danger: "danger",
};

export function NotificationItem({
  title,
  message,
  severity = "info",
  timestamp,
  isRead = false,
  onClick,
  onMarkRead,
  markReadLabel = "Mark read",
  unreadLabel = "Unread",
  className = "",
}: NotificationItemProps): ReactNode {
  return (
    <div
      className={`flex items-start justify-between gap-3 rounded-xl border p-3 text-start transition-colors ${
        isRead
          ? "border-surface-border bg-surface-canvas/60 text-text-secondary"
          : "border-brand-gold/50 bg-surface-card text-text-primary shadow-sm"
      } ${onClick ? "cursor-pointer hover:bg-surface-hover" : ""} ${className}`}
      onClick={onClick}
    >
      <div className="flex min-w-0 flex-1 items-start gap-2.5">
        <span
          aria-hidden="true"
          className={`mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full ${
            isRead ? "bg-surface-border" : "bg-brand-gold"
          }`}
        />
        <div className="min-w-0 flex-1 space-y-1">
          <div className="flex flex-wrap items-center gap-1.5">
            <span className={`text-sm ${isRead ? "font-medium" : "font-bold text-text-primary"}`}>
              {title}
            </span>
            {!isRead ? (
              <Badge tone={severityToTone[severity]} size="sm">
                {unreadLabel}
              </Badge>
            ) : null}
          </div>
          <p className="text-xs leading-relaxed text-text-secondary">{message}</p>
          {timestamp ? (
            <p className="text-[11px] tabular-nums text-text-secondary">{timestamp}</p>
          ) : null}
        </div>
      </div>

      {!isRead && onMarkRead ? (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onMarkRead();
          }}
          className="shrink-0 rounded-lg border border-surface-border bg-surface-canvas px-2.5 py-1 text-xs font-medium text-text-primary transition-colors hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-gold"
        >
          {markReadLabel}
        </button>
      ) : null}
    </div>
  );
}
