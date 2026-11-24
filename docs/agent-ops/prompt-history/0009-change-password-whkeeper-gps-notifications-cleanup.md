# PROMPT-0009: Change Password & Logout, Strict Single-Warehouse `WH_KEEPER`, Role-Gated 10s Live Google Maps GPS Tracker, Remove API JSON Inspector from Web ERP, and Persistent Notifications with Alert Popups & Mark-All-Read

- **Status:** `COMPLETED`
- **Prompt ID:** `PROMPT-0009`
- **Archived At:** `2026-11-24`
- **QA & Security Auditor Verdict:** `PASS`
- **Scope:** `apps/backend-api`, `packages/ui-kit`, `apps/design-system-studio`, `packages/api-client`, `apps/web-erp`, `apps/rep-pwa`, `docs/architecture/{06-API-Specification,07-Security}.md`

---

## 1. Execution Summary

Delivered all 6 production security, governance, mapping, UX cleanup, and durable alerting capabilities end-to-end:

1. **Change Password & Logout (`apps/backend-api`, `@almomiaa/api-client`, `apps/web-erp`, `apps/rep-pwa`):**
   - Added `[Authorize] POST /api/v1/auth/change-password` (`ChangePasswordCommand` + FluentValidation + `AuthenticationService.ChangePasswordAsync`): validates current password, enforces `>= 8` chars & confirmation match & distinct new password, updates `PasswordHash`, rotates `SecurityStamp` (immediately invalidating every active JWT across all devices), revokes all active `RefreshTokenSessions`, clears the `almomiaa_refresh` HttpOnly cookie, appends `[ops].[AuditLogs]` without logging any password, and returns `204 No Content`.
   - Added **Change Password** drawer + polished **Logout** in both `apps/web-erp` (`ErpAppLayout.tsx`, `AuthProvider.tsx`, `LoginForm.tsx`) and `apps/rep-pwa` (`RepMobileShell.tsx`, `AppProviders.tsx`, `LoginPage.tsx`), immediately disconnecting SignalR and redirecting to `/login?passwordChanged=1` with a localized confirmation banner.
2. **Strict Single-Warehouse `WH_KEEPER` Invariant (`apps/backend-api` + `apps/web-erp`):**
   - Enforced in `IamAndCustomersServices.cs` across `CreateUserAsync`, `UpdateUserWarehousesAsync`, and `UpdateUserRolesAsync` that any user with `WH_KEEPER` must have **exactly 1** assigned warehouse (`AssignedWarehouseIds.Count == 1` and `DefaultWarehouseId == AssignedWarehouseIds[0]`, returning `422` for 0 or >1).
   - Enforced single-warehouse selector in `UsersWorkspace.tsx` and locked `WH_KEEPER` strictly to their 1 assigned warehouse in `InventoryWorkspace.tsx`.
3. **Role-Gated 10-Second Live Google Maps GPS Tracker (`apps/backend-api` + `apps/web-erp` ONLY):**
   - Added strict `"GpsTrackRoles"` authorization policy allowing **ONLY** `SUPER_ADMIN`, `EXEC_MGR`, and `SALES_MGR` (`403` for all other roles) on `GET /api/v1/gps/live` and `GET /api/v1/gps/reps/{repId}/history`.
   - Added `/gps` (`GpsTrackingWorkspace.tsx`) in `apps/web-erp` (sidebar & route restricted to those 3 roles) refreshing automatically every **10 seconds** with **Hybrid Google Maps** (Google Maps JS API multi-pin map when `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY` is configured + seamless zero-key Google Maps embed & "Open in Google Maps" fallback locally). No auto-GPS streaming added to `apps/rep-pwa`.
4. **Removed API JSON Inspector from `apps/web-erp`:**
   - Deleted `ApiActivityProvider.tsx`, removed inspector telemetry hooks from `api-client.ts`, removed the header `API Inspector` button/drawer from `ErpAppLayout.tsx`, and removed remaining `JsonResponseViewer` blocks from `DashboardWorkspace.tsx` and `ReportsWorkspace.tsx` (**0** remaining API inspector references in `apps/web-erp/src`).
