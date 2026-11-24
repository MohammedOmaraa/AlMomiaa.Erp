import { useId, type SelectHTMLAttributes } from "react";

export type SelectOption = {
  value: string;
  label: string;
  disabled?: boolean;
};

export type SelectFieldProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  options: SelectOption[];
  placeholderOption?: string;
  hint?: string;
  error?: string;
};

export function SelectField({
  id,
  label,
  options,
  placeholderOption,
  hint,
  error,
  disabled,
  required,
  className = "",
  ...selectProps
}: SelectFieldProps) {
  const generatedId = useId();
  const selectId = id ?? generatedId;
  const hintId = hint ? `${selectId}-hint` : undefined;
  const errorId = error ? `${selectId}-error` : undefined;
  const describedBy = [selectProps["aria-describedby"], hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className="w-full text-start">
      <label htmlFor={selectId} className="mb-1.5 block text-sm font-medium text-text-primary">
        {label}
        {required && (
          <span aria-hidden="true" className="ms-1 text-status-danger">
            *
          </span>
        )}
      </label>
      <select
        {...selectProps}
        id={selectId}
        required={required}
        disabled={disabled}
        aria-invalid={error ? true : selectProps["aria-invalid"]}
        aria-describedby={describedBy}
        className={`min-h-11 w-full rounded-lg border bg-surface-card px-3 text-start text-sm text-text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface-canvas disabled:cursor-not-allowed disabled:opacity-60 ${error ? "border-status-danger" : "border-surface-border"} ${className}`}
      >
        {placeholderOption && <option value="">{placeholderOption}</option>}
        {options.map((opt) => (
          <option key={opt.value} value={opt.value} disabled={opt.disabled}>
            {opt.label}
          </option>
        ))}
      </select>
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
