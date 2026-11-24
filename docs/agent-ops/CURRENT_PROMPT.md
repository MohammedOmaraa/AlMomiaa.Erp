# PROMPT-0009: Change Password & Logout, Strict Single-Warehouse `WH_KEEPER`, Role-Gated 10s Live Google Maps GPS Tracker, Remove API JSON Inspector from Web ERP, and Persistent Notifications with Alert Popups & Mark-All-Read

- **Status:** `COMPLETED (Archived as 0009-change-password-whkeeper-gps-notifications-cleanup.md)`
- **Prompt ID:** `PROMPT-0009`
- **Scope:** `apps/backend-api` (Auth change-password, `WH_KEEPER` 1-warehouse enforcement, `GET /api/v1/gps/live` & history role-gated to `SUPER_ADMIN`/`EXEC_MGR`/`SALES_MGR`, `[ops].[Notifications]` entity + SQL Server migration + `NotificationsController` + SignalR push), `packages/ui-kit` (`NotificationItem` reusable component), `apps/design-system-studio` (`NotificationItem.stories.tsx`), `packages/api-client` (change-password, notifications, live GPS, new SignalR notification events), `apps/web-erp` (remove API JSON Inspector, add Change Password & Logout, add Notifications Bell/Tab + Live Alert Popups + Mark All Read + Mute toggle, enforce single-warehouse `WH_KEEPER` UI, add 10s Hybrid Google Maps `/gps` screen), `apps/rep-pwa` (add Change Password & Logout, add Notifications Bell/Drawer + Live Alert Popups + Mark All Read + Mute toggle), `docs/architecture/{05-Database,06-API-Specification,07-Security}.md`
- **Reference baseline:** Architecture v3.2 (`00-ADR-Architecture-Decisions.md`..`08-Development-Roadmap.md`); `PROMPT-0001`..`PROMPT-0008` completed (live `.NET 9` API + SignalR `/hubs/operations` on `:5000`, live SQL Server `AlMomiaa` with 3 migrations, 22/22 backend tests passing, 26 `@almomiaa/ui` components + 26 Storybook 8 stories, `@almomiaa/api-client` with 7/7 tests, `apps/web-erp` on `:3000`, `apps/rep-pwa` on `:5173`)
- **Confirmed User Decisions:**
  1. **Hybrid Google Maps (`/gps`, every 10s):** Interactive Google Maps JavaScript API multi-marker map when `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY` is configured, with seamless zero-key Google Maps embed (`https://maps.google.com/maps?q=LAT,LNG&z=15&output=embed`) + "Open in Google Maps" link fallback so it works locally immediately. Strictly restricted to `SUPER_ADMIN`, `EXEC_MGR`, and `SALES_MGR`.
  2. **Change Password Security Behavior:** Verify current password, update hash, rotate `SecurityStamp` (invalidating all active JWTs immediately), revoke all active refresh-token sessions, clear refresh cookie, and immediately log the user out on frontend/PWA so they sign back in with the new password.
  3. **Persistent Notifications + Alert Popups + Mark All Read + Mute Toggle:** Persist notifications in `[ops].[Notifications]` (applied via EF Core migration to local SQL Server `AlMomiaa`), deliver live SignalR alert popups + unread badge + Notifications Bell/Drawer/Tab (`Unread` / `All`), expose `POST /api/v1/notifications/mark-all-read` (marks all read in DB so the user is never re-notified for read items), AND include a **"Mute Live Alert Popups"** preference toggle in the Notifications panel.
  4. **GPS Scope:** Backend live GPS read endpoints + `apps/web-erp` 10-second Google Maps tracking page ONLY (do **NOT** add automatic browser geolocation streaming in `apps/rep-pwa`).
  5. **Remove API JSON Inspector from `apps/web-erp`:** Completely remove the developer API JSON inspector button/drawer (`فاحص استجابات نقاط النهاية الحية`) and `ApiActivityProvider` from `apps/web-erp` (keep `JsonResponseViewer` inside `packages/ui-kit` & Storybook).
  6. **`WH_KEEPER` Single Warehouse Rule:** Enforce strictly across Backend API (`POST /users`, `PUT /users/{id}/warehouses`, `PUT /users/{id}/roles`) and `apps/web-erp` (`/users` & `/inventory`) that every `WH_KEEPER` must have **exactly 1 warehouse** assigned (`AssignedWarehouseIds.Count == 1` and `DefaultWarehouseId == AssignedWarehouseIds[0]`).

---

