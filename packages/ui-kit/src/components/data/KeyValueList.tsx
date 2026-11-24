import type { ReactNode } from "react";

export type KeyValueItem = {
  id: string;
  label: string;
  value: ReactNode;
  mono?: boolean;
  dir?: "ltr" | "rtl";
};

export type KeyValueListProps = {
  items: KeyValueItem[];
  columns?: 1 | 2 | 3;
  className?: string;
};

const gridCols: Record<1 | 2 | 3, string> = {
  1: "grid-cols-1",
  2: "grid-cols-1 sm:grid-cols-2",
  3: "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3",
};

export function KeyValueList({ items, columns = 2, className = "" }: KeyValueListProps) {
  return (
    <dl className={`grid gap-3 ${gridCols[columns]} ${className}`}>
      {items.map((item) => (
        <div
          key={item.id}
          className="rounded-lg border border-surface-border bg-surface-canvas/70 px-3.5 py-2.5 text-start"
        >
          <dt className="text-xs font-medium text-text-secondary">{item.label}</dt>
          <dd
            dir={item.dir}
            className={`mt-1 break-words text-sm font-semibold text-text-primary ${item.mono ? "font-mono tabular-nums" : ""}`}
          >
            {item.value}
          </dd>
        </div>
      ))}
    </dl>
  );
}
