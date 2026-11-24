import type { ReactNode } from "react";

export type BadgeTone = "neutral" | "gold" | "success" | "warning" | "danger" | "info";

export type BadgeProps = {
  tone?: BadgeTone;
  size?: "sm" | "md";
  mono?: boolean;
  className?: string;
  children: ReactNode;
};

const toneStyles: Record<BadgeTone, string> = {
  neutral: "border-surface-border bg-surface-canvas text-text-secondary",
  gold: "border-brand-gold bg-brand-gold-light text-surface-obsidian",
  success: "border-status-success/20 bg-status-success-bg text-status-success",
  warning: "border-status-warning/20 bg-status-warning-bg text-status-warning",
  danger: "border-status-danger/20 bg-status-danger-bg text-status-danger",
  info: "border-status-info/20 bg-status-info-bg text-status-info",
};

const sizeStyles = {
  sm: "px-2 py-0.5 text-xs",
  md: "px-2.5 py-1 text-xs",
};

export function Badge({
  tone = "neutral",
  size = "sm",
  mono = false,
  className = "",
  children,
}: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full border font-semibold leading-tight ${toneStyles[tone]} ${sizeStyles[size]} ${mono ? "font-mono" : ""} ${className}`}
    >
      {children}
    </span>
  );
}