5. **Persistent Notifications (`[ops].[Notifications]`) + Live Alert Popups + Mark All Read + Mute Toggle:**
   - Added `UserNotification` domain entity, `[ops].[Notifications]` EF Core configuration with filtered index `IX_Notifications_Recipient_Unread`, and generated & applied additive migration `20261124093036_AddOpsNotificationsTable` directly to local SQL Server `AlMomiaa` (now 45 canonical tables).
   - Added `NotificationsController` (`GET /api/v1/notifications`, `PATCH /api/v1/notifications/{id}/read`, `POST /api/v1/notifications/mark-all-read`), automatic notification persistence & SignalR `NotificationReceived` / `NotificationsMarkedAllRead` dispatch on order workflow actions.
   - Added reusable `NotificationItem` component in `@almomiaa/ui` (**27** total components) + CSF3 Storybook 8 story (**27** stories).
   - Wired **Notifications Bell** (with unread count badge), **Notifications Drawer** (`Unread`/`All` tabs, **"Mark All Read"** clearing unread badges/alerts durably across sessions, and **"Mute Live Alert Popups"** toggle) and live alert banners in both `apps/web-erp` and `apps/rep-pwa`.

---

## 2. Verification & QA Results

- `dotnet build apps/backend-api/AlMomiaa.Erp.sln --no-restore`: **PASS (0 warnings, 0 errors)**
- `dotnet ef database update` (`20261124093036_AddOpsNotificationsTable`): **PASS (applied to local SQL Server `AlMomiaa`)**
- `dotnet test apps/backend-api/AlMomiaa.Erp.sln --no-build`: **PASS (26/26 passed: 1 Architecture + 25 Auth/Schema/E2E/SignalR/Security/Notifications)**
- `pnpm --filter @almomiaa/api-client build` & `test`: **PASS (8/8 passed)**
- `pnpm -r typecheck` & `pnpm -r lint` (5/5 packages/apps): **PASS (0 errors)**
- `apps/design-system-studio` `build-storybook`: **PASS (27 stories, 169 modules)**
- `apps/rep-pwa` production PWA build: **PASS (`manifest.webmanifest`, `sw.js`, `NetworkOnly` for `/api/` & `/hubs/`)**
- `apps/web-erp` Next.js 15 production build: **PASS (all 11 app routes including `/gps`)**
- Static QA & Security Audit (119 TS/TSX files): **0 API inspector references in `web-erp`, 0 forbidden imports in `ui-kit`, 0 hardcoded hex colors, 0 directional Tailwind classes, 0 `any`/`@ts-ignore`, 0 client-side token storage, 0 hardcoded API keys/secrets** (**QA PASS**).

---

## 3. Acceptance Criteria (Definition of Done)

- [x] `POST /api/v1/auth/change-password` validates current/new password, rotates `SecurityStamp`, revokes all refresh sessions, clears refresh cookie, audits without secrets, and both `apps/web-erp` and `apps/rep-pwa` provide Change Password + Logout flows redirecting cleanly to `/login`.
- [x] `WH_KEEPER` is strictly enforced to have **exactly 1 warehouse** across Backend (`POST /users`, `PUT /users/{id}/warehouses`, `PUT /users/{id}/roles` reject 0 or >1 with `422`) and `apps/web-erp` (`/users` & `/inventory`).
- [x] `GET /api/v1/gps/live` and `GET /api/v1/gps/reps/{repId}/history` are strictly restricted to `SUPER_ADMIN`, `EXEC_MGR`, and `SALES_MGR` (`403` for all other roles), and `apps/web-erp` `/gps` renders the 10-second auto-refreshing Hybrid Google Maps tracker exclusively for those 3 roles.
- [x] The API JSON Inspector (`فاحص استجابات نقاط النهاية الحية` / `ApiActivityProvider` / `JsonResponseViewer`) is completely removed from `apps/web-erp`.
- [x] `[ops].[Notifications]` migration is generated and applied to local SQL Server `AlMomiaa`; alerts persist and stream live via SignalR; `GET /api/v1/notifications`, `PATCH /api/v1/notifications/{id}/read`, and `POST /api/v1/notifications/mark-all-read` work end-to-end; both `apps/web-erp` and `apps/rep-pwa` display live alert popups, a Notifications Bell/Drawer (`Unread`/`All` tabs), **"Mark All Read"**, and a **"Mute Live Alert Popups"** toggle.
- [x] `NotificationItem` is exported from `@almomiaa/ui` (27 total components) with a CSF3 Storybook 8 story (27 stories) and clean `build-storybook`.
- [x] All builds, tests, lint, typecheck, and `@QA & Security Auditor` checks returned **`PASS`**; archived as `docs/agent-ops/prompt-history/0009-change-password-whkeeper-gps-notifications-cleanup.md`, updated `docs/agent-ops/PROJECT_LOG.md`, and marked `CURRENT_PROMPT.md` completed.