## 1. Purpose of Prompt

Harden production usability, governance, and real-time operational awareness across AlMomiaa ERP (`apps/backend-api`, `apps/web-erp`, `apps/rep-pwa`, `@almomiaa/api-client`, `@almomiaa/ui`):
1. Give every authenticated employee self-service **Change Password** (with full session revocation & forced re-login) and polished **Logout** across Backend, Web ERP, and Rep PWA.
2. Enforce the strict business invariant that a **Warehouse Keeper (`WH_KEEPER`)** owns and operates **only one warehouse** — both server-side and in the UI.
3. Provide **Live Field Rep GPS Tracking on Google Maps (10-second refresh)** strictly restricted to **`SUPER_ADMIN`, `EXEC_MGR`, and `SALES_MGR`**.
4. Clean up `apps/web-erp` into pure production UX by removing the developer **API JSON Inspector** (`فاحص استجابات نقاط النهاية الحية`).
5. Upgrade operational alerts into a durable **Notification Center + Live Alert Popups** backed by `[ops].[Notifications]` in SQL Server, with a **Notifications Tab/Drawer**, per-item read, **"Mark All Read"** endpoint (persisted so already-read alerts never notify the user again), and a **"Mute Live Alert Popups"** toggle.

---

## 2. Target Execution Agents and Order

