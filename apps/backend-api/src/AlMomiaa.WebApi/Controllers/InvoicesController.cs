using AlMomiaa.Application.Sales;
using AlMomiaa.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Tags("ZATCA Invoices & EGS")]
public sealed class InvoicesController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.SettingsManage)]
    [HttpPost("egs-devices")]
    [ProducesResponseType<ZatcaDeviceSequenceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [EndpointSummary("Register a ZATCA Phase 2 EGS device sequence counter (ICV/PIH)")]
    public async Task<ActionResult<ZatcaDeviceSequenceDto>> RegisterEgsDevice(
        [FromBody] RegisterEgsDeviceRequest request,
        CancellationToken ct)
    {
        var created = await mediator.Send(new RegisterEgsDeviceCommand(request), ct);
        return Created($"/api/v1/invoices/egs-devices/{created.EgsDeviceId}", created);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.InvoicesView)]
    [HttpGet("{id:guid}/print")]
    [ProducesResponseType<InvoicePrintDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get ZATCA invoice print payload with customer details, lines, XML hash, and TLV Base64 QR code")]
    public async Task<ActionResult<InvoicePrintDto>> GetInvoicePrint(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoicePrintQuery(id), ct);
        return Ok(result);
    }

    [Authorize(Policy = CanonicalRolesAndPermissions.Permissions.InvoicesUploadCopy)]
    [HttpPost("{id:guid}/upload-copy")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Attach a signed/stamped paper invoice copy URL to an invoice")]
    public async Task<ActionResult<InvoiceDto>> UploadCopy(
        Guid id,
        [FromBody] UploadInvoiceCopyRequest request,
        CancellationToken ct)
    {
        var updated = await mediator.Send(new UploadInvoiceCopyCommand(id, request.UploadedInvoiceCopyUrl), ct);
        return Ok(updated);
    }
}
