using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Procurement;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/procurement")]
[Tags("Procurement & Containers")]
public sealed class ProcurementController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.ProcurementManage)]
    [HttpGet("suppliers")]
    [ProducesResponseType<PagedResult<SupplierDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List suppliers with optional search filter")]
    public async Task<ActionResult<PagedResult<SupplierDto>>> GetSuppliers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetSuppliersQuery(search, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.ProcurementManage)]
    [HttpPost("suppliers")]
    [ProducesResponseType<SupplierDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a local or international supplier")]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateSupplierCommand(request), ct);
        return Created($"/api/v1/procurement/suppliers/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.ProcurementManage)]
    [HttpGet("purchase-orders")]
    [ProducesResponseType<PagedResult<PurchaseOrderDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List purchase orders with optional status filter")]
    public async Task<ActionResult<PagedResult<PurchaseOrderDto>>> GetPurchaseOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetPurchaseOrdersQuery(status, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.ProcurementManage)]
    [HttpPost("purchase-orders")]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Create a purchase order and increment incoming stock quantities")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreatePurchaseOrderCommand(request), ct);
        return Created($"/api/v1/procurement/purchase-orders/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.ProcurementManage)]
    [HttpPost("containers")]
    [ProducesResponseType<ContainerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Register an import shipping container linked to a purchase order")]
    public async Task<ActionResult<ContainerDto>> CreateContainer([FromBody] CreateContainerRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateContainerCommand(request), ct);
        return Created($"/api/v1/procurement/containers/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.ProcurementReceive)]
    [HttpPost("goods-receipts")]
    [ProducesResponseType<GoodsReceiptDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Record a Goods Receipt Note (GRN), recalculate WeightedAvgCost, update stock, and append StockMovements")]
    public async Task<ActionResult<GoodsReceiptDto>> CreateGoodsReceipt([FromBody] CreateGoodsReceiptRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateGoodsReceiptCommand(request), ct);
        return Created($"/api/v1/procurement/goods-receipts/{created.Id}", created);
    }
}
