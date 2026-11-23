using AlMomiaa.Application.Catalog;
using AlMomiaa.Application.Common.Models;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/catalog")]
[Tags("Catalog & Pricing")]
public sealed class CatalogController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogView)]
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<CategoryNodeDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("Get hierarchical product category tree")]
    public async Task<ActionResult<IReadOnlyList<CategoryNodeDto>>> GetCategories(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCategoryTreeQuery(), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogManage)]
    [HttpPost("categories")]
    [ProducesResponseType<CategoryNodeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Create a product category or subcategory")]
    public async Task<ActionResult<CategoryNodeDto>> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateCategoryCommand(request), ct);
        return Created($"/api/v1/catalog/categories/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogView)]
    [HttpGet("brands")]
    [ProducesResponseType<IReadOnlyList<BrandDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List product brands")]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetBrands(CancellationToken ct)
    {
        var result = await mediator.Send(new GetBrandsQuery(), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogManage)]
    [HttpPost("brands")]
    [ProducesResponseType<BrandDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Create a product brand")]
    public async Task<ActionResult<BrandDto>> CreateBrand([FromBody] CreateBrandRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateBrandCommand(request), ct);
        return Created($"/api/v1/catalog/brands/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogView)]
    [HttpGet("products")]
    [ProducesResponseType<PagedResult<MasterProductDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List master products with units (WeightedAvgCost masked for non-cost-authorized roles)")]
    public async Task<ActionResult<PagedResult<MasterProductDto>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? brandId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetProductsQuery(search, categoryId, brandId, isActive, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogView)]
    [HttpGet("categories/{categoryId:int}/products")]
    [ProducesResponseType<PagedResult<CategoryProductPriceDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List category products with customer tier price resolution and live warehouse availability")]
    public async Task<ActionResult<PagedResult<CategoryProductPriceDto>>> GetCategoryProducts(
        int categoryId,
        [FromQuery] Guid? customerId,
        [FromQuery] int? warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCategoryProductsQuery(categoryId, customerId, warehouseId, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogManage)]
    [HttpPost("products")]
    [ProducesResponseType<MasterProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a master product with multi-unit definitions (Carton/Box/Piece)")]
    public async Task<ActionResult<MasterProductDto>> CreateProduct([FromBody] CreateMasterProductRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateMasterProductCommand(request), ct);
        return Created($"/api/v1/catalog/products/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogManage)]
    [HttpPost("products/{id:guid}/suppliers")]
    [ProducesResponseType<SupplierProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Link a master product to a supplier with supplier SKU, barcode, and purchase price")]
    public async Task<ActionResult<SupplierProductDto>> LinkSupplierProduct(
        Guid id,
        [FromBody] LinkSupplierProductRequest request,
        CancellationToken ct)
    {
        var linked = await mediator.Send(new LinkSupplierProductCommand(id, request), ct);
        return Created($"/api/v1/catalog/products/{id}/suppliers/{linked.Id}", linked);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogView)]
    [HttpGet("price-lists")]
    [ProducesResponseType<IReadOnlyList<PriceListDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List active tiered price lists and unit pricing rules")]
    public async Task<ActionResult<IReadOnlyList<PriceListDto>>> GetPriceLists(CancellationToken ct)
    {
        var result = await mediator.Send(new GetPriceListsQuery(), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CatalogManage)]
    [HttpPost("price-lists")]
    [ProducesResponseType<PriceListDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a tiered price list (MR-02) with product-unit minimum quantity pricing")]
    public async Task<ActionResult<PriceListDto>> CreatePriceList([FromBody] CreatePriceListRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreatePriceListCommand(request), ct);
        return Created($"/api/v1/catalog/price-lists/{created.Id}", created);
    }
}
