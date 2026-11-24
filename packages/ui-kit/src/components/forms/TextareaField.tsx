import { useId, type TextareaHTMLAttributes } from "react";

export type TextareaFieldProps = TextareaHTMLAttributes<HTMLTextAreaElement> & {
  label: string;
  hint?: string;
  error?: string;
};

export function TextareaField({
  id,
  label,
  hint,
  error,
  disabled,
  required,
  rows = 3,
  className = "",
  ...textareaProps
}: TextareaFieldProps) {
  const generatedId = useId();
  const fieldId = id ?? generatedId;
  const hintId = hint ? `${fieldId}-hint` : undefined;
  const errorId = error ? `${fieldId}-error` : undefined;
  const describedBy = [textareaProps["aria-describedby"], hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className="w-full text-start">
      <label htmlFor={fieldId} className="mb-1.5 block text-sm font-medium text-text-primary">
        {label}
        {required && (
          <span aria-hidden="true" className="ms-1 text-status-danger">
            *
          </span>
        )}
      </label>
      <textarea
        {...textareaProps}
        id={fieldId}
        rows={rows}
        required={required}
        disabled={disabled}
        aria-invalid={error ? true : textareaProps["aria-invalid"]}
        aria-describedby={describedBy}
        className={`w-full rounded-lg border bg-surface-card p-3 text-start text-sm text-text-primary placeholder:text-text-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface-canvas disabled:cursor-not-allowed disabled:opacity-60 ${error ? "border-status-danger" : "border-surface-border"} ${className}`}
      />
      {hint && !error && (
        <p id={hintId} className="mt-1.5 text-sm text-text-secondary">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className="mt-1.5 text-sm text-status-danger" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
