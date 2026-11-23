using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Sales;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Tags("Sales Orders & Approval Workflow")]
public sealed class SalesOrdersController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CustomersView)]
    [HttpGet]
    [ProducesResponseType<PagedResult<SalesOrderDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List sales orders (row-scoped for Sales Reps and Warehouse Keepers)")]
    public async Task<ActionResult<PagedResult<SalesOrderDto>>> GetOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetSalesOrdersQuery(status, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.OrdersCreate)]
    [HttpPost]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Submit a sales order (requires X-Idempotency-Key GUID; evaluates MR-01/MR-02/MR-03/RISK-01/RISK-03)")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateSalesOrderRequest request, CancellationToken ct)
    {
        if (!IdempotencyHeaderHelper.TryParse(Request, out var idempotencyKey, out var error))
        {
            return error!;
        }

        var created = await mediator.Send(new CreateSalesOrderCommand(request, idempotencyKey), ct);
        return Created($"/api/v1/orders/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.OrdersEditSubmitted)]
    [HttpPut("{id:guid}/manager-edit")]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Edit quantities or prices on a submitted order (Sales Manager / Super Admin only; reconciles reservations and audits diff)")]
    public async Task<ActionResult<SalesOrderDto>> ManagerEdit(
        Guid id,
        [FromBody] ManagerEditOrderRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new EditSubmittedOrderByManagerCommand(id, request), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.OrdersOverridePrice)]
    [HttpPost("{id:guid}/override-price")]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Approve pending price override lines below tier or floor price (MR-02)")]
    public async Task<ActionResult<SalesOrderDto>> OverridePrice(Guid id, CancellationToken ct)
    {
        var updated = await mediator.Send(new OverrideOrderPriceCommand(id), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.OrdersCoapproveCredit)]
    [HttpPost("{id:guid}/co-approve-credit")]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Finance/Executive credit co-approval for credit-limit or overdue-debt exceptions (MR-03)")]
    public async Task<ActionResult<SalesOrderDto>> CoApproveCredit(Guid id, CancellationToken ct)
    {
        var updated = await mediator.Send(new CoApproveOrderCreditCommand(id), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.OrdersApprove)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Approve a pending sales order after all credit, price override, and stock checks are satisfied")]
    public async Task<ActionResult<SalesOrderDto>> ApproveOrder(Guid id, CancellationToken ct)
    {
        var updated = await mediator.Send(new ApproveSalesOrderCommand(id), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.OrdersApprove)]
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType<SalesOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Reject a sales order and release its soft-reserved inventory")]
    public async Task<ActionResult<SalesOrderDto>> RejectOrder(
        Guid id,
        [FromBody] RejectSalesOrderRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new RejectSalesOrderCommand(id, request.RejectionReason), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.WarehouseShip)]
    [HttpPost("{id:guid}/confirm-shipment")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Confirm warehouse shipment, deduct stock, allocate ZATCA ICV/PIH/QR, and issue tax invoice (requires X-Idempotency-Key GUID)")]
    public async Task<IActionResult> ConfirmShipment(
        Guid id,
        [FromBody] ConfirmShipmentRequest request,
        CancellationToken ct)
    {
        if (!IdempotencyHeaderHelper.TryParse(Request, out var idempotencyKey, out var error))
        {
            return error!;
        }

        var invoice = await mediator.Send(new ConfirmOrderShipmentCommand(id, request, idempotencyKey), ct);
        return Ok(invoice);
    }
}
