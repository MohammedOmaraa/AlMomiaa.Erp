import type { ReactNode } from "react";

export interface StepProgressItem {
  id: string;
  label: string;
  description?: string;
}

export interface StepProgressProps {
  steps: readonly StepProgressItem[];
  activeStepIndex: number;
  onStepSelect?: (index: number) => void;
  className?: string;
  ariaLabel?: string;
}

export function StepProgress({
  steps,
  activeStepIndex,
  onStepSelect,
  className = "",
  ariaLabel = "Progress steps"
}: StepProgressProps): ReactNode {
  return (
    <nav aria-label={ariaLabel} className={`w-full ${className}`}>
      <ol className="flex items-center justify-between gap-2">
        {steps.map((step, index) => {
          const isCompleted = index < activeStepIndex;
          const isCurrent = index === activeStepIndex;
          const isClickable = Boolean(onStepSelect) && isCompleted;

          const circleClass = isCompleted
            ? "bg-brand-gold text-surface-obsidian border-brand-gold"
            : isCurrent
              ? "bg-surface-obsidian text-brand-gold border-brand-gold ring-2 ring-brand-gold/30"
              : "bg-surface-card text-text-secondary border-surface-border";

          const labelClass = isCurrent
            ? "font-semibold text-text-primary"
            : isCompleted
              ? "font-medium text-text-primary"
              : "text-text-secondary";

          return (
            <li key={step.id} className="flex flex-1 items-center gap-2">
              <button
                type="button"
                disabled={!isClickable}
                onClick={isClickable ? () => onStepSelect?.(index) : undefined}
                aria-current={isCurrent ? "step" : undefined}
                className={`group flex min-w-0 flex-1 items-center gap-2.5 rounded-lg p-1.5 text-start transition-colors ${
                  isClickable
                    ? "cursor-pointer hover:bg-surface-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-gold"
                    : "cursor-default"
                }`}
              >
                <span
                  className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full border text-xs font-bold tabular-nums transition-colors ${circleClass}`}
                >
                  {isCompleted ? (
                    <svg
                      viewBox="0 0 20 20"
                      fill="currentColor"
                      className="h-4 w-4"
                      aria-hidden="true"
                    >
                      <path
                        fillRule="evenodd"
                        d="M16.704 5.29a1 1 0 0 1 .006 1.414l-7.25 7.3a1 1 0 0 1-1.42 0l-3.25-3.27a1 1 0 1 1 1.42-1.408l2.54 2.556 6.54-6.586a1 1 0 0 1 1.414-.006Z"
                        clipRule="evenodd"
                      />
                    </svg>
                  ) : (
                    index + 1
                  )}
                </span>
                <span className="min-w-0 flex-1">
                  <span className={`block truncate text-xs sm:text-sm ${labelClass}`}>
                    {step.label}
                  </span>
                  {step.description ? (
                    <span className="hidden truncate text-xs text-text-secondary sm:block">
                      {step.description}
                    </span>
                  ) : null}
                </span>
              </button>
              {index < steps.length - 1 ? (
                <span
                  aria-hidden="true"
                  className={`hidden h-0.5 w-6 shrink-0 rounded-full sm:block ${
                    isCompleted ? "bg-brand-gold" : "bg-surface-border"
                  }`}
                />
              ) : null}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
