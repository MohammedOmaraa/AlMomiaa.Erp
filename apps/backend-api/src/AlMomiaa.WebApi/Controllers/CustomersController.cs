using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Customers;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Tags("Customers & Grades")]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CustomersView)]
    [HttpGet]
    [ProducesResponseType<PagedResult<CustomerDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List customers (row-scoped to assigned customers for Sales Reps)")]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int? gradeId,
        [FromQuery] string? city,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomersQuery(search, gradeId, city, isActive, page, pageSize), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CustomersView)]
    [HttpGet("{id:guid}/credit-status")]
    [ProducesResponseType<CustomerCreditStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get live customer credit limit, balance, overdue debt, and recent orders")]
    public async Task<ActionResult<CustomerCreditStatusDto>> GetCreditStatus(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerCreditStatusQuery(id), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CustomersManage)]
    [HttpPost]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new customer with credit grade, limit, price list, and assigned sales rep")]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var created = await mediator.Send(new CreateCustomerCommand(request), ct);
        return Created($"/api/v1/customers/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CustomersView)]
    [HttpGet("grades")]
    [ProducesResponseType<IReadOnlyList<CustomerGradeDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List customer credit grades (A, B, C, D) and default payment policies")]
    public async Task<ActionResult<IReadOnlyList<CustomerGradeDto>>> GetGrades(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerGradesQuery(), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.SettingsManage)]
    [HttpPut("grades")]
    [ProducesResponseType<IReadOnlyList<CustomerGradeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Update dynamic payment-day and credit allowance policies for customer grades")]
    public async Task<ActionResult<IReadOnlyList<CustomerGradeDto>>> UpdateGrades(
        [FromBody] UpdateCustomerGradesRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new UpdateCustomerGradesCommand(request), ct);
        return Ok(updated);
    }
}
