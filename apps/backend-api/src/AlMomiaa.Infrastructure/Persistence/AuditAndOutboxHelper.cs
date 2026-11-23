using System.Text.Json;
using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Domain.SystemOps;

namespace AlMomiaa.Infrastructure.Persistence;

public static class AuditAndOutboxHelper
{
    public static void AppendAudit(
        AlMomiaaDbContext db,
        ICurrentUserService currentUser,
        string actionName,
        string entityName,
        string entityId,
        object? oldValues,
        object? newValues,
        DateTime utcNow)
    {
        var role = currentUser.RoleCodes.Count > 0 ? string.Join(',', currentUser.RoleCodes) : "SYSTEM";
        if (role.Length > 50)
        {
            role = role[..50];
        }

        db.AuditLogs.Add(AuditLog.Append(
            currentUser.UserId,
            role,
            actionName,
            entityName,
            entityId,
            oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            newValues is null ? null : JsonSerializer.Serialize(newValues),
            currentUser.IpAddress,
            utcNow));
    }

    public static void EnqueueOutbox(
        AlMomiaaDbContext db,
        string messageType,
        object payload,
        DateTime utcNow)
    {
        db.OutboxMessages.Add(OutboxMessage.Enqueue(
            messageType,
            JsonSerializer.Serialize(payload),
            utcNow));
    }
}