1. **`@Backend .NET`** —
   - **A. Change Password (`POST /api/v1/auth/change-password`) & Logout Hardening:**
     - Add `ChangePasswordRequest(CurrentPassword, NewPassword, ConfirmNewPassword)`, `ChangePasswordCommand`, FluentValidator (current required, new password min 8 chars + complexity/trimmed check, `NewPassword != CurrentPassword`, `ConfirmNewPassword == NewPassword`), and `ChangePasswordCommandHandler` in `AlMomiaa.Application/Authentication/Commands/ChangePassword/`.
     - Implement `ChangePasswordAsync` in `AuthenticationService.cs`: verify current password via `IPasswordHasher`, set new hash, call `user.RotateSecurityStamp()` & update `ISecurityStampStore` immediately (invalidating every active access JWT across all devices), revoke ALL non-revoked `[id].[RefreshTokenSessions]` for the user, write `[ops].[AuditLogs]` (`ChangePassword`, NEVER logging any password value!), and save in one transaction.
     - Expose `[Authorize] [HttpPost("change-password")]` in `AuthController.cs`: validates trusted origin, runs `ChangePasswordCommand`, clears `almomiaa_refresh` cookie (`Max-Age=0`), and returns `204 No Content` (returns `400` on validation errors / wrong current password via RFC 7807 ProblemDetails).
   - **B. `WH_KEEPER` Strictly Single Warehouse Invariant:**
     - Add a shared validation guard in `IamModuleService.cs` (`EnsureWarehouseKeeperSingleWarehouseRule`): whenever the resulting role set of a user includes `WH_KEEPER` (and NOT `SUPER_ADMIN`? Enforce strictly for any `WH_KEEPER` role assignment!), the user MUST have **exactly 1** assigned warehouse (`warehouseIds.Count == 1`) AND `DefaultWarehouseId == warehouseIds[0]`.
     - Enforce this rule in:
       1. `CreateUserAsync` (`POST /api/v1/users`) — rejects `WH_KEEPER` with 0 or >1 warehouses (`BusinessRuleException` -> `422 Unprocessable Entity`), and automatically aligns `DefaultWarehouseId = warehouseIds[0]`.
       2. `UpdateUserWarehousesAsync` (`PUT /api/v1/users/{id}/warehouses`) — rejects updating a `WH_KEEPER` to 0 or >1 warehouses (`422`), and aligns `DefaultWarehouseId = warehouseIds[0]`.
       3. `UpdateUserRolesAsync` (`PUT /api/v1/users/{id}/roles`) — rejects assigning `WH_KEEPER` to a user whose currently assigned warehouse count is != 1 (`422`, with clear message instructing to assign exactly 1 warehouse first).
   - **C. Role-Restricted Live GPS Endpoints (`SUPER_ADMIN`, `EXEC_MGR`, `SALES_MGR` ONLY):**
     - Register strict authorization policy `"GpsTrackRoles"` in `Program.cs` requiring `ctx.User.IsInRole("SUPER_ADMIN") || ctx.User.IsInRole("EXEC_MGR") || ctx.User.IsInRole("SALES_MGR")` (matching both `ClaimTypes.Role` and `RoleCodes`).
     - Add `GetLiveRepLocationsQuery` (`GET /api/v1/gps/live`) and `GetRepGpsHistoryQuery` (`GET /api/v1/gps/reps/{repId:guid}/history?hours=24&limit=100`) in `AlMomiaa.Application/Operations/` and `OperationsModuleService.cs`, querying `[ops].[GpsRouteHistory]` joined with `[id].[Users]` (returning `RepId`, `EmployeeCode`, `FullNameAr`, `FullNameEn`, `Phone`, `Latitude`, `Longitude`, `AccuracyMeters`, `NearCustomerId`, `RecordedAtUtc`, `IsOnlineRecent` where ping age <= 2 minutes).
     - Add `[Authorize(Policy = "GpsTrackRoles")]` endpoints on `GpsController.cs` (`GET /api/v1/gps/live`, `GET /api/v1/gps/reps/{repId:guid}/history`).
   - **D. Persistent `[ops].[Notifications]` Table, Migration, Endpoints & SignalR Push:**
     - Add `UserNotification` domain entity in `AlMomiaa.Domain/SystemOps/UserNotification.cs` (`Id`, `RecipientUserId`, `EventType`, `Severity`, `TitleAr`, `TitleEn`, `MessageAr`, `MessageEn`, `EntityName`, `EntityId`, `IsRead`, `ReadAtUtc`, `CreatedAtUtc`) with `Create(...)` and `MarkAsRead(utcNow)` methods (zero external dependencies).
     - Add `UserNotificationConfiguration` in `AlMomiaa.Infrastructure/Persistence/Configurations/System/SystemConfigurations.cs` mapping to `[ops].[Notifications]` with FK to `[id].[Users]` (`DeleteBehavior.Cascade`) and filtered covering index `IX_Notifications_Recipient_Unread` on `(RecipientUserId, CreatedAtUtc)` `WHERE [IsRead] = 0`.
     - Add `DbSet<UserNotification> Notifications` to `AlMomiaaDbContext`, generate additive EF Core migration `AddOpsNotificationsTable`, and apply it to local SQL Server `AlMomiaa` (`dotnet ef database update`).
     - Add `INotificationsModuleService` & CQRS queries/commands (`GetMyNotificationsQuery`, `MarkNotificationReadCommand`, `MarkAllNotificationsReadCommand`) and `NotificationsController` (`/api/v1/notifications`, `[Authorize]`):
       - `GET /api/v1/notifications?unreadOnly=&page=1&pageSize=30` -> `{ items, totalCount, unreadCount, nextCursor }` (strictly filtered to `RecipientUserId == currentUser.UserId`).
       - `PATCH /api/v1/notifications/{id:guid}/read` -> marks single notification owned by current user read (`200 OK`, returns updated `NotificationDto` + `unreadCount`).
       - `POST /api/v1/notifications/mark-all-read` -> bulk updates all unread notifications of current user (`IsRead = true`, `ReadAtUtc = utcNow`), broadcasts `NotificationsMarkedAllRead` to `user:{userId}` via SignalR, and returns `{ markedCount, unreadCount: 0 }`.
     - Persist bilingual `UserNotification` rows automatically inside `SalesModuleService` (`CreateSalesOrderAsync` -> notify active `SALES_MGR` & `SUPER_ADMIN` users + `ACCOUNTANT`/`EXEC_MGR` if credit hold; `ApproveSalesOrderAsync` -> notify submitting `RepId` + assigned warehouse keepers; `RejectSalesOrderAsync` -> notify submitting `RepId`; `EditSubmittedOrderByManagerAsync` / `OverrideOrderPriceAsync` / `CoApproveOrderCreditAsync` -> notify submitting `RepId`) AND push `NotificationReceived` via `IOperationsRealtimeNotifier` -> `SignalROperationsRealtimeNotifier` to each recipient's `user:{recipientId}` group!
   - **E. Backend Tests:**
     - Add comprehensive integration tests (`AccountSecurityGpsWarehouseAndNotificationsTests.cs`) covering:
       1. `POST /api/v1/auth/change-password`: rejects wrong current password (`400`), rejects weak/mismatched password (`400`), succeeds (`204`), immediately rejects old JWT (`401` via rotated `SecurityStamp`), rejects old refresh cookie (`401`), and accepts login with the new password (`200`).
       2. `WH_KEEPER` single-warehouse invariant: rejects 0 or 2 warehouses on `POST /users` (`422`), rejects 0 or 2 warehouses on `PUT /users/{id}/warehouses` (`422`), rejects assigning `WH_KEEPER` role to a multi-warehouse user (`422`), accepts exactly 1 warehouse (`201`/`200`).
       3. `GET /api/v1/gps/live`: allows `SUPER_ADMIN`, `EXEC_MGR`, `SALES_MGR` (`200` with latest rep coordinates), strictly forbids `SALES_REP`, `WH_KEEPER`, `ACCOUNTANT`, `PURCH_MGR` (`403`).
       4. Persistent Notifications + Mark All Read: creating & approving an order persists unread `UserNotification` rows for `SALES_MGR` and `SALES_REP`, `GET /api/v1/notifications` returns `unreadCount > 0`, `POST /api/v1/notifications/mark-all-read` marks all read (`unreadCount == 0`), and subsequent `GET /api/v1/notifications?unreadOnly=true` returns 0 unread items.
     - Sync `docs/architecture/{05-Database,06-API-Specification,07-Security}.md`.
