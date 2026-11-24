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

    [Authorize(Policy = "GpsTrackRoles")]
    [HttpGet("live")]
    [ProducesResponseType<IReadOnlyList<LiveRepGpsDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get latest live GPS coordinates for all field reps (restricted strictly to SUPER_ADMIN, EXEC_MGR, SALES_MGR)")]
    public async Task<ActionResult<IReadOnlyList<LiveRepGpsDto>>> GetLiveLocations(CancellationToken ct)
    {
        var locations = await mediator.Send(new GetLiveRepLocationsQuery(), ct);
        return Ok(locations);
    }

    [Authorize(Policy = "GpsTrackRoles")]
    [HttpGet("reps/{repId:guid}/history")]
    [ProducesResponseType<IReadOnlyList<RepGpsHistoryPointDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get recent GPS route history points for a specific rep (restricted strictly to SUPER_ADMIN, EXEC_MGR, SALES_MGR)")]
    public async Task<ActionResult<IReadOnlyList<RepGpsHistoryPointDto>>> GetRepHistory(
        Guid repId,
        [FromQuery] int hours = 24,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        var points = await mediator.Send(new GetRepGpsHistoryQuery(repId, hours, limit), ct);
        return Ok(points);
    }
}
