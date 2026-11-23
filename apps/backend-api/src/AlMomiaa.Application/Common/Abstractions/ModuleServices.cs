using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Users;
using AlMomiaa.Application.Customers;
using AlMomiaa.Application.Catalog;
using AlMomiaa.Application.Inventory;
using AlMomiaa.Application.Procurement;
using AlMomiaa.Application.Sales;
using AlMomiaa.Application.Finance;
using AlMomiaa.Application.Operations;

namespace AlMomiaa.Application.Common.Abstractions;

public interface IIamModuleService
{
    Task<PagedResult<UserSummaryDto>> GetUsersAsync(GetUsersQuery query, CancellationToken ct);
    Task<UserDetailDto> GetUserByIdAsync(Guid userId, CancellationToken ct);
    Task<UserSummaryDto> CreateUserAsync(CreateUserCommand command, CancellationToken ct);
    Task<UserSummaryDto> UpdateUserStatusAsync(UpdateUserStatusCommand command, CancellationToken ct);
    Task<UserDetailDto> UpdateUserRolesAsync(UpdateUserRolesCommand command, CancellationToken ct);
    Task<UserDetailDto> UpdateUserWarehousesAsync(UpdateUserWarehousesCommand command, CancellationToken ct);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct);
    Task<RoleDto> CreateRoleAsync(CreateRoleCommand command, CancellationToken ct);
    Task<RoleDto> UpdateRolePermissionsAsync(UpdateRolePermissionsCommand command, CancellationToken ct);
}

public interface ICustomersModuleService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(GetCustomersQuery query, CancellationToken ct);
    Task<CustomerCreditStatusDto> GetCustomerCreditStatusAsync(Guid customerId, CancellationToken ct);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerCommand command, CancellationToken ct);
    Task<IReadOnlyList<CustomerGradeDto>> GetGradesAsync(CancellationToken ct);
    Task<IReadOnlyList<CustomerGradeDto>> UpdateGradesAsync(UpdateCustomerGradesCommand command, CancellationToken ct);
}

public interface ICatalogModuleService
{
    Task<IReadOnlyList<CategoryNodeDto>> GetCategoryTreeAsync(CancellationToken ct);
    Task<CategoryNodeDto> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken ct);
    Task<IReadOnlyList<BrandDto>> GetBrandsAsync(CancellationToken ct);
    Task<BrandDto> CreateBrandAsync(CreateBrandCommand command, CancellationToken ct);
    Task<PagedResult<MasterProductDto>> GetProductsAsync(GetProductsQuery query, CancellationToken ct);
    Task<PagedResult<CategoryProductPriceDto>> GetCategoryProductsAsync(GetCategoryProductsQuery query, CancellationToken ct);
    Task<MasterProductDto> CreateMasterProductAsync(CreateMasterProductCommand command, CancellationToken ct);
    Task<SupplierProductDto> LinkSupplierProductAsync(LinkSupplierProductCommand command, CancellationToken ct);
    Task<IReadOnlyList<PriceListDto>> GetPriceListsAsync(CancellationToken ct);
    Task<PriceListDto> CreatePriceListAsync(CreatePriceListCommand command, CancellationToken ct);
}

public interface IInventoryModuleService
{
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(CancellationToken ct);
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseCommand command, CancellationToken ct);
    Task<PagedResult<WarehouseStockDto>> GetWarehouseStocksAsync(GetWarehouseStocksQuery query, CancellationToken ct);
    Task<StockTransferDto> CreateStockTransferAsync(CreateStockTransferCommand command, CancellationToken ct);
    Task<StockTransferDto> ReceiveStockTransferAsync(ReceiveStockTransferCommand command, CancellationToken ct);
    Task<IReadOnlyList<WarehouseStockDto>> SubmitInventoryCountAsync(SubmitInventoryCountCommand command, CancellationToken ct);
}

