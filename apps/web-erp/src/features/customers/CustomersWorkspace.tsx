"use client";

import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import type {
  CustomerCreditStatusDto,
  CustomerDto,
  CustomerGradeDto,
  SalesRepOptionDto,
} from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
  KeyValueList,
  PaginationBar,
  SelectField,
  SurfaceCard,
  Tabs,
  TextField,
  type DataTableColumn,
} from "@almomiaa/ui";
import { ErpAppLayout } from "../../components/ErpAppLayout";
import { erpApi, formatApiError } from "../../lib/api-client";
import { formatDateTime, formatSar } from "../../lib/formatters";
import { hasPermission } from "../../lib/permissions";
import { useAuth } from "../../providers/AuthProvider";
import { useLocale } from "../../providers/LocaleProvider";

export function CustomersWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const isAr = locale === "ar-SA";

  const [tab, setTab] = useState("list");
  const [customers, setCustomers] = useState<CustomerDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [repFilterId, setRepFilterId] = useState("");
  const [grades, setGrades] = useState<CustomerGradeDto[]>([]);
  const [salesReps, setSalesReps] = useState<SalesRepOptionDto[]>([]);
  const [creditStatus, setCreditStatus] = useState<CustomerCreditStatusDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{
    tone: "success" | "danger";
    title: string;
    message: string;
    details?: string[];
  } | null>(null);

  // Create customer form
  const [createOpen, setCreateOpen] = useState(false);
  const [code, setCode] = useState("");
  const [nameAr, setNameAr] = useState("");
  const [nameEn, setNameEn] = useState("");
  const [phone, setPhone] = useState("");
  const [address, setAddress] = useState("");
  const [city, setCity] = useState("Riyadh");
  const [category, setCategory] = useState("Supermarket");
  const [vatNumber, setVatNumber] = useState("");
  const [gradeId, setGradeId] = useState("2");
  const [creditLimit, setCreditLimit] = useState("50000");
  const [assignedRepId, setAssignedRepId] = useState("");

  // Single Reassign Rep Drawer
  const [reassignTarget, setReassignTarget] = useState<CustomerDto | null>(null);
  const [reassignRepId, setReassignRepId] = useState("");

  // Bulk Link 1 Rep -> 1 or More Customers Drawer
  const [bulkOpen, setBulkOpen] = useState(false);
  const [bulkRepId, setBulkRepId] = useState("");
  const [bulkSearch, setBulkSearch] = useState("");
  const [bulkSelectedIds, setBulkSelectedIds] = useState<string[]>([]);
  const [allCustomersForBulk, setAllCustomersForBulk] = useState<CustomerDto[]>([]);

  const canManage = hasPermission(user, "customers:manage");
  const canEditGrades = hasPermission(user, "settings:manage");

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [custRes, gradeRes, repsRes] = await Promise.all([
        erpApi.customers.getCustomers({ search: search || undefined, page, pageSize: 25 }),
        erpApi.customers.getGrades(),
        erpApi.customers.getSalesReps().catch(() => [] as SalesRepOptionDto[]),
      ]);
      setCustomers(custRes.items);
      setTotal(custRes.totalCount);
      setGrades(gradeRes);
      setSalesReps(repsRes);

      if (!assignedRepId) {
        if (repsRes[0]) {
          setAssignedRepId(repsRes[0].id);
        } else if (user) {
          setAssignedRepId(user.id);
        }
      }
      if (!bulkRepId && repsRes[0]) {
        setBulkRepId(repsRes[0].id);
      }
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [assignedRepId, bulkRepId, page, search, user]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const filteredCustomers = useMemo(() => {
    if (!repFilterId) return customers;
    return customers.filter((c) => c.assignedRepId === repFilterId);
  }, [customers, repFilterId]);

  const bulkFilteredCustomers = useMemo(() => {
    const term = bulkSearch.trim().toLowerCase();
    if (!term) return allCustomersForBulk;
    return allCustomersForBulk.filter(
      (c) =>
        c.customerCode.toLowerCase().includes(term)
        || c.nameAr.toLowerCase().includes(term)
        || (c.nameEn ?? "").toLowerCase().includes(term)
        || c.city.toLowerCase().includes(term),
    );
  }, [allCustomersForBulk, bulkSearch]);

  const repSelectOptions = useMemo(
    () =>
      salesReps.map((rep) => ({
        value: rep.id,
        label: isAr
          ? `${rep.employeeCode} — ${rep.fullNameAr} (${rep.assignedCustomersCount} عميل)`
          : `${rep.employeeCode} — ${rep.fullNameEn ?? rep.fullNameAr} (${rep.assignedCustomersCount} customers)`,
      })),
    [isAr, salesReps],
  );

  const handleOpenBulkDrawer = async () => {
    setBulkOpen(true);
    setBulkSelectedIds([]);
    setBulkSearch("");
    if (!bulkRepId && salesReps[0]) {
      setBulkRepId(salesReps[0].id);
    }
    try {
      const res = await erpApi.customers.getCustomers({ page: 1, pageSize: 200 });
      setAllCustomersForBulk(res.items);
    } catch {
      setAllCustomersForBulk(customers);
    }
  };

  const handleCreateCustomer = async (e: FormEvent) => {
    e.preventDefault();
    if (!user) return;
    const effectiveRepId = assignedRepId || user.id;
    setSubmitting(true);
    setBanner(null);
    try {
      const created = await erpApi.customers.createCustomer({
        customerCode: code,
        nameAr,
        nameEn: nameEn || null,
        phone,
        address,
        city: city || "Riyadh",
        category,
        vatNumber: vatNumber || null,
        gradeId: Number(gradeId),
        creditLimit: Number(creditLimit),
        assignedRepId: effectiveRepId,
      });
      setBanner({
        tone: "success",
        title: isAr ? "تم إضافة العميل وربطه بالمندوب" : "Customer Created & Linked",
        message: `${created.customerCode} — ${created.nameAr}${created.assignedRepNameAr ? ` (${created.assignedRepNameAr})` : ""}`,
      });
      setCreateOpen(false);
      setCode("");
      setNameAr("");
      setNameEn("");
      setPhone("");
      setAddress("");
      setVatNumber("");
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleSingleReassign = async (e: FormEvent) => {
    e.preventDefault();
    if (!reassignTarget || !reassignRepId) return;
    setSubmitting(true);
    setBanner(null);
    try {
      const updated = await erpApi.customers.assignCustomerRep(reassignTarget.id, reassignRepId);
      setBanner({
        tone: "success",
        title: isAr ? "تم تحديث المندوب المسؤول" : "Sales Rep Reassigned",
        message: isAr
          ? `تم ربط العميل ${updated.nameAr} بالمندوب ${updated.assignedRepNameAr ?? updated.assignedRepCode ?? ""}`
          : `${updated.nameEn ?? updated.nameAr} is now assigned to ${updated.assignedRepNameEn ?? updated.assignedRepCode ?? ""}`,
      });
      setReassignTarget(null);
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleBulkAssign = async (e: FormEvent) => {
    e.preventDefault();
    if (!bulkRepId || bulkSelectedIds.length === 0) return;
    setSubmitting(true);
    setBanner(null);
    try {
      const res = await erpApi.customers.bulkAssignCustomersRep({
        assignedRepId: bulkRepId,
        customerIds: bulkSelectedIds,
      });
      setBanner({
        tone: "success",
        title: isAr ? "تم ربط المندوب بالعملاء بنجاح" : "Bulk Rep Assignment Completed",
        message: isAr
          ? `تم ربط ${res.updatedCount} عميل بالمندوب ${res.assignedRepNameAr} (${res.assignedRepCode})`
          : `Linked ${res.updatedCount} customer(s) to ${res.assignedRepNameAr} (${res.assignedRepCode})`,
      });
      setBulkOpen(false);
      setBulkSelectedIds([]);
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const handleViewCredit = async (id: string) => {
    try {
      const status = await erpApi.customers.getCreditStatus(id);
      setCreditStatus(status);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    }
  };

  const handleSaveGrades = async () => {
    setSubmitting(true);
    try {
      const updated = await erpApi.customers.updateGrades(
        grades.map((g) => ({
          gradeCode: g.gradeCode,
          defaultPaymentDays: g.defaultPaymentDays,
          allowCredit: g.allowCredit,
        })),
      );
      setGrades(updated);
      setBanner({
        tone: "success",
        title: isAr ? "تم تحديث سياسات التصنيف" : "Grades Updated",
        message: isAr ? "تم حفظ فترات السداد والائتمان بنجاح" : "Payment periods saved",
      });
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const toggleBulkCustomer = (customerId: string) => {
    setBulkSelectedIds((prev) =>
      prev.includes(customerId) ? prev.filter((id) => id !== customerId) : [...prev, customerId],
    );
  };

  const columns: DataTableColumn<CustomerDto>[] = [
    { key: "code", header: isAr ? "الرمز" : "Code", mono: true, render: (c) => c.customerCode },
    { key: "name", header: isAr ? "العميل" : "Customer", render: (c) => (isAr ? c.nameAr : (c.nameEn ?? c.nameAr)) },
    {
      key: "rep",
      header: isAr ? "المندوب المسؤول" : "Assigned Sales Rep",
      render: (c) => (
        <div className="flex flex-wrap items-center gap-1.5">
          {c.assignedRepCode && <Badge tone="info" mono>{c.assignedRepCode}</Badge>}
          <span className="text-sm text-text-primary">
            {isAr
              ? (c.assignedRepNameAr ?? c.assignedRepCode ?? c.assignedRepId.slice(0, 8))
              : (c.assignedRepNameEn ?? c.assignedRepNameAr ?? c.assignedRepCode ?? c.assignedRepId.slice(0, 8))}
          </span>
        </div>
      ),
    },
    { key: "cat", header: isAr ? "التصنيف" : "Category", render: (c) => <Badge tone="neutral">{c.category}</Badge> },
    { key: "grade", header: isAr ? "الدرجة" : "Grade", render: (c) => <Badge tone="gold" mono>{c.gradeCode}</Badge> },
    { key: "limit", header: isAr ? "الحد الائتماني" : "Credit Limit", align: "end", numeric: true, render: (c) => formatSar(c.creditLimit, locale) },
    { key: "bal", header: isAr ? "الرصيد" : "Balance", align: "end", numeric: true, render: (c) => formatSar(c.currentBalance, locale) },
    {
      key: "actions",
      header: isAr ? "إجراءات" : "Actions",
      render: (c) => (
        <div className="flex flex-wrap gap-1.5">
          <Button variant="secondary" size="compact" onClick={() => void handleViewCredit(c.id)}>
            {isAr ? "حالة الائتمان" : "Credit Status"}
          </Button>
          {canManage && (
            <Button
              variant="ghost"
              size="compact"
              onClick={() => {
                setReassignTarget(c);
                setReassignRepId(c.assignedRepId);
              }}
            >
              {isAr ? "تغيير المندوب" : "Change Rep"}
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "العملاء والتصنيفات الائتمانية" : "Customers & Credit Grades"}
      subtitle={
        isAr
          ? "إدارة ملفات العملاء، ربط المناديب بعميل أو أكثر، فحص الحد الائتماني، وتعديل سياسات الدرجات A-D"
          : "Manage customers, link sales representatives to one or more customers, and govern A-D credit grades"
      }
      requiredPermissions={["customers:view", "customers:manage"]}
      headerActions={
        <div className="flex flex-wrap gap-2">
          {canManage && (
            <Button size="compact" onClick={() => setCreateOpen(true)}>
              {isAr ? "+ عميل جديد" : "+ New Customer"}
            </Button>
          )}
          {canManage && (
            <Button variant="secondary" size="compact" onClick={() => void handleOpenBulkDrawer()}>
              {isAr ? "ربط مندوب بعملاء" : "Assign Rep to Customers"}
            </Button>
          )}
          <Button variant="ghost" size="compact" onClick={() => void loadData()}>
            {isAr ? "تحديث" : "Refresh"}
          </Button>
        </div>
      }
    >
      {banner && (
        <AlertBanner
          tone={banner.tone}
          title={banner.title}
          message={banner.message}
          details={banner.details}
          onDismiss={() => setBanner(null)}
        />
      )}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "list", label: isAr ? "العملاء" : "Customers", badge: total },
          { id: "reps", label: isAr ? "مناديب المبيعات" : "Sales Reps", badge: salesReps.length },
          { id: "grades", label: isAr ? "الدرجات الائتمانية (A–D)" : "Credit Grades (A–D)", badge: grades.length },
        ]}
      />

      {tab === "list" && (
        <SurfaceCard title={isAr ? "دليل العملاء" : "Customer Directory"}>
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-3">
              <div className="w-full sm:w-64">
                <TextField label={isAr ? "بحث بالاسم أو الرمز أو الجوال" : "Search Name / Code / Phone"} value={search} onChange={(e) => setSearch(e.target.value)} />
              </div>
              {salesReps.length > 0 && (
                <div className="w-full sm:w-72">
                  <SelectField
                    label={isAr ? "تصفية حسب المندوب" : "Filter by Sales Rep"}
                    value={repFilterId}
                    onChange={(e) => setRepFilterId(e.target.value)}
                    options={[
                      { value: "", label: isAr ? "جميع المناديب" : "All Sales Reps" },
                      ...repSelectOptions,
                    ]}
                  />
                </div>
              )}
              <Button variant="secondary" onClick={() => void loadData()}>
                {isAr ? "بحث" : "Search"}
              </Button>
            </div>
            <DataTable columns={columns} rows={filteredCustomers} rowKey={(c) => c.id} loading={loading} />
            <PaginationBar page={page} pageSize={25} totalCount={total} onPageChange={setPage} />
          </div>
        </SurfaceCard>
      )}

      {tab === "reps" && (
        <SurfaceCard
          title={isAr ? "مناديب المبيعات ومحافظ العملاء المرتبطة" : "Sales Representatives & Customer Portfolios"}
          description={
            isAr
              ? "استعرض عدد العملاء المرتبطين بكل مندوب أو اربط مندوباً بعميل أو أكثر مباشرة"
              : "Review customer counts per sales representative and bulk-link customers directly"
          }
        >
          <DataTable
            rows={salesReps}
            rowKey={(r) => r.id}
            loading={loading}
            columns={[
              { key: "code", header: isAr ? "الرمز الوظيفي" : "Employee Code", mono: true, render: (r) => r.employeeCode },
              { key: "name", header: isAr ? "اسم المندوب" : "Sales Rep", render: (r) => (isAr ? r.fullNameAr : (r.fullNameEn ?? r.fullNameAr)) },
              { key: "type", header: isAr ? "النوع" : "Rep Type", render: (r) => <Badge tone="neutral">{r.repType ?? "Sales"}</Badge> },
              { key: "phone", header: isAr ? "الجوال" : "Phone", mono: true, render: (r) => r.phone },
              {
                key: "count",
                header: isAr ? "العملاء المرتبطون" : "Assigned Customers",
                align: "end",
                numeric: true,
                render: (r) => <Badge tone="gold" mono>{r.assignedCustomersCount}</Badge>,
              },
              {
                key: "act",
                header: isAr ? "إجراء" : "Action",
                render: (r) => (
                  <div className="flex flex-wrap gap-1.5">
                    <Button
                      variant="secondary"
                      size="compact"
                      onClick={() => {
                        setRepFilterId(r.id);
                        setTab("list");
                      }}
                    >
                      {isAr ? "عرض عملائه" : "View Customers"}
                    </Button>
                    {canManage && (
                      <Button
                        variant="ghost"
                        size="compact"
                        onClick={() => {
                          setBulkRepId(r.id);
                          void handleOpenBulkDrawer();
                        }}
                      >
                        {isAr ? "ربط عملاء" : "Assign Customers"}
                      </Button>
                    )}
                  </div>
                ),
              },
            ]}
          />
        </SurfaceCard>
      )}

      {tab === "grades" && (
        <SurfaceCard title={isAr ? "سياسات الدرجات الائتمانية" : "Customer Grade Policies"}>
          <div className="space-y-4">
            {canEditGrades && (
              <div className="flex justify-end">
                <Button size="compact" loading={submitting} onClick={() => void handleSaveGrades()}>
                  {isAr ? "حفظ التغييرات" : "Save Policies"}
                </Button>
              </div>
            )}
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {grades.map((g) => (
                <div key={g.id} className="space-y-3 rounded-xl border border-surface-border bg-surface-canvas p-4">
                  <div className="flex items-center justify-between">
                    <Badge tone="gold" size="md" mono>Grade {g.gradeCode}</Badge>
                    <Badge tone={g.allowCredit ? "success" : "warning"}>
                      {g.allowCredit ? (isAr ? "آجل" : "Credit") : (isAr ? "نقدي فقط" : "Cash Only")}
                    </Badge>
                  </div>
                  <TextField
                    label={isAr ? "أيام السداد" : "Payment Days"}
                    value={String(g.defaultPaymentDays)}
                    inputMode="numeric"
                    disabled={!canEditGrades}
                    onChange={(e) =>
                      setGrades((prev) =>
                        prev.map((item) => (item.id === g.id ? { ...item, defaultPaymentDays: Number(e.target.value) || 0 } : item)),
                      )
                    }
                  />
                  <label className="flex items-center gap-2 text-sm text-text-primary">
                    <input
                      type="checkbox"
                      checked={g.allowCredit}
                      disabled={!canEditGrades}
                      onChange={(e) =>
                        setGrades((prev) =>
                          prev.map((item) => (item.id === g.id ? { ...item, allowCredit: e.target.checked } : item)),
                        )
                      }
                    />
                    <span>{isAr ? "السماح بالبيع الآجل" : "Allow Credit"}</span>
                  </label>
                </div>
              ))}
            </div>
          </div>
        </SurfaceCard>
      )}

      {/* Create Customer Drawer with Live Sales Rep Dropdown */}
      <DrawerModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title={isAr ? "إضافة عميل جديد" : "Create Customer"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setCreateOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-customer-form" loading={submitting}>{isAr ? "حفظ" : "Save"}</Button>
          </>
        }
      >
        <form id="create-customer-form" onSubmit={handleCreateCustomer} className="space-y-3">
          <TextField label={isAr ? "رمز العميل" : "Customer Code"} value={code} onChange={(e) => setCode(e.target.value)} required placeholder="CUST-RYD-010" />
          <TextField label={isAr ? "الاسم بالعربية" : "Name (AR)"} value={nameAr} onChange={(e) => setNameAr(e.target.value)} required />
          <TextField label={isAr ? "الاسم بالإنجليزية" : "Name (EN)"} value={nameEn} onChange={(e) => setNameEn(e.target.value)} />
          <TextField label={isAr ? "الجوال" : "Phone"} value={phone} onChange={(e) => setPhone(e.target.value)} required placeholder="+966551112233" />
          <TextField label={isAr ? "العنوان" : "Address"} value={address} onChange={(e) => setAddress(e.target.value)} required />
          <TextField label={isAr ? "المدينة" : "City"} value={city} onChange={(e) => setCity(e.target.value)} />
          <SelectField
            label={isAr ? "فئة النشاط" : "Category"}
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            options={[
              { value: "Supermarket", label: "Supermarket" },
              { value: "Market", label: "Market" },
              { value: "Store", label: "Store" },
              { value: "Restaurant", label: "Restaurant" },
              { value: "Wholesale", label: "Wholesale" },
            ]}
          />
          {repSelectOptions.length > 0 && (
            <SelectField
              label={isAr ? "مندوب المبيعات المسؤول" : "Assigned Sales Representative"}
              value={assignedRepId}
              onChange={(e) => setAssignedRepId(e.target.value)}
              hint={isAr ? "سيظهر هذا العميل مباشرة في تطبيق PWA الخاص بالمندوب المختار" : "This customer will immediately appear in the selected sales rep's PWA"}
              options={repSelectOptions}
            />
          )}
          <TextField label={isAr ? "الرقم الضريبي (15 رقم اختياري)" : "VAT Number (15 digits optional)"} value={vatNumber} onChange={(e) => setVatNumber(e.target.value)} placeholder="310123456700003" />
          <SelectField
            label={isAr ? "الدرجة الائتمانية" : "Credit Grade"}
            value={gradeId}
            onChange={(e) => setGradeId(e.target.value)}
            options={grades.map((g) => ({ value: String(g.id), label: `Grade ${g.gradeCode} (${g.defaultPaymentDays}d)` }))}
          />
          <TextField label={isAr ? "الحد الائتماني (ر.س)" : "Credit Limit (SAR)"} value={creditLimit} inputMode="decimal" onChange={(e) => setCreditLimit(e.target.value)} required />
        </form>
      </DrawerModal>

      {/* Single Customer Change Rep Drawer */}
      <DrawerModal
        open={reassignTarget !== null}
        onClose={() => setReassignTarget(null)}
        title={isAr ? "تغيير المندوب المسؤول عن العميل" : "Reassign Sales Representative"}
        subtitle={reassignTarget ? `${reassignTarget.customerCode} — ${isAr ? reassignTarget.nameAr : (reassignTarget.nameEn ?? reassignTarget.nameAr)}` : undefined}
        footer={
          <>
            <Button variant="secondary" onClick={() => setReassignTarget(null)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="reassign-rep-form" loading={submitting} disabled={!reassignRepId}>
              {isAr ? "تأكيد الربط" : "Confirm Assignment"}
            </Button>
          </>
        }
      >
        <form id="reassign-rep-form" onSubmit={handleSingleReassign} className="space-y-4">
          <SelectField
            label={isAr ? "اختر المندوب الجديد" : "Select New Sales Rep"}
            value={reassignRepId}
            onChange={(e) => setReassignRepId(e.target.value)}
            options={repSelectOptions}
          />
        </form>
      </DrawerModal>

      {/* Bulk Assign 1 Sales Rep -> 1 or More Customers Drawer */}
      <DrawerModal
        open={bulkOpen}
        onClose={() => setBulkOpen(false)}
        title={isAr ? "ربط مندوب بعميل أو أكثر" : "Link Sales Rep to 1 or More Customers"}
        subtitle={
          isAr
            ? "اختر مندوب المبيعات ثم حدد عميلاً واحداً أو عدة عملاء لربطهم به في خطوة واحدة"
            : "Pick one sales representative and check one or more customers to assign atomically"
        }
        footer={
          <>
            <Button variant="secondary" onClick={() => setBulkOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button
              type="submit"
              form="bulk-assign-rep-form"
              loading={submitting}
              disabled={!bulkRepId || bulkSelectedIds.length === 0}
            >
              {isAr
                ? `تأكيد ربط (${bulkSelectedIds.length}) عميل`
                : `Assign (${bulkSelectedIds.length}) Customer(s)`}
            </Button>
          </>
        }
      >
        <form id="bulk-assign-rep-form" onSubmit={handleBulkAssign} className="space-y-4">
          <SelectField
            label={isAr ? "1. اختر مندوب المبيعات" : "1. Select Sales Representative"}
            value={bulkRepId}
            onChange={(e) => setBulkRepId(e.target.value)}
            options={repSelectOptions}
          />

          <div className="space-y-3 rounded-xl border border-surface-border bg-surface-canvas p-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="text-sm font-semibold text-text-primary">
                {isAr ? "2. حدد العملاء المراد ربطهم" : "2. Select Customers to Link"}
              </span>
              <div className="flex gap-1.5">
                <Button
                  type="button"
                  variant="secondary"
                  size="compact"
                  onClick={() => setBulkSelectedIds(bulkFilteredCustomers.map((c) => c.id))}
                >
                  {isAr ? "تحديد الكل" : "Select All"}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="compact"
                  onClick={() => setBulkSelectedIds([])}
                >
                  {isAr ? "مسح التحديد" : "Clear"}
                </Button>
              </div>
            </div>

            <TextField
              label={isAr ? "بحث سريع في العملاء" : "Quick Filter Customers"}
              value={bulkSearch}
              onChange={(e) => setBulkSearch(e.target.value)}
              placeholder={isAr ? "اسم العميل أو الرمز أو المدينة..." : "Customer name, code, or city..."}
            />

            <Badge tone="gold" mono>
              {isAr ? `العملاء المحددون: ${bulkSelectedIds.length}` : `Selected Customers: ${bulkSelectedIds.length}`}
            </Badge>

            <div className="max-h-72 space-y-1.5 overflow-y-auto">
              {bulkFilteredCustomers.map((c) => {
                const checked = bulkSelectedIds.includes(c.id);
                return (
                  <label
                    key={c.id}
                    className="flex cursor-pointer items-center justify-between gap-2 rounded-lg border border-surface-border bg-surface-panel p-2.5"
                  >
                    <span className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() => toggleBulkCustomer(c.id)}
                      />
                      <span>
                        <span className="block text-sm font-medium text-text-primary">
                          {isAr ? c.nameAr : (c.nameEn ?? c.nameAr)}
                        </span>
                        <span className="block font-mono text-xs text-text-muted">
                          {c.customerCode} • {c.city}
                        </span>
                      </span>
                    </span>
                    <Badge tone={c.assignedRepId === bulkRepId ? "success" : "neutral"}>
                      {c.assignedRepNameAr ?? c.assignedRepCode ?? "—"}
                    </Badge>
                  </label>
                );
              })}
            </div>
          </div>
        </form>
      </DrawerModal>

      <DrawerModal
        open={creditStatus !== null}
        onClose={() => setCreditStatus(null)}
        title={creditStatus ? `${creditStatus.customerCode} — ${creditStatus.nameAr}` : ""}
        subtitle={isAr ? "التقييم الائتماني الفوري وآخر الطلبات" : "Real-time credit evaluation and recent orders"}
      >
        {creditStatus && (
          <div className="space-y-4">
            <KeyValueList
              items={[
                { id: "grade", label: isAr ? "الدرجة" : "Grade", value: <Badge tone="gold" mono>{creditStatus.gradeCode}</Badge> },
                { id: "days", label: isAr ? "أيام السداد الفعلية" : "Effective Payment Days", value: `${creditStatus.effectivePaymentDays}d`, mono: true },
                { id: "limit", label: isAr ? "الحد الائتماني" : "Credit Limit", value: formatSar(creditStatus.creditLimit, locale), mono: true },
                { id: "bal", label: isAr ? "الرصيد الحالي" : "Current Balance", value: formatSar(creditStatus.currentBalance, locale), mono: true },
                { id: "overdue", label: isAr ? "المديونية المتأخرة" : "Overdue Debt", value: formatSar(creditStatus.overdueDebt, locale), mono: true },
              ]}
            />
            <DataTable
              rows={creditStatus.recentOrders}
              rowKey={(o) => o.id}
              columns={[
                { key: "no", header: isAr ? "رقم الطلب" : "Order #", mono: true, render: (o) => o.orderNumber },
                { key: "st", header: isAr ? "الحالة" : "Status", render: (o) => <Badge tone="info">{o.status}</Badge> },
                { key: "tot", header: isAr ? "الإجمالي" : "Total", align: "end", numeric: true, render: (o) => formatSar(o.totalAmount, locale) },
                { key: "dt", header: isAr ? "التاريخ" : "Date", render: (o) => formatDateTime(o.createdAtUtc, locale) },
              ]}
            />
          </div>
        )}
      </DrawerModal>
    </ErpAppLayout>
  );
}
