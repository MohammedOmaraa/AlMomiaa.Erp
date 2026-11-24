"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { RoleDto, UserDetailDto, UserSummaryDto, WarehouseDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
  KeyValueList,
  PaginationBar,
  PasswordField,
  SelectField,
  SurfaceCard,
  Tabs,
  TextField,
  type DataTableColumn,
} from "@almomiaa/ui";
import { ErpAppLayout } from "../../components/ErpAppLayout";
import { erpApi, formatApiError } from "../../lib/api-client";
import { formatDateTime } from "../../lib/formatters";
import { hasPermission } from "../../lib/permissions";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";

const CANONICAL_PERMISSIONS = [
  "permissions:manage", "users:manage", "settings:manage",
  "dashboards:executive", "dashboards:profit", "dashboards:sales", "dashboards:finance", "dashboards:inventory",
  "customers:view", "customers:manage", "catalog:view", "catalog:manage",
  "orders:create", "orders:edit_submitted", "orders:override_price", "orders:coapprove_credit", "orders:approve",
  "warehouse:ship", "delivery:dispatch", "delivery:update_pod",
  "invoices:view", "invoices:upload_copy", "sales:returns",
  "collections:create", "finance:confirm_treasury", "finance:verify_bank", "finance:close_period",
  "procurement:manage", "procurement:receive",
  "inventory:view", "inventory:transfer", "inventory:adjust",
  "audit:view", "gps:stream",
];

