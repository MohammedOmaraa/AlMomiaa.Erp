using AlMomiaa.Application.Notifications;

namespace AlMomiaa.Application.Common.Abstractions;

public sealed record OrderSubmittedNotification(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    Guid RepId,
    string RepName,
    int WarehouseId,
    decimal TotalAmount,
    bool ExceededCreditWarning,
    bool RequiresFinanceCoApproval,
    bool HasStockShortage,
    bool HasPriceOverridePending,
    DateTime CreatedAtUtc);

public sealed record CreditCoApprovalRequiredNotification(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    decimal CustomerBalanceAtOrder,
    decimal CustomerCreditLimitAtOrder,
    decimal TotalAmount);

public sealed record OrderApprovedNotification(
    Guid OrderId,
    string OrderNumber,
    Guid RepId,
    int WarehouseId,
    decimal TotalAmount,
    DateTime ApprovedAtUtc);

public sealed record OrderRejectedNotification(
    Guid OrderId,
    string OrderNumber,
    Guid RepId,
    string RejectionReason,
    DateTime RejectedAtUtc);

public sealed record OrderUpdatedNotification(
    Guid OrderId,
    string OrderNumber,
    Guid RepId,
    string Status,
    decimal TotalAmount,
    string ChangeType);

public interface IOperationsRealtimeNotifier
{
    Task NotifyOrderSubmittedAsync(OrderSubmittedNotification notification, CancellationToken ct = default);
    Task NotifyCreditCoApprovalRequiredAsync(CreditCoApprovalRequiredNotification notification, CancellationToken ct = default);
    Task NotifyOrderApprovedAsync(OrderApprovedNotification notification, CancellationToken ct = default);
    Task NotifyOrderRejectedAsync(OrderRejectedNotification notification, CancellationToken ct = default);
    Task NotifyOrderUpdatedAsync(OrderUpdatedNotification notification, CancellationToken ct = default);
    Task NotifyUserNotificationAsync(Guid recipientUserId, UserNotificationDto notification, CancellationToken ct = default);
    Task NotifyAllNotificationsReadAsync(Guid recipientUserId, CancellationToken ct = default);
}
