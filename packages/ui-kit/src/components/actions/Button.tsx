import type { ButtonHTMLAttributes, ReactNode } from "react";

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "compact" | "comfortable";
  loading?: boolean;
  children: ReactNode;
};

const variants = {
  primary: "bg-brand-gold text-surface-obsidian hover:bg-brand-gold-hover",
  secondary: "border border-surface-border bg-surface-card text-text-primary hover:bg-surface-hover",
  ghost: "bg-transparent text-text-primary hover:bg-surface-hover",
  danger: "bg-status-danger-action text-text-inverse hover:opacity-90",
};

export function Button({
  variant = "primary",
  size = "comfortable",
  loading = false,
  disabled,
  className = "",
  children,
  ...props
}: ButtonProps) {
  const dimension = size === "comfortable" ? "min-h-11 px-4" : "min-h-11 px-3";
  return (
    <button
      {...props}
      type={props.type ?? "button"}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={`inline-flex min-w-11 items-center justify-center gap-2 rounded-lg text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface-canvas disabled:cursor-not-allowed disabled:opacity-60 ${dimension} ${variants[variant]} ${className}`}
    >
      {loading && (
        <span
          aria-hidden="true"
          className="size-4 animate-spin rounded-full border-2 border-current border-e-transparent"
        />
      )}
      {children}
    </button>
  );
}
