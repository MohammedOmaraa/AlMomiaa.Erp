using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Sales;

public sealed record SalesOrderItemDto(
    Guid Id,
    Guid MasterProductId,
    Guid ProductUnitId,
    decimal ConversionFactor,
    decimal OrderedQty,
    decimal ApprovedQty,
    decimal? ShippedQty,
    decimal StandardTierPrice,
    decimal UnitPrice,
    bool PriceOverridePending,
    Guid? PriceOverrideApprovedByUserId,
    decimal VatAmount,
    decimal LineTotal);

public sealed record SalesOrderDto(
    Guid Id,
    string OrderNumber,
    string OrderChannel,
    Guid CustomerId,
    Guid RepId,
    int WarehouseId,
    string Status,
    decimal CustomerBalanceAtOrder,
    decimal CustomerCreditLimitAtOrder,
    bool ExceededCreditWarning,
    bool RequiresFinanceCoApproval,
    Guid? FinanceCoApprovedByUserId,
    bool HasStockShortage,
    bool HasPriceOverridePending,
    DateTime? ReservationExpiresAtUtc,
    string ReservationState,
    decimal SubTotal,
    decimal VatAmount,
    decimal TotalAmount,
    Guid? ApprovedByManagerId,
    DateTime? ApprovedAtUtc,
    string? RejectionReason,
    Guid? EditedByManagerId,
    DateTime CreatedAtUtc,
    IReadOnlyList<SalesOrderItemDto> Items);

public sealed record ZatcaDeviceSequenceDto(string EgsDeviceId, int? WarehouseId, long CurrentIcv, string LastInvoiceHash, string CertificateSerial, DateTime UpdatedAtUtc);

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid SalesOrderId,
    Guid CustomerId,
    string InvoiceType,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    decimal SubTotal,
    decimal VatAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingBalance,
    string PaymentStatus,
    string? UploadedInvoiceCopyUrl,
    string EgsDeviceId,
    long ZatcaIcv,
    Guid ZatcaUuid,
    string ZatcaPreviousInvoiceHash,
    string ZatcaXmlHash,
    string ZatcaQrCodeBase64,
    string ZatcaSubmissionStatus);

public sealed record InvoicePrintDto(InvoiceDto Invoice, string CustomerNameAr, string? CustomerNameEn, string? CustomerVatNumber, IReadOnlyList<SalesOrderItemDto> Lines);

public sealed record SalesReturnItemDto(Guid Id, Guid MasterProductId, Guid ProductUnitId, decimal ReturnedQty, bool IsSellableRestock, decimal UnitPrice, decimal VatAmount, decimal LineTotal);
public sealed record SalesReturnDto(Guid Id, string CreditNoteNumber, Guid OriginalInvoiceId, Guid CustomerId, int WarehouseId, string ReturnReason, decimal SubTotal, decimal VatAmount, decimal TotalAmount, string EgsDeviceId, long ZatcaIcv, Guid ZatcaUuid, string ZatcaXmlHash, string ZatcaQrCodeBase64, string ZatcaSubmissionStatus, DateTime CreatedAtUtc, IReadOnlyList<SalesReturnItemDto> Items);

public sealed record DeliveryDispatchItemDto(Guid Id, Guid InvoiceId, string DeliveryStatus, DateTime? DeliveredAtUtc, string? SignedPodImageUrl, string? DeliveryNotes);
public sealed record DeliveryDispatchDto(Guid Id, string DispatchNumber, int WarehouseId, Guid DriverOrRepId, string? VehiclePlateNumber, string Status, DateTime? DispatchedAtUtc, DateTime? CompletedAtUtc, DateTime CreatedAtUtc, IReadOnlyList<DeliveryDispatchItemDto> Items);

public sealed record GetSalesOrdersQuery(string? Status, int Page = 1, int PageSize = 50) : IRequest<PagedResult<SalesOrderDto>>;

