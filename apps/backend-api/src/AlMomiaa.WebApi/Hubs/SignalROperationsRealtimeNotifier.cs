using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Domain.Identity;
using Microsoft.AspNetCore.SignalR;

namespace AlMomiaa.WebApi.Hubs;

public sealed class SignalROperationsRealtimeNotifier(
    IHubContext<OperationsHub> hubContext,
    ILogger<SignalROperationsRealtimeNotifier> logger) : IOperationsRealtimeNotifier
{
    public async Task NotifyOrderSubmittedAsync(OrderSubmittedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients
                .Groups(
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SalesManager),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SuperAdmin),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.ExecutiveManager))
                .SendAsync("OrderSubmitted", notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch SignalR OrderSubmitted event for {OrderNumber}", notification.OrderNumber);
        }
    }

    public async Task NotifyCreditCoApprovalRequiredAsync(CreditCoApprovalRequiredNotification notification, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients
                .Groups(
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.Accountant),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.ExecutiveManager),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SuperAdmin))
                .SendAsync("CreditCoApprovalRequired", notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch SignalR CreditCoApprovalRequired event for {OrderNumber}", notification.OrderNumber);
        }
    }

    public async Task NotifyOrderApprovedAsync(OrderApprovedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients
                .Groups(
                    OperationsHub.UserGroup(notification.RepId),
                    OperationsHub.WarehouseGroup(notification.WarehouseId),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.PurchasingManager),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SalesManager),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SuperAdmin))
                .SendAsync("OrderApproved", notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch SignalR OrderApproved event for {OrderNumber}", notification.OrderNumber);
        }
    }

    public async Task NotifyOrderRejectedAsync(OrderRejectedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients
                .Groups(
                    OperationsHub.UserGroup(notification.RepId),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SalesManager),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SuperAdmin))
                .SendAsync("OrderRejected", notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch SignalR OrderRejected event for {OrderNumber}", notification.OrderNumber);
        }
    }

    public async Task NotifyOrderUpdatedAsync(OrderUpdatedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients
                .Groups(
                    OperationsHub.UserGroup(notification.RepId),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SalesManager),
                    OperationsHub.RoleGroup(CanonicalRolesAndPermissions.Roles.SuperAdmin))
                .SendAsync("OrderUpdated", notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch SignalR OrderUpdated event for {OrderNumber}", notification.OrderNumber);
        }
    }
}
