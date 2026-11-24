using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Catalog;

public sealed record CategoryNodeDto(int Id, int? ParentCategoryId, string NameAr, string? NameEn, int DisplayOrder, bool IsActive, IReadOnlyList<CategoryNodeDto> Children);
public sealed record BrandDto(int Id, string NameAr, string? NameEn, bool IsActive);

public sealed record ProductUnitDto(
    Guid Id,
    string UnitNameAr,
    string UnitNameEn,
    decimal ConversionFactor,
    string Barcode,
    decimal WholesalePrice,
    decimal RetailPrice,
    decimal MinAllowedPrice,
    bool IsDefaultUnit);

public sealed record MasterProductDto(
    Guid Id,
    string SKU,
    string Barcode,
    string NameAr,
    string? NameEn,
    int CategoryId,
    int BrandId,
    string BaseUnit,
    decimal? WeightedAvgCost,
    decimal WholesaleSellingPrice,
    decimal RetailSellingPrice,
    decimal MinAllowedSellingPrice,
    decimal VatRate,
    bool IsActive,
    IReadOnlyList<ProductUnitDto> Units);

public sealed record ProductWarehouseStockBreakdownDto(
    int WarehouseId,
    string WarehouseCode,
    string WarehouseNameAr,
    string? WarehouseNameEn,
    string WarehouseType,
    decimal PhysicalQty,
    decimal ReservedQty,
    decimal IncomingQty,
    decimal AvailableQty,
    decimal MinReorderQty);

public sealed record CategoryProductPriceDto(
    Guid MasterProductId,
    string SKU,
    string NameAr,
    string? NameEn,
    Guid DefaultUnitId,
    decimal ResolvedUnitPrice,
    decimal MinAllowedPrice,
    decimal AvailableQty,
    string BaseUnit = "Piece",
    string DefaultUnitNameAr = "حبة",
    string DefaultUnitNameEn = "Piece",
    decimal TotalPhysicalQty = 0m,
    decimal TotalAvailableQty = 0m,
    IReadOnlyList<ProductWarehouseStockBreakdownDto>? WarehouseStocks = null);

public sealed record ProductAvailabilityDto(
    Guid MasterProductId,
    string SKU,
    string NameAr,
    string? NameEn,
    string BaseUnit,
    decimal WholesaleSellingPrice,
    decimal RetailSellingPrice,
    decimal MinAllowedSellingPrice,
    decimal VatRate,
    decimal TotalPhysicalQty,
    decimal TotalReservedQty,
    decimal TotalIncomingQty,
    decimal TotalAvailableQty,
    IReadOnlyList<ProductUnitDto> Units,
    IReadOnlyList<ProductWarehouseStockBreakdownDto> WarehouseStocks);

public sealed record SupplierProductDto(
    Guid Id,
    Guid MasterProductId,
    Guid SupplierId,
    string? SupplierSku,
    string? SupplierBarcode,
    decimal LastPurchasePrice,
    int? LeadTimeDays,
    bool IsPreferredSupplier,
    DateTime UpdatedAtUtc);

public sealed record PriceListItemDto(Guid Id, Guid ProductUnitId, decimal MinQuantity, decimal TierUnitPrice);
public sealed record PriceListDto(int Id, string PriceListCode, string NameAr, string? NameEn, bool IsDefault, bool IsActive, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, IReadOnlyList<PriceListItemDto> Items);

public sealed record GetCategoryTreeQuery : IRequest<IReadOnlyList<CategoryNodeDto>>;
public sealed record CreateCategoryRequest(int? ParentCategoryId, string NameAr, string? NameEn, int DisplayOrder = 0);
public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<CategoryNodeDto>;

public sealed record GetBrandsQuery : IRequest<IReadOnlyList<BrandDto>>;
public sealed record CreateBrandRequest(string NameAr, string? NameEn);
public sealed record CreateBrandCommand(CreateBrandRequest Request) : IRequest<BrandDto>;

