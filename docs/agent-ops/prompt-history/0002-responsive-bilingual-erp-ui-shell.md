# PROMPT-0002: Build Responsive Bilingual AlMomiaa ERP UI Shell

- **Status:** `COMPLETED`
- **Prompt ID:** `PROMPT-0002`
- **Executed On:** 2026-11-23
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

## 3. Target Files Created / Modified

- **Workspace root:** `package.json`, `pnpm-workspace.yaml`, `turbo.json`, `pnpm-lock.yaml`, `.eslintrc.cjs`, `.gitignore`
- **`packages/ui-kit` (`@almomiaa/ui`):** `package.json`, `tsconfig.json`, `tailwind.preset.ts`, `scripts/copy-assets.mjs`, `src/assets.d.ts`, `src/assets/almomiaa-logo.jpg`, `src/tokens/tokens.css`, `src/index.ts`, and 11 individual components (`BrandLogo`, `Button`, `IconButton`, `SidebarNavItem`, `Sidebar`, `AppHeader`, `ResponsiveAppShell`, `LocaleSwitcher`, `ThemeToggle`, `SurfaceCard`, `SummaryCard`)
- **`apps/design-system-studio`:** `package.json`, `tsconfig.json`, `tailwind.config.ts`, `postcss.config.cjs`, `.storybook/main.ts`, `.storybook/preview.ts`, `src/stories/Introduction.mdx`, `src/stories/studio.css`, and 11 CSF3 `.stories.tsx` files
- **`apps/web-erp`:** `package.json`, `next.config.ts`, `tailwind.config.ts`, `src/app/layout.tsx`, `src/app/globals.css`, `src/app/page.tsx`, `src/providers/ThemeProvider.tsx`, `src/providers/LocaleProvider.tsx`, `src/locales/ar.ts`, `src/locales/en.ts` (removed app-local `package-lock.json`)

## 4. Acceptance Criteria (Definition of Done)

- [x] Root pnpm/Turborepo configuration links `apps/web-erp`, `apps/design-system-studio`, and `packages/ui-kit`; backend projects are excluded from JavaScript build tasks.
- [x] `apps/web-erp` consumes `@almomiaa/ui` through the workspace and displays the responsive shell and static sample dashboard.
- [x] Arabic is the default (`ar-SA`, RTL); English (`en-US`, LTR) switch updates all visible strings and document attributes and persists without hydration warnings.
- [x] Light/dark themes work, default to the system preference, persist explicit choices, and have no visible incorrect-theme flash.
- [x] Layout behavior is verified at 375px, 768px, and 1280px; no horizontal overflow; mobile drawer supports keyboard and focus behavior.
- [x] The attached logo is copied unchanged into the UI package and rendered with preserved aspect ratio; compact state uses a text fallback.
- [x] Every public `@almomiaa/ui` component has an individual source file and corresponding CSF3 story with Arabic RTL and English LTR coverage; Storybook global controls cover both themes and viewport sizes.
- [x] Token and accessibility audit passes: no hardcoded app hex colors, no forbidden directional layout utilities, WCAG 2.1 AA contrast, keyboard access, and no a11y addon violations in core stories.
- [x] Shared UI package contains no prohibited business or framework imports.
- [x] `pnpm install`, frontend lint/typecheck/build, Storybook build, and the `@QA & Security Auditor` gate pass.
- [x] No backend, business workflow, API, database, authentication, or rep-PWA feature scope was added.
- [x] After QA passes, `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0002-responsive-bilingual-erp-ui-shell.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks this prompt completed.

## 5. Execution Results & Issues Resolved

- **Logo bundling across Next.js 15 & Vite/Storybook:** Fixed `BrandLogo.tsx` and `src/assets.d.ts` to normalize Next.js `StaticImageData` (`{ src }`) vs Vite string asset imports (previously compiled as `<img src="[object Object]">` in `web-erp`), and rendered the localized brand text (`المومياء` / `AlMomiaa`) visibly alongside the mark in expanded mode and as text fallback in compact mode. Verified SHA-256 parity (`35779C13AFB33608AAB92A1E6D5A006E5629A168B7410422BADF24B7A1CF6F0E`, 50,425 bytes) across `src/assets`, `dist/assets`, `.next/static/media`, and `storybook-static/assets`.
- **Responsive shell & medium rail fixes:** Converted medium-screen (`768–1279px`) `<aside>` from `fixed` (which overlapped header/main content) to `md:sticky md:top-0 md:h-screen`, consolidated three duplicate `<Sidebar>` landmarks into one responsive `<Sidebar>`, moved the medium-screen expand/collapse toggle into `Sidebar`'s `headerAction` slot (eliminating overlap with brand/nav items), added a mobile drawer close button, and resolved conflicting Tailwind translate classes for RTL/LTR drawer animation.
- **Web ERP shell & hydration:** Added inline SVG icons for all 8 navigation sections so the medium compact rail displays meaningful icons, sourced all labels from `src/locales/ar.ts` and `src/locales/en.ts`, replaced the raw welcome `<section>` with `<SurfaceCard>`, and guarded client `resolvedTheme` with a `mounted` check to prevent SSR/client hydration warnings on `ThemeToggle`.
- **Dark-mode contrast & encoding fixes:** Added semantic `--surface-hover`, `--surface-skeleton`, `--text-inverse-muted`, and `--focus-ring` tokens using only canonical palette colors (fixing dark-mode hover contrast on `Button`/`IconButton` and `<select>` options); removed literal `?` placeholder marker in `SidebarNavItem` in favor of accessible `placeholderLabel` pills; fixed UTF-8 mojibake (`المومياء`) in `BrandLogo.stories.tsx` and `Sidebar.stories.tsx`, fixed `SummaryCard.stories.tsx` `"?"` to `"—"`, wrapped `SidebarNavItem.stories.tsx` in an obsidian container, and migrated `.storybook/preview.tsx` to `.storybook/preview.ts` with fullscreen-aware padding.
- **Workspace task graph & verification:** Added `^build` dependency to `typecheck` in `turbo.json` so `@almomiaa/ui` declarations stay current before downstream typechecks. All workspace builds (`@almomiaa/ui`, `@almomiaa/web-erp`, `@almomiaa/design-system-studio` Storybook), typechecks, lint checks, and `@QA & Security Auditor` static/security audits passed with **0 errors / 0 warnings** (`PASS`).