public sealed record CreateSalesOrderItemRequest(Guid MasterProductId, Guid ProductUnitId, decimal OrderedQty, decimal UnitPrice);
public sealed record CreateSalesOrderRequest(string? OrderChannel, Guid CustomerId, int WarehouseId, CreateSalesOrderItemRequest[] Items);
public sealed record CreateSalesOrderCommand(CreateSalesOrderRequest Request, Guid IdempotencyKey) : IRequest<SalesOrderDto>;

public sealed record ManagerEditOrderItemRequest(Guid SalesOrderItemId, decimal ApprovedQty, decimal UnitPrice);
public sealed record ManagerEditOrderRequest(ManagerEditOrderItemRequest[] Items);
public sealed record EditSubmittedOrderByManagerCommand(Guid SalesOrderId, ManagerEditOrderRequest Request) : IRequest<SalesOrderDto>;

public sealed record OverrideOrderPriceCommand(Guid SalesOrderId) : IRequest<SalesOrderDto>;
public sealed record CoApproveOrderCreditCommand(Guid SalesOrderId) : IRequest<SalesOrderDto>;
public sealed record ApproveSalesOrderCommand(Guid SalesOrderId) : IRequest<SalesOrderDto>;

public sealed record RejectSalesOrderRequest(string RejectionReason);
public sealed record RejectSalesOrderCommand(Guid SalesOrderId, string RejectionReason) : IRequest<SalesOrderDto>;

public sealed record ConfirmShipmentItemRequest(Guid SalesOrderItemId, decimal ShippedQty);
public sealed record ConfirmShipmentRequest(string EgsDeviceId, string? InvoiceType, ConfirmShipmentItemRequest[]? Items);
public sealed record ConfirmOrderShipmentCommand(Guid SalesOrderId, ConfirmShipmentRequest Request, Guid IdempotencyKey) : IRequest<InvoiceDto>;

public sealed record RegisterEgsDeviceRequest(string EgsDeviceId, int? WarehouseId, string CertificateSerial);
public sealed record RegisterEgsDeviceCommand(RegisterEgsDeviceRequest Request) : IRequest<ZatcaDeviceSequenceDto>;

public sealed record GetInvoicePrintQuery(Guid InvoiceId) : IRequest<InvoicePrintDto>;

public sealed record UploadInvoiceCopyRequest(string UploadedInvoiceCopyUrl);
public sealed record UploadInvoiceCopyCommand(Guid InvoiceId, string UploadedInvoiceCopyUrl) : IRequest<InvoiceDto>;

public sealed record CreateSalesReturnItemRequest(Guid MasterProductId, Guid ProductUnitId, decimal ReturnedQty, bool IsSellableRestock, decimal UnitPrice);
public sealed record CreateSalesReturnRequest(Guid OriginalInvoiceId, int WarehouseId, string ReturnReason, string EgsDeviceId, CreateSalesReturnItemRequest[] Items);
public sealed record CreateSalesReturnCommand(CreateSalesReturnRequest Request) : IRequest<SalesReturnDto>;

public sealed record CreateDeliveryDispatchRequest(int WarehouseId, Guid DriverOrRepId, string? VehiclePlateNumber, Guid[] InvoiceIds);
public sealed record CreateDeliveryDispatchCommand(CreateDeliveryDispatchRequest Request) : IRequest<DeliveryDispatchDto>;

public sealed record UpdateDeliveryPodRequest(string DeliveryStatus, string? SignedPodImageUrl, string? DeliveryNotes);
public sealed record UpdateDeliveryPodCommand(Guid DispatchId, Guid InvoiceId, UpdateDeliveryPodRequest Request) : IRequest<DeliveryDispatchDto>;

public sealed class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    private static readonly string[] ValidChannels = ["PreSales", "CashVan"];

    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("X-Idempotency-Key header with a valid GUID is required.");
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.OrderChannel)
            .Must(c => string.IsNullOrWhiteSpace(c) || ValidChannels.Contains(c))
            .WithMessage("OrderChannel must be PreSales or CashVan.");
        RuleFor(x => x.Request.CustomerId).NotEmpty();
        RuleFor(x => x.Request.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MasterProductId).NotEmpty();
            item.RuleFor(i => i.ProductUnitId).NotEmpty();
            item.RuleFor(i => i.OrderedQty).GreaterThan(0m);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0m);
        });
    }
}

