import type { Config } from "tailwindcss";

const preset: Partial<Config> = {
  theme: {
    extend: {
      colors: {
        brand: {
          gold: "var(--brand-gold-primary)",
          "gold-hover": "var(--brand-gold-hover)",
          "gold-light": "var(--brand-gold-light)",
          "gold-metallic": "var(--brand-gold-metallic)",
        },
        surface: {
          obsidian: "var(--surface-obsidian)",
          charcoal: "var(--surface-charcoal)",
          canvas: "var(--surface-canvas)",
          card: "var(--surface-card)",
          border: "var(--surface-border)",
          hover: "var(--surface-hover)",
          skeleton: "var(--surface-skeleton)",
        },
        text: {
          primary: "var(--text-primary)",
          secondary: "var(--text-secondary)",
          inverse: "var(--text-inverse)",
          "inverse-muted": "var(--text-inverse-muted)",
        },
        focus: {
          ring: "var(--focus-ring)",
        },
        status: {
          success: "var(--status-success-fg)",
          "success-bg": "var(--status-success-bg)",
          warning: "var(--status-warning-fg)",
          "warning-bg": "var(--status-warning-bg)",
          danger: "var(--status-danger-fg)",
          "danger-action": "var(--status-danger-action-bg)",
          "danger-bg": "var(--status-danger-bg)",
          info: "var(--status-info-fg)",
          "info-bg": "var(--status-info-bg)",
        },
      },
      fontFamily: {
        arabic: ["var(--font-arabic)"],
        latin: ["var(--font-latin)"],
        mono: ["var(--font-mono)"],
      },
      boxShadow: {
        focus: "0 0 0 3px var(--focus-ring)",
      },
    },
  },
};

export default preset;
