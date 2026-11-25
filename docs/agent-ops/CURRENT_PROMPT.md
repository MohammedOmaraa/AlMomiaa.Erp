# PROMPT-0013: Fix `apps/rep-pwa` Vercel API 404 Proxy, Dark Mode (`data-theme`), and Account Logout / Switch Account UX

- **Status:** `COMPLETED — ARCHIVED (docs/agent-ops/prompt-history/0013-fix-pwa-vercel-proxy-dark-mode-and-logout.md)`
- **Prompt ID:** `PROMPT-0013`
- **Scope:** `apps/rep-pwa` (replace unsupported `[...path].ts` with standard Vercel Function `api/proxy.ts` + `vercel.json` `/api/v1/:path*` & `/health` rewrites, fix dark mode by setting `data-theme="dark"|"light"` + `localStorage` persistence, and upgrade Logout / Switch Account UX with guaranteed state cleanup & `/login` navigation), plus verification via `scripts/verify-deployed-apps.mjs` and full monorepo builds/lint/tests.
- **Reference baseline:** Architecture v3.2 (`00-ADR`..`08-Development-Roadmap`); `PROMPT-0001`..`PROMPT-0012` completed (`PROJECT_LOG.md` Next Prompt Number: `0013`). `apps/web-erp` (`https://almomiaa-erp.vercel.app`) is live and login works; `apps/rep-pwa` (`https://almomiaa-rep.vercel.app`) currently fails login with Vercel `404` JSON, dark mode toggle does not change colors, and logout/switch-account needs a clear, reliable flow.

---

## 1. Purpose of Prompt (Root-Cause Analysis & Objective)

Resolve all three issues reported on the live Sales Rep Mobile PWA (`https://almomiaa-rep.vercel.app`):

1. **Fix `404` (`{"error": {"code": "404", "message": "The page could not be found"}}`) on PWA Login & API Calls:**
   - **Root Cause:** In non-Next.js (Vite) Vercel projects, standalone Serverless Functions under `api/` do **not** recognize Next.js catch-all `[...path].ts` filenames. Requests to `POST /api/v1/auth/login` never reached the proxy function and returned Vercel's built-in 404 JSON.
   - **Fix:** Replace `apps/rep-pwa/api/[...path].ts` with a standard Vercel Function **`apps/rep-pwa/api/proxy.ts`** and route `/api/v1/:path*` -> `/api/proxy?__path=/api/v1/:path*` and `/health` -> `/api/proxy?__path=/health` in `apps/rep-pwa/vercel.json`.
   - `api/proxy.ts` strips the internal `__path` query param before forwarding all real query parameters (`?search=...&page=...`) to `BACKEND_API_ORIGIN`, preserves the strict SSRF allowlist (`/api/v1/*` and `/health` only), enforces `AbortSignal.timeout(25_000)` (`504`/`502` ProblemDetails), and preserves `Cookie` & all `Set-Cookie` (`HttpOnly; Secure; SameSite=Strict`) headers same-origin!
2. **Fix Dark Mode in `apps/rep-pwa`:**
   - **Root Cause:** `@almomiaa/ui/tokens.css` activates dark surface/text tokens via the `[data-theme="dark"]` CSS attribute selector, whereas `apps/rep-pwa/src/providers/AppProviders.tsx` only toggled `.classList.toggle("dark")` and never set `document.documentElement.dataset.theme` (nor persisted the preference).
   - **Fix:** Update `AppProviders.tsx` to set BOTH `document.documentElement.setAttribute("data-theme", next)` AND `document.documentElement.classList.toggle("dark", next === "dark")`, persist theme (`almomiaa-rep-theme`) and locale (`almomiaa-rep-locale`) in `localStorage`, and apply them immediately on mount (and add a tiny inline pre-paint script in `apps/rep-pwa/index.html` or early effect so dark mode has zero flash!).
3. **Make Logout & Switch Account Prominent and Reliable in `apps/rep-pwa`:**
   - Add a clean **Account / Switch Account Drawer** (or header action + Home card button) in `RepMobileShell.tsx` and `RepHomePage.tsx` with clear **تسجيل الخروج / تبديل الحساب (`Sign Out / Switch Account`)** and **تغيير كلمة المرور (`Change Password`)** buttons.
   - Ensure `logout()` in `AppProviders.tsx` calls `POST /api/v1/auth/logout`, clears `accessTokenStore`, resets `user`, `notifications`, `unreadCount`, `toasts`, disconnects SignalR cleanly, sets `authStatus = "unauthenticated"`, and navigates cleanly to `/login` so the user can immediately sign into another account (e.g., switching between `admin` and `REP-DEMO`).
   - Also make `RepHomePage` and `RepMobileShell` clearly show when logged in as a non-rep/admin account with a quick **"تبديل الحساب / Switch Account"** button right inside the warning/header bar.

