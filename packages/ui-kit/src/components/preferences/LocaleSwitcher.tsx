import type { SelectHTMLAttributes } from "react";

export type Locale = "ar-SA" | "en-US";

export type LocaleSwitcherProps = Omit<SelectHTMLAttributes<HTMLSelectElement>, "value" | "onChange"> & {
  value: Locale;
  onChange: (locale: Locale) => void;
  label?: string;
};

export function LocaleSwitcher({
  value,
  onChange,
  label = "Language",
  className = "",
  ...props
}: LocaleSwitcherProps) {
  return (
    <label
      className={`inline-flex min-h-11 items-center gap-2 rounded-lg border border-surface-border bg-surface-card px-2 text-sm text-text-primary ${className}`}
    >
      <span className="sr-only">{label}</span>
      <select
        {...props}
        aria-label={label}
        value={value}
        onChange={(event) => onChange(event.currentTarget.value as Locale)}
        className="min-h-10 max-w-32 bg-transparent text-start text-text-primary outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
      >
        <option value="ar-SA" className="bg-surface-card text-text-primary">
          العربية
        </option>
        <option value="en-US" className="bg-surface-card text-text-primary">
          English
        </option>
      </select>
    </label>
  );
}
