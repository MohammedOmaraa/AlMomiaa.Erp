using AlMomiaa.Application.Common.Models;
using AlMomiaa.Application.Finance;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/finance")]
[Tags("Collections, Custody & Finance")]
public sealed class FinanceController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.DashboardsFinance)]
    [HttpGet("accounts")]
    [ProducesResponseType<IReadOnlyList<FinancialAccountDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List financial accounts (MainTreasurySafe, BankAccount, RepCustodyWallet, AR/AP, Revenue)")]
    public async Task<ActionResult<IReadOnlyList<FinancialAccountDto>>> GetAccounts(CancellationToken ct)
    {
        var accounts = await mediator.Send(new GetFinancialAccountsQuery(), ct);
        return Ok(accounts);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.SettingsManage)]
    [HttpPost("accounts")]
    [ProducesResponseType<FinancialAccountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a financial account (MainTreasurySafe, BankAccount, RepCustodyWallet, etc.)")]
    public async Task<ActionResult<FinancialAccountDto>> CreateAccount(
        [FromBody] CreateFinancialAccountRequest request,
        CancellationToken ct)
    {
        var created = await mediator.Send(new CreateFinancialAccountCommand(request), ct);
        return Created($"/api/v1/finance/accounts/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.DashboardsFinance)]
    [HttpGet("fiscal-periods")]
    [ProducesResponseType<IReadOnlyList<FiscalPeriodDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List fiscal periods and open/closed status")]
    public async Task<ActionResult<IReadOnlyList<FiscalPeriodDto>>> GetFiscalPeriods(CancellationToken ct)
    {
        var periods = await mediator.Send(new GetFiscalPeriodsQuery(), ct);
        return Ok(periods);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.FinanceClosePeriod)]
    [HttpPost("fiscal-periods")]
    [ProducesResponseType<FiscalPeriodDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Open a new monthly fiscal period for double-entry postings")]
    public async Task<ActionResult<FiscalPeriodDto>> CreateFiscalPeriod(
        [FromBody] CreateFiscalPeriodRequest request,
        CancellationToken ct)
    {
        var created = await mediator.Send(new CreateFiscalPeriodCommand(request), ct);
        return Created($"/api/v1/finance/fiscal-periods/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.FinanceClosePeriod)]
    [HttpPost("fiscal-periods/{id:int}/close")]
    [ProducesResponseType<FiscalPeriodDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Close and lock a fiscal period against future financial postings (MR-05)")]
    public async Task<ActionResult<FiscalPeriodDto>> CloseFiscalPeriod(int id, CancellationToken ct)
    {
        var closed = await mediator.Send(new CloseFiscalPeriodCommand(id), ct);
        return Ok(closed);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.DashboardsFinance)]
    [HttpGet("journal-entries")]
    [ProducesResponseType<PagedResult<JournalEntryDto>>(StatusCodes.Status200OK)]
    [EndpointSummary("List append-only double-entry journal entries and their debit/credit lines")]
    public async Task<ActionResult<PagedResult<JournalEntryDto>>> GetJournalEntries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetJournalEntriesQuery(page, pageSize), ct);
        return Ok(result);
    }
}
