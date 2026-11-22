---
name: design-token-governance
description: Maps AlMomiaa Gold (#CFA434) and Obsidian (#1C1B1A) CSS variables, Tailwind CSS theme extensions, bilingual RTL/LTR typography rules, and Storybook CSF3 story templates for packages/ui-kit and apps/design-system-studio.
---

# Design Token Governance Skill — AlMomiaa ERP

Use this skill whenever creating or styling components in `packages/ui-kit`, writing stories in `apps/design-system-studio`, or composing screens in `apps/web-erp` and `apps/rep-pwa`.

---

## 1. Single Source of Truth Brand Tokens (Extracted from AlMomiaa Logo)
All CSS custom properties live in `packages/ui-kit/src/tokens/tokens.css` and are mapped into the shared Tailwind preset (`packages/ui-kit/tailwind.preset.ts`).

```css
:root {
  /* Brand Gold */
  --brand-gold-primary: #CFA434;
  --brand-gold-hover: #B88E24;
  --brand-gold-light: #F5EED8;
  --brand-gold-metallic: #D4AF37;

  /* Surfaces */
  --surface-obsidian: #1C1B1A;
  --surface-charcoal: #262523;
  --surface-canvas: #F8F7F4;
  --surface-card: #FFFFFF;
  --surface-border: #E5E1D8;

  /* Typography */
  --text-primary: #1C1B1A;
  --text-secondary: #5C5852;
  --text-inverse: #F8F7F4;

  /* Semantic ERP Status */
  --status-success-bg: #E8F5EE;
  --status-success-fg: #156F42;
  --status-warning-bg: #F5EED8;
  --status-warning-fg: #8C6812;
  --status-danger-bg: #FDECEC;
  --status-danger-fg: #B42318;
  --status-info-bg: #EAF2FB;
  --status-info-fg: #175CD3;

  /* Font Stacks */
  --font-arabic: 'IBM Plex Sans Arabic', 'Cairo', sans-serif;
  --font-latin: 'Inter', 'Plus Jakarta Sans', sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, monospace;
}
```

---

## 2. Strict Usage Rules
1. **Zero Hardcoded Hex Colors in Apps:** `apps/web-erp` and `apps/rep-pwa` MUST reference Tailwind theme tokens (e.g., `bg-brand-gold`, `hover:bg-brand-gold-hover`, `bg-surface-obsidian`, `bg-surface-canvas`) or `@almomiaa/ui` components—never raw `#XXXXXX` classes.
2. **Contrast & Accessibility (WCAG 2.1 AA):**
   - On `--brand-gold-primary` (`#CFA434`) buttons, use `--surface-obsidian` (`#1C1B1A`) bold text (contrast ratio > 7:1) rather than white text.
   - On `--surface-obsidian` (`#1C1B1A`) sidebars/headers, use `--brand-gold-primary` (`#CFA434`) for active icons/accents and `--text-inverse` (`#F8F7F4`) for body text.
3. **RTL / LTR Logical CSS:**
   - Always use logical properties: `ms-*`, `me-*`, `ps-*`, `pe-*`, `start-*`, `end-*`, `rounded-s-*`, `rounded-e-*`, `border-s-*`, `border-e-*`, `text-start`, `text-end`.
   - Directional icons (chevrons, back arrows, send icons) MUST apply `rtl:rotate-180`.
4. **Typography Switching:**
   - Root `[dir="rtl"]` defaults to `var(--font-arabic)`.
   - Root `[dir="ltr"]`, SKUs, VAT numbers, IBANs, and SAR numerical columns apply `var(--font-latin)` or `var(--font-mono)` with `tabular-nums`.
5. **Mobile PWA Touch Ergonomics (`apps/rep-pwa`):**
   - Interactive controls shared with `rep-pwa` MUST support a touch-friendly size variant (`min-h-[44px] min-w-[44px]`).

---

## 3. Mandatory Storybook CSF3 Story Template (`apps/design-system-studio`)
Every component added to `packages/ui-kit/src/components/<ComponentName>.tsx` MUST ship with `apps/design-system-studio/src/stories/<ComponentName>.stories.tsx` testing both RTL Arabic and LTR English:

```tsx
import type { Meta, StoryObj } from '@storybook/react';
import { ComponentName } from '@almomiaa/ui';

const meta: Meta<typeof ComponentName> = {
  title: 'ERP/<Category>/<ComponentName>',
  component: ComponentName,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<typeof ComponentName>;

export const ArabicRTL: Story = {
  args: { /* realistic AlMomiaa Arabic ERP props */ },
  parameters: { direction: 'rtl' },
};

export const EnglishLTR: Story = {
  args: { /* realistic AlMomiaa English ERP props */ },
  parameters: { direction: 'ltr' },
};
```
