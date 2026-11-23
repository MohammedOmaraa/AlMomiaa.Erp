using AlMomiaa.Application.Sales;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/returns")]
[Tags("ZATCA Invoices & EGS")]
public sealed class ReturnsController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.SalesReturns)]
    [HttpPost]
    [ProducesResponseType<SalesReturnDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Create a ZATCA Credit Note (type 381), credit customer balance, and optionally restock sellable items")]
    public async Task<ActionResult<SalesReturnDto>> CreateReturn([FromBody] CreateSalesReturnRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateSalesReturnCommand(request), ct);
        return Created($"/api/v1/returns/{created.Id}", created);
    }
}
