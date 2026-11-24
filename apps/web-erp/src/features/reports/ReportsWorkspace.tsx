"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { AuditLogDto } from "@almomiaa/api-client";
import {
  AlertBanner,
  Badge,
  Button,
  DataTable,
  DrawerModal,
  JsonResponseViewer,
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

export function ReportsWorkspace() {
  const { locale } = useLocale();
  const { user } = useAuth();
  const isAr = locale === "ar-SA";

  const [tab, setTab] = useState("audit");
  const [logs, setLogs] = useState<AuditLogDto[]>([]);
  const [total, setTotal] = useState(0);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [entityFilter, setEntityFilter] = useState("");
  const [selectedLog, setSelectedLog] = useState<AuditLogDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [banner, setBanner] = useState<{ tone: "success" | "danger"; title: string; message: string; details?: string[] } | null>(null);

  // GPS form
  const [lat, setLat] = useState("24.7136");
  const [lng, setLng] = useState("46.6753");
  const [acc, setAcc] = useState("8.5");

  const canAudit = hasPermission(user, "audit:view");
  const canGps = hasPermission(user, "gps:stream");

  const loadAudit = useCallback(
    async (cursor?: string) => {
      if (!canAudit) {
        setLoading(false);
        return;
      }
      setLoading(true);
      try {
        const res = await erpApi.operations.getAuditLogs({
          entityName: entityFilter || undefined,
          cursor,
          pageSize: 25,
        });
        setLogs(res.items);
        setTotal(res.totalCount);
        setNextCursor(res.nextCursor);
      } catch (err) {
        const f = formatApiError(err);
        setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
      } finally {
        setLoading(false);
      }
    },
    [canAudit, entityFilter]
  );

  useEffect(() => {
    void loadAudit();
  }, [loadAudit]);

  const handleSendGps = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setBanner(null);
    try {
      const res = await erpApi.operations.recordGpsPings([
        {
          latitude: Number(lat),
          longitude: Number(lng),
          accuracyMeters: Number(acc),
          recordedAtUtc: new Date().toISOString(),
        },
      ]);
      setBanner({ tone: "success", title: isAr ? "تم استقبال إحداثيات GPS" : "GPS Pings Accepted", message: `acceptedCount: ${res.acceptedCount}` });
    } catch (err) {
      const f = formatApiError(err);
      setBanner({ tone: "danger", title: f.title, message: f.message, details: f.details });
    } finally {
      setSubmitting(false);
    }
  };

  const columns: DataTableColumn<AuditLogDto>[] = [
    { key: "id", header: "ID", mono: true, render: (a) => a.id },
    { key: "act", header: isAr ? "الإجراء" : "Action", render: (a) => <Badge tone="gold">{a.actionName}</Badge> },
    { key: "ent", header: isAr ? "الكيان" : "Entity", mono: true, render: (a) => a.entityName },
    { key: "role", header: isAr ? "الدور" : "Role", mono: true, render: (a) => a.userRole },
    { key: "dt", header: isAr ? "التوقيت" : "Timestamp", render: (a) => formatDateTime(a.timestampUtc, locale) },
    {
      key: "diff",
      header: isAr ? "التفاصيل" : "Diff",
      render: (a) => (
        <Button variant="secondary" size="compact" onClick={() => setSelectedLog(a)}>
          {isAr ? "عرض JSON" : "Inspect"}
        </Button>
      ),
    },
  ];

  return (
    <ErpAppLayout
      title={isAr ? "سجل التدقيق المحصّن وتتبع GPS" : "Immutable Audit Logs & Field GPS"}
      subtitle={isAr ? "تتبع كافة التغييرات عبر النظام مع الفروقات قبل/بعد وإرسال نقاط التتبع الميداني" : "Keyset-paginated audit trail with before/after JSON snapshots and GPS telemetry"}
      requiredPermissions={["audit:view", "gps:stream"]}
      headerActions={<Button variant="ghost" size="compact" onClick={() => void loadAudit()}>{isAr ? "تحديث" : "Refresh"}</Button>}
    >
      {banner && <AlertBanner tone={banner.tone} title={banner.title} message={banner.message} details={banner.details} onDismiss={() => setBanner(null)} />}

      <Tabs
        activeId={tab}
        onChange={setTab}
        items={[
          { id: "audit", label: isAr ? "سجل التدقيق" : "Audit Logs", badge: total, disabled: !canAudit },
          { id: "gps", label: isAr ? "إحداثيات GPS" : "GPS Telemetry", disabled: !canGps },
        ]}
      />

      {tab === "audit" && (
        <SurfaceCard title={isAr ? "سجل العمليات المحصّن ([ops].[AuditLogs])" : "Audit Trail ([ops].[AuditLogs])"}>
          <div className="space-y-4">
            <div className="flex flex-wrap items-end gap-2">
              <div className="w-64">
                <TextField label={isAr ? "تصفية باسم الكيان" : "Filter by EntityName"} value={entityFilter} onChange={(e) => setEntityFilter(e.target.value)} placeholder="SalesOrder, User, Collection..." />
              </div>
              <Button variant="secondary" onClick={() => void loadAudit()}>{isAr ? "تصفية" : "Filter"}</Button>
              {nextCursor && <Button variant="secondary" onClick={() => void loadAudit(nextCursor)}>{isAr ? "الصفحة التالية (Cursor)" : "Next Cursor Page"}</Button>}
            </div>
            <DataTable columns={columns} rows={logs} rowKey={(a) => String(a.id)} loading={loading} />
          </div>
        </SurfaceCard>
      )}

      {tab === "gps" && (
        <SurfaceCard title={isAr ? "إرسال نقطة تتبع GPS (لمندوبي المبيعات)" : "Stream Field Rep GPS Ping"}>
          <form onSubmit={handleSendGps} className="max-w-md space-y-3">
            <TextField label="Latitude (-90..90)" value={lat} onChange={(e) => setLat(e.target.value)} required />
            <TextField label="Longitude (-180..180)" value={lng} onChange={(e) => setLng(e.target.value)} required />
            <TextField label="Accuracy (meters)" value={acc} onChange={(e) => setAcc(e.target.value)} />
            <Button type="submit" loading={submitting}>{isAr ? "إرسال GPS" : "Send GPS Ping"}</Button>
          </form>
        </SurfaceCard>
      )}

      <DrawerModal
        open={selectedLog !== null}
        onClose={() => setSelectedLog(null)}
        title={selectedLog ? `${selectedLog.actionName} — ${selectedLog.entityName}` : ""}
        subtitle={selectedLog ? `ID #${selectedLog.id} • ${selectedLog.entityId}` : undefined}
        size="lg"
      >
        {selectedLog && (
          <JsonResponseViewer
            method="AUDIT"
            endpoint={`/api/v1/audit-logs#${selectedLog.id}`}
            status={200}
            timestamp={selectedLog.timestampUtc}
            payload={{
              userId: selectedLog.userId,
              userRole: selectedLog.userRole,
              ipAddress: selectedLog.ipAddress,
              oldValues: selectedLog.oldValuesJson ? JSON.parse(selectedLog.oldValuesJson) : null,
              newValues: selectedLog.newValuesJson ? JSON.parse(selectedLog.newValuesJson) : null,
            }}
          />
        )}
      </DrawerModal>
    </ErpAppLayout>
  );
}
