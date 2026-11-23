# PROMPT-0002: Build Responsive Bilingual AlMomiaa ERP UI Shell

- **Status:** `COMPLETED (Archived as 0002-responsive-bilingual-erp-ui-shell.md)`
- **Prompt ID:** `PROMPT-0002`
- **Scope:** Root pnpm/Turborepo workspace wiring, `packages/ui-kit`, `apps/design-system-studio`, and `apps/web-erp`
- **Reference baseline:** Architecture v3.1 (`docs/architecture/00-ADR-Architecture-Decisions.md`)
- **User-supplied brand asset:** `C:\Users\Mohamed Omara\AppData\Roaming\Code\agentSessionData\d7a2c0c0-a7f9-4155-adb5-299d51a77582\attachments\d6be2f1a-40e9-4fd1-9660-bf707a95cf28\logo.jpg`

## 1. Purpose of Prompt

Turn the existing `apps/web-erp` scaffold into a polished, responsive AlMomiaa ERP application shell with bilingual Arabic/English support, true RTL/LTR layout, and persistent light/dark theme selection. Establish the shared `@almomiaa/ui` component library and Storybook 8 studio so the web app uses reusable, individually maintained components.

Use the attached logo unchanged. The Arabic brand label in the interface must be `المومياء`; the English label must be `AlMomiaa`.

This prompt is for design-system and application-shell work only. Do not implement real ERP workflows, API calls, authentication, business data, or backend features.

## 2. Scope and Target Execution Agents

1. `@Design System UI` — create the minimal pnpm/Turborepo workspace wiring required for the selected apps, implement `packages/ui-kit` tokens and reusable components, and configure Storybook 8 in `apps/design-system-studio`.
2. `@Frontend Engineer` — integrate `@almomiaa/ui` into `apps/web-erp`, implement the app shell, language/theme preferences, and the static dashboard sample.
3. `@QA & Security Auditor` — verify accessibility, responsive behavior, localization, token usage, workspace boundaries, Storybook coverage, and build/typecheck results.
4. `@Orchestrator` — coordinate the sequence, resolve integration issues, and archive/log the prompt only after QA passes.

Do not invoke `@Backend .NET`; no backend work is in scope.

## 3. Target Files to Create or Modify

Inspect the existing scaffold first. Preserve compatible work and modify only the following paths plus directly necessary package lock/config files:

### Workspace root

- `package.json` — private workspace manifest, pinned pnpm package-manager version, and `build`, `lint`, `typecheck`, and `test` scripts for JavaScript packages only.
- `pnpm-workspace.yaml` — include `apps/*` and `packages/*`.
- `turbo.json` — define the workspace task graph for build, lint, typecheck, and Storybook build without attempting to run .NET projects through Turbo.
- `pnpm-lock.yaml` — generate/update using pnpm.

Migrate `apps/web-erp` to the pnpm workspace and `workspace:*` dependency for `@almomiaa/ui`. Remove its app-local `package-lock.json` only after the root lockfile is generated and the app builds successfully with pnpm. Do not add unrelated workspace packages or scripts.

### `packages/ui-kit` (`@almomiaa/ui`)

Create a strict, pure React/Tailwind component package with:

- `package.json`, `tsconfig.json`, `tailwind.preset.ts`, and `src/index.ts`.
- Package exports for the public component entrypoint, `tailwind.preset.ts`, and `src/tokens/tokens.css`.
- `src/tokens/tokens.css` as the single source of brand and semantic theme tokens.
- The attached logo copied unchanged to `src/assets/almomiaa-logo.jpg`. The supplied attachment is read-only; never edit or overwrite the source file.
- Each reusable component in its own file, with typed props and no business/API dependencies:
  - `src/components/brand/BrandLogo.tsx`
  - `src/components/actions/Button.tsx`
  - `src/components/actions/IconButton.tsx`
  - `src/components/navigation/SidebarNavItem.tsx`
  - `src/components/navigation/Sidebar.tsx`
  - `src/components/layout/AppHeader.tsx`
  - `src/components/layout/ResponsiveAppShell.tsx`
  - `src/components/preferences/LocaleSwitcher.tsx`
  - `src/components/preferences/ThemeToggle.tsx`
  - `src/components/surfaces/SurfaceCard.tsx`
  - `src/components/surfaces/SummaryCard.tsx`

Keep the library prop-driven and portable across Next.js and Vite. It must not import Axios, Next.js router APIs, TanStack Query, business DTOs, or app-specific locale/theme providers. Components receive navigation items, current locale/theme, and callbacks through typed props. Verify the bundled logo asset can be consumed by both the Next.js app and Storybook without an external URL.