public sealed class EditSubmittedOrderByManagerCommandValidator : AbstractValidator<EditSubmittedOrderByManagerCommand>
{
    public EditSubmittedOrderByManagerCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SalesOrderItemId).NotEmpty();
            item.RuleFor(i => i.ApprovedQty).GreaterThan(0m);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0m);
        });
    }
}

public sealed class RejectSalesOrderCommandValidator : AbstractValidator<RejectSalesOrderCommand>
{
    public RejectSalesOrderCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500);
    }
}

public sealed class ConfirmOrderShipmentCommandValidator : AbstractValidator<ConfirmOrderShipmentCommand>
{
    private static readonly string[] ValidInvoiceTypes = ["StandardB2B", "SimplifiedB2C"];

    public ConfirmOrderShipmentCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("X-Idempotency-Key header with a valid GUID is required.");
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.EgsDeviceId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.InvoiceType)
            .Must(t => string.IsNullOrWhiteSpace(t) || ValidInvoiceTypes.Contains(t))
            .WithMessage("InvoiceType must be StandardB2B or SimplifiedB2C.");
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SalesOrderItemId).NotEmpty();
            item.RuleFor(i => i.ShippedQty).GreaterThan(0m);
        }).When(x => x.Request.Items is not null);
    }
}

public sealed class RegisterEgsDeviceCommandValidator : AbstractValidator<RegisterEgsDeviceCommand>
{
    public RegisterEgsDeviceCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.EgsDeviceId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.CertificateSerial).NotEmpty().MaximumLength(128);
    }
}

public sealed class UploadInvoiceCopyCommandValidator : AbstractValidator<UploadInvoiceCopyCommand>
{
    public UploadInvoiceCopyCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.UploadedInvoiceCopyUrl).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateSalesReturnCommandValidator : AbstractValidator<CreateSalesReturnCommand>
{
    public CreateSalesReturnCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.OriginalInvoiceId).NotEmpty();
        RuleFor(x => x.Request.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Request.ReturnReason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.EgsDeviceId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MasterProductId).NotEmpty();
            item.RuleFor(i => i.ProductUnitId).NotEmpty();
            item.RuleFor(i => i.ReturnedQty).GreaterThan(0m);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0m);
        });
    }
}

public sealed class CreateDeliveryDispatchCommandValidator : AbstractValidator<CreateDeliveryDispatchCommand>
{
    public CreateDeliveryDispatchCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Request.DriverOrRepId).NotEmpty();
        RuleFor(x => x.Request.VehiclePlateNumber).MaximumLength(30);
        RuleFor(x => x.Request.InvoiceIds).NotEmpty();
    }
}

public sealed class UpdateDeliveryPodCommandValidator : AbstractValidator<UpdateDeliveryPodCommand>
{
    private static readonly string[] ValidStatuses = ["PreparedAtWarehouse", "OutForDelivery", "Delivered", "FailedAttempt"];

    public UpdateDeliveryPodCommandValidator()
    {
        RuleFor(x => x.DispatchId).NotEmpty();
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.DeliveryStatus)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("DeliveryStatus must be PreparedAtWarehouse, OutForDelivery, Delivered, or FailedAttempt.");
        RuleFor(x => x.Request.SignedPodImageUrl).MaximumLength(500);
        RuleFor(x => x.Request.DeliveryNotes).MaximumLength(500);
    }
}

