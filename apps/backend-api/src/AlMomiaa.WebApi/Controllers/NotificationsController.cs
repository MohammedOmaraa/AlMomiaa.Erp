using AlMomiaa.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
[Tags("Notifications & Alerts")]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<NotificationsPageDto>(StatusCodes.Status200OK)]
    [EndpointSummary("List current user's persistent notifications and unread count")]
    public async Task<ActionResult<NotificationsPageDto>> GetMyNotifications(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMyNotificationsQuery(unreadOnly, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType<MarkNotificationReadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [EndpointSummary("Mark a single notification as read for the current user")]
    public async Task<ActionResult<MarkNotificationReadResponse>> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new MarkNotificationReadCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("mark-all-read")]
    [ProducesResponseType<MarkAllNotificationsReadResponse>(StatusCodes.Status200OK)]
    [EndpointSummary("Mark all unread notifications as read for the current user and suppress further alerting")]
    public async Task<ActionResult<MarkAllNotificationsReadResponse>> MarkAllRead(CancellationToken ct)
    {
        var result = await mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return Ok(result);
    }
}
