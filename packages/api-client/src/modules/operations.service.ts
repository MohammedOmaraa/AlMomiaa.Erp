import type { AxiosInstance } from "axios";
import type { PagedResult } from "../common/types.js";

export interface HealthDto {
  status: string;
}

export interface ExecutiveDashboardDto {
  snapshotDate: string;
  totalMainSafeCash: number;
  totalRepCustodyCash: number;
  totalBankBalance: number;
  totalReceivables: number;
  totalPayables: number;
  totalInventoryValuation: number;
  pendingOrdersCount: number;
  lowStockItemsCount: number;
}

export interface RoleDashboardDto {
  roleView: string;
  metrics: Record<string, number>;
  generatedAtUtc: string;
}

export interface AuditLogDto {
  id: number;
  userId: string;
  userRole: string;
  actionName: string;
  entityName: string;
  entityId: string;
  oldValuesJson: string | null;
  newValuesJson: string | null;
  ipAddress: string | null;
  timestampUtc: string;
}

export interface GpsPingItemRequest {
  latitude: number;
  longitude: number;
  accuracyMeters?: number | null;
  nearCustomerId?: string | null;
  recordedAtUtc: string;
}

export interface LiveRepGpsDto {
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

export interface RepGpsHistoryPointDto {
  id: number;
  latitude: number;
  longitude: number;
  accuracyMeters: number | null;
  nearCustomerId: string | null;
  recordedAtUtc: string;
}

export function createOperationsService(http: AxiosInstance) {
  return {
    getHealth: async () => {
      const base = http.defaults.baseURL ?? "/api/v1";
      const healthUrl = base.replace(/\/api\/v1\/?$/, "") + "/health";
      return (await http.get<HealthDto>(healthUrl, { baseURL: "" })).data;
    },
    getExecutiveDashboard: async () =>
      (await http.get<ExecutiveDashboardDto>("/dashboards/executive")).data,
    getRoleDashboard: async (roleView: "sales" | "inventory" | "warehouse" | "finance" | "rep") =>
      (await http.get<RoleDashboardDto>(`/dashboards/role/${roleView}`)).data,
    getAuditLogs: async (params?: { entityName?: string; userId?: string; cursor?: string; pageSize?: number }) =>
      (await http.get<PagedResult<AuditLogDto>>("/audit-logs", { params })).data,
    recordGpsPings: async (pings: GpsPingItemRequest[]) =>
      (await http.post<{ acceptedCount: number }>("/gps/pings", { pings })).data,
    getLiveGps: async () =>
      (await http.get<LiveRepGpsDto[]>("/gps/live")).data,
    getRepGpsHistory: async (repId: string, params?: { hours?: number; limit?: number }) =>
      (await http.get<RepGpsHistoryPointDto[]>(`/gps/reps/${repId}/history`, { params })).data,
  };
}
