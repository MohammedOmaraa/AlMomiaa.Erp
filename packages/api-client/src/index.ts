import type { AxiosInstance } from "axios";

export type {
  AuthenticatedUser,
  AuthenticationResponse,
  ChangePasswordRequest,
  LoginRequest,
  ProblemDetails,
} from "./auth/auth.types.js";

export {
  authenticatedUserSchema,
  authenticationResponseSchema,
  changePasswordRequestSchema,
  loginRequestSchema,
  problemDetailsSchema,
} from "./auth/auth.schemas.js";

export { createAccessTokenStore } from "./auth/access-token-store.js";
export type { AccessTokenStore } from "./auth/access-token-store.js";

export { createApiClient } from "./http/axios.js";
export type { ApiClientConfig } from "./http/axios.js";

export { createAuthService, extractProblemDetails } from "./auth/auth.service.js";
export type { AuthService } from "./auth/auth.service.js";

export { createIdempotencyKey, redactSensitiveData } from "./common/types.js";
export type { ApiCallLog, PagedResult } from "./common/types.js";

export { createUsersService } from "./modules/users.service.js";
export type {
  CreateRoleRequest,
  CreateUserRequest,
  RoleDto,
  UserDetailDto,
  UserSummaryDto,
} from "./modules/users.service.js";

export { createCustomersService } from "./modules/customers.service.js";
export type {
  BulkAssignCustomersRepResponse,
  CreateCustomerRequest,
  CustomerCreditStatusDto,
  CustomerDto,
  CustomerGradeDto,
  RecentCustomerOrderDto,
  SalesRepOptionDto,
  UpdateCustomerGradeItem,
} from "./modules/customers.service.js";

export { createCatalogService } from "./modules/catalog.service.js";
export type {
  BrandDto,
  CategoryNodeDto,
  CategoryProductPriceDto,
  CreateMasterProductRequest,
  CreatePriceListRequest,
  CreateProductUnitRequest,
  InitialWarehouseStockRequest,
  LinkSupplierProductRequest,
  MasterProductDto,
  PriceListDto,
  PriceListItemDto,
  ProductAvailabilityDto,
  ProductUnitDto,
  ProductWarehouseStockBreakdownDto,
  SupplierProductDto,
} from "./modules/catalog.service.js";

export { createInventoryService } from "./modules/inventory.service.js";
export type {
  CreateWarehouseRequest,
  StockTransferDto,
  StockTransferItemDto,
  WarehouseDto,
  WarehouseStockDto,
} from "./modules/inventory.service.js";

export { createProcurementService } from "./modules/procurement.service.js";
export type {
  ContainerDto,
  GoodsReceiptDto,
  GoodsReceiptItemDto,
  PurchaseOrderDto,
  PurchaseOrderItemDto,
  SupplierDto,
} from "./modules/procurement.service.js";

export { createSalesService } from "./modules/sales.service.js";
export type {
  DeliveryDispatchDto,
  DeliveryDispatchItemDto,
  InvoiceDto,
  InvoicePrintDto,
  SalesOrderDto,
  SalesOrderItemDto,
  SalesReturnDto,
  SalesReturnItemDto,
  ZatcaDeviceSequenceDto,
} from "./modules/sales.service.js";

export { createFinanceService } from "./modules/finance.service.js";
export type {
  CollectionAllocationDto,
  CollectionDto,
  FinancialAccountDto,
  FiscalPeriodDto,
  JournalEntryDto,
  JournalLineDto,
} from "./modules/finance.service.js";

export { createOperationsService } from "./modules/operations.service.js";
export type {
  AuditLogDto,
  ExecutiveDashboardDto,
  GpsPingItemRequest,
  HealthDto,
  LiveRepGpsDto,
  RepGpsHistoryPointDto,
  RoleDashboardDto,
} from "./modules/operations.service.js";

export { createNotificationsService } from "./modules/notifications.service.js";
export type {
  MarkAllNotificationsReadResponse,
  MarkNotificationReadResponse,
  NotificationsPageDto,
  NotificationSeverity,
  UserNotificationDto,
} from "./modules/notifications.service.js";

export {
  computeJitteredReconnectDelay,
  createOperationsHubClient,
} from "./realtime/operations-hub.js";
export type {
  CreditCoApprovalRequiredEvent,
  OperationsHubClient,
  OperationsHubClientOptions,
  OperationsHubEvent,
  OperationsHubState,
  OrderApprovedEvent,
  OrderRejectedEvent,
  OrderSubmittedEvent,
  OrderUpdatedEvent,
} from "./realtime/operations-hub.js";

import { createUsersService } from "./modules/users.service.js";
import { createCustomersService } from "./modules/customers.service.js";
import { createCatalogService } from "./modules/catalog.service.js";
import { createInventoryService } from "./modules/inventory.service.js";
import { createProcurementService } from "./modules/procurement.service.js";
import { createSalesService } from "./modules/sales.service.js";
import { createFinanceService } from "./modules/finance.service.js";
import { createOperationsService } from "./modules/operations.service.js";
import { createNotificationsService } from "./modules/notifications.service.js";

export function createErpApi(http: AxiosInstance) {
  return {
    users: createUsersService(http),
    customers: createCustomersService(http),
    catalog: createCatalogService(http),
    inventory: createInventoryService(http),
    procurement: createProcurementService(http),
    sales: createSalesService(http),
    finance: createFinanceService(http),
    operations: createOperationsService(http),
    notifications: createNotificationsService(http),
  };
}

export type ErpApi = ReturnType<typeof createErpApi>;
