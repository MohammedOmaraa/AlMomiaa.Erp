namespace AlMomiaa.Domain.SystemOps;

public sealed class AuditLog
{
    private AuditLog() { }

    public long Id { get; private set; }
    public Guid UserId { get; private set; }
    public string UserRole { get; private set; } = string.Empty;
    public string ActionName { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime TimestampUtc { get; private set; }

    public static AuditLog Append(
        Guid userId,
        string userRole,
        string actionName,
        string entityName,
        string entityId,
        string? oldValuesJson,
        string? newValuesJson,
        string? ipAddress,
        DateTime timestampUtc) =>
        new()
        {
            UserId = userId,
            UserRole = userRole,
            ActionName = actionName,
            EntityName = entityName,
            EntityId = entityId,
            OldValuesJson = oldValuesJson,
            NewValuesJson = newValuesJson,
            IpAddress = ipAddress,
            TimestampUtc = timestampUtc
        };
}
