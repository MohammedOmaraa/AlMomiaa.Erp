import { useId, useState, type InputHTMLAttributes } from "react";

export type PasswordFieldProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  label: string;
  hint?: string;
  error?: string;
  showPasswordLabel?: string;
  hidePasswordLabel?: string;
};

export function PasswordField({
  id, label, hint, error, disabled, required, className = "",
  showPasswordLabel = "Show password", hidePasswordLabel = "Hide password", ...inputProps
}: PasswordFieldProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const hintId = hint ? inputId + "-hint" : undefined;
  const errorId = error ? inputId + "-error" : undefined;
  const describedBy = [inputProps["aria-describedby"], hintId, errorId].filter(Boolean).join(" ") || undefined;
  const [visible, setVisible] = useState(false);
  const toggleLabel = visible ? hidePasswordLabel : showPasswordLabel;

  return (
    <div className="w-full text-start">
      <label htmlFor={inputId} className="mb-1.5 block text-sm font-medium text-text-primary">
        {label}
        {required && <span aria-hidden="true" className="ms-1 text-status-danger">*</span>}
      </label>
      <div className="relative">
        <input
          {...inputProps}
          id={inputId}
          type={visible ? "text" : "password"}
          required={required}
          disabled={disabled}
          aria-invalid={error ? true : inputProps["aria-invalid"]}
          aria-describedby={describedBy}
          className={"min-h-11 w-full rounded-lg border bg-surface-card py-2 ps-3 pe-14 text-start text-sm text-text-primary placeholder:text-text-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface-canvas disabled:cursor-not-allowed disabled:opacity-60 " + (error ? "border-status-danger " : "border-surface-border ") + className}
        />
        <button
          type="button"
          aria-label={toggleLabel}
          aria-pressed={visible}
          aria-controls={inputId}
          disabled={disabled}
          onClick={() => setVisible((current) => !current)}
          className="absolute inset-y-0 end-0 inline-flex min-h-11 min-w-11 items-center justify-center rounded-e-lg px-3 text-sm font-medium text-text-secondary hover:bg-surface-hover focus-visible:z-10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-focus-ring disabled:cursor-not-allowed disabled:opacity-60"
        >
          <span aria-hidden="true">{visible ? "\u25c9" : "\u25ce"}</span>
        </button>
      </div>
      {hint && !error && <p id={hintId} className="mt-1.5 text-sm text-text-secondary">{hint}</p>}
      {error && <p id={errorId} className="mt-1.5 text-sm text-status-danger" role="alert">{error}</p>}
    </div>
  );
}
