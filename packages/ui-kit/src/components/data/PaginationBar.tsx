import { Button } from "../actions/Button";

export type PaginationBarProps = {
  page: number;
  pageSize: number;
  totalCount: number;
  hasNext?: boolean;
  onPageChange: (nextPage: number) => void;
  onPageSizeChange?: (nextPageSize: number) => void;
  pageSizeOptions?: number[];
  labels?: {
    previous?: string;
    next?: string;
    showing?: string;
    of?: string;
    page?: string;
    perPage?: string;
  };
  className?: string;
};

export function PaginationBar({
  page,
  pageSize,
  totalCount,
  hasNext,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50, 100],
  labels,
  className = "",
}: PaginationBarProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / Math.max(1, pageSize)));
  const canPrev = page > 1;
  const canNext = hasNext ?? page < totalPages;
  const startItem = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const endItem = Math.min(totalCount, page * pageSize);

  return (
    <div
      className={`flex flex-wrap items-center justify-between gap-3 border-t border-surface-border pt-3 text-sm text-text-secondary ${className}`}
    >
      <div className="flex items-center gap-2 tabular-nums">
        <span>
          {labels?.showing ?? "Showing"} <strong className="text-text-primary">{startItem}-{endItem}</strong>{" "}
          {labels?.of ?? "of"} <strong className="text-text-primary">{totalCount}</strong>
        </span>
        {onPageSizeChange && (
          <label className="ms-2 inline-flex items-center gap-1.5 text-xs">
            <span>{labels?.perPage ?? "Rows"}</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="rounded-md border border-surface-border bg-surface-card px-2 py-1 text-xs font-medium text-text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
            >
              {pageSizeOptions.map((opt) => (
                <option key={opt} value={opt}>
                  {opt}
                </option>
              ))}
            </select>
          </label>
        )}
      </div>

      <div className="flex items-center gap-2">
        <span className="text-xs tabular-nums">
          {labels?.page ?? "Page"} {page} / {totalPages}
        </span>
        <Button
          variant="secondary"
          size="compact"
          disabled={!canPrev}
          onClick={() => onPageChange(page - 1)}
        >
          {labels?.previous ?? "Previous"}
        </Button>
        <Button
          variant="secondary"
          size="compact"
          disabled={!canNext}
          onClick={() => onPageChange(page + 1)}
        >
          {labels?.next ?? "Next"}
        </Button>
      </div>
    </div>
  );
}