public interface IProcurementModuleService
{
    Task<PagedResult<SupplierDto>> GetSuppliersAsync(GetSuppliersQuery query, CancellationToken ct);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierCommand command, CancellationToken ct);
    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersAsync(GetPurchaseOrdersQuery query, CancellationToken ct);
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderCommand command, CancellationToken ct);
    Task<ContainerDto> CreateContainerAsync(CreateContainerCommand command, CancellationToken ct);
    Task<GoodsReceiptDto> CreateGoodsReceiptAsync(CreateGoodsReceiptCommand command, CancellationToken ct);
}

public interface ISalesModuleService
{
    Task<PagedResult<SalesOrderDto>> GetSalesOrdersAsync(GetSalesOrdersQuery query, CancellationToken ct);
    Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderCommand command, CancellationToken ct);
    Task<SalesOrderDto> EditSubmittedOrderByManagerAsync(EditSubmittedOrderByManagerCommand command, CancellationToken ct);
    Task<SalesOrderDto> OverrideOrderPriceAsync(OverrideOrderPriceCommand command, CancellationToken ct);
    Task<SalesOrderDto> CoApproveOrderCreditAsync(CoApproveOrderCreditCommand command, CancellationToken ct);
    Task<SalesOrderDto> ApproveSalesOrderAsync(ApproveSalesOrderCommand command, CancellationToken ct);
    Task<SalesOrderDto> RejectSalesOrderAsync(RejectSalesOrderCommand command, CancellationToken ct);
    Task<InvoiceDto> ConfirmOrderShipmentAsync(ConfirmOrderShipmentCommand command, CancellationToken ct);
    Task<ZatcaDeviceSequenceDto> RegisterEgsDeviceAsync(RegisterEgsDeviceCommand command, CancellationToken ct);
    Task<InvoicePrintDto> GetInvoicePrintAsync(Guid invoiceId, CancellationToken ct);
    Task<InvoiceDto> UploadInvoiceCopyAsync(UploadInvoiceCopyCommand command, CancellationToken ct);
    Task<SalesReturnDto> CreateSalesReturnAsync(CreateSalesReturnCommand command, CancellationToken ct);
    Task<DeliveryDispatchDto> CreateDeliveryDispatchAsync(CreateDeliveryDispatchCommand command, CancellationToken ct);
    Task<DeliveryDispatchDto> UpdateDeliveryPodAsync(UpdateDeliveryPodCommand command, CancellationToken ct);
}

public interface IFinanceModuleService
{
    Task<IReadOnlyList<FinancialAccountDto>> GetFinancialAccountsAsync(CancellationToken ct);
    Task<FinancialAccountDto> CreateFinancialAccountAsync(CreateFinancialAccountCommand command, CancellationToken ct);
    Task<IReadOnlyList<FiscalPeriodDto>> GetFiscalPeriodsAsync(CancellationToken ct);
    Task<FiscalPeriodDto> CreateFiscalPeriodAsync(CreateFiscalPeriodCommand command, CancellationToken ct);
    Task<FiscalPeriodDto> CloseFiscalPeriodAsync(CloseFiscalPeriodCommand command, CancellationToken ct);
    Task<CollectionDto> RecordCashCollectionAsync(RecordCashCollectionCommand command, CancellationToken ct);
    Task<CollectionDto> ConfirmCashCollectionAsync(ConfirmCashCollectionCommand command, CancellationToken ct);
    Task<CollectionDto> RecordTreasuryHandoverAsync(RecordTreasuryHandoverCommand command, CancellationToken ct);
    Task<CollectionDto> RecordBankTransferAsync(RecordBankTransferCommand command, CancellationToken ct);
    Task<CollectionDto> VerifyBankTransferAsync(VerifyBankTransferCommand command, CancellationToken ct);
    Task<PagedResult<JournalEntryDto>> GetJournalEntriesAsync(GetJournalEntriesQuery query, CancellationToken ct);
}

public interface IOperationsModuleService
{
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(CancellationToken ct);
    Task<RoleDashboardDto> GetRoleDashboardAsync(string roleView, CancellationToken ct);
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(GetAuditLogsQuery query, CancellationToken ct);
    Task<int> RecordGpsPingsAsync(RecordGpsPingsCommand command, CancellationToken ct);
}