public sealed class GetSalesOrdersHandler(ISalesModuleService service) : IRequestHandler<GetSalesOrdersQuery, PagedResult<SalesOrderDto>>
{
    public Task<PagedResult<SalesOrderDto>> Handle(GetSalesOrdersQuery request, CancellationToken ct) => service.GetSalesOrdersAsync(request, ct);
}
public sealed class CreateSalesOrderHandler(ISalesModuleService service) : IRequestHandler<CreateSalesOrderCommand, SalesOrderDto>
{
    public Task<SalesOrderDto> Handle(CreateSalesOrderCommand request, CancellationToken ct) => service.CreateSalesOrderAsync(request, ct);
}
public sealed class EditSubmittedOrderByManagerHandler(ISalesModuleService service) : IRequestHandler<EditSubmittedOrderByManagerCommand, SalesOrderDto>
{
    public Task<SalesOrderDto> Handle(EditSubmittedOrderByManagerCommand request, CancellationToken ct) => service.EditSubmittedOrderByManagerAsync(request, ct);
}
public sealed class OverrideOrderPriceHandler(ISalesModuleService service) : IRequestHandler<OverrideOrderPriceCommand, SalesOrderDto>
{
    public Task<SalesOrderDto> Handle(OverrideOrderPriceCommand request, CancellationToken ct) => service.OverrideOrderPriceAsync(request, ct);
}
public sealed class CoApproveOrderCreditHandler(ISalesModuleService service) : IRequestHandler<CoApproveOrderCreditCommand, SalesOrderDto>
{
    public Task<SalesOrderDto> Handle(CoApproveOrderCreditCommand request, CancellationToken ct) => service.CoApproveOrderCreditAsync(request, ct);
}
public sealed class ApproveSalesOrderHandler(ISalesModuleService service) : IRequestHandler<ApproveSalesOrderCommand, SalesOrderDto>
{
    public Task<SalesOrderDto> Handle(ApproveSalesOrderCommand request, CancellationToken ct) => service.ApproveSalesOrderAsync(request, ct);
}
public sealed class RejectSalesOrderHandler(ISalesModuleService service) : IRequestHandler<RejectSalesOrderCommand, SalesOrderDto>
{
    public Task<SalesOrderDto> Handle(RejectSalesOrderCommand request, CancellationToken ct) => service.RejectSalesOrderAsync(request, ct);
}
public sealed class ConfirmOrderShipmentHandler(ISalesModuleService service) : IRequestHandler<ConfirmOrderShipmentCommand, InvoiceDto>
{
    public Task<InvoiceDto> Handle(ConfirmOrderShipmentCommand request, CancellationToken ct) => service.ConfirmOrderShipmentAsync(request, ct);
}
public sealed class RegisterEgsDeviceHandler(ISalesModuleService service) : IRequestHandler<RegisterEgsDeviceCommand, ZatcaDeviceSequenceDto>
{
    public Task<ZatcaDeviceSequenceDto> Handle(RegisterEgsDeviceCommand request, CancellationToken ct) => service.RegisterEgsDeviceAsync(request, ct);
}
public sealed class GetInvoicePrintHandler(ISalesModuleService service) : IRequestHandler<GetInvoicePrintQuery, InvoicePrintDto>
{
    public Task<InvoicePrintDto> Handle(GetInvoicePrintQuery request, CancellationToken ct) => service.GetInvoicePrintAsync(request.InvoiceId, ct);
}
public sealed class UploadInvoiceCopyHandler(ISalesModuleService service) : IRequestHandler<UploadInvoiceCopyCommand, InvoiceDto>
{
    public Task<InvoiceDto> Handle(UploadInvoiceCopyCommand request, CancellationToken ct) => service.UploadInvoiceCopyAsync(request, ct);
}
public sealed class CreateSalesReturnHandler(ISalesModuleService service) : IRequestHandler<CreateSalesReturnCommand, SalesReturnDto>
{
    public Task<SalesReturnDto> Handle(CreateSalesReturnCommand request, CancellationToken ct) => service.CreateSalesReturnAsync(request, ct);
}
public sealed class CreateDeliveryDispatchHandler(ISalesModuleService service) : IRequestHandler<CreateDeliveryDispatchCommand, DeliveryDispatchDto>
{
    public Task<DeliveryDispatchDto> Handle(CreateDeliveryDispatchCommand request, CancellationToken ct) => service.CreateDeliveryDispatchAsync(request, ct);
}
public sealed class UpdateDeliveryPodHandler(ISalesModuleService service) : IRequestHandler<UpdateDeliveryPodCommand, DeliveryDispatchDto>
{
    public Task<DeliveryDispatchDto> Handle(UpdateDeliveryPodCommand request, CancellationToken ct) => service.UpdateDeliveryPodAsync(request, ct);
}
