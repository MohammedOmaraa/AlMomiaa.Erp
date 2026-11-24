using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Notifications;
using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.SystemOps;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Persistence.Services;

public sealed class NotificationsModuleService(
    AlMomiaaDbContext db,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IOperationsRealtimeNotifier realtimeNotifier) : INotificationsModuleService
{
    public async Task<NotificationsPageDto> GetMyNotificationsAsync(GetMyNotificationsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var userId = currentUser.UserId;

        var baseQuery = db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId);

        var unreadCount = await baseQuery.CountAsync(n => !n.IsRead, ct);

        if (query.UnreadOnly == true)
        {
            baseQuery = baseQuery.Where(n => !n.IsRead);
        }

        var totalCount = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => MapDto(n))
            .ToListAsync(ct);

        var nextCursor = (page * pageSize) < totalCount ? (page + 1).ToString() : null;
        return new NotificationsPageDto(items, totalCount, unreadCount, nextCursor);
    }

    public async Task<MarkNotificationReadResponse> MarkNotificationReadAsync(Guid notificationId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, ct)
            ?? throw new NotFoundException("Notification", notificationId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        notification.MarkAsRead(now);
        await db.SaveChangesAsync(ct);

        var unreadCount = await db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == userId && !n.IsRead, ct);

        return new MarkNotificationReadResponse(MapDto(notification), unreadCount);
    }

    public async Task<MarkAllNotificationsReadResponse> MarkAllNotificationsReadAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var unread = await db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var item in unread)
        {
            item.MarkAsRead(now);
        }

        if (unread.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        await realtimeNotifier.NotifyAllNotificationsReadAsync(userId, ct);
        return new MarkAllNotificationsReadResponse(unread.Count, 0);
    }

    public static UserNotificationDto MapDto(UserNotification n) =>
        new(
            n.Id,
            n.EventType,
            n.Severity,
            n.TitleAr,
            n.TitleEn,
            n.MessageAr,
            n.MessageEn,
            n.EntityName,
            n.EntityId,
            n.IsRead,
            n.ReadAtUtc,
            n.CreatedAtUtc);
}
