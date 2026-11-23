import type { ButtonHTMLAttributes, ReactNode } from "react";

export type IconButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  label: string;
  children: ReactNode;
  variant?: "solid" | "subtle";
};

export function IconButton({
  label,
  children,
  variant = "subtle",
  className = "",
  ...props
}: IconButtonProps) {
  return (
    <button
      {...props}
      type={props.type ?? "button"}
      aria-label={label}
      className={`inline-flex size-11 shrink-0 items-center justify-center rounded-lg text-lg transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface-canvas disabled:cursor-not-allowed disabled:opacity-60 ${
        variant === "solid"
          ? "bg-brand-gold text-surface-obsidian hover:bg-brand-gold-hover"
          : "bg-transparent text-text-primary hover:bg-surface-hover"
      } ${className}`}
    >
      {children}
    </button>
  );
}