### `apps/design-system-studio`

Create a standalone Storybook 8 + Vite studio with:

- `package.json`
- `.storybook/main.ts`
- `.storybook/preview.ts`
- `storybook` and `build-storybook` scripts in its package manifest.
- `src/stories/<ComponentName>.stories.tsx` for every public component exported by `@almomiaa/ui`
- `src/stories/Introduction.mdx` documenting the brand, token usage, directions, themes, and responsive preview controls

Use CSF3 stories with `autodocs`. Every component story must demonstrate Arabic RTL and English LTR, plus loading/empty/error states where the component meaningfully owns those states; do not invent business behavior for purely presentational controls. Provide Storybook controls/decorators for light/dark theme and responsive viewports. `ResponsiveAppShell` stories must cover expanded desktop, compact medium-screen, and open/closed mobile drawer states. Configure the a11y addon and ensure stories render without a Next.js runtime.

### `apps/web-erp`

Update the existing app scaffold with:

- `package.json` — use workspace `@almomiaa/ui` and `next-themes`; preserve strict TypeScript.
- `next.config.ts` — configure workspace package transpilation as required.
- `tailwind.config.ts` — consume the shared UI preset.
- `src/app/layout.tsx`, `src/app/globals.css`, and `src/app/page.tsx`.
- `src/providers/ThemeProvider.tsx` and `src/providers/LocaleProvider.tsx`.
- Separate Arabic and English message modules under `src/locales/`.
- Only add app-specific components/providers when needed; keep every reusable visual component in `packages/ui-kit`.

## 4. UI Behavior and Content Requirements

### Responsive application shell

- Large screens (`>=1280px`): expanded persistent aside navigation and full workspace header.
- Medium screens (`768–1279px`): compact icon rail with an accessible expand/collapse control.
- Small screens (`<768px`): compact header and off-canvas aside drawer with scrim; support Escape, focus management, close-on-navigation, and restored focus.
- Prevent horizontal overflow at 375px, 768px, and 1280px widths. Use logical CSS/Tailwind properties only.
- Use the starter navigation sections with these labels: Dashboard / لوحة التحكم, Sales / المبيعات, Customers / العملاء, Inventory / المخزون, Purchasing / المشتريات, Finance / المالية, Reports / التقارير, Settings / الإعدادات. Dashboard is the only active sample page; other items must be clearly non-functional placeholders, not dead `href="#"` links.

### Arabic/English and direction

- Initial locale is `ar-SA`, direction `rtl`; English locale is `en-US`, direction `ltr`.
- Provide a visible language switcher that updates all shell/sample labels, the document `lang` and `dir`, and persists the selection across reloads.
- Persist locale in a server-readable `almomiaa-locale` cookie. Initialize the locale provider from the cookie so root `html` attributes and the initial render agree without hydration mismatch.
- Render the Arabic brand text exactly as `المومياء` and the English text as `AlMomiaa`.
- Use `--font-arabic` for RTL and `--font-latin` for LTR/Latin identifiers. Do not fetch fonts at runtime from external services.

### Theme

- Provide light and dark modes and a visible theme toggle.
- Initial mode follows the system preference; persist the user's explicit selection.
- Use `next-themes` with the `data-theme` attribute, system detection, and the `almomiaa-theme` storage key.
- Avoid a flash of the wrong theme and avoid server/client hydration mismatch.
- Theme state must be app-owned; shared components receive theme state/callbacks rather than accessing Next.js APIs or storage directly.

### Static sample dashboard

- Render a concise Arabic-first welcome/dashboard preview inside the shell, with translated English content when selected.
- Use only clearly labeled illustrative values or neutral placeholders; no API, database, authentication, real customer/product data, or business actions.

## 5. Design Tokens and Visual Governance

Follow `.github/skills/design-token-governance/SKILL.md` as the single source of truth:

