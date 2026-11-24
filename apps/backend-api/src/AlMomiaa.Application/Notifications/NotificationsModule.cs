using AlMomiaa.Application.Common.Abstractions;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Notifications;

public sealed record UserNotificationDto(
    Guid Id,
    string EventType,
    string Severity,
    string TitleAr,
    string TitleEn,
    string MessageAr,
    string MessageEn,
    string? EntityName,
    string? EntityId,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc);

public sealed record NotificationsPageDto(
    IReadOnlyList<UserNotificationDto> Items,
    int TotalCount,
    int UnreadCount,
    string? NextCursor);

public sealed record MarkNotificationReadResponse(
    UserNotificationDto Notification,
    int UnreadCount);

public sealed record MarkAllNotificationsReadResponse(
    int MarkedCount,
    int UnreadCount);

public sealed record GetMyNotificationsQuery(
    bool? UnreadOnly = null,
    int Page = 1,
    int PageSize = 30) : IRequest<NotificationsPageDto>;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : IRequest<MarkNotificationReadResponse>;
public sealed record MarkAllNotificationsReadCommand : IRequest<MarkAllNotificationsReadResponse>;

public sealed class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetMyNotificationsHandler(INotificationsModuleService service)
    : IRequestHandler<GetMyNotificationsQuery, NotificationsPageDto>
{
    public Task<NotificationsPageDto> Handle(GetMyNotificationsQuery request, CancellationToken ct) =>
        service.GetMyNotificationsAsync(request, ct);
}

public sealed class MarkNotificationReadHandler(INotificationsModuleService service)
    : IRequestHandler<MarkNotificationReadCommand, MarkNotificationReadResponse>
{
    public Task<MarkNotificationReadResponse> Handle(MarkNotificationReadCommand request, CancellationToken ct) =>
        service.MarkNotificationReadAsync(request.NotificationId, ct);
}

public sealed class MarkAllNotificationsReadHandler(INotificationsModuleService service)
    : IRequestHandler<MarkAllNotificationsReadCommand, MarkAllNotificationsReadResponse>
{
    public Task<MarkAllNotificationsReadResponse> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct) =>
        service.MarkAllNotificationsReadAsync(ct);
}
