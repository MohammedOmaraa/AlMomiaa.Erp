import type { AxiosInstance } from "axios";
import type { PagedResult } from "../common/types.js";

export interface SupplierDto {
  id: string;
  supplierCode: string;
  nameAr: string;
  nameEn: string | null;
  country: string;
  phone: string | null;
  vatNumber: string | null;
  currentPayableBalance: number;
  isActive: boolean;
}

export interface PurchaseOrderItemDto {
  id: string;
  masterProductId: string;
  orderedQty: number;
  receivedQty: number;
  unitCostSAR: number;
  lineTotalSAR: number;
}

export interface PurchaseOrderDto {
  id: string;
  poNumber: string;
  supplierId: string;
  targetWarehouseId: number;
  orderType: string;
  status: string;
  totalAmountSAR: number;
  approvedByUserId: string | null;
  createdAtUtc: string;
  items: PurchaseOrderItemDto[];
}

export interface ContainerDto {
  id: string;
  purchaseOrderId: string;
  containerNumber: string;
  billOfLading: string | null;
  shippingLine: string | null;
  portOfArrival: string;
  estimatedArrivalDate: string;
  actualArrivalDate: string | null;
  status: string;
  supplierGoodsCostSAR: number;
  freightAndCustomsCostSAR: number;
  totalLandedCostSAR: number;
}

export interface GoodsReceiptItemDto {
  id: string;
  masterProductId: string;
  receivedQty: number;
  allocatedLandedUnitCostSAR: number;
  lotNumber: string | null;
  expiryDate: string | null;
}

export interface GoodsReceiptDto {
  id: string;
  grnNumber: string;
  purchaseOrderId: string;
  containerId: string | null;
  warehouseId: number;
  receivedByUserId: string;
  receivedAtUtc: string;
  items: GoodsReceiptItemDto[];
}

export function createProcurementService(http: AxiosInstance) {
  return {
    getSuppliers: async (params?: { search?: string; page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<SupplierDto>>("/procurement/suppliers", { params })).data,
    createSupplier: async (request: {
      supplierCode: string;
      nameAr: string;
      nameEn?: string | null;
      country?: string | null;
      phone?: string | null;
      vatNumber?: string | null;
    }) => (await http.post<SupplierDto>("/procurement/suppliers", request)).data,
    getPurchaseOrders: async (params?: { status?: string; page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<PurchaseOrderDto>>("/procurement/purchase-orders", { params })).data,
    createPurchaseOrder: async (request: {
      supplierId: string;
      targetWarehouseId: number;
      orderType: string;
      items: Array<{ masterProductId: string; orderedQty: number; unitCostSAR: number }>;
    }) => (await http.post<PurchaseOrderDto>("/procurement/purchase-orders", request)).data,
    createContainer: async (request: {
      purchaseOrderId: string;
      containerNumber: string;
      billOfLading?: string | null;
      shippingLine?: string | null;
      portOfArrival: string;
      estimatedArrivalDate: string;
      actualArrivalDate?: string | null;
      status: string;
      supplierGoodsCostSAR: number;
      freightAndCustomsCostSAR: number;
    }) => (await http.post<ContainerDto>("/procurement/containers", request)).data,
    createGoodsReceipt: async (request: {
      purchaseOrderId: string;
      containerId?: string | null;
      warehouseId: number;
      items: Array<{
        masterProductId: string;
        receivedQty: number;
        allocatedLandedUnitCostSAR: number;
        lotNumber?: string | null;
        expiryDate?: string | null;
      }>;
    }) => (await http.post<GoodsReceiptDto>("/procurement/goods-receipts", request)).data,
  };
}