---

## 2. Target Execution Agents and Order

1. **`@Frontend Engineer`** —
   - **A. Fix Vercel Serverless API Proxy (`apps/rep-pwa/api/proxy.ts` & `apps/rep-pwa/vercel.json`):**
     - Remove `apps/rep-pwa/api/[...path].ts` and create `apps/rep-pwa/api/proxy.ts` (standard Vercel Node function — NO catch-all brackets!).
     - Parse `__path` query param from `req.url`, strip `__path` from forwarded query parameters, validate strict allowlist (`pathname === "/health" || pathname === "/api/v1" || pathname.startsWith("/api/v1/")`), forward safe headers (`accept`, `accept-language`, `authorization`, `content-type`, `cookie`, `origin`, `referer`, `sec-fetch-site`, `x-idempotency-key`), enforce `AbortSignal.timeout(25_000)`, forward `Set-Cookie` via `getSetCookie()`, and return `504`/`502` RFC 7807 `ProblemDetails` on upstream failure.
     - Update `apps/rep-pwa/vercel.json`:
       ```json
       "rewrites": [
         { "source": "/api/v1/:path*", "destination": "/api/proxy?__path=/api/v1/:path*" },
         { "source": "/health", "destination": "/api/proxy?__path=/health" },
         {
           "source": "/((?!api/|assets/|icons/|manifest\\.webmanifest|sw\\.js|registerSW\\.js|workbox-.*\\.js).*)",
           "destination": "/index.html"
         }
       ]
       ```
   - **B. Fix Dark Mode (`apps/rep-pwa/src/providers/AppProviders.tsx` & `apps/rep-pwa/index.html`):**
     - Read initial theme from `localStorage.getItem("almomiaa-rep-theme")` (default `"light"`) and apply `document.documentElement.setAttribute("data-theme", theme)` + `document.documentElement.classList.toggle("dark", theme === "dark")` on mount and on every `setTheme(next)` toggle.
     - Persist locale (`almomiaa-rep-locale`) in `localStorage` as well so Arabic/English preference survives reloads.
     - Update `ThemeToggle` labels in `LoginPage.tsx` and `RepMobileShell.tsx` to pass clear bilingual `labels` so the toggle is intuitive in both Arabic and English.
   - **C. Upgrade Logout & Switch Account UX (`apps/rep-pwa/src/providers/AppProviders.tsx`, `apps/rep-pwa/src/components/RepMobileShell.tsx`, `apps/rep-pwa/src/pages/RepHomePage.tsx`, `apps/rep-pwa/src/locales/{ar,en}.ts`):**
     - In `AppProviders.tsx`: guarantee `logout()` clears `accessTokenStore`, `user`, `notifications`, `unreadCount`, `toasts`, `lastEvent`, sets `authStatus = "unauthenticated"`, and swallows transient network errors in `finally` so logout NEVER gets stuck.
     - In `RepMobileShell.tsx`:
       - Replace the cramped header buttons with clean, high-contrast mobile-friendly controls: Notifications badge, LocaleSwitcher, ThemeToggle, and an **الحساب / Account** button opening an Account Drawer (showing full name, employee code, role badges, **تغيير كلمة المرور / Change Password**, and prominent red/danger **تسجيل الخروج وتبديل الحساب / Sign Out & Switch Account** button that logs out and routes to `/login` via `navigate("/login", { replace: true })`), PLUS a direct compact **خروج / Sign Out** icon/button right in the header for one-tap logout!
     - In `RepHomePage.tsx`: add a clean **"تبديل الحساب / Switch Account"** quick button in the welcome hero card next to Install App so switching accounts takes 1 tap.
   - **D. Verify Local Proxy & Build:**
     - Add a lightweight Node test (or test script assertion in `packages/api-client` / `scripts/test-pwa-proxy.mjs`) verifying `apps/rep-pwa/api/proxy.ts` query-param forwarding (`__path` stripped, real query params preserved, non-allowlisted paths rejected with `404`).

