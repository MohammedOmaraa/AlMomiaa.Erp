# PROMPT-0013: Fix `apps/rep-pwa` Vercel API 404 Proxy, Dark Mode (`data-theme`), and Account Logout / Switch Account UX

- **Status:** `COMPLETED — QA PASS`
- **Prompt ID:** `PROMPT-0013`
- **Archived At:** `2026-11-25`
- **Scope:** `apps/rep-pwa` (replace unsupported `[...path].ts` with standard Vercel Function `api/proxy.ts` + `vercel.json` `/api/v1/:path*` & `/health` rewrites, fix dark mode by setting `data-theme="dark"|"light"` + `localStorage` persistence + pre-paint theme script, and upgrade Logout / Switch Account UX across header, Account Drawer, and Home card).

---

## 1. Execution Summary

1. **Fixed Vercel `404` on PWA Login & API Calls (`apps/rep-pwa/api/proxy.ts` & `apps/rep-pwa/vercel.json`):**
   - Removed Next.js-only `api/[...path].ts` (unsupported in non-Next.js Vercel Vite projects) and replaced it with standard Vercel Function `api/proxy.ts`.
   - Added explicit `/api/v1/:path* -> /api/proxy?__path=/api/v1/:path*` and `/health -> /api/proxy?__path=/health` rewrites in `vercel.json` ahead of the SPA fallback.
   - `api/proxy.ts` exports `resolveSafeTargetPath` enforcing strict `/api/v1/*` and `/health` SSRF allowlisting (blocking `..`, `//`, `\`, and scheme injections), stripping internal `__path` while forwarding all real query parameters, enforcing `AbortSignal.timeout(25_000)` (`504`/`502`), and preserving `Cookie` + all `Set-Cookie` headers same-origin.
2. **Fixed Dark Mode (`apps/rep-pwa/src/providers/AppProviders.tsx` & `apps/rep-pwa/index.html`):**
   - Updated `AppProviders.tsx` to set BOTH `document.documentElement.setAttribute("data-theme", next)` (activating `[data-theme="dark"]` in `@almomiaa/ui/tokens.css`) AND `.classList.toggle("dark", next === "dark")`, persisted in `localStorage` (`almomiaa-rep-theme`, `almomiaa-rep-locale`), plus added a pre-paint theme script in `index.html` to eliminate flash-of-light-theme.
   - Wired bilingual `ThemeToggle` labels on both `LoginPage.tsx` and `RepMobileShell.tsx`.
3. **Upgraded Logout & Switch Account UX (`AppProviders.tsx`, `RepMobileShell.tsx`, `RepHomePage.tsx`, `locales/{ar,en}.ts`):**
   - Wrapped `logout()` in `try ... finally` so token, user, notifications, unreadCount, toasts, and SignalR state ALWAYS clear cleanly even on transient network errors.
   - Added a prominent one-tap red **خروج / Sign Out** button in the header, an **Account & Switch Account Drawer** (showing employee name, code, role badges, Change Password, and **تسجيل الخروج وتبديل الحساب / Sign Out & Switch Account**), and a quick **Sign Out** button on the Home welcome card, all navigating immediately to `/login`.

---

## 2. Verification & QA Gate (`PASS`)

- [x] `apps/rep-pwa/api/[...path].ts` removed (`OLD_CATCHALL_EXISTS=False`); `apps/rep-pwa/api/proxy.ts` strict `tsc --noEmit --strict` passed (`0` errors).
- [x] Unit test on `resolveSafeTargetPath`: **6/6 assertions passed** (rewritten `/api/v1/*`, query param preservation, `/health`, path traversal & SSRF rejections).
- [x] `apps/rep-pwa/vercel.json`: valid JSON with `/api/v1/:path*` and `/health` rewrites ordered before SPA fallback.
- [x] `pnpm.cmd -r typecheck` & `pnpm.cmd -r lint`: **5/5 workspaces passed**.
- [x] `apps/rep-pwa` production Vite + Workbox PWA build: **passed** (`dist/sw.js` & `dist/manifest.webmanifest` generated).
- [x] Static QA & Security audit: **0** hex colors, **0** directional Tailwind classes, **0** `any`/`@ts-ignore`, **0** secrets.
