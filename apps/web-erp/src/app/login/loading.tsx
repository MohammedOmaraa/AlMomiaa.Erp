export default function LoginLoading() {
  return (
    <div className="flex min-h-screen w-full items-center justify-center bg-surface-canvas p-4 text-text-primary">
      <div
        role="status"
        className="w-full max-w-md space-y-4 rounded-xl border border-surface-border bg-surface-card p-6 sm:p-8"
      >
        <div className="h-28 animate-pulse rounded-lg bg-surface-skeleton" />
        <div className="h-11 animate-pulse rounded-lg bg-surface-skeleton" />
        <div className="h-11 animate-pulse rounded-lg bg-surface-skeleton" />
        <div className="h-11 animate-pulse rounded-lg bg-surface-skeleton" />
      </div>
    </div>
  );
}
