using AlMomiaa.Application.Finance;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/collections")]
[Tags("Collections, Custody & Finance")]
public sealed class CollectionsController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CollectionsCreate)]
    [HttpPost("cash")]
    [ProducesResponseType<CollectionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Record a cash collection into rep custody wallet and post balanced double-entry journal (requires X-Idempotency-Key GUID)")]
    public async Task<IActionResult> RecordCashCollection([FromBody] RecordCashCollectionRequest request, CancellationToken ct)
    {
        if (!IdempotencyHeaderHelper.TryParse(Request, out var idempotencyKey, out var error))
        {
            return error!;
        }

        var created = await mediator.Send(new RecordCashCollectionCommand(request, idempotencyKey), ct);
        return Created($"/api/v1/collections/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CollectionsCreate)]
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType<CollectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Record customer confirmation of a cash receipt")]
    public async Task<ActionResult<CollectionDto>> ConfirmCashCollection(Guid id, CancellationToken ct)
    {
        var updated = await mediator.Send(new ConfirmCashCollectionCommand(id), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.FinanceConfirmTreasury)]
    [HttpPost("handover")]
    [ProducesResponseType<CollectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Confirm handover of rep custody cash into MainTreasurySafe and post balanced ledger entry")]
    public async Task<ActionResult<CollectionDto>> RecordTreasuryHandover(
        [FromBody] RecordTreasuryHandoverRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new RecordTreasuryHandoverCommand(request), ct);
        return Ok(updated);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.CollectionsCreate)]
    [HttpPost("bank-transfer")]
    [ProducesResponseType<CollectionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Submit a customer bank transfer receipt with SHA-256 proof deduplication (requires X-Idempotency-Key GUID)")]
    public async Task<IActionResult> RecordBankTransfer([FromBody] RecordBankTransferRequest request, CancellationToken ct)
    {
        if (!IdempotencyHeaderHelper.TryParse(Request, out var idempotencyKey, out var error))
        {
            return error!;
        }

        var created = await mediator.Send(new RecordBankTransferCommand(request, idempotencyKey), ct);
        return Created($"/api/v1/collections/{created.Id}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.FinanceVerifyBank)]
    [HttpPost("{id:guid}/verify-bank")]
    [ProducesResponseType<CollectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Approve or reject a pending bank transfer, preserving OriginalValueDateUtc while enforcing open fiscal period (MR-05)")]
    public async Task<ActionResult<CollectionDto>> VerifyBankTransfer(
        Guid id,
        [FromBody] VerifyBankTransferRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new VerifyBankTransferCommand(id, request), ct);
        return Ok(updated);
    }
}
