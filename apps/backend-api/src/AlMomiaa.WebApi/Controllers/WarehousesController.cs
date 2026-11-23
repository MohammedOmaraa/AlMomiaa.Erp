using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Inventory;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/warehouses")]
[Tags("Inventory & Warehouses")]
public sealed class WarehousesController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.InventoryView)]
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WarehouseDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List warehouses (scoped to assigned warehouses for Warehouse Keepers)")]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> GetWarehouses(CancellationToken ct)
    {
        var result = await mediator.Send(new GetWarehousesQuery(), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.SettingsManage)]
    [HttpPost]
    [ProducesResponseType<WarehouseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a MainWarehouse, BranchWarehouse, or CashVan MobileVan warehouse")]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] CreateWarehouseRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateWarehouseCommand(request), ct);
        return Created($"/api/v1/warehouses/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.InventoryView)]
    [HttpGet("{id:int}/stocks")]
    [ProducesResponseType<PagedResult<WarehouseStockDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get physical, reserved, incoming, and available stock balances for a warehouse")]
    public async Task<ActionResult<PagedResult<WarehouseStockDto>>> GetWarehouseStocks(
        int id,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetWarehouseStocksQuery(id, search, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.InventoryTransfer)]
    [HttpPost("transfers")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Dispatch a stock transfer between warehouses or load a CashVan mobile warehouse")]
    public async Task<ActionResult<StockTransferDto>> CreateTransfer([FromBody] CreateStockTransferRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateStockTransferCommand(request), ct);
        return Created($"/api/v1/warehouses/transfers/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.InventoryTransfer)]
    [HttpPut("transfers/{id:guid}/receive")]
    [ProducesResponseType<StockTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Receive an in-transit stock transfer at the destination warehouse")]
    public async Task<ActionResult<StockTransferDto>> ReceiveTransfer(
        Guid id,
        [FromBody] ReceiveStockTransferRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new ReceiveStockTransferCommand(id, request), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.InventoryAdjust)]
    [HttpPost("{id:int}/inventory-count")]
    [ProducesResponseType<IReadOnlyList<WarehouseStockDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Submit physical inventory count adjustments and append immutable StockMovements")]
    public async Task<ActionResult<IReadOnlyList<WarehouseStockDto>>> SubmitInventoryCount(
        int id,
        [FromBody] SubmitInventoryCountRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new SubmitInventoryCountCommand(id, request), ct);
        return Ok(updated);
    }
}