public sealed record GetProductsQuery(string? Search, int? CategoryId, int? BrandId, bool? IsActive, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<MasterProductDto>>;

public sealed record GetCategoryProductsQuery(int CategoryId, Guid? CustomerId, int? WarehouseId, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<CategoryProductPriceDto>>;

public sealed record GetProductAvailabilityQuery(Guid MasterProductId) : IRequest<ProductAvailabilityDto>;

public sealed record CreateProductUnitRequest(
    string UnitNameAr,
    string UnitNameEn,
    decimal ConversionFactor,
    string Barcode,
    decimal WholesalePrice,
    decimal RetailPrice,
    decimal MinAllowedPrice,
    bool IsDefaultUnit);

public sealed record InitialWarehouseStockRequest(
    int WarehouseId,
    decimal InitialQty,
    decimal MinReorderQty = 0m);

public sealed record CreateMasterProductRequest(
    string SKU,
    string Barcode,
    string NameAr,
    string? NameEn,
    int CategoryId,
    int BrandId,
    string BaseUnit,
    decimal WeightedAvgCost,
    decimal WholesaleSellingPrice,
    decimal RetailSellingPrice,
    decimal MinAllowedSellingPrice,
    decimal VatRate,
    CreateProductUnitRequest[] Units,
    InitialWarehouseStockRequest[]? InitialWarehouseStocks = null);

public sealed record CreateMasterProductCommand(CreateMasterProductRequest Request) : IRequest<MasterProductDto>;

public sealed record LinkSupplierProductRequest(
    Guid SupplierId,
    string? SupplierSku,
    string? SupplierBarcode,
    decimal LastPurchasePrice,
    int? LeadTimeDays,
    bool IsPreferredSupplier);

public sealed record LinkSupplierProductCommand(Guid MasterProductId, LinkSupplierProductRequest Request) : IRequest<SupplierProductDto>;

public sealed record GetPriceListsQuery : IRequest<IReadOnlyList<PriceListDto>>;

public sealed record CreatePriceListItemRequest(Guid ProductUnitId, decimal MinQuantity, decimal TierUnitPrice);
public sealed record CreatePriceListRequest(
    string PriceListCode,
    string NameAr,
    string? NameEn,
    bool IsDefault,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    CreatePriceListItemRequest[]? Items);

public sealed record CreatePriceListCommand(CreatePriceListRequest Request) : IRequest<PriceListDto>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.NameEn).MaximumLength(150);
        RuleFor(x => x.Request.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.NameEn).MaximumLength(100);
    }
}

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 250);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public sealed class CreateMasterProductCommandValidator : AbstractValidator<CreateMasterProductCommand>
{
    public CreateMasterProductCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.SKU).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Barcode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Request.NameEn).MaximumLength(250);
        RuleFor(x => x.Request.CategoryId).GreaterThan(0);
        RuleFor(x => x.Request.BrandId).GreaterThan(0);
        RuleFor(x => x.Request.BaseUnit).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.WeightedAvgCost).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Request.WholesaleSellingPrice).GreaterThan(0m);
        RuleFor(x => x.Request.RetailSellingPrice).GreaterThan(0m);
        RuleFor(x => x.Request.MinAllowedSellingPrice)
            .GreaterThan(0m)
            .LessThanOrEqualTo(x => x.Request.WholesaleSellingPrice)
            .LessThanOrEqualTo(x => x.Request.RetailSellingPrice)
            .WithMessage("MinAllowedSellingPrice must not exceed WholesaleSellingPrice or RetailSellingPrice.");
        RuleFor(x => x.Request.VatRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.Request.Units).NotEmpty()
            .Must(units => units.Count(u => u.IsDefaultUnit) == 1)
            .WithMessage("Exactly one default ProductUnit must be specified.");

        RuleForEach(x => x.Request.Units).ChildRules(unit =>
        {
            unit.RuleFor(u => u.UnitNameAr).NotEmpty().MaximumLength(50);
            unit.RuleFor(u => u.UnitNameEn).NotEmpty().MaximumLength(50);
            unit.RuleFor(u => u.ConversionFactor).GreaterThan(0m);
            unit.RuleFor(u => u.Barcode).NotEmpty().MaximumLength(50);
            unit.RuleFor(u => u.WholesalePrice).GreaterThan(0m);
            unit.RuleFor(u => u.RetailPrice).GreaterThan(0m);
            unit.RuleFor(u => u.MinAllowedPrice)
                .GreaterThan(0m)
                .LessThanOrEqualTo(u => u.WholesalePrice)
                .LessThanOrEqualTo(u => u.RetailPrice);
        });

        RuleFor(x => x.Request.InitialWarehouseStocks)
            .Must(list => list is null || list.Select(i => i.WarehouseId).Distinct().Count() == list.Length)
            .WithMessage("Duplicate WarehouseId entries are not allowed in InitialWarehouseStocks.");

        RuleForEach(x => x.Request.InitialWarehouseStocks).ChildRules(ws =>
        {
            ws.RuleFor(w => w.WarehouseId).GreaterThan(0);
            ws.RuleFor(w => w.InitialQty).GreaterThanOrEqualTo(0m);
            ws.RuleFor(w => w.MinReorderQty).GreaterThanOrEqualTo(0m);
        }).When(x => x.Request.InitialWarehouseStocks is not null);
    }
}