2. **`@Design System UI`** —
   - Add reusable pure component `NotificationItem` (`packages/ui-kit/src/components/feedback/NotificationItem.tsx`) supporting severity tone (`info | success | warning | danger`), unread gold indicator dot, title, message, timestamp, and optional click/`onMarkRead` action — strict semantic tokens & logical RTL/LTR CSS, exported from `packages/ui-kit/src/index.ts` (27 total components).
   - Add CSF3 Storybook 8 story `apps/design-system-studio/src/stories/NotificationItem.stories.tsx` and update `Introduction.mdx` (27 components).
3. **`@Frontend Engineer`** —
   - **A. `@almomiaa/api-client` Expansion:**
     - Add `changePassword(request: { currentPassword, newPassword, confirmNewPassword })` to `AuthService` (`POST /auth/change-password`, clears in-memory access token on success).
     - Add `notifications` service (`src/modules/notifications.service.ts`: `getNotifications`, `markRead`, `markAllRead`) and live GPS methods on `operations` service (`getLiveGps`, `getRepGpsHistory`).
     - Extend `src/realtime/operations-hub.ts` with `NotificationReceived` (`UserNotificationDto`) and `NotificationsMarkedAllRead` events, export all new types from `src/index.ts`, and add unit tests in `tests/erp-client.test.mjs`.
   - **B. `apps/web-erp` Updates:**
     - **Remove API JSON Inspector (`فاحص استجابات نقاط النهاية الحية`):** Delete `src/providers/ApiActivityProvider.tsx`, remove `ApiActivityProvider` from `src/app/layout.tsx`, remove `subscribeApiActivity` / `onApiCall` activity tracking from `src/lib/api-client.ts`, remove the `API Inspector` button and `DrawerModal` + `JsonResponseViewer` from `src/components/ErpAppLayout.tsx`, and remove unused inspector strings from `src/locales/ar.ts` & `src/locales/en.ts`.
     - **Notifications Center + Alert Popups + Mark All Read + Mute Toggle:**
       - Upgrade `RealtimeProvider.tsx` to load persistent notifications via `GET /api/v1/notifications`, listen to `NotificationReceived` & `NotificationsMarkedAllRead` over SignalR, display unread incoming alerts as dismissible alert popups (respecting the user's **"Mute Live Alert Popups"** toggle!), expose `markRead(id)`, `markAllRead()` (`POST /api/v1/notifications/mark-all-read` — immediately resets unread count to `0`, clears all active alert popups, and stops notifying for read alerts), and `mutedPopups` / `setMutedPopups`.
       - Add a **Notifications Bell** button (with unread badge count) in `ErpAppLayout.tsx` opening a **Notifications Drawer** with `Unread` / `All` tabs, **"Mark All Read"** (`تحديد الكل كمقروء`) button, **"Mute Live Alert Popups"** (`كتم التنبيهات المنبثقة`) toggle, and clickable `NotificationItem` list.
     - **Change Password & Logout:**
       - Add a **"Change Password"** (`تغيير كلمة المرور`) button in `ErpAppLayout.tsx` sidebar footer alongside **Logout**, opening a validated `DrawerModal` (`currentPassword`, `newPassword`, `confirmNewPassword`) that calls `authService.changePassword(...)`, closes SignalR, clears auth state, and redirects to `/login?passwordChanged=1` (showing a clean success `AlertBanner` on `/login`).
     - **`WH_KEEPER` Single Warehouse UI Enforcement:**
       - In `src/features/users/UsersWorkspace.tsx`: when creating a user with `WH_KEEPER` or editing warehouses of a `WH_KEEPER`, render a single-select warehouse dropdown (auto-setting `defaultWarehouseId = warehouseId` and `assignedWarehouseIds = [warehouseId]`) with a clear localized rule banner.
       - In `src/features/inventory/InventoryWorkspace.tsx`: when the logged-in user is a scoped `WH_KEEPER`, lock the warehouse view strictly to their 1 assigned warehouse (hide multi-warehouse switcher).
     - **Live 10-Second Google Maps GPS Tracking Screen (`/gps`):**
       - Add `/gps` route (`src/app/gps/page.tsx` + `src/features/gps/GpsTrackingWorkspace.tsx`) and sidebar nav item visible **ONLY** when `user.roleCodes` intersects `["SUPER_ADMIN", "EXEC_MGR", "SALES_MGR"]`!
       - Auto-polls `GET /api/v1/gps/live` every **10 seconds** (with pause/resume + manual refresh + 10s countdown badge).
       - Implements **Hybrid Google Maps**: loads Google Maps JS API with live markers & info windows when `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY` is present, and seamlessly renders a live interactive Google Maps embed (`https://maps.google.com/maps?q=LAT,LNG&z=15&output=embed`) + "Open in Google Maps" button for the selected rep when no API key is configured, alongside the live rep list table (name, code, coordinates, accuracy, online status, last ping timestamp, recent route history).
       - Update `apps/web-erp/.env.local.example` documenting optional `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY`.
   - **C. `apps/rep-pwa` Updates:**
     - **Change Password & Logout:** Add a **Change Password** modal/drawer accessible from `RepMobileShell` header menu + polished Logout (disconnects SignalR, clears in-memory JWT, calls `/auth/logout`, redirects to `/login`; after Change Password redirects to `/login?passwordChanged=1` with a localized success banner).
     - **Persistent Notifications + Alert Popups + Notifications Drawer + Mark All Read + Mute Toggle:**
       - Upgrade `AppProviders.tsx` to hydrate `GET /api/v1/notifications`, react to `NotificationReceived` & `NotificationsMarkedAllRead` over SignalR, show unread live alert banners (respecting the **"Mute Live Alert Popups"** toggle), and add a **Notifications Bell** button (with unread badge) in `RepMobileShell.tsx` opening the Notifications Drawer (`Unread` / `All` tabs, **"Mark All Read"**, **"Mute Live Alert Popups"** toggle, and `NotificationItem` list).
     - Do **NOT** add auto-GPS streaming to `apps/rep-pwa` (per confirmed decision A4).
4. **`@QA & Security Auditor`** —
   - Verify `.NET 9` build (0 warnings, 0 errors), SQL Server migration applied cleanly to local `AlMomiaa` DB (`dotnet ef migrations has-pending-model-changes` = zero drift), and all `dotnet test` tests passing.
   - Verify `pnpm -r typecheck`, `pnpm -r lint`, `@almomiaa/ui` build, `@almomiaa/api-client` build & tests, `apps/design-system-studio` `build-storybook` (27 stories), `apps/rep-pwa` production PWA build, and `apps/web-erp` Next.js 15 production build.
   - Verify static security & hygiene gates:
     - Zero references to `ApiActivityProvider` / `JsonResponseViewer` / `فاحص استجابات` in `apps/web-erp/src`.
     - Zero forbidden imports in `packages/ui-kit`, zero hardcoded hex colors in TSX/TS, zero directional Tailwind classes, zero `any`/`@ts-ignore`, zero JWT/refresh-token browser storage, zero committed secrets (including zero hardcoded Google Maps API keys!).
     - Confirm `GET /api/v1/gps/live` returns `403` for non-privileged roles and `200` for `SUPER_ADMIN`, `EXEC_MGR`, `SALES_MGR`.
5. **`@Orchestrator`** — Archive as `docs/agent-ops/prompt-history/0009-change-password-whkeeper-gps-notifications-cleanup.md`, update `docs/agent-ops/PROJECT_LOG.md`, and mark `CURRENT_PROMPT.md` completed only after QA returns `PASS`.

---

## 3. Target Files to Create / Modify

### `apps/backend-api/`
- `src/AlMomiaa.Domain/SystemOps/UserNotification.cs` (new)
- `src/AlMomiaa.Application/Authentication/Commands/ChangePassword/ChangePasswordCommand.cs` (new)
- `src/AlMomiaa.Application/Authentication/Abstractions/IAuthenticationService.cs` (modify — add `ChangePasswordAsync`)
- `src/AlMomiaa.Application/Common/Abstractions/IOperationsRealtimeNotifier.cs` (modify — add `NotifyUserNotificationAsync` & `NotifyAllNotificationsReadAsync`)
- `src/AlMomiaa.Application/Operations/OperationsModule.cs` (modify — add `LiveRepGpsDto`, `RepGpsHistoryPointDto`, `GetLiveRepLocationsQuery`, `GetRepGpsHistoryQuery`)
- `src/AlMomiaa.Application/Notifications/NotificationsModule.cs` (new — `UserNotificationDto`, `NotificationsPageDto`, `MarkAllNotificationsReadResponse`, `GetMyNotificationsQuery`, `MarkNotificationReadCommand`, `MarkAllNotificationsReadCommand`, `INotificationsModuleService`)
- `src/AlMomiaa.Infrastructure/Persistence/AlMomiaaDbContext.cs` (modify — add `DbSet<UserNotification> Notifications`)
- `src/AlMomiaa.Infrastructure/Persistence/Configurations/System/SystemConfigurations.cs` (modify — add `UserNotificationConfiguration` -> `[ops].[Notifications]`)
- `src/AlMomiaa.Infrastructure/Persistence/Migrations/*_AddOpsNotificationsTable.cs` (new — additive migration applied to SQL Server `AlMomiaa`)
- `src/AlMomiaa.Infrastructure/Authentication/AuthenticationService.cs` (modify — implement `ChangePasswordAsync` with stamp rotation & full session revocation)
- `src/AlMomiaa.Infrastructure/Persistence/Services/IamAndCustomersServices.cs` (modify — enforce `WH_KEEPER` strictly 1 warehouse in `CreateUserAsync`, `UpdateUserWarehousesAsync`, `UpdateUserRolesAsync`)
- `src/AlMomiaa.Infrastructure/Persistence/Services/FinanceAndOperationsServices.cs` (modify — implement `GetLiveRepLocationsAsync`, `GetRepGpsHistoryAsync`)
- `src/AlMomiaa.Infrastructure/Persistence/Services/NotificationsModuleService.cs` (new — persistent notifications CRUD + bulk mark-all-read)
- `src/AlMomiaa.Infrastructure/Persistence/Services/ProcurementAndSalesServices.cs` (modify — persist `UserNotification` rows & push `NotificationReceived` on order workflow events)
- `src/AlMomiaa.Infrastructure/Realtime/NoOpOperationsRealtimeNotifier.cs` & `DependencyInjection.cs` (modify)
- `src/AlMomiaa.WebApi/Hubs/SignalROperationsRealtimeNotifier.cs` (modify — dispatch `NotificationReceived` & `NotificationsMarkedAllRead` to `user:{userId}`)
- `src/AlMomiaa.WebApi/Controllers/AuthController.cs` (modify — add `[Authorize] POST /api/v1/auth/change-password`)
- `src/AlMomiaa.WebApi/Controllers/GpsController.cs` (modify — add `[Authorize(Policy = "GpsTrackRoles")] GET /api/v1/gps/live` & `GET /api/v1/gps/reps/{repId:guid}/history`)
- `src/AlMomiaa.WebApi/Controllers/NotificationsController.cs` (new — `GET /api/v1/notifications`, `PATCH /api/v1/notifications/{id:guid}/read`, `POST /api/v1/notifications/mark-all-read`)
- `src/AlMomiaa.WebApi/Program.cs` (modify — add `"GpsTrackRoles"` policy)
- `tests/AlMomiaa.AuthenticationTests/AccountSecurityGpsWarehouseAndNotificationsTests.cs` (new)

### `packages/ui-kit/` & `apps/design-system-studio/`
- `packages/ui-kit/src/components/feedback/NotificationItem.tsx` (new)
- `packages/ui-kit/src/index.ts` (modify — export `NotificationItem` & `NotificationItemProps`)
- `apps/design-system-studio/src/stories/NotificationItem.stories.tsx` (new)
- `apps/design-system-studio/src/stories/Introduction.mdx` (modify — 27 components)

### `packages/api-client/`
- `src/auth/auth.types.ts`, `src/auth/auth.schemas.ts`, `src/auth/auth.service.ts` (modify — add `ChangePasswordRequest` & `changePassword`)
- `src/modules/notifications.service.ts` (new)
- `src/modules/operations.service.ts` (modify — add `getLiveGps` & `getRepGpsHistory`)
- `src/realtime/operations-hub.ts` (modify — add `NotificationReceived` & `NotificationsMarkedAllRead` events)
- `src/index.ts` & `tests/erp-client.test.mjs` (modify)

### `apps/web-erp/`
- `src/providers/ApiActivityProvider.tsx` (DELETE — remove API JSON Inspector)
- `src/lib/api-client.ts` (modify — remove `subscribeApiActivity` / `onApiCall` inspector hooks)
- `src/app/layout.tsx` (modify — remove `ApiActivityProvider`)
- `src/providers/RealtimeProvider.tsx` (modify — integrate persistent notifications, `markRead`, `markAllRead`, `mutedPopups` toggle, SignalR `NotificationReceived` / `NotificationsMarkedAllRead`)
- `src/components/ErpAppLayout.tsx` (modify — remove API Inspector button/drawer, add Notifications Bell & Drawer with Unread/All tabs + Mark All Read + Mute toggle, add Change Password drawer & sidebar button, add role-restricted `/gps` nav item for `SUPER_ADMIN`/`EXEC_MGR`/`SALES_MGR`)
- `src/app/login/page.tsx` & `src/features/auth/LoginForm.tsx` (modify — show localized success banner when `?passwordChanged=1`)
- `src/features/users/UsersWorkspace.tsx` (modify — enforce single-warehouse select for `WH_KEEPER`)
- `src/features/inventory/InventoryWorkspace.tsx` (modify — lock `WH_KEEPER` to their 1 assigned warehouse)
- `src/app/gps/page.tsx` & `src/features/gps/GpsTrackingWorkspace.tsx` (new — 10-second Hybrid Google Maps live GPS tracker strictly for `SUPER_ADMIN`, `EXEC_MGR`, `SALES_MGR`)
- `src/locales/ar.ts`, `src/locales/en.ts`, `.env.local.example` (modify)

### `apps/rep-pwa/`
- `src/providers/AppProviders.tsx` (modify — add persistent notifications, `markRead`, `markAllRead`, `mutedPopups`, `changePassword`)
- `src/components/RepMobileShell.tsx` (modify — add Notifications Bell & Drawer with Unread/All tabs + Mark All Read + Mute toggle, plus Change Password modal/drawer and Logout)
- `src/pages/LoginPage.tsx` (modify — show localized success banner when `?passwordChanged=1`)
- `src/locales/ar.ts`, `src/locales/en.ts` (modify)

### `docs/architecture/`
- `05-Database.md`, `06-API-Specification.md`, `07-Security.md` (sync `[ops].[Notifications]`, `POST /auth/change-password`, `GET /gps/live`, `GET/PATCH/POST /notifications`, and `WH_KEEPER` single-warehouse invariant)

---

## 4. Frontend & Backend Data Schema

### Change Password (`POST /api/v1/auth/change-password`)
```ts
interface ChangePasswordRequest {
  currentPassword: string;     // min 1
  newPassword: string;         // min 8, max 128, != currentPassword
  confirmNewPassword: string;  // must === newPassword
}
```
- Response: `204 No Content` + `Set-Cookie: almomiaa_refresh=; Max-Age=0; ...`

### Persistent Notification (`[ops].[Notifications]`)
```ts
interface UserNotificationDto {
  id: string;
  eventType: string; // OrderSubmitted | OrderApproved | OrderRejected | OrderUpdated | CreditCoApprovalRequired
  severity: "info" | "success" | "warning" | "danger";
  titleAr: string;
  titleEn: string;
  messageAr: string;
  messageEn: string;
  entityName: string | null;
  entityId: string | null;
  isRead: boolean;
  readAtUtc: string | null;
  createdAtUtc: string;
}

interface NotificationsPageDto {
  items: UserNotificationDto[];
  totalCount: number;
  unreadCount: number;
  nextCursor: string | null;
}

interface MarkAllNotificationsReadResponse {
  markedCount: number;
  unreadCount: number; // always 0
}
```

### Live Rep GPS (`GET /api/v1/gps/live` — `SUPER_ADMIN`, `EXEC_MGR`, `SALES_MGR` ONLY)
```ts
interface LiveRepGpsDto {
  repId: string;
  employeeCode: string;
  fullNameAr: string;
  fullNameEn: string | null;
  phone: string;
  latitude: number;
  longitude: number;
  accuracyMeters: number | null;
  nearCustomerId: string | null;
  recordedAtUtc: string;
  isOnlineRecent: boolean;
}
```

---

## 5. Security, Design System & Operational Rules

1. **Zero Password Leakage:** Never log `currentPassword`, `newPassword`, or `confirmNewPassword` in structured logs, audit logs, telemetry, or error responses.
2. **Immediate Session Invalidation on Password Change:** Rotate `SecurityStamp` in DB + `ISecurityStampStore` and revoke all `RefreshTokenSessions` inside the same transaction so stolen JWTs and refresh cookies fail immediately (`401`).
3. **Strict Role Gate on GPS Tracking:** `GET /api/v1/gps/live` and `GET /api/v1/gps/reps/{repId}/history` enforce `"GpsTrackRoles"` (`SUPER_ADMIN`, `EXEC_MGR`, `SALES_MGR` ONLY); UI hides `/gps` nav link and shows `403 Access Restricted` for any other role.
4. **Strict `WH_KEEPER` Single-Warehouse Invariant:** Enforced at API service layer (`CreateUser`, `UpdateUserWarehouses`, `UpdateUserRoles`) AND UI (`/users`, `/inventory`).
5. **Notification Isolation:** Users can ONLY read or mark read notifications where `RecipientUserId == CurrentUser.UserId`; `POST /api/v1/notifications/mark-all-read` updates `IsRead = true` in SQL Server so read notifications never re-trigger unread badges or live alert popups.
6. **Zero Hardcoded Secrets & Strict Design Tokens:** Never hardcode Google Maps API keys (use optional `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY` env var); zero hardcoded hex colors in TSX/TS; zero directional Tailwind utilities (`ml-`/`mr-`/`pl-`/`pr-`/`left-`/`right-`/`text-left`/`text-right`); zero `any`/`@ts-ignore`; zero JWT storage in `localStorage`/`sessionStorage`/IndexedDB (note: the non-sensitive boolean UI preference `mutedPopups` may live in memory or non-auth UI preference, never tokens!).

---

## 6. Acceptance Criteria (Definition of Done)

- [x] `POST /api/v1/auth/change-password` validates current/new password, rotates `SecurityStamp`, revokes all refresh sessions, clears refresh cookie, audits the action without secrets, and both `apps/web-erp` and `apps/rep-pwa` provide Change Password + Logout flows that disconnect SignalR and redirect to `/login`.
- [x] `WH_KEEPER` is strictly enforced to have **exactly 1 warehouse** across Backend (`POST /users`, `PUT /users/{id}/warehouses`, `PUT /users/{id}/roles` reject 0 or >1 with `422`) and `apps/web-erp` (`/users` & `/inventory`).
- [x] `GET /api/v1/gps/live` and `GET /api/v1/gps/reps/{repId}/history` are strictly restricted to `SUPER_ADMIN`, `EXEC_MGR`, and `SALES_MGR` (`403` for all other roles), and `apps/web-erp` `/gps` renders the 10-second auto-refreshing Hybrid Google Maps tracker exclusively for those 3 roles.
- [x] The API JSON Inspector (`فاحص استجابات نقاط النهاية الحية` / `ApiActivityProvider` / `JsonResponseViewer` drawer) is completely removed from `apps/web-erp`.
- [x] `[ops].[Notifications]` migration is generated and applied to local SQL Server `AlMomiaa`; alerts persist and stream live via SignalR; `GET /api/v1/notifications`, `PATCH /api/v1/notifications/{id}/read`, and `POST /api/v1/notifications/mark-all-read` work end-to-end; both `apps/web-erp` and `apps/rep-pwa` display live alert popups, a Notifications Bell/Drawer (`Unread`/`All` tabs), **"Mark All Read"** (clearing unread count and preventing re-notification), and a **"Mute Live Alert Popups"** toggle.
- [x] `NotificationItem` is exported from `@almomiaa/ui` (27 total components) with a CSF3 Storybook 8 story (27 stories) and clean `build-storybook`.
- [x] All backend builds (`0` warnings/errors), backend tests (`22+` including new `AccountSecurityGpsWarehouseAndNotificationsTests`), `@almomiaa/api-client` tests, `pnpm -r typecheck`, `pnpm -r lint`, `apps/rep-pwa` PWA build, `apps/web-erp` Next.js 15 build, and `@QA & Security Auditor` gate return **`PASS`**, after which `@Orchestrator` archives this prompt as `docs/agent-ops/prompt-history/0009-change-password-whkeeper-gps-notifications-cleanup.md`, updates `docs/agent-ops/PROJECT_LOG.md`, and marks `CURRENT_PROMPT.md` completed.
