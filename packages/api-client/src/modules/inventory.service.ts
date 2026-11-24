import type { AxiosInstance } from "axios";
import type { PagedResult } from "../common/types.js";

export interface WarehouseDto {
  id: number;
  warehouseCode: string;
  nameAr: string;
  nameEn: string | null;
  warehouseType: string;
  assignedRepId: string | null;
  vehiclePlateNumber: string | null;
  city: string;
  isActive: boolean;
}

export interface WarehouseStockDto {
  id: string;
  warehouseId: number;
  masterProductId: string;
  sku: string;
  productNameAr: string;
  physicalQty: number;
  reservedQty: number;
  incomingQty: number;
  availableQty: number;
  minReorderQty: number;
}

export interface StockTransferItemDto {
  id: string;
  masterProductId: string;
  requestedQty: number;
  dispatchedQty: number;
  receivedQty: number | null;
}

export interface StockTransferDto {
  id: string;
  transferNumber: string;
  sourceWarehouseId: number;
  destinationWarehouseId: number;
  status: string;
  dispatchedByUserId: string;
  dispatchedAtUtc: string;
  receivedByUserId: string | null;
  receivedAtUtc: string | null;
  items: StockTransferItemDto[];
}

export interface CreateWarehouseRequest {
  warehouseCode: string;
  nameAr: string;
  nameEn?: string | null;
  warehouseType: string;
  assignedRepId?: string | null;
  vehiclePlateNumber?: string | null;
  city?: string | null;
}

export function createInventoryService(http: AxiosInstance) {
  return {
    getWarehouses: async () =>
      (await http.get<WarehouseDto[]>("/warehouses")).data,
    createWarehouse: async (request: CreateWarehouseRequest) =>
      (await http.post<WarehouseDto>("/warehouses", request)).data,
    getWarehouseStocks: async (warehouseId: number, params?: { search?: string; page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<WarehouseStockDto>>(`/warehouses/${warehouseId}/stocks`, { params })).data,
    createTransfer: async (request: {
      transferNumber?: string | null;
      sourceWarehouseId: number;
      destinationWarehouseId: number;
      items: Array<{ masterProductId: string; requestedQty: number; dispatchedQty: number }>;
    }) => (await http.post<StockTransferDto>("/warehouses/transfers", request)).data,
    receiveTransfer: async (transferId: string, items: Array<{ stockTransferItemId: string; receivedQty: number }>) =>
      (await http.put<StockTransferDto>(`/warehouses/transfers/${transferId}/receive`, { items })).data,
    submitInventoryCount: async (
      warehouseId: number,
      items: Array<{ masterProductId: string; countedPhysicalQty: number; lotNumber?: string | null; expiryDate?: string | null }>
    ) => (await http.post<WarehouseStockDto[]>(`/warehouses/${warehouseId}/inventory-count`, { items })).data,
  };
}
