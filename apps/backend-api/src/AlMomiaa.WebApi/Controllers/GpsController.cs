using AlMomiaa.Application.Operations;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/gps")]
[Tags("Dashboards, Audit & GPS")]
public sealed class GpsController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.GpsStream)]
    [HttpPost("pings")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Stream a batch of field sales rep GPS location pings (max 100 per batch)")]
    public async Task<IActionResult> RecordPings([FromBody] RecordGpsPingsRequest request, CancellationToken ct)
    {
        var count = await mediator.Send(new RecordGpsPingsCommand(request), ct);
        return Accepted(new { acceptedCount = count });
    }
}
