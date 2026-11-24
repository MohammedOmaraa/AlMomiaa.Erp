import { useState } from "react";
import { Button } from "../actions/Button";
import { Badge, type BadgeTone } from "./Badge";

export type JsonResponseViewerProps = {
  method: string;
  endpoint: string;
  status?: number;
  durationMs?: number;
  timestamp?: string;
  payload: unknown;
  requestPayload?: unknown;
  copyLabel?: string;
  copiedLabel?: string;
  requestLabel?: string;
  responseLabel?: string;
  className?: string;
};

function getStatusTone(status?: number): BadgeTone {
  if (status === undefined) return "neutral";
  if (status >= 200 && status < 300) return "success";
  if (status >= 400 && status < 500) return "warning";
  if (status >= 500) return "danger";
  return "info";
}

function formatJson(value: unknown): string {
  if (value === undefined) return "null";
  if (typeof value === "string") return value;
  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
}

export function JsonResponseViewer({
  method,
  endpoint,
  status,
  durationMs,
  timestamp,
  payload,
  requestPayload,
  copyLabel = "Copy JSON",
  copiedLabel = "Copied",
  requestLabel = "Request Body",
  responseLabel = "Response Body",
  className = "",
}: JsonResponseViewerProps) {
  const [copied, setCopied] = useState(false);
  const [showRequest, setShowRequest] = useState(false);

  const activePayload = showRequest && requestPayload !== undefined ? requestPayload : payload;
  const formatted = formatJson(activePayload);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(formatted);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      // Clipboard may be unavailable in restricted contexts
    }
  };

  return (
    <div className={`rounded-xl border border-surface-border bg-surface-card p-3.5 ${className}`}>
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-surface-border pb-2.5">
        <div className="flex min-w-0 flex-wrap items-center gap-2" dir="ltr">
          <Badge tone="gold" mono>
            {method.toUpperCase()}
          </Badge>
          {status !== undefined && (
            <Badge tone={getStatusTone(status)} mono>
              {status}
            </Badge>
          )}
          <code className="truncate font-mono text-xs font-semibold text-text-primary">{endpoint}</code>
          {durationMs !== undefined && (
            <span className="font-mono text-xs text-text-secondary tabular-nums">{durationMs}ms</span>
          )}
        </div>

        <div className="flex items-center gap-1.5">
          {requestPayload !== undefined && (
            <Button
              variant="ghost"
              size="compact"
              onClick={() => setShowRequest((prev) => !prev)}
            >
              {showRequest ? responseLabel : requestLabel}
            </Button>
          )}
          <Button variant="secondary" size="compact" onClick={handleCopy}>
            {copied ? copiedLabel : copyLabel}
          </Button>
        </div>
      </div>

      {timestamp && (
        <p className="mt-2 font-mono text-xs text-text-secondary" dir="ltr">
          {timestamp}
        </p>
      )}

      <pre
        dir="ltr"
        className="mt-2 max-h-80 overflow-auto rounded-lg border border-surface-border bg-surface-obsidian p-3 text-start font-mono text-xs leading-relaxed text-text-inverse"
      >
        {formatted}
      </pre>
    </div>
  );
}
