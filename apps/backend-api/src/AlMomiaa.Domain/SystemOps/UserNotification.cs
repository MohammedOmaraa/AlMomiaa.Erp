using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.SystemOps;

public sealed class UserNotification
{
    private UserNotification()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Severity { get; private set; } = "info";
    public string TitleAr { get; private set; } = string.Empty;
    public string TitleEn { get; private set; } = string.Empty;
    public string MessageAr { get; private set; } = string.Empty;
    public string MessageEn { get; private set; } = string.Empty;
    public string? EntityName { get; private set; }
    public string? EntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public User RecipientUser { get; private set; } = null!;

    public static UserNotification Create(
        Guid recipientUserId,
        string eventType,
        string severity,
        string titleAr,
        string titleEn,
        string messageAr,
        string messageEn,
        string? entityName,
        string? entityId,
        DateTime createdAtUtc)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("RecipientUserId is required.", nameof(recipientUserId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleEn);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageEn);

        var normalizedSeverity = severity?.Trim().ToLowerInvariant() switch
        {
            "success" => "success",
            "warning" => "warning",
            "danger" => "danger",
            _ => "info"
        };

        return new UserNotification
        {
            Id = Guid.CreateVersion7(),
            RecipientUserId = recipientUserId,
            EventType = eventType.Trim(),
            Severity = normalizedSeverity,
            TitleAr = titleAr.Trim(),
            TitleEn = titleEn.Trim(),
            MessageAr = messageAr.Trim(),
            MessageEn = messageEn.Trim(),
            EntityName = string.IsNullOrWhiteSpace(entityName) ? null : entityName.Trim(),
            EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim(),
            IsRead = false,
            ReadAtUtc = null,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void MarkAsRead(DateTime readAtUtc)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAtUtc = readAtUtc;
        }
    }
}