- Define all CSS color custom properties in `packages/ui-kit/src/tokens/tokens.css` and export them from `@almomiaa/ui`.
- Preserve canonical gold and obsidian tokens (`#CFA434`, `#B88E24`, `#F5EED8`, `#1C1B1A`, `#262523`, `#F8F7F4`) and semantic status tokens. Add light/dark semantic surface, text, border, and focus tokens without inventing off-brand colors.
- Map shared tokens into `tailwind.preset.ts`. Apps must consume tokens; no hardcoded hex values or one-off color definitions in `apps/web-erp`.
- Meet WCAG 2.1 AA contrast. On gold primary controls use obsidian text; on obsidian surfaces use inverse text and gold accents.
- Use logical classes/properties (`ms-*`, `me-*`, `ps-*`, `pe-*`, `start-*`, `end-*`, `text-start`, `text-end`) and direction-aware icon rotation. Do not use `ml-*`, `mr-*`, `pl-*`, `pr-*`, `left-*`, or `right-*` for directional layout.
- Use the supplied JPEG unchanged on an obsidian/charcoal surface with preserved aspect ratio and `object-fit: contain`. Do not crop, recolor, or remove its background. When the aside is compact, show the Arabic/English text fallback instead of a cropped logo.

## 6. Storybook Component Mapping

| Component | Responsibility | Required story coverage |
|---|---|---|
| `BrandLogo` | Supplied mark and localized brand text | Full and compact; Arabic RTL and English LTR; light/dark surfaces |
| `Button` / `IconButton` | Accessible shared actions | Variants, keyboard focus, disabled state, both directions, both themes |
| `SidebarNavItem` | One accessible navigation entry | Active, inactive, placeholder, collapsed, RTL/LTR |
| `Sidebar` | Aside navigation list and brand area | Expanded, compact, mobile drawer, both directions |
| `AppHeader` | Workspace header and preference controls | Arabic/English labels, both themes, responsive controls |
| `ResponsiveAppShell` | Header/aside/content composition | Desktop, medium rail, mobile drawer open/closed, RTL/LTR and light/dark |
| `LocaleSwitcher` | `ar-SA` / `en-US` control | Both selected locales and keyboard operation |
| `ThemeToggle` | Light/dark control | Both states, accessible labels, keyboard operation |
| `SurfaceCard` / `SummaryCard` | Reusable static dashboard surfaces | Content and placeholder states, both directions and themes |

Every new public component must have exactly one corresponding CSF3 story file (combined files are acceptable only for explicitly paired variants such as `Button` and `IconButton`). Stories must be deterministic and use mock props only; include meaningful loading/empty/error or applicable disabled/active/placeholder states without inventing feature workflows.

## 7. Data, Security, and Domain Rules

- No backend/API work, DTOs, database entities, migrations, authentication, RBAC controls, or business workflow implementation.
- Do not imply frontend navigation visibility is authorization. Placeholder sections carry no role-specific permission logic.
- Do not add credentials, environment-specific API URLs, ZATCA configuration, or mock personal/customer data.
- Locale/theme persistence is presentation preference only; do not store sensitive or business data.
- Respect API and security boundaries from `docs/architecture/06-API-Specification.md` and `docs/architecture/07-Security.md` if future integration points are referenced, but do not implement those integrations.

## 8. Acceptance Criteria (Definition of Done)

- [ ] Root pnpm/Turborepo configuration links `apps/web-erp`, `apps/design-system-studio`, and `packages/ui-kit`; backend projects are excluded from JavaScript build tasks.
- [ ] `apps/web-erp` consumes `@almomiaa/ui` through the workspace and displays the responsive shell and static sample dashboard.
- [ ] Arabic is the default (`ar-SA`, RTL); English (`en-US`, LTR) switch updates all visible strings and document attributes and persists without hydration warnings.
- [ ] Light/dark themes work, default to the system preference, persist explicit choices, and have no visible incorrect-theme flash.
- [ ] Layout behavior is verified at 375px, 768px, and 1280px; no horizontal overflow; mobile drawer supports keyboard and focus behavior.
- [ ] The attached logo is copied unchanged into the UI package and rendered with preserved aspect ratio; compact state uses a text fallback.
- [ ] Every public `@almomiaa/ui` component has an individual source file and corresponding CSF3 story with Arabic RTL and English LTR coverage; Storybook global controls cover both themes and viewport sizes.
- [ ] Token and accessibility audit passes: no hardcoded app hex colors, no forbidden directional layout utilities, WCAG 2.1 AA contrast, keyboard access, and no a11y addon violations in core stories.
- [ ] Shared UI package contains no prohibited business or framework imports.
- [ ] `pnpm install`, frontend lint/typecheck/build, Storybook build, and the `@QA & Security Auditor` gate pass.
- [ ] No backend, business workflow, API, database, authentication, or rep-PWA feature scope was added.
- [ ] After QA passes, `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0002-responsive-bilingual-erp-ui-shell.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks this prompt completed.