public sealed class LinkSupplierProductCommandValidator : AbstractValidator<LinkSupplierProductCommand>
{
    public LinkSupplierProductCommandValidator()
    {
        RuleFor(x => x.MasterProductId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.SupplierId).NotEmpty();
        RuleFor(x => x.Request.SupplierSku).MaximumLength(50);
        RuleFor(x => x.Request.SupplierBarcode).MaximumLength(50);
        RuleFor(x => x.Request.LastPurchasePrice).GreaterThan(0m);
        RuleFor(x => x.Request.LeadTimeDays).InclusiveBetween(0, 365).When(x => x.Request.LeadTimeDays.HasValue);
    }
}

public sealed class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.PriceListCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.NameEn).MaximumLength(150);
        RuleFor(x => x.Request.EffectiveToUtc)
            .GreaterThan(x => x.Request.EffectiveFromUtc!.Value)
            .When(x => x.Request.EffectiveFromUtc.HasValue && x.Request.EffectiveToUtc.HasValue);

        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductUnitId).NotEmpty();
            item.RuleFor(i => i.MinQuantity).GreaterThan(0m);
            item.RuleFor(i => i.TierUnitPrice).GreaterThan(0m);
        }).When(x => x.Request.Items is not null);
    }
}

public sealed class GetCategoryTreeHandler(ICatalogModuleService service) : IRequestHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryNodeDto>>
{
    public Task<IReadOnlyList<CategoryNodeDto>> Handle(GetCategoryTreeQuery request, CancellationToken ct) => service.GetCategoryTreeAsync(ct);
}

public sealed class CreateCategoryHandler(ICatalogModuleService service) : IRequestHandler<CreateCategoryCommand, CategoryNodeDto>
{
    public Task<CategoryNodeDto> Handle(CreateCategoryCommand request, CancellationToken ct) => service.CreateCategoryAsync(request, ct);
}

public sealed class GetBrandsHandler(ICatalogModuleService service) : IRequestHandler<GetBrandsQuery, IReadOnlyList<BrandDto>>
{
    public Task<IReadOnlyList<BrandDto>> Handle(GetBrandsQuery request, CancellationToken ct) => service.GetBrandsAsync(ct);
}

public sealed class CreateBrandHandler(ICatalogModuleService service) : IRequestHandler<CreateBrandCommand, BrandDto>
{
    public Task<BrandDto> Handle(CreateBrandCommand request, CancellationToken ct) => service.CreateBrandAsync(request, ct);
}

public sealed class GetProductsHandler(ICatalogModuleService service) : IRequestHandler<GetProductsQuery, PagedResult<MasterProductDto>>
{
    public Task<PagedResult<MasterProductDto>> Handle(GetProductsQuery request, CancellationToken ct) => service.GetProductsAsync(request, ct);
}

public sealed class GetCategoryProductsHandler(ICatalogModuleService service) : IRequestHandler<GetCategoryProductsQuery, PagedResult<CategoryProductPriceDto>>
{
    public Task<PagedResult<CategoryProductPriceDto>> Handle(GetCategoryProductsQuery request, CancellationToken ct) => service.GetCategoryProductsAsync(request, ct);
}

public sealed class GetProductAvailabilityHandler(ICatalogModuleService service) : IRequestHandler<GetProductAvailabilityQuery, ProductAvailabilityDto>
{
    public Task<ProductAvailabilityDto> Handle(GetProductAvailabilityQuery request, CancellationToken ct) => service.GetProductAvailabilityAsync(request.MasterProductId, ct);
}

public sealed class CreateMasterProductHandler(ICatalogModuleService service) : IRequestHandler<CreateMasterProductCommand, MasterProductDto>
{
    public Task<MasterProductDto> Handle(CreateMasterProductCommand request, CancellationToken ct) => service.CreateMasterProductAsync(request, ct);
}

public sealed class LinkSupplierProductHandler(ICatalogModuleService service) : IRequestHandler<LinkSupplierProductCommand, SupplierProductDto>
{
    public Task<SupplierProductDto> Handle(LinkSupplierProductCommand request, CancellationToken ct) => service.LinkSupplierProductAsync(request, ct);
}

public sealed class GetPriceListsHandler(ICatalogModuleService service) : IRequestHandler<GetPriceListsQuery, IReadOnlyList<PriceListDto>>
{
    public Task<IReadOnlyList<PriceListDto>> Handle(GetPriceListsQuery request, CancellationToken ct) => service.GetPriceListsAsync(ct);
}

public sealed class CreatePriceListHandler(ICatalogModuleService service) : IRequestHandler<CreatePriceListCommand, PriceListDto>
{
    public Task<PriceListDto> Handle(CreatePriceListCommand request, CancellationToken ct) => service.CreatePriceListAsync(request, ct);
}
