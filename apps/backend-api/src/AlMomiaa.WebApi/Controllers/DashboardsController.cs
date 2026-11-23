using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Operations;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/dashboards")]
[Tags("Dashboards, Audit & GPS")]
public sealed class DashboardsController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.DashboardsExecutive)]
    [HttpGet("executive")]
    [ProducesResponseType<ExecutiveDashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Get executive KPI snapshot (Main Safe, Rep Custody, Bank Balance, Receivables, Payables, Inventory Valuation)")]
    public async Task<ActionResult<ExecutiveDashboardDto>> GetExecutiveDashboard(CancellationToken ct)
    {
        var result = await mediator.Send(new GetExecutiveDashboardQuery(), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("role/{roleView}")]
    [ProducesResponseType<RoleDashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Get role-scoped operational KPI metrics (sales, inventory, warehouse, finance, rep)")]
    public async Task<ActionResult<RoleDashboardDto>> GetRoleDashboard(string roleView, CancellationToken ct)
    {
        var normalized = roleView.Trim().ToLowerInvariant();
        var requiredPermission = normalized switch
        {
            "sales" or "rep" => CanonicalRolesAndPermissions.Permissions.DashboardsSales,
            "inventory" or "warehouse" => CanonicalRolesAndPermissions.Permissions.DashboardsInventory,
            "finance" => CanonicalRolesAndPermissions.Permissions.DashboardsFinance,
            _ => null
        };

        if (requiredPermission is not null && !currentUser.HasPermission(requiredPermission))
        {
            return Forbid();
        }

        var result = await mediator.Send(new GetRoleDashboardQuery(roleView), ct);
        return Ok(result);
    }
}
