using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Operations;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Tags("Dashboards, Audit & GPS")]
public sealed class AuditLogsController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.AuditView)]
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("Query immutable ERP audit logs with keyset cursor pagination")]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] Guid? userId,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(entityName, userId, cursor, pageSize), ct);
        return Ok(result);
    }
}