export function UsersWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const isAr = locale === "ar-SA";

  const [activeTab, setActiveTab] = useState("employees");
  const [users, setUsers] = useState<UserSummaryDto[]>([]);
  const [totalUsers, setTotalUsers] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [banner, setBanner] = useState<{ tone: "success" | "danger"; title: string; message: string; details?: string[] } | null>(null);

  // Drawers
  const [createUserOpen, setCreateUserOpen] = useState(false);
  const [detailUser, setDetailUser] = useState<UserDetailDto | null>(null);
  const [editRolesTarget, setEditRolesTarget] = useState<UserSummaryDto | null>(null);
  const [selectedRoleIds, setSelectedRoleIds] = useState<number[]>([]);
  const [editWhTarget, setEditWhTarget] = useState<UserSummaryDto | null>(null);
  const [defaultWhInput, setDefaultWhInput] = useState("");
  const [assignedWhInput, setAssignedWhInput] = useState("");

  const [createRoleOpen, setCreateRoleOpen] = useState(false);
  const [editPermsRole, setEditPermsRole] = useState<RoleDto | null>(null);
  const [selectedPerms, setSelectedPerms] = useState<string[]>([]);

  // Create user form
  const [empCode, setEmpCode] = useState("");
  const [fullNameAr, setFullNameAr] = useState("");
  const [fullNameEn, setFullNameEn] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [repType, setRepType] = useState("");
  const [newUserRoleId, setNewUserRoleId] = useState("4");
  const [newUserWarehouseId, setNewUserWarehouseId] = useState("");
  const [createVan, setCreateVan] = useState(false);
  const [plateNo, setPlateNo] = useState("");

  // Create role form
  const [roleCode, setRoleCode] = useState("");
  const [roleNameAr, setRoleNameAr] = useState("");
  const [roleNameEn, setRoleNameEn] = useState("");
  const [newRolePerms, setNewRolePerms] = useState<string[]>(["catalog:view", "customers:view"]);

  const [submitting, setSubmitting] = useState(false);

  const canManagePerms = hasPermission(user, "permissions:manage");
  const canManageUsers = hasPermission(user, "users:manage");

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const tasks: Promise<void>[] = [];
      if (canManageUsers) {
        tasks.push(
          erpApi.users.getUsers({ search: search || undefined, page, pageSize: 25 }).then((res) => {
            setUsers(res.items);
            setTotalUsers(res.totalCount);
          })
        );
      }
      if (canManagePerms) {
        tasks.push(erpApi.users.getRoles().then(setRoles));
      }
      tasks.push(
        erpApi.inventory
          .getWarehouses()
          .then((list) => {
            setWarehouses(list);
            setNewUserWarehouseId((prev) => prev || (list[0] ? String(list[0].id) : ""));
          })
          .catch(() => undefined),
      );
      await Promise.all(tasks);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [canManagePerms, canManageUsers, page, search]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const selectedNewRole = roles.find((r) => String(r.id) === newUserRoleId);
  const isCreatingWhKeeper = selectedNewRole?.roleCode === "WH_KEEPER";

  const handleCreateUser = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setBanner(null);
    try {
      const singleWhId = newUserWarehouseId ? Number(newUserWarehouseId) : null;
      if (isCreatingWhKeeper && (!singleWhId || singleWhId <= 0)) {
        setBanner({
          tone: "danger",
          title: isAr ? "مطلوب مستودع واحد" : "Single Warehouse Required",
          message: isAr
            ? "يجب اختيار مستودع واحد بالضبط لأمين المستودع (WH_KEEPER)."
            : "Warehouse Keeper (WH_KEEPER) must be assigned to exactly one warehouse.",
        });
        setSubmitting(false);
        return;
      }

      const created = await erpApi.users.createUser({
        employeeCode: empCode,
        fullNameAr,
        fullNameEn: fullNameEn || null,
        phone,
        email: email || null,
        password,
        repType: repType || null,
        defaultWarehouseId: singleWhId,
        assignedWarehouseIds: singleWhId ? [singleWhId] : [],
        roleIds: [Number(newUserRoleId)],
        createMobileVan: isCreatingWhKeeper ? false : createVan,
        vehiclePlateNumber: plateNo || null,
      });
      setBanner({ tone: "success", title: isAr ? "تم إنشاء الموظف" : "Employee Created", message: `${created.employeeCode} (${created.fullNameAr})` });
      setCreateUserOpen(false);
      setEmpCode("");
      setFullNameAr("");
      setFullNameEn("");
      setPhone("");
      setEmail("");
      setPassword("");
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleStatus = async (u: UserSummaryDto) => {
    try {
      await erpApi.users.updateUserStatus(u.id, !u.isActive);
      setBanner({ tone: "success", title: isAr ? "تم تحديث الحالة" : "Status Updated", message: `${u.employeeCode} -> ${!u.isActive ? "Active" : "Inactive"}` });
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    }
  };

  const handleInspectUser = async (id: string) => {
    try {
      const detail = await erpApi.users.getUserById(id);
      setDetailUser(detail);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    }
  };

  const handleSaveUserRoles = async () => {
    if (!editRolesTarget) return;
    setSubmitting(true);
    try {
      const updated = await erpApi.users.updateUserRoles(editRolesTarget.id, selectedRoleIds);
      setBanner({ tone: "success", title: isAr ? "تم تحديث الأدوار" : "Roles Updated", message: `${updated.employeeCode}: ${updated.roleCodes.join(", ")}` });
      setEditRolesTarget(null);
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const isEditingWhKeeper = Boolean(editWhTarget?.roleCodes.includes("WH_KEEPER"));

  const handleSaveUserWarehouses = async () => {
    if (!editWhTarget) return;
    setSubmitting(true);
    try {
      let parsedDefault: number | null;
      let parsedAssigned: number[];
      if (isEditingWhKeeper) {
        const single = Number(defaultWhInput.trim());
        if (!Number.isInteger(single) || single <= 0) {
          setBanner({
            tone: "danger",
            title: isAr ? "مطلوب مستودع واحد" : "Single Warehouse Required",
            message: isAr
              ? "أمين المستودع (WH_KEEPER) يجب أن يرتبط بمستودع واحد فقط."
              : "Warehouse Keeper (WH_KEEPER) must be assigned to exactly one warehouse.",
          });
          setSubmitting(false);
          return;
        }
        parsedDefault = single;
        parsedAssigned = [single];
      } else {
        parsedAssigned = assignedWhInput
          .split(",")
          .map((s) => Number(s.trim()))
          .filter((n) => Number.isInteger(n) && n > 0);
        parsedDefault = defaultWhInput.trim() ? Number(defaultWhInput.trim()) : null;
      }
      await erpApi.users.updateUserWarehouses(editWhTarget.id, parsedDefault, parsedAssigned);
      setBanner({ tone: "success", title: isAr ? "تم تحديث المستودعات" : "Warehouses Updated", message: editWhTarget.employeeCode });
      setEditWhTarget(null);
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateRole = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      const created = await erpApi.users.createRole({
        roleCode,
        nameAr: roleNameAr,
        nameEn: roleNameEn,
        permissions: newRolePerms,
      });
      setBanner({ tone: "success", title: isAr ? "تم إنشاء الدور المخصص" : "Custom Role Created", message: `${created.roleCode} (#${created.id})` });
      setCreateRoleOpen(false);
      setRoleCode("");
      setRoleNameAr("");
      setRoleNameEn("");
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleSaveRolePerms = async () => {
    if (!editPermsRole) return;
    setSubmitting(true);
    try {
      await erpApi.users.updateRolePermissions(editPermsRole.id, selectedPerms);
      setBanner({ tone: "success", title: isAr ? "تم تحديث الصلاحيات" : "Permissions Updated", message: editPermsRole.roleCode });
      setEditPermsRole(null);
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const userColumns: DataTableColumn<UserSummaryDto>[] = [
    { key: "code", header: isAr ? "الرمز" : "Code", mono: true, render: (u) => u.employeeCode },
    { key: "name", header: isAr ? "الاسم" : "Name", render: (u) => (isAr ? u.fullNameAr : (u.fullNameEn ?? u.fullNameAr)) },
    { key: "phone", header: isAr ? "الجوال" : "Phone", mono: true, render: (u) => <span dir="ltr">{u.phone}</span> },
    {
      key: "roles",
      header: isAr ? "الأدوار" : "Roles",
      render: (u) => (
        <div className="flex flex-wrap gap-1">
          {u.roleCodes.map((r) => (
            <Badge key={r} tone="gold" mono>{r}</Badge>
          ))}
        </div>
      ),
    },
    {
      key: "status",
      header: isAr ? "الحالة" : "Status",
      render: (u) => <Badge tone={u.isActive ? "success" : "danger"}>{u.isActive ? (isAr ? "نشط" : "Active") : (isAr ? "معطل" : "Inactive")}</Badge>,
    },
    {
      key: "actions",
      header: isAr ? "إجراءات" : "Actions",
      render: (u) => (
        <div className="flex flex-wrap gap-1.5">
          <Button variant="secondary" size="compact" onClick={() => void handleInspectUser(u.id)}>
            {isAr ? "الصلاحيات" : "Inspect"}
          </Button>
          {canManagePerms && (
            <Button
              variant="secondary"
              size="compact"
              onClick={() => {
                setEditRolesTarget(u);
                setSelectedRoleIds(roles.filter((r) => u.roleCodes.includes(r.roleCode)).map((r) => r.id));
              }}
            >
              {isAr ? "الأدوار" : "Roles"}
            </Button>
          )}
          <Button
            variant="ghost"
            size="compact"
            onClick={() => {
              setEditWhTarget(u);
              setDefaultWhInput(u.defaultWarehouseId ? String(u.defaultWarehouseId) : "");
              setAssignedWhInput(u.assignedWarehouseIds.join(", "));
            }}
          >
            {isAr ? "المستودعات" : "Warehouses"}
          </Button>
          <Button variant={u.isActive ? "danger" : "primary"} size="compact" onClick={() => void handleToggleStatus(u)}>
            {u.isActive ? (isAr ? "تعطيل" : "Disable") : (isAr ? "تفعيل" : "Enable")}
          </Button>
        </div>
      ),
    },
  ];

  const roleColumns: DataTableColumn<RoleDto>[] = [
    { key: "id", header: "ID", mono: true, render: (r) => r.id },
    { key: "code", header: isAr ? "رمز الدور" : "Role Code", mono: true, render: (r) => <Badge tone="gold" mono>{r.roleCode}</Badge> },
    { key: "name", header: isAr ? "الاسم" : "Name", render: (r) => (isAr ? r.nameAr : r.nameEn) },
    { key: "count", header: isAr ? "الصلاحيات" : "Claims", numeric: true, render: (r) => r.permissions.length },
    {
      key: "actions",
      header: isAr ? "إجراءات" : "Actions",
      render: (r) => (
        <Button
          variant="secondary"
          size="compact"
          onClick={() => {
            setEditPermsRole(r);
            setSelectedPerms([...r.permissions]);
          }}
        >
          {isAr ? "تعديل الصلاحيات" : "Edit Claims"}
        </Button>
      ),
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "المستخدمون والأدوار والصلاحيات (IAM)" : "Users, Roles & Permissions (IAM)"}
      subtitle={isAr ? "إنشاء الموظفين والأدوار المخصصة وتعيين الصلاحيات والمستودعات مع تحديث فوري للجلسة" : "Manage employees, custom roles, permissions, and warehouse assignments"}
      requiredPermissions={["users:manage", "permissions:manage"]}
      headerActions={
        <div className="flex flex-wrap gap-2">
          {canManageUsers && <Button size="compact" onClick={() => setCreateUserOpen(true)}>{isAr ? "+ موظف جديد" : "+ New User"}</Button>}
          {canManagePerms && <Button variant="secondary" size="compact" onClick={() => setCreateRoleOpen(true)}>{isAr ? "+ دور مخصص" : "+ Custom Role"}</Button>}
          <Button variant="ghost" size="compact" onClick={() => void loadData()}>{isAr ? "تحديث" : "Refresh"}</Button>
        </div>
      }
    >
      {banner && <AlertBanner tone={banner.tone} title={banner.title} message={banner.message} details={banner.details} onDismiss={() => setBanner(null)} />}

      <Tabs
        activeId={activeTab}
        onChange={setActiveTab}
        items={[
          { id: "employees", label: isAr ? "الموظفون" : "Employees", badge: totalUsers },
          { id: "roles", label: isAr ? "الأدوار والصلاحيات" : "Roles & Permissions", badge: roles.length, disabled: !canManagePerms },
        ]}
      />

      {activeTab === "employees" && (
        <SurfaceCard title={isAr ? "قائمة الموظفين" : "Employee Directory"}>
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-2">
              <div className="w-full sm:w-72">
                <TextField label={isAr ? "بحث بالاسم أو الرمز أو الجوال" : "Search employees"} value={search} onChange={(e) => setSearch(e.target.value)} />
              </div>
              <Button variant="secondary" onClick={() => void loadData()}>{isAr ? "بحث" : "Search"}</Button>
            </div>
            <DataTable columns={userColumns} rows={users} rowKey={(u) => u.id} loading={loading} />
            <PaginationBar page={page} pageSize={25} totalCount={totalUsers} onPageChange={setPage} />
          </div>
        </SurfaceCard>
      )}

      {activeTab === "roles" && (
        <SurfaceCard title={isAr ? "الأدوار ومصفوفة الصلاحيات" : "Roles & Permission Matrix"}>
          <DataTable columns={roleColumns} rows={roles} rowKey={(r) => String(r.id)} loading={loading} />
        </SurfaceCard>
      )}

      <DrawerModal
        open={createUserOpen}
        onClose={() => setCreateUserOpen(false)}
        title={isAr ? "إنشاء موظف جديد" : "Create Employee"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setCreateUserOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-user-form" loading={submitting}>{isAr ? "إنشاء" : "Create"}</Button>
          </>
        }
      >
        <form id="create-user-form" onSubmit={handleCreateUser} className="space-y-3">
          <TextField label={isAr ? "الرقم الوظيفي" : "Employee Code"} value={empCode} onChange={(e) => setEmpCode(e.target.value)} required placeholder="REP-2026-01" />
          <TextField label={isAr ? "الاسم بالعربية" : "Full Name (AR)"} value={fullNameAr} onChange={(e) => setFullNameAr(e.target.value)} required />
          <TextField label={isAr ? "الاسم بالإنجليزية" : "Full Name (EN)"} value={fullNameEn} onChange={(e) => setFullNameEn(e.target.value)} />
          <TextField label={isAr ? "رقم الجوال" : "Phone"} value={phone} onChange={(e) => setPhone(e.target.value)} required placeholder="+9665XXXXXXXX" />
          <TextField label={isAr ? "البريد الإلكتروني" : "Email"} value={email} onChange={(e) => setEmail(e.target.value)} />
          <PasswordField label={isAr ? "كلمة المرور" : "Password"} value={password} onChange={(e) => setPassword(e.target.value)} required hint={isAr ? "8 أحرف على الأقل: كبير + صغير + رقم + رمز" : "Min 8 chars: upper + lower + digit + symbol"} />
          <SelectField
            label={isAr ? "الدور الأساسي" : "Primary Role"}
            value={newUserRoleId}
            onChange={(e) => setNewUserRoleId(e.target.value)}
            options={roles.length > 0 ? roles.map((r) => ({ value: String(r.id), label: `${r.roleCode} — ${isAr ? r.nameAr : r.nameEn}` })) : [{ value: "4", label: "SALES_REP" }]}
          />
          {isCreatingWhKeeper && (
            <AlertBanner
              tone="warning"
              message={
                isAr
                  ? "أمين المستودع (WH_KEEPER) يجب أن يرتبط بمستودع واحد فقط."
                  : "Warehouse Keeper (WH_KEEPER) must be assigned to exactly one warehouse."
              }
            />
          )}
          <SelectField
            label={
              isCreatingWhKeeper
                ? isAr
                  ? "المستودع الوحيد المخصص (إلزامي لـ WH_KEEPER)"
                  : "Single Assigned Warehouse (Required for WH_KEEPER)"
                : isAr
                  ? "المستودع الافتراضي (اختياري)"
                  : "Default Warehouse (Optional)"
            }
            value={newUserWarehouseId}
            onChange={(e) => setNewUserWarehouseId(e.target.value)}
            placeholderOption={isCreatingWhKeeper ? undefined : isAr ? "غير محدد" : "None"}
            options={warehouses.map((w) => ({
              value: String(w.id),
              label: `#${w.id} — ${w.warehouseCode} (${isAr ? w.nameAr : (w.nameEn ?? w.nameAr)})`,
            }))}
          />
          <SelectField
            label={isAr ? "نوع المندوب (اختياري)" : "Rep Type (Optional)"}
            value={repType}
            onChange={(e) => setRepType(e.target.value)}
            placeholderOption={isAr ? "غير محدد" : "None"}
            options={[
              { value: "Wholesale", label: "Wholesale" },
              { value: "Retail", label: "Retail" },
              { value: "CashVan", label: "CashVan" },
            ]}
          />
          <label className="flex items-center gap-2 text-sm text-text-primary">
            <input type="checkbox" checked={createVan} onChange={(e) => setCreateVan(e.target.checked)} />
            <span>{isAr ? "إنشاء مستودع سيارة متنقلة (MobileVan)" : "Auto-provision MobileVan warehouse"}</span>
          </label>
          {createVan && <TextField label={isAr ? "رقم اللوحة" : "Vehicle Plate"} value={plateNo} onChange={(e) => setPlateNo(e.target.value)} />}
        </form>
      </DrawerModal>

      <DrawerModal
        open={detailUser !== null}
        onClose={() => setDetailUser(null)}
        title={detailUser ? `${detailUser.employeeCode} — ${isAr ? detailUser.fullNameAr : (detailUser.fullNameEn ?? detailUser.fullNameAr)}` : ""}
        subtitle={isAr ? "الصلاحيات الفعلية المحسوبة من جميع الأدوار المسندة" : "Computed effective permissions across all assigned roles"}
      >
        {detailUser && (
          <div className="space-y-4">
            <KeyValueList
              items={[
                { id: "id", label: "User ID", value: detailUser.id, mono: true, dir: "ltr" },
                { id: "phone", label: isAr ? "الجوال" : "Phone", value: detailUser.phone, mono: true, dir: "ltr" },
                { id: "roles", label: isAr ? "الأدوار" : "Roles", value: detailUser.roleCodes.join(", "), mono: true },
                { id: "wh", label: isAr ? "المستودعات" : "Warehouses", value: detailUser.assignedWarehouseIds.join(", ") || "—", mono: true },
                { id: "created", label: isAr ? "تاريخ الإنشاء" : "Created", value: formatDateTime(detailUser.createdAtUtc, locale) },
              ]}
            />
            <div>
              <h3 className="mb-2 text-sm font-semibold text-text-primary">
                {isAr ? "الصلاحيات الفعلية" : "Effective Permissions"} ({detailUser.effectivePermissions.length})
              </h3>
              <div className="flex flex-wrap gap-1.5" dir="ltr">
                {detailUser.effectivePermissions.map((perm) => (
                  <Badge key={perm} tone="info" mono>{perm}</Badge>
                ))}
              </div>
            </div>
          </div>
        )}
      </DrawerModal>

      <DrawerModal
        open={editRolesTarget !== null}
        onClose={() => setEditRolesTarget(null)}
        title={isAr ? "تعديل أدوار الموظف" : "Assign User Roles"}
        subtitle={editRolesTarget?.employeeCode}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditRolesTarget(null)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button loading={submitting} onClick={() => void handleSaveUserRoles()}>{isAr ? "حفظ الأدوار" : "Save Roles"}</Button>
          </>
        }
      >
        <div className="space-y-2">
          {roles.map((r) => (
            <label key={r.id} className="flex items-center justify-between rounded-lg border border-surface-border bg-surface-canvas px-3 py-2.5 text-sm">
              <span className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={selectedRoleIds.includes(r.id)}
                  onChange={(e) =>
                    setSelectedRoleIds((prev) =>
                      e.target.checked ? [...prev, r.id] : prev.filter((id) => id !== r.id)
                    )
                  }
                />
                <span className="font-semibold text-text-primary">{isAr ? r.nameAr : r.nameEn}</span>
              </span>
              <Badge tone="gold" mono>{r.roleCode}</Badge>
            </label>
          ))}
        </div>
      </DrawerModal>

      <DrawerModal
        open={editWhTarget !== null}
        onClose={() => setEditWhTarget(null)}
        title={isAr ? "تعيين مستودعات الموظف" : "Assign User Warehouses"}
        subtitle={editWhTarget?.employeeCode}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditWhTarget(null)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button loading={submitting} onClick={() => void handleSaveUserWarehouses()}>{isAr ? "حفظ" : "Save"}</Button>
          </>
        }
      >
        <div className="space-y-3">
          {isEditingWhKeeper ? (
            <>
              <AlertBanner
                tone="warning"
                message={
                  isAr
                    ? "سياسة النظام: أمين المستودع (WH_KEEPER) يرتبط بمستودع واحد فقط ولا يمكن إسناد أكثر من مستودع له."
                    : "Policy: Warehouse Keeper (WH_KEEPER) must be assigned to strictly one warehouse."
                }
              />
              {warehouses.length > 0 ? (
                <SelectField
                  label={isAr ? "المستودع الوحيد المخصص" : "Single Assigned Warehouse"}
                  value={defaultWhInput}
                  onChange={(e) => {
                    setDefaultWhInput(e.target.value);
                    setAssignedWhInput(e.target.value);
                  }}
                  options={warehouses.map((w) => ({
                    value: String(w.id),
                    label: `#${w.id} — ${w.warehouseCode} (${isAr ? w.nameAr : (w.nameEn ?? w.nameAr)})`,
                  }))}
                />
              ) : (
                <TextField
                  label={isAr ? "معرف المستودع الوحيد (ID)" : "Single Warehouse ID"}
                  value={defaultWhInput}
                  onChange={(e) => {
                    setDefaultWhInput(e.target.value);
                    setAssignedWhInput(e.target.value);
                  }}
                  placeholder="1"
                />
              )}
            </>
          ) : (
            <>
              <TextField label={isAr ? "المستودع الافتراضي (ID)" : "Default Warehouse ID"} value={defaultWhInput} onChange={(e) => setDefaultWhInput(e.target.value)} placeholder="1" />
              <TextField label={isAr ? "المستودعات المسندة (أرقام مفصولة بفاصلة)" : "Assigned Warehouse IDs (comma-separated)"} value={assignedWhInput} onChange={(e) => setAssignedWhInput(e.target.value)} placeholder="1, 2" />
            </>
          )}
        </div>
      </DrawerModal>

      <DrawerModal
        open={createRoleOpen}
        onClose={() => setCreateRoleOpen(false)}
        title={isAr ? "إنشاء دور مخصص بصلاحيات محددة" : "Create Custom Role"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setCreateRoleOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-role-form" loading={submitting}>{isAr ? "إنشاء الدور" : "Create Role"}</Button>
          </>
        }
      >
        <form id="create-role-form" onSubmit={handleCreateRole} className="space-y-3">
          <TextField label={isAr ? "رمز الدور" : "Role Code"} value={roleCode} onChange={(e) => setRoleCode(e.target.value)} required placeholder="CUSTOM_AUDITOR" />
          <TextField label={isAr ? "الاسم بالعربية" : "Name (AR)"} value={roleNameAr} onChange={(e) => setRoleNameAr(e.target.value)} required />
          <TextField label={isAr ? "الاسم بالإنجليزية" : "Name (EN)"} value={roleNameEn} onChange={(e) => setRoleNameEn(e.target.value)} required />
          <div className="space-y-1.5">
            <p className="text-sm font-semibold text-text-primary">{isAr ? "اختر الصلاحيات" : "Select Canonical Permissions"}</p>
            <div className="grid grid-cols-1 gap-1.5 sm:grid-cols-2" dir="ltr">
              {CANONICAL_PERMISSIONS.map((claim) => (
                <label key={claim} className="flex items-center gap-2 rounded-md border border-surface-border px-2.5 py-1.5 font-mono text-xs text-text-primary">
                  <input
                    type="checkbox"
                    checked={newRolePerms.includes(claim)}
                    onChange={(e) =>
                      setNewRolePerms((prev) =>
                        e.target.checked ? [...prev, claim] : prev.filter((c) => c !== claim)
                      )
                    }
                  />
                  <span>{claim}</span>
                </label>
              ))}
            </div>
          </div>
        </form>
      </DrawerModal>

      <DrawerModal
        open={editPermsRole !== null}
        onClose={() => setEditPermsRole(null)}
        title={isAr ? "تعديل صلاحيات الدور" : "Edit Role Permissions"}
        subtitle={editPermsRole?.roleCode}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditPermsRole(null)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button loading={submitting} onClick={() => void handleSaveRolePerms()}>{isAr ? "حفظ الصلاحيات" : "Save Permissions"}</Button>
          </>
        }
      >
        <div className="grid grid-cols-1 gap-1.5 sm:grid-cols-2" dir="ltr">
          {CANONICAL_PERMISSIONS.map((claim) => (
            <label key={claim} className="flex items-center gap-2 rounded-md border border-surface-border px-2.5 py-1.5 font-mono text-xs text-text-primary">
              <input
                type="checkbox"
                checked={selectedPerms.includes(claim)}
                onChange={(e) =>
                  setSelectedPerms((prev) =>
                    e.target.checked ? [...prev, claim] : prev.filter((c) => c !== claim)
                  )
                }
              />
              <span>{claim}</span>
            </label>
          ))}
        </div>
      </DrawerModal>
    </ErpAppLayout>
  );
}
