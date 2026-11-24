"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { FinancialAccountDto, FiscalPeriodDto, JournalEntryDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
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

function randomSha256Hex(): string {
  const bytes = new Uint8Array(32);
  if (typeof globalThis.crypto !== "undefined") {
    globalThis.crypto.getRandomValues(bytes);
  }
  return Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
}

export function FinanceWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const isAr = locale === "ar-SA";

  const [tab, setTab] = useState("accounts");
  const [accounts, setAccounts] = useState<FinancialAccountDto[]>([]);
  const [periods, setPeriods] = useState<FiscalPeriodDto[]>([]);
  const [journals, setJournals] = useState<JournalEntryDto[]>([]);
  const [totalJournals, setTotalJournals] = useState(0);
  const [journalPage, setJournalPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{ tone: "success" | "danger"; title: string; message: string; details?: string[] } | null>(null);

  // Create Account Drawer
  const [accOpen, setAccOpen] = useState(false);
  const [accCode, setAccCode] = useState("");
  const [accNameAr, setAccNameAr] = useState("");
  const [accType, setAccType] = useState("MainTreasurySafe");
  const [bankName, setBankName] = useState("");
  const [iban, setIban] = useState("");

  // Open Fiscal Period
  const now = new Date();
  const [fpYear, setFpYear] = useState(String(now.getUTCFullYear()));
  const [fpMonth, setFpMonth] = useState(String(now.getUTCMonth() + 1));

  // Cash & Handover forms
  const [cashCustId, setCashCustId] = useState("");
  const [cashAmount, setCashAmount] = useState("100");
  const [cashCollId, setCashCollId] = useState("");
  const [handoverSafeId, setHandoverSafeId] = useState("");

  // Bank Transfer & Verify forms
  const [btCustId, setBtCustId] = useState("");
  const [btAmount, setBtAmount] = useState("150");
  const [btRef, setBtRef] = useState("SNB-TRX-001");
  const [btUrl, setBtUrl] = useState("https://storage.almomiaa.sa/transfers/slip.jpg");
  const [btHash, setBtHash] = useState(() => randomSha256Hex());
  const [verifyCollId, setVerifyCollId] = useState("");
  const [verifyBankId, setVerifyBankId] = useState("");

  const canManageSettings = hasPermission(user, "settings:manage");
  const canClosePeriod = hasPermission(user, "finance:close_period");
  const canFinanceDash = hasPermission(user, "dashboards:finance");

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      if (canFinanceDash) {
        const [accRes, fpRes, jeRes] = await Promise.all([
          erpApi.finance.getAccounts(),
          erpApi.finance.getFiscalPeriods(),
          erpApi.finance.getJournalEntries({ page: journalPage, pageSize: 20 }),
        ]);
        setAccounts(accRes);
        setPeriods(fpRes);
        setJournals(jeRes.items);
        setTotalJournals(jeRes.totalCount);
        const safe = accRes.find((a) => a.accountType === "MainTreasurySafe");
        const bank = accRes.find((a) => a.accountType === "BankAccount");
        if (!handoverSafeId && safe) setHandoverSafeId(safe.id);
        if (!verifyBankId && bank) setVerifyBankId(bank.id);
      }
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setLoading(false);
    }
  }, [canFinanceDash, handoverSafeId, journalPage, verifyBankId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const runFinanceAction = async (title: string, fn: () => Promise<string | void>) => {
    setSubmitting(true);
    setBanner(null);
    try {
      const msg = await fn();
      setBanner({ tone: "success", title, message: msg ?? (isAr ? "تمت العملية بنجاح" : "Completed") });
      await loadData();
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const journalColumns: DataTableColumn<JournalEntryDto>[] = [
    { key: "no", header: isAr ? "رقم القيد" : "Entry #", mono: true, render: (j) => j.entryNumber },
    { key: "ref", header: isAr ? "المرجع" : "Ref", render: (j) => <Badge tone="info">{j.referenceType}</Badge> },
    { key: "desc", header: isAr ? "البيان" : "Description", render: (j) => j.descriptionAr },
    { key: "dr", header: isAr ? "إجمالي مدين" : "Total Debit", align: "end", numeric: true, render: (j) => formatSar(j.lines.reduce((s, l) => s + l.debitAmount, 0), locale) },
    { key: "cr", header: isAr ? "إجمالي دائن" : "Total Credit", align: "end", numeric: true, render: (j) => formatSar(j.lines.reduce((s, l) => s + l.creditAmount, 0), locale) },
    { key: "dt", header: isAr ? "تاريخ الترحيل" : "Posted", render: (j) => formatDateTime(j.postingDateUtc, locale) },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "المالية والخزينة والتحصيل والقيود المحاسبية" : "Finance, Custody Collections, Fiscal Periods & Ledger"}
      subtitle={isAr ? "تحصيل نقدي وعهد المناديب والتحويلات البنكية وإغلاق الفترات المالية (MR-05) والقيود المزدوجة" : "Accounts, cash custody handovers, bank transfer verification, fiscal locks, and balanced journals"}
      requiredPermissions={["dashboards:finance", "collections:create", "finance:confirm_treasury", "finance:verify_bank", "finance:close_period"]}
      headerActions={
        <div className="flex gap-2">
          {canManageSettings && <Button size="compact" onClick={() => setAccOpen(true)}>{isAr ? "+ حساب مالي" : "+ Account"}</Button>}
          <Button variant="ghost" size="compact" onClick={() => void loadData()}>{isAr ? "تحديث" : "Refresh"}</Button>
        </div>
      }
    >
      {banner && <AlertBanner tone={banner.tone} title={banner.title} message={banner.message} details={banner.details} onDismiss={() => setBanner(null)} />}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "accounts", label: isAr ? "الحسابات المالية" : "Accounts", badge: accounts.length },
          { id: "collections", label: isAr ? "التحصيل النقدي والبنكي" : "Collections & Custody" },
          { id: "periods", label: isAr ? "الفترات المالية (MR-05)" : "Fiscal Periods", badge: periods.length },
          { id: "journals", label: isAr ? "القيود المزدوجة" : "Journal Entries", badge: totalJournals },
        ]}
      />

      {tab === "accounts" && (
        <SurfaceCard title={isAr ? "دليل الحسابات والأرصدة" : "Financial Accounts"}>
          <DataTable
            rows={accounts}
            rowKey={(a) => a.id}
            loading={loading}
            columns={[
              { key: "code", header: isAr ? "الرمز" : "Code", mono: true, render: (a) => a.accountCode },
              { key: "name", header: isAr ? "الحساب" : "Account", render: (a) => (isAr ? a.nameAr : (a.nameEn ?? a.nameAr)) },
              { key: "type", header: isAr ? "النوع" : "Type", render: (a) => <Badge tone="gold">{a.accountType}</Badge> },
              { key: "bal", header: isAr ? "الرصيد" : "Balance", align: "end", numeric: true, render: (a) => formatSar(a.currentBalance, locale) },
            ]}
          />
        </SurfaceCard>
      )}

      {tab === "collections" && (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          <SurfaceCard title={isAr ? "تحصيل نقدي -> تأكيد العميل -> توريد للخزينة" : "Cash Collection -> Confirm -> Treasury Handover"}>
            <div className="space-y-4">
              <form
                onSubmit={(e: FormEvent) => {
                  e.preventDefault();
                  void runFinanceAction("Cash Collected", async () => {
                    const c = await erpApi.finance.recordCashCollection({ customerId: cashCustId, amount: Number(cashAmount) });
                    setCashCollId(c.id);
                    return `${c.receiptNumber} (${c.status})`;
                  });
                }}
                className="space-y-3"
              >
                <TextField label={isAr ? "معرف العميل (Customer GUID)" : "Customer ID"} value={cashCustId} onChange={(e) => setCashCustId(e.target.value)} required />
                <TextField label={isAr ? "المبلغ النقدي (ر.س)" : "Cash Amount (SAR)"} value={cashAmount} onChange={(e) => setCashAmount(e.target.value)} required />
                <Button type="submit" loading={submitting}>{isAr ? "تسجيل تحصيل نقدي" : "Record Cash Collection"}</Button>
              </form>

              <div className="space-y-2 border-t border-surface-border pt-3">
                <TextField label={isAr ? "معرف سند التحصيل (Collection GUID)" : "Collection ID"} value={cashCollId} onChange={(e) => setCashCollId(e.target.value)} />
                <SelectField
                  label={isAr ? "الخزينة الرئيسية" : "Target MainTreasurySafe"}
                  value={handoverSafeId}
                  onChange={(e) => setHandoverSafeId(e.target.value)}
                  options={accounts.filter((a) => a.accountType === "MainTreasurySafe").map((a) => ({ value: a.id, label: `${a.accountCode} — ${a.nameAr}` }))}
                />
                <div className="flex flex-wrap gap-2">
                  <Button variant="secondary" size="compact" loading={submitting} onClick={() => void runFinanceAction("Customer Confirmed", async () => { await erpApi.finance.confirmCashCollection(cashCollId); })}>
                    {isAr ? "تأكيد العميل" : "Customer Confirm"}
                  </Button>
                  <Button size="compact" loading={submitting} onClick={() => void runFinanceAction("Treasury Handover", async () => { await erpApi.finance.recordTreasuryHandover({ collectionId: cashCollId, targetTreasuryAccountId: handoverSafeId }); })}>
                    {isAr ? "توريد للخزينة" : "Treasury Handover"}
                  </Button>
                </div>
              </div>
            </div>
          </SurfaceCard>

          <SurfaceCard title={isAr ? "حوالة بنكية (SHA-256 Dedupe) -> مطابقة المحاسب" : "Bank Transfer Proof -> Accountant Verification"}>
            <div className="space-y-4">
              <form
                onSubmit={(e: FormEvent) => {
                  e.preventDefault();
                  void runFinanceAction("Bank Transfer Submitted", async () => {
                    const c = await erpApi.finance.recordBankTransfer({
                      customerId: btCustId,
                      amount: Number(btAmount),
                      bankReferenceNumber: btRef,
                      paymentProofImageUrl: btUrl,
                      paymentProofImageHash: btHash,
                    });
                    setVerifyCollId(c.id);
                    setBtHash(randomSha256Hex());
                    return `${c.receiptNumber} (${c.status})`;
                  });
                }}
                className="space-y-3"
              >
                <TextField label={isAr ? "معرف العميل (Customer GUID)" : "Customer ID"} value={btCustId} onChange={(e) => setBtCustId(e.target.value)} required />
                <div className="grid grid-cols-2 gap-2">
                  <TextField label={isAr ? "المبلغ (ر.س)" : "Amount (SAR)"} value={btAmount} onChange={(e) => setBtAmount(e.target.value)} required />
                  <TextField label={isAr ? "المرجع البنكي" : "Bank Ref #"} value={btRef} onChange={(e) => setBtRef(e.target.value)} required />
                </div>
                <TextField label={isAr ? "رابط إيصال التحويل" : "Proof Image URL"} value={btUrl} onChange={(e) => setBtUrl(e.target.value)} required />
                <div className="flex items-end gap-2">
                  <div className="flex-1"><TextField label="SHA-256 Hash (64 hex)" value={btHash} onChange={(e) => setBtHash(e.target.value)} required /></div>
                  <Button variant="secondary" size="compact" onClick={() => setBtHash(randomSha256Hex())}>{isAr ? "توليد" : "Gen"}</Button>
                </div>
                <Button type="submit" loading={submitting}>{isAr ? "إرسال الحوالة" : "Submit Bank Transfer"}</Button>
              </form>

              <div className="space-y-2 border-t border-surface-border pt-3">
                <TextField label={isAr ? "معرف الحوالة (Collection GUID)" : "Bank Collection ID"} value={verifyCollId} onChange={(e) => setVerifyCollId(e.target.value)} />
                <SelectField
                  label={isAr ? "الحساب البنكي المستهدف" : "Target Bank Account"}
                  value={verifyBankId}
                  onChange={(e) => setVerifyBankId(e.target.value)}
                  options={accounts.filter((a) => a.accountType === "BankAccount").map((a) => ({ value: a.id, label: `${a.accountCode} — ${a.nameAr}` }))}
                />
                <div className="flex flex-wrap gap-2">
                  <Button size="compact" loading={submitting} onClick={() => void runFinanceAction("Bank Transfer Verified", async () => { await erpApi.finance.verifyBankTransfer(verifyCollId, { targetBankAccountId: verifyBankId, approve: true }); })}>
                    {isAr ? "اعتماد الحوالة" : "Approve Transfer"}
                  </Button>
                  <Button variant="danger" size="compact" loading={submitting} onClick={() => void runFinanceAction("Bank Transfer Rejected", async () => { await erpApi.finance.verifyBankTransfer(verifyCollId, { targetBankAccountId: verifyBankId, approve: false }); })}>
                    {isAr ? "رفض الحوالة" : "Reject Transfer"}
                  </Button>
                </div>
              </div>
            </div>
          </SurfaceCard>
        </div>
      )}

      {tab === "periods" && (
        <SurfaceCard title={isAr ? "الفترات المالية الشهرية (MR-05)" : "Monthly Fiscal Periods (MR-05)"}>
          <div className="space-y-4">
            {canClosePeriod && (
              <form
                onSubmit={(e: FormEvent) => {
                  e.preventDefault();
                  void runFinanceAction("Fiscal Period Opened", async () => {
                    const fp = await erpApi.finance.createFiscalPeriod({ fiscalYear: Number(fpYear), fiscalMonth: Number(fpMonth) });
                    return `${fp.fiscalYear}-${String(fp.fiscalMonth).padStart(2, "0")}`;
                  });
                }}
                className="flex flex-wrap items-end gap-2"
              >
                <div className="w-32"><TextField label={isAr ? "السنة" : "Year"} value={fpYear} onChange={(e) => setFpYear(e.target.value)} required /></div>
                <div className="w-32"><TextField label={isAr ? "الشهر" : "Month"} value={fpMonth} onChange={(e) => setFpMonth(e.target.value)} required /></div>
                <Button type="submit" loading={submitting}>{isAr ? "فتح فترة مالية" : "Open Period"}</Button>
              </form>
            )}
            <DataTable
              rows={periods}
              rowKey={(p) => String(p.id)}
              loading={loading}
              columns={[
                { key: "id", header: "ID", mono: true, render: (p) => p.id },
                { key: "ym", header: isAr ? "الفترة" : "Period", mono: true, render: (p) => `${p.fiscalYear}-${String(p.fiscalMonth).padStart(2, "0")}` },
                { key: "st", header: isAr ? "الحالة" : "Status", render: (p) => <Badge tone={p.isClosed ? "danger" : "success"}>{p.isClosed ? (isAr ? "مغلقة" : "Closed") : (isAr ? "مفتوحة" : "Open")}</Badge> },
                {
                  key: "act",
                  header: isAr ? "إجراء" : "Action",
                  render: (p) =>
                    !p.isClosed && canClosePeriod ? (
                      <Button variant="danger" size="compact" onClick={() => void runFinanceAction("Fiscal Period Closed", async () => { await erpApi.finance.closeFiscalPeriod(p.id); })}>
                        {isAr ? "إغلاق الفترة" : "Close Period"}
                      </Button>
                    ) : null,
                },
              ]}
            />
          </div>
        </SurfaceCard>
      )}

      {tab === "journals" && (
        <SurfaceCard title={isAr ? "دفتر اليومية المزدوج (غير قابل للتعديل)" : "Append-Only Double-Entry Journal"}>
          <div className="space-y-4">
            <DataTable columns={journalColumns} rows={journals} rowKey={(j) => j.id} loading={loading} />
            <PaginationBar page={journalPage} pageSize={20} totalCount={totalJournals} onPageChange={setJournalPage} />
          </div>
        </SurfaceCard>
      )}

      <DrawerModal
        open={accOpen}
        onClose={() => setAccOpen(false)}
        title={isAr ? "إضافة حساب مالي جديد" : "Create Financial Account"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setAccOpen(false)}>{isAr ? "إلغاء" : "Cancel"}</Button>
            <Button type="submit" form="create-acc-form" loading={submitting}>{isAr ? "حفظ" : "Save"}</Button>
          </>
        }
      >
        <form
          id="create-acc-form"
          onSubmit={(e: FormEvent) => {
            e.preventDefault();
            void runFinanceAction("Account Created", async () => {
              const a = await erpApi.finance.createAccount({
                accountCode: accCode,
                nameAr: accNameAr,
                accountType: accType,
                bankName: bankName || null,
                iban: iban || null,
              });
              setAccOpen(false);
              setAccCode("");
              setAccNameAr("");
              return `${a.accountCode} (${a.accountType})`;
            });
          }}
          className="space-y-3"
        >
          <TextField label={isAr ? "رمز الحساب" : "Account Code"} value={accCode} onChange={(e) => setAccCode(e.target.value)} required placeholder="SAFE-RYD-02" />
          <TextField label={isAr ? "الاسم بالعربية" : "Name (AR)"} value={accNameAr} onChange={(e) => setAccNameAr(e.target.value)} required />
          <SelectField
            label={isAr ? "نوع الحساب" : "Account Type"}
            value={accType}
            onChange={(e) => setAccType(e.target.value)}
            options={[
              { value: "MainTreasurySafe", label: "MainTreasurySafe" },
              { value: "BankAccount", label: "BankAccount" },
              { value: "RepCustodyWallet", label: "RepCustodyWallet" },
              { value: "AccountsReceivable", label: "AccountsReceivable" },
              { value: "AccountsPayable", label: "AccountsPayable" },
              { value: "SalesRevenue", label: "SalesRevenue" },
              { value: "VatOutput", label: "VatOutput" },
              { value: "InventoryAsset", label: "InventoryAsset" },
              { value: "COGS", label: "COGS" },
            ]}
          />
          <TextField label={isAr ? "اسم البنك (اختياري)" : "Bank Name (Optional)"} value={bankName} onChange={(e) => setBankName(e.target.value)} />
          <TextField label="IBAN" value={iban} onChange={(e) => setIban(e.target.value)} />
        </form>
      </DrawerModal>
    </ErpAppLayout>
  );
}
