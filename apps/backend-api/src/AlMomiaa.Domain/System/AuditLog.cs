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
}