2. **`@QA & Security Auditor`** —
   - Verify `apps/rep-pwa/api/[...path].ts` is deleted and `apps/rep-pwa/api/proxy.ts` passes strict `tsc --noEmit --strict`.
   - Verify `apps/rep-pwa/vercel.json` is valid JSON with `/api/v1/:path*` and `/health` rewrites BEFORE the SPA catch-all rewrite.
   - Verify `pnpm.cmd -r typecheck`, `pnpm.cmd -r lint`, `apps/rep-pwa` production Vite + Workbox PWA build (`dist/sw.js` & `dist/manifest.webmanifest`), and `apps/web-erp` Next.js 15 build all pass.
   - Verify static rules: zero hardcoded hex colors in TSX/TS, zero directional Tailwind classes, zero `any`/`@ts-ignore`, zero committed secrets, SSRF allowlist enforced.

3. **`@Orchestrator`** — Archive as `docs/agent-ops/prompt-history/0013-fix-pwa-vercel-proxy-dark-mode-and-logout.md`, update `docs/agent-ops/PROJECT_LOG.md`, and mark `CURRENT_PROMPT.md` completed only after QA returns `PASS`.

---

## 3. Target Files to Create / Modify

### `apps/rep-pwa/`
- `api/[...path].ts` (delete — unsupported catch-all filename on non-Next.js Vercel projects)
- `api/proxy.ts` (new — standard Vercel Serverless Function proxying `/api/v1/*` and `/health` via `__path` query param with strict SSRF allowlist & cookie preservation)
- `vercel.json` (modify — route `/api/v1/:path*` and `/health` to `/api/proxy?__path=...`)
- `src/providers/AppProviders.tsx` (modify — set `data-theme="dark"|"light"` + `.dark` class + `localStorage` persistence for theme/locale; full state cleanup on `logout()`)
- `src/components/RepMobileShell.tsx` (modify — Account Drawer + direct one-tap Sign Out / Switch Account button + bilingual ThemeToggle labels + explicit `navigate("/login", { replace: true })` on logout)
- `src/pages/RepHomePage.tsx` (modify — add quick Switch Account / Logout button in welcome card)
- `src/pages/LoginPage.tsx` (modify — pass bilingual ThemeToggle labels)
- `src/locales/ar.ts` & `src/locales/en.ts` (modify — add `account`, `switchAccount`, `lightMode`, `darkMode` labels)

---

## 4. Security & Domain Rules

1. **SSRF Allowlist on `api/proxy.ts`:** Strictly reject any `__path` that is not `/health`, `/api/v1`, or starting with `/api/v1/` (and reject `..` path traversal or absolute URLs like `https://...` in `__path`!) with `404` JSON.
2. **Same-Origin Cookie Security:** Forward `Cookie` inbound and ALL `Set-Cookie` headers outbound so `almomiaa_refresh` (`HttpOnly; Secure; SameSite=Strict`) works identically on Android, iOS, and Desktop PWAs.
3. **Design System & RTL/LTR Hygiene:** Dark mode MUST work through `[data-theme="dark"]` tokens (`bg-surface-canvas`, `bg-surface-card`, `text-text-primary`, `border-surface-border`), zero hardcoded hex colors in TSX/TS, zero directional Tailwind classes, zero `any`/`@ts-ignore`.

---

## 5. Acceptance Criteria (Definition of Done)

- [ ] `apps/rep-pwa/api/[...path].ts` is replaced by `apps/rep-pwa/api/proxy.ts` and wired via `apps/rep-pwa/vercel.json` so `POST /api/v1/auth/login` and `GET /health` never return Vercel's `404` page-not-found JSON.
- [ ] `apps/rep-pwa/api/proxy.ts` blocks path traversal / non-`/api/v1/*` paths (`404`), strips internal `__path` while forwarding real query parameters, enforces `25s` timeout (`504`/`502`), and preserves `Set-Cookie` headers.
- [ ] Toggling Dark Mode in `apps/rep-pwa` (on both `/login` and authenticated screens) immediately switches `[data-theme="dark"]` CSS tokens across the entire app and persists across page reloads.
- [ ] Users in `apps/rep-pwa` have a clear, one-tap **Sign Out / Switch Account (`تسجيل الخروج / تبديل الحساب`)** flow (in the header, Account Drawer, and Home card) that revokes the cookie, clears all user/notification state, and redirects cleanly to `/login`.
- [ ] `pnpm.cmd -r typecheck`, `pnpm.cmd -r lint`, `apps/rep-pwa/api/proxy.ts` strict `tsc`, `apps/rep-pwa` Vite + Workbox PWA build, `apps/web-erp` build, and `@QA & Security Auditor` gate return **`PASS`**, after which `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0013-fix-pwa-vercel-proxy-dark-mode-and-logout.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks `CURRENT_PROMPT.md` completed.
