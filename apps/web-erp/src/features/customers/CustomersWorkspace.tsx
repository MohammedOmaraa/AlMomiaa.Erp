"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { CustomerCreditStatusDto, CustomerDto, CustomerGradeDto } from "@almomiaa/api-client";
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
  const [grades, setGrades] = useState<CustomerGradeDto[]>([]);
  const [creditStatus, setCreditStatus] = useState<CustomerCreditStatusDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{ tone: "success" | "danger"; title: string; message: string; details?: string[] } | null>(null);

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

  const canManage = hasPermission(user, "customers:manage");
  const canEditGrades = hasPermission(user, "settings:manage");

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [custRes, gradeRes] = await Promise.all([
        erpApi.customers.getCustomers({ search: search || undefined, page, pageSize: 25 }),
        erpApi.customers.getGrades(),
      ]);
      setCustomers(custRes.items);
      setTotal(custRes.totalCount);
      setGrades(gradeRes);
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [page, search]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const handleCreateCustomer = async (e: FormEvent) => {
    e.preventDefault();
    if (!user) return;
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
        assignedRepId: user.id,
      });
      setBanner({ tone: "success", title: isAr ? "تم إضافة العميل" : "Customer Created", message: `${created.customerCode} — ${created.nameAr}` });
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
        }))
      );
      setGrades(updated);
      setBanner({ tone: "success", title: isAr ? "تم تحديث سياسات التصنيف" : "Grades Updated", message: isAr ? "تم حفظ فترات السداد والائتمان بنجاح" : "Payment periods saved" });
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const columns: DataTableColumn<CustomerDto>[] = [
    { key: "code", header: isAr ? "الرمز" : "Code", mono: true, render: (c) => c.customerCode },
    { key: "name", header: isAr ? "العميل" : "Customer", render: (c) => (isAr ? c.nameAr : (c.nameEn ?? c.nameAr)) },
    { key: "cat", header: isAr ? "التصنيف" : "Category", render: (c) => <Badge tone="neutral">{c.category}</Badge> },
    { key: "grade", header: isAr ? "الدرجة" : "Grade", render: (c) => <Badge tone="gold" mono>{c.gradeCode}</Badge> },
    { key: "limit", header: isAr ? "الحد الائتماني" : "Credit Limit", align: "end", numeric: true, render: (c) => formatSar(c.creditLimit, locale) },
    { key: "bal", header: isAr ? "الرصيد" : "Balance", align: "end", numeric: true, render: (c) => formatSar(c.currentBalance, locale) },
    {
      key: "actions",
      header: isAr ? "إجراءات" : "Actions",
      render: (c) => (
        <Button variant="secondary" size="compact" onClick={() => void handleViewCredit(c.id)}>
          {isAr ? "حالة الائتمان" : "Credit Status"}
        </Button>
      ),
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "العملاء والتصنيفات الائتمانية" : "Customers & Credit Grades"}
      subtitle={isAr ? "إدارة ملفات العملاء، فحص الحد الائتماني والمديونيات المتأخرة، وتعديل سياسات الدرجات A-D" : "Manage customers, live credit status, and A-D credit grade policies"}
      requiredPermissions={["customers:view", "customers:manage"]}
      headerActions={
        <div className="flex gap-2">
          {canManage && <Button size="compact" onClick={() => setCreateOpen(true)}>{isAr ? "+ عميل جديد" : "+ New Customer"}</Button>}
          <Button variant="ghost" size="compact" onClick={() => void loadData()}>{isAr ? "تحديث" : "Refresh"}</Button>
        </div>
      }
    >
      {banner && <AlertBanner tone={banner.tone} title={banner.title} message={banner.message} details={banner.details} onDismiss={() => setBanner(null)} />}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "list", label: isAr ? "العملاء" : "Customers", badge: total },
          { id: "grades", label: isAr ? "الدرجات الائتمانية (A–D)" : "Credit Grades (A–D)", badge: grades.length },
        ]}
      />

      {tab === "list" && (
        <SurfaceCard title={isAr ? "دليل العملاء" : "Customer Directory"}>
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-2">
              <div className="w-full sm:w-72">
                <TextField label={isAr ? "بحث" : "Search"} value={search} onChange={(e) => setSearch(e.target.value)} />
              </div>
              <Button variant="secondary" onClick={() => void loadData()}>{isAr ? "بحث" : "Search"}</Button>
            </div>
            <DataTable columns={columns} rows={customers} rowKey={(c) => c.id} loading={loading} />
            <PaginationBar page={page} pageSize={25} totalCount={total} onPageChange={setPage} />
          </div>
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
                  <Badge tone={g.allowCredit ? "success" : "warning"}>{g.allowCredit ? (isAr ? "آجل" : "Credit") : (isAr ? "نقدي فقط" : "Cash Only")}</Badge>
                </div>
                <TextField
                  label={isAr ? "أيام السداد" : "Payment Days"}
                  value={String(g.defaultPaymentDays)}
                  disabled={!canEditGrades}
                  onChange={(e) =>
                    setGrades((prev) =>
                      prev.map((item) => (item.id === g.id ? { ...item, defaultPaymentDays: Number(e.target.value) || 0 } : item))
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
                        prev.map((item) => (item.id === g.id ? { ...item, allowCredit: e.target.checked } : item))
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
          <TextField label={isAr ? "الرقم الضريبي (15 رقم اختياري)" : "VAT Number (15 digits optional)"} value={vatNumber} onChange={(e) => setVatNumber(e.target.value)} placeholder="310123456700003" />
          <SelectField
            label={isAr ? "الدرجة الائتمانية" : "Credit Grade"}
            value={gradeId}
            onChange={(e) => setGradeId(e.target.value)}
            options={grades.map((g) => ({ value: String(g.id), label: `Grade ${g.gradeCode} (${g.defaultPaymentDays}d)` }))}
          />
          <TextField label={isAr ? "الحد الائتماني (ر.س)" : "Credit Limit (SAR)"} value={creditLimit} onChange={(e) => setCreditLimit(e.target.value)} required />
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
