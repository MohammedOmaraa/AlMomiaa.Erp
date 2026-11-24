import type { AxiosInstance } from "axios";
import { createIdempotencyKey, type PagedResult } from "../common/types.js";

export interface SalesOrderItemDto {
  id: string;
  masterProductId: string;
  productUnitId: string;
  conversionFactor: number;
  orderedQty: number;
  approvedQty: number;
  shippedQty: number | null;
  standardTierPrice: number;
  unitPrice: number;
  priceOverridePending: boolean;
  priceOverrideApprovedByUserId: string | null;
  vatAmount: number;
  lineTotal: number;
}

export interface SalesOrderDto {
  id: string;
  orderNumber: string;
  orderChannel: string;
  customerId: string;
  repId: string;
  warehouseId: number;
  status: string;
  customerBalanceAtOrder: number;
  customerCreditLimitAtOrder: number;
  exceededCreditWarning: boolean;
  requiresFinanceCoApproval: boolean;
  financeCoApprovedByUserId: string | null;
  hasStockShortage: boolean;
  hasPriceOverridePending: boolean;
  reservationExpiresAtUtc: string | null;
  reservationState: string;
  subTotal: number;
  vatAmount: number;
  totalAmount: number;
  approvedByManagerId: string | null;
  approvedAtUtc: string | null;
  rejectionReason: string | null;
  editedByManagerId: string | null;
  createdAtUtc: string;
  items: SalesOrderItemDto[];
}

export interface ZatcaDeviceSequenceDto {
  egsDeviceId: string;
  warehouseId: number | null;
  currentIcv: number;
  lastInvoiceHash: string;
  certificateSerial: string;
  updatedAtUtc: string;
}

export interface InvoiceDto {
  id: string;
  invoiceNumber: string;
  salesOrderId: string;
  customerId: string;
  invoiceType: string;
  issueDateUtc: string;
  dueDateUtc: string;
  subTotal: number;
  vatAmount: number;
  totalAmount: number;
  paidAmount: number;
  remainingBalance: number;
  paymentStatus: string;
  uploadedInvoiceCopyUrl: string | null;
  egsDeviceId: string;
  zatcaIcv: number;
  zatcaUuid: string;
  zatcaPreviousInvoiceHash: string;
  zatcaXmlHash: string;
  zatcaQrCodeBase64: string;
  zatcaSubmissionStatus: string;
}

export interface InvoicePrintDto {
  invoice: InvoiceDto;
  customerNameAr: string;
  customerNameEn: string | null;
  customerVatNumber: string | null;
  lines: SalesOrderItemDto[];
}

export interface SalesReturnItemDto {
  id: string;
  masterProductId: string;
  productUnitId: string;
  returnedQty: number;
  isSellableRestock: boolean;
  unitPrice: number;
  vatAmount: number;
  lineTotal: number;
}

export interface SalesReturnDto {
  id: string;
  creditNoteNumber: string;
  originalInvoiceId: string;
  customerId: string;
  warehouseId: number;
  returnReason: string;
  subTotal: number;
  vatAmount: number;
  totalAmount: number;
  egsDeviceId: string;
  zatcaIcv: number;
  zatcaUuid: string;
  zatcaXmlHash: string;
  zatcaQrCodeBase64: string;
  zatcaSubmissionStatus: string;
  createdAtUtc: string;
  items: SalesReturnItemDto[];
}

export interface DeliveryDispatchItemDto {
  id: string;
  invoiceId: string;
  deliveryStatus: string;
  deliveredAtUtc: string | null;
  signedPodImageUrl: string | null;
  deliveryNotes: string | null;
}

export interface DeliveryDispatchDto {
  id: string;
  dispatchNumber: string;
  warehouseId: number;
  driverOrRepId: string;
  vehiclePlateNumber: string | null;
  status: string;
  dispatchedAtUtc: string | null;
  completedAtUtc: string | null;
  createdAtUtc: string;
  items: DeliveryDispatchItemDto[];
}

export function createSalesService(http: AxiosInstance) {
  return {
    getOrders: async (params?: { status?: string; page?: number; pageSize?: number }) =>
      (await http.get<PagedResult<SalesOrderDto>>("/orders", { params })).data,
    createOrder: async (
      request: {
        orderChannel?: string | null;
        customerId: string;
        warehouseId: number;
        items: Array<{ masterProductId: string; productUnitId: string; orderedQty: number; unitPrice: number }>;
      },
      idempotencyKey?: string
    ) =>
      (
        await http.post<SalesOrderDto>("/orders", request, {
          headers: { "X-Idempotency-Key": idempotencyKey ?? createIdempotencyKey() },
        })
      ).data,
    managerEditOrder: async (
      orderId: string,
      items: Array<{ salesOrderItemId: string; approvedQty: number; unitPrice: number }>
    ) => (await http.put<SalesOrderDto>(`/orders/${orderId}/manager-edit`, { items })).data,
    overrideOrderPrice: async (orderId: string) =>
      (await http.post<SalesOrderDto>(`/orders/${orderId}/override-price`)).data,
    coApproveOrderCredit: async (orderId: string) =>
      (await http.post<SalesOrderDto>(`/orders/${orderId}/co-approve-credit`)).data,
    approveOrder: async (orderId: string) =>
      (await http.post<SalesOrderDto>(`/orders/${orderId}/approve`)).data,
    rejectOrder: async (orderId: string, rejectionReason: string) =>
      (await http.post<SalesOrderDto>(`/orders/${orderId}/reject`, { rejectionReason })).data,
    confirmShipment: async (
      orderId: string,
      request: {
        egsDeviceId: string;
        invoiceType?: string | null;
        items?: Array<{ salesOrderItemId: string; shippedQty: number }> | null;
      },
      idempotencyKey?: string
    ) =>
      (
        await http.post<InvoiceDto>(`/orders/${orderId}/confirm-shipment`, request, {
          headers: { "X-Idempotency-Key": idempotencyKey ?? createIdempotencyKey() },
        })
      ).data,
    registerEgsDevice: async (request: { egsDeviceId: string; warehouseId?: number | null; certificateSerial: string }) =>
      (await http.post<ZatcaDeviceSequenceDto>("/invoices/egs-devices", request)).data,
    getInvoicePrint: async (invoiceId: string) =>
      (await http.get<InvoicePrintDto>(`/invoices/${invoiceId}/print`)).data,
    uploadInvoiceCopy: async (invoiceId: string, uploadedInvoiceCopyUrl: string) =>
      (await http.post<InvoiceDto>(`/invoices/${invoiceId}/upload-copy`, { uploadedInvoiceCopyUrl })).data,
    createSalesReturn: async (request: {
      originalInvoiceId: string;
      warehouseId: number;
      returnReason: string;
      egsDeviceId: string;
      items: Array<{
        masterProductId: string;
        productUnitId: string;
        returnedQty: number;
        isSellableRestock: boolean;
        unitPrice: number;
      }>;
    }) => (await http.post<SalesReturnDto>("/returns", request)).data,
    createDeliveryDispatch: async (request: {
      warehouseId: number;
      driverOrRepId: string;
      vehiclePlateNumber?: string | null;
      invoiceIds: string[];
    }) => (await http.post<DeliveryDispatchDto>("/delivery/dispatches", request)).data,
    updateDeliveryPod: async (
      dispatchId: string,
      invoiceId: string,
      request: { deliveryStatus: string; signedPodImageUrl?: string | null; deliveryNotes?: string | null }
    ) => (await http.put<DeliveryDispatchDto>(`/delivery/dispatches/${dispatchId}/items/${invoiceId}/pod`, request)).data,
  };
}
