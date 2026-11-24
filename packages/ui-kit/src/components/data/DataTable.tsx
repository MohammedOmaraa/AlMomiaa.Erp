import type { ReactNode } from "react";
import { EmptyState } from "./EmptyState";

export type DataTableColumn<T> = {
  key: string;
  header: string;
  align?: "start" | "center" | "end";
  mono?: boolean;
  numeric?: boolean;
  className?: string;
  render: (row: T) => ReactNode;
};

export type DataTableProps<T> = {
  columns: DataTableColumn<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  loading?: boolean;
  skeletonRowCount?: number;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyAction?: ReactNode;
  onRowClick?: (row: T) => void;
  className?: string;
};

const alignMap = {
  start: "text-start",
  center: "text-center",
  end: "text-end",
};

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading = false,
  skeletonRowCount = 5,
  emptyTitle = "No records found",
  emptyDescription,
  emptyAction,
  onRowClick,
  className = "",
}: DataTableProps<T>) {
  if (!loading && rows.length === 0) {
    return (
      <EmptyState
        title={emptyTitle}
        description={emptyDescription}
        action={emptyAction}
        className={className}
      />
    );
  }

  return (
    <div
      className={`w-full overflow-x-auto rounded-xl border border-surface-border bg-surface-card ${className}`}
    >
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-surface-border bg-surface-canvas/80">
            {columns.map((col) => (
              <th
                key={col.key}
                scope="col"
                className={`whitespace-nowrap px-3.5 py-3 text-xs font-semibold uppercase tracking-wide text-text-secondary ${alignMap[col.align ?? "start"]} ${col.className ?? ""}`}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-surface-border">
          {loading
            ? Array.from({ length: skeletonRowCount }).map((_, rowIdx) => (
                <tr key={`skeleton-${rowIdx}`}>
                  {columns.map((col) => (
                    <td key={`${col.key}-${rowIdx}`} className="px-3.5 py-3">
                      <div className="h-4 w-3/4 animate-pulse rounded bg-surface-skeleton" />
                    </td>
                  ))}
                </tr>
              ))
            : rows.map((row) => (
                <tr
                  key={rowKey(row)}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  className={`transition-colors hover:bg-surface-hover/50 ${onRowClick ? "cursor-pointer" : ""}`}
                >
                  {columns.map((col) => (
                    <td
                      key={col.key}
                      className={`px-3.5 py-3 text-text-primary ${alignMap[col.align ?? "start"]} ${col.mono ? "font-mono text-xs" : ""} ${col.numeric ? "tabular-nums" : ""} ${col.className ?? ""}`}
                    >
                      {col.render(row)}
                    </td>
                  ))}
                </tr>
              ))}
        </tbody>
      </table>
    </div>
  );
}
