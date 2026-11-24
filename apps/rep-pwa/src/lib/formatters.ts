import type { Locale } from "@almomiaa/ui";

export function formatSar(value: number | null | undefined, locale: Locale): string {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return "—";
  }
  const formatted = new Intl.NumberFormat(locale, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
  return locale === "ar-SA" ? `${formatted} ر.س` : `SAR ${formatted}`;
}

export function formatNumber(value: number | null | undefined, locale: Locale): string {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return "—";
  }
  return new Intl.NumberFormat(locale, {
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatDateTime(iso: string | null | undefined, locale: Locale): string {
  if (!iso) return "—";
  const dt = new Date(iso);
  if (Number.isNaN(dt.getTime())) return iso;
  return new Intl.DateTimeFormat(locale, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(dt);
}
