using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Notifications;

namespace AlMomiaa.Infrastructure.Realtime;

public sealed class NoOpOperationsRealtimeNotifier : IOperationsRealtimeNotifier
{
    public Task NotifyOrderSubmittedAsync(OrderSubmittedNotification notification, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyCreditCoApprovalRequiredAsync(CreditCoApprovalRequiredNotification notification, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyOrderApprovedAsync(OrderApprovedNotification notification, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyOrderRejectedAsync(OrderRejectedNotification notification, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyOrderUpdatedAsync(OrderUpdatedNotification notification, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyUserNotificationAsync(Guid recipientUserId, UserNotificationDto notification, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyAllNotificationsReadAsync(Guid recipientUserId, CancellationToken ct = default) => Task.CompletedTask;
}
