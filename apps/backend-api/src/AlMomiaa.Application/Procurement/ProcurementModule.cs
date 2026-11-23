using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Procurement;

public sealed record SupplierDto(Guid Id, string SupplierCode, string NameAr, string? NameEn, string Country, string? Phone, string? VatNumber, decimal CurrentPayableBalance, bool IsActive);
public sealed record PurchaseOrderItemDto(Guid Id, Guid MasterProductId, decimal OrderedQty, decimal ReceivedQty, decimal UnitCostSAR, decimal LineTotalSAR);
public sealed record PurchaseOrderDto(Guid Id, string PoNumber, Guid SupplierId, int TargetWarehouseId, string OrderType, string Status, decimal TotalAmountSAR, Guid? ApprovedByUserId, DateTime CreatedAtUtc, IReadOnlyList<PurchaseOrderItemDto> Items);
public sealed record ContainerDto(Guid Id, Guid PurchaseOrderId, string ContainerNumber, string? BillOfLading, string? ShippingLine, string PortOfArrival, DateOnly EstimatedArrivalDate, DateOnly? ActualArrivalDate, string Status, decimal SupplierGoodsCostSAR, decimal FreightAndCustomsCostSAR, decimal TotalLandedCostSAR);
public sealed record GoodsReceiptItemDto(Guid Id, Guid MasterProductId, decimal ReceivedQty, decimal AllocatedLandedUnitCostSAR, string? LotNumber, DateOnly? ExpiryDate);
public sealed record GoodsReceiptDto(Guid Id, string GrnNumber, Guid PurchaseOrderId, Guid? ContainerId, int WarehouseId, Guid ReceivedByUserId, DateTime ReceivedAtUtc, IReadOnlyList<GoodsReceiptItemDto> Items);

public sealed record GetSuppliersQuery(string? Search, int Page = 1, int PageSize = 50) : IRequest<PagedResult<SupplierDto>>;
public sealed record CreateSupplierRequest(string SupplierCode, string NameAr, string? NameEn, string? Country, string? Phone, string? VatNumber);
public sealed record CreateSupplierCommand(CreateSupplierRequest Request) : IRequest<SupplierDto>;

public sealed record GetPurchaseOrdersQuery(string? Status, int Page = 1, int PageSize = 50) : IRequest<PagedResult<PurchaseOrderDto>>;
public sealed record CreatePurchaseOrderItemRequest(Guid MasterProductId, decimal OrderedQty, decimal UnitCostSAR);
public sealed record CreatePurchaseOrderRequest(Guid SupplierId, int TargetWarehouseId, string OrderType, CreatePurchaseOrderItemRequest[] Items);
public sealed record CreatePurchaseOrderCommand(CreatePurchaseOrderRequest Request) : IRequest<PurchaseOrderDto>;

public sealed record CreateContainerRequest(
    Guid PurchaseOrderId,
    string ContainerNumber,
    string? BillOfLading,
    string? ShippingLine,
    string PortOfArrival,
    DateOnly EstimatedArrivalDate,
    DateOnly? ActualArrivalDate,
    string Status,
    decimal SupplierGoodsCostSAR,
    decimal FreightAndCustomsCostSAR);
public sealed record CreateContainerCommand(CreateContainerRequest Request) : IRequest<ContainerDto>;

public sealed record CreateGoodsReceiptItemRequest(Guid MasterProductId, decimal ReceivedQty, decimal AllocatedLandedUnitCostSAR, string? LotNumber, DateOnly? ExpiryDate);
public sealed record CreateGoodsReceiptRequest(Guid PurchaseOrderId, Guid? ContainerId, int WarehouseId, CreateGoodsReceiptItemRequest[] Items);
public sealed record CreateGoodsReceiptCommand(CreateGoodsReceiptRequest Request) : IRequest<GoodsReceiptDto>;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.SupplierCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.NameEn).MaximumLength(200);
        RuleFor(x => x.Request.Country).MaximumLength(100);
        RuleFor(x => x.Request.Phone).MaximumLength(30);
        RuleFor(x => x.Request.VatNumber).MaximumLength(20);
    }
}

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    private static readonly string[] ValidOrderTypes = ["ContainerImport", "LocalSupplierOrder"];

    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.SupplierId).NotEmpty();
        RuleFor(x => x.Request.TargetWarehouseId).GreaterThan(0);
        RuleFor(x => x.Request.OrderType)
            .NotEmpty()
            .Must(t => ValidOrderTypes.Contains(t))
            .WithMessage("OrderType must be ContainerImport or LocalSupplierOrder.");
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MasterProductId).NotEmpty();
            item.RuleFor(i => i.OrderedQty).GreaterThan(0m);
            item.RuleFor(i => i.UnitCostSAR).GreaterThan(0m);
        });
    }
}

public sealed class CreateContainerCommandValidator : AbstractValidator<CreateContainerCommand>
{
    private static readonly string[] ValidStatuses = ["AtSea", "ArrivedPort", "CustomsClearance", "InTransitRiyadh", "ReceivedAtWarehouse"];

    public CreateContainerCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Request.ContainerNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.BillOfLading).MaximumLength(100);
        RuleFor(x => x.Request.ShippingLine).MaximumLength(100);
        RuleFor(x => x.Request.PortOfArrival).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Container Status must be AtSea, ArrivedPort, CustomsClearance, InTransitRiyadh, or ReceivedAtWarehouse.");
        RuleFor(x => x.Request.SupplierGoodsCostSAR).GreaterThan(0m);
        RuleFor(x => x.Request.FreightAndCustomsCostSAR).GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateGoodsReceiptCommandValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Request.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MasterProductId).NotEmpty();
            item.RuleFor(i => i.ReceivedQty).GreaterThan(0m);
            item.RuleFor(i => i.AllocatedLandedUnitCostSAR).GreaterThan(0m);
            item.RuleFor(i => i.LotNumber).MaximumLength(50);
        });
    }
}

public sealed class GetSuppliersHandler(IProcurementModuleService service) : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierDto>>
{
    public Task<PagedResult<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken ct) => service.GetSuppliersAsync(request, ct);
}

public sealed class CreateSupplierHandler(IProcurementModuleService service) : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    public Task<SupplierDto> Handle(CreateSupplierCommand request, CancellationToken ct) => service.CreateSupplierAsync(request, ct);
}

public sealed class GetPurchaseOrdersHandler(IProcurementModuleService service) : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderDto>>
{
    public Task<PagedResult<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken ct) => service.GetPurchaseOrdersAsync(request, ct);
}

public sealed class CreatePurchaseOrderHandler(IProcurementModuleService service) : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    public Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken ct) => service.CreatePurchaseOrderAsync(request, ct);
}

public sealed class CreateContainerHandler(IProcurementModuleService service) : IRequestHandler<CreateContainerCommand, ContainerDto>
{
    public Task<ContainerDto> Handle(CreateContainerCommand request, CancellationToken ct) => service.CreateContainerAsync(request, ct);
}

public sealed class CreateGoodsReceiptHandler(IProcurementModuleService service) : IRequestHandler<CreateGoodsReceiptCommand, GoodsReceiptDto>
{
    public Task<GoodsReceiptDto> Handle(CreateGoodsReceiptCommand request, CancellationToken ct) => service.CreateGoodsReceiptAsync(request, ct);
}
