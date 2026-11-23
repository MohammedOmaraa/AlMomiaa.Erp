namespace AlMomiaa.Domain.Identity;

public sealed class RefreshTokenSession
{
    public static readonly TimeSpan MaxSessionLifetime = TimeSpan.FromDays(14);

    private RefreshTokenSession()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private init; } = null!;
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }
    public string? RevocationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public static RefreshTokenSession Issue(
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        var maxExpiry = issuedAtUtc.Add(MaxSessionLifetime);
        var boundedExpiry = expiresAtUtc > maxExpiry ? maxExpiry : expiresAtUtc;

        if (boundedExpiry <= issuedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Refresh session expiry must be in the future.");
        }

        return new RefreshTokenSession
        {
            Id = id ?? Guid.CreateVersion7(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = boundedExpiry
        };
    }

    public bool IsActive(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public void Rotate(Guid replacementSessionId, DateTime revokedAtUtc)
    {
        if (RevokedAtUtc is not null)
        {
            throw new InvalidOperationException("Cannot rotate an already revoked refresh session.");
        }

        RevokedAtUtc = revokedAtUtc;
        ReplacedBySessionId = replacementSessionId;
        RevocationReason = "rotated";
    }

    public void Revoke(DateTime revokedAtUtc, string reason)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc;
        RevocationReason = reason;
    }
}
