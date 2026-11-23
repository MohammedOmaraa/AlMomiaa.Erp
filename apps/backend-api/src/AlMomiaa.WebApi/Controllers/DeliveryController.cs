using AlMomiaa.Application.Sales;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/delivery")]
[Tags("Delivery & Proof of Delivery")]
public sealed class DeliveryController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.DeliveryDispatch)]
    [HttpPost("dispatches")]
    [ProducesResponseType<DeliveryDispatchDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Create a delivery dispatch manifest assigning shipped invoices to a driver or rep")]
    public async Task<ActionResult<DeliveryDispatchDto>> CreateDispatch([FromBody] CreateDeliveryDispatchRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateDeliveryDispatchCommand(request), ct);
        return Created($"/api/v1/delivery/dispatches/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.DeliveryUpdatePod)]
    [HttpPut("dispatches/{id:guid}/items/{invoiceId:guid}/pod")]
    [ProducesResponseType<DeliveryDispatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update delivery Proof of Delivery (POD) status and signed receipt URL for an invoice")]
    public async Task<ActionResult<DeliveryDispatchDto>> UpdatePod(
        Guid id,
        Guid invoiceId,
        [FromBody] UpdateDeliveryPodRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateDeliveryPodCommand(id, invoiceId, request), ct);
        return Ok(updated);
    }
}
