using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Inventory;

public sealed record WarehouseDto(int Id, string WarehouseCode, string NameAr, string? NameEn, string WarehouseType, Guid? AssignedRepId, string? VehiclePlateNumber, string City, bool IsActive);

public sealed record WarehouseStockDto(
    Guid Id,
    int WarehouseId,
    Guid MasterProductId,
    string SKU,
    string ProductNameAr,
    decimal PhysicalQty,
    decimal ReservedQty,
    decimal IncomingQty,
    decimal AvailableQty,
    decimal MinReorderQty);

public sealed record StockTransferItemDto(Guid Id, Guid MasterProductId, decimal RequestedQty, decimal DispatchedQty, decimal? ReceivedQty);

public sealed record StockTransferDto(
    Guid Id,
    string TransferNumber,
    int SourceWarehouseId,
    int DestinationWarehouseId,
    string Status,
    Guid DispatchedByUserId,
    DateTime DispatchedAtUtc,
    Guid? ReceivedByUserId,
    DateTime? ReceivedAtUtc,
    IReadOnlyList<StockTransferItemDto> Items);

public sealed record GetWarehousesQuery : IRequest<IReadOnlyList<WarehouseDto>>;

public sealed record CreateWarehouseRequest(
    string WarehouseCode,
    string NameAr,
    string? NameEn,
    string WarehouseType,
    Guid? AssignedRepId,
    string? VehiclePlateNumber,
    string? City);

public sealed record CreateWarehouseCommand(CreateWarehouseRequest Request) : IRequest<WarehouseDto>;

public sealed record GetWarehouseStocksQuery(int WarehouseId, string? Search, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<WarehouseStockDto>>;

public sealed record CreateStockTransferItemRequest(Guid MasterProductId, decimal RequestedQty, decimal DispatchedQty);
public sealed record CreateStockTransferRequest(
    string? TransferNumber,
    int SourceWarehouseId,
    int DestinationWarehouseId,
    CreateStockTransferItemRequest[] Items);
public sealed record CreateStockTransferCommand(CreateStockTransferRequest Request) : IRequest<StockTransferDto>;

public sealed record ReceiveStockTransferItemRequest(Guid StockTransferItemId, decimal ReceivedQty);
public sealed record ReceiveStockTransferRequest(ReceiveStockTransferItemRequest[] Items);
public sealed record ReceiveStockTransferCommand(Guid StockTransferId, ReceiveStockTransferRequest Request) : IRequest<StockTransferDto>;

public sealed record InventoryCountItemRequest(Guid MasterProductId, decimal CountedPhysicalQty, string? LotNumber, DateOnly? ExpiryDate);
public sealed record SubmitInventoryCountRequest(InventoryCountItemRequest[] Items);
public sealed record SubmitInventoryCountCommand(int WarehouseId, SubmitInventoryCountRequest Request) : IRequest<IReadOnlyList<WarehouseStockDto>>;

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    private static readonly string[] ValidTypes = ["MainWarehouse", "BranchWarehouse", "MobileVan"];

    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.WarehouseCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.NameEn).MaximumLength(150);
        RuleFor(x => x.Request.WarehouseType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("WarehouseType must be MainWarehouse, BranchWarehouse, or MobileVan.");
        RuleFor(x => x.Request.AssignedRepId)
            .NotEmpty()
            .When(x => x.Request.WarehouseType == "MobileVan")
            .WithMessage("AssignedRepId is required when WarehouseType is MobileVan.");
        RuleFor(x => x.Request.VehiclePlateNumber).MaximumLength(30);
        RuleFor(x => x.Request.City).MaximumLength(100);
    }
}

public sealed class GetWarehouseStocksQueryValidator : AbstractValidator<GetWarehouseStocksQuery>
{
    public GetWarehouseStocksQueryValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 250);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public sealed class CreateStockTransferCommandValidator : AbstractValidator<CreateStockTransferCommand>
{
    public CreateStockTransferCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.TransferNumber).MaximumLength(30);
        RuleFor(x => x.Request.SourceWarehouseId).GreaterThan(0);
        RuleFor(x => x.Request.DestinationWarehouseId)
            .GreaterThan(0)
            .NotEqual(x => x.Request.SourceWarehouseId)
            .WithMessage("DestinationWarehouseId must differ from SourceWarehouseId.");
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MasterProductId).NotEmpty();
            item.RuleFor(i => i.RequestedQty).GreaterThan(0m);
            item.RuleFor(i => i.DispatchedQty).GreaterThan(0m).LessThanOrEqualTo(i => i.RequestedQty);
        });
    }
}

public sealed class ReceiveStockTransferCommandValidator : AbstractValidator<ReceiveStockTransferCommand>
{
    public ReceiveStockTransferCommandValidator()
    {
        RuleFor(x => x.StockTransferId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StockTransferItemId).NotEmpty();
            item.RuleFor(i => i.ReceivedQty).GreaterThan(0m);
        });
    }
}

public sealed class SubmitInventoryCountCommandValidator : AbstractValidator<SubmitInventoryCountCommand>
{
    public SubmitInventoryCountCommandValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MasterProductId).NotEmpty();
            item.RuleFor(i => i.CountedPhysicalQty).GreaterThanOrEqualTo(0m);
            item.RuleFor(i => i.LotNumber).MaximumLength(50);
        });
    }
}

public sealed class GetWarehousesHandler(IInventoryModuleService service) : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    public Task<IReadOnlyList<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken ct) => service.GetWarehousesAsync(ct);
}

public sealed class CreateWarehouseHandler(IInventoryModuleService service) : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    public Task<WarehouseDto> Handle(CreateWarehouseCommand request, CancellationToken ct) => service.CreateWarehouseAsync(request, ct);
}

public sealed class GetWarehouseStocksHandler(IInventoryModuleService service) : IRequestHandler<GetWarehouseStocksQuery, PagedResult<WarehouseStockDto>>
{
    public Task<PagedResult<WarehouseStockDto>> Handle(GetWarehouseStocksQuery request, CancellationToken ct) => service.GetWarehouseStocksAsync(request, ct);
}

public sealed class CreateStockTransferHandler(IInventoryModuleService service) : IRequestHandler<CreateStockTransferCommand, StockTransferDto>
{
    public Task<StockTransferDto> Handle(CreateStockTransferCommand request, CancellationToken ct) => service.CreateStockTransferAsync(request, ct);
}

public sealed class ReceiveStockTransferHandler(IInventoryModuleService service) : IRequestHandler<ReceiveStockTransferCommand, StockTransferDto>
{
    public Task<StockTransferDto> Handle(ReceiveStockTransferCommand request, CancellationToken ct) => service.ReceiveStockTransferAsync(request, ct);
}

public sealed class SubmitInventoryCountHandler(IInventoryModuleService service) : IRequestHandler<SubmitInventoryCountCommand, IReadOnlyList<WarehouseStockDto>>
{
    public Task<IReadOnlyList<WarehouseStockDto>> Handle(SubmitInventoryCountCommand request, CancellationToken ct) => service.SubmitInventoryCountAsync(request, ct);
}
