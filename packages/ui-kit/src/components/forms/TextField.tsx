import { useId, type InputHTMLAttributes } from "react";

export type TextFieldProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  label: string;
  hint?: string;
  error?: string;
};

export function TextField({ id, label, hint, error, disabled, required, className = "", ...inputProps }: TextFieldProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const hintId = hint ? inputId + "-hint" : undefined;
  const errorId = error ? inputId + "-error" : undefined;
  const describedBy = [inputProps["aria-describedby"], hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className="w-full text-start">
      <label htmlFor={inputId} className="mb-1.5 block text-sm font-medium text-text-primary">
        {label}
        {required && <span aria-hidden="true" className="ms-1 text-status-danger">*</span>}
      </label>
      <input
        {...inputProps}
        id={inputId}
        type="text"
        required={required}
        disabled={disabled}
        aria-invalid={error ? true : inputProps["aria-invalid"]}
        aria-describedby={describedBy}
        className={"min-h-11 w-full rounded-lg border bg-surface-card px-3 text-start text-sm text-text-primary placeholder:text-text-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface-canvas disabled:cursor-not-allowed disabled:opacity-60 " + (error ? "border-status-danger " : "border-surface-border ") + className}
      />
      {hint && !error && <p id={hintId} className="mt-1.5 text-sm text-text-secondary">{hint}</p>}
      {error && <p id={errorId} className="mt-1.5 text-sm text-status-danger" role="alert">{error}</p>}
    </div>
  );
}
