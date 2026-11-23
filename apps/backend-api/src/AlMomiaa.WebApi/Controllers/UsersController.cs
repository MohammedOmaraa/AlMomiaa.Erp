using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Users;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1")]
[Tags("Users & Roles")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.UsersManage)]
    [HttpGet("users")]
    [ProducesResponseType<PagedResult<UserSummaryDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List employees with optional search, role, and active status filters")]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? roleCode,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetUsersQuery(search, roleCode, isActive, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.UsersManage)]
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get full employee profile including assigned roles, warehouses, and effective permission claims")]
    public async Task<ActionResult<UserDetailDto>> GetUserById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.UsersManage)]
    [HttpPost("users")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new employee account, assign roles/warehouses, and optionally provision a CashVan mobile warehouse and custody wallet")]
    public async Task<ActionResult<UserSummaryDto>> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateUserCommand(request), ct);
        return Created($"/api/v1/users/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.UsersManage)]
    [HttpPatch("users/{id:guid}/status")]
    [ProducesResponseType<UserSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Activate or deactivate an employee account (deactivation immediately revokes active sessions)")]
    public async Task<ActionResult<UserSummaryDto>> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateUserStatusCommand(id, request.IsActive), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.PermissionsManage)]
    [HttpPut("users/{id:guid}/roles")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Assign or replace roles on an employee account and immediately rotate SecurityStamp")]
    public async Task<ActionResult<UserDetailDto>> UpdateUserRoles(
        Guid id,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateUserRolesCommand(id, request.RoleIds), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.UsersManage)]
    [HttpPut("users/{id:guid}/warehouses")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Assign or replace warehouses and default warehouse on an employee account and rotate SecurityStamp")]
    public async Task<ActionResult<UserDetailDto>> UpdateUserWarehouses(
        Guid id,
        [FromBody] UpdateUserWarehousesRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateUserWarehousesCommand(id, request.DefaultWarehouseId, request.AssignedWarehouseIds), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.PermissionsManage)]
    [HttpGet("roles")]
    [ProducesResponseType<IReadOnlyList<RoleDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List all roles and their assigned permission claims")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken ct)
    {
        var roles = await mediator.Send(new GetRolesQuery(), ct);
        return Ok(roles);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.PermissionsManage)]
    [HttpPost("roles")]
    [ProducesResponseType<RoleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a custom role with a tailored set of canonical ERP permission claims")]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateRoleCommand(request), ct);
        return Created($"/api/v1/roles/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.PermissionsManage)]
    [HttpPut("roles/{roleId:int}/permissions")]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Replace permission claims for a role and rotate security stamps for affected users")]
    public async Task<ActionResult<RoleDto>> UpdateRolePermissions(
        int roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateRolePermissionsCommand(roleId, request.Permissions), ct);
        return Ok(updated);
    }
}
