import { IconButton } from "../actions/IconButton";

export type Theme = "light" | "dark";
export type ThemeToggleProps = {
  value: Theme;
  onChange: (theme: Theme) => void;
  labels?: { light: string; dark: string };
};

export function ThemeToggle({ value, onChange, labels = { light: "Switch to dark theme", dark: "Switch to light theme" } }: ThemeToggleProps) {
  const next = value === "light" ? "dark" : "light";
  return (
    <IconButton label={value === "light" ? labels.light : labels.dark} aria-pressed={value === "dark"} onClick={() => onChange(next)}>
      <span aria-hidden="true">{value === "light" ? "\u263e" : "\u2600"}</span>
    </IconButton>
  );
}
