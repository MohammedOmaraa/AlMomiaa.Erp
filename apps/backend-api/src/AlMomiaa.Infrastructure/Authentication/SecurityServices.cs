using AlMomiaa.Application.Authentication.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AlMomiaa.Infrastructure.Authentication;

public sealed class LoginRateLimitOptions
{
    public const string SectionName = "Auth:LoginRateLimit";
    public int MaxAttemptsPerClientAndIdentifier { get; init; } = 5;
    public int MaxAttemptsPerIdentifier { get; init; } = 20;
    public int WindowMinutes { get; init; } = 15;
}

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();

    public string HashPassword(string password) =>
        _inner.HashPassword(new object(), password);

    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(new object(), hash, password) != PasswordVerificationResult.Failed;
}

public sealed class RedisSecurityStampStore(IConnectionMultiplexer redis) : ISecurityStampStore
{
    public static string BuildKey(Guid userId) => $"user:stamp:{userId:D}";

    public async Task<string?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(BuildKey(userId)).WaitAsync(cancellationToken);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            throw new SecurityStampUnavailableException("Redis security-stamp store is unavailable.", ex);
        }
    }

    public async Task SetAsync(Guid userId, Guid securityStamp, CancellationToken cancellationToken)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.StringSetAsync(BuildKey(userId), securityStamp.ToString("D")).WaitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            throw new SecurityStampUnavailableException("Redis security-stamp store is unavailable.", ex);
        }
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(BuildKey(userId)).WaitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            throw new SecurityStampUnavailableException("Redis security-stamp store is unavailable.", ex);
        }
    }
}

public sealed class LoginAttemptLimiter(
    IConnectionMultiplexer redis,
    IOptions<LoginRateLimitOptions>? options = null) : ILoginAttemptLimiter
{
    private readonly LoginRateLimitOptions _options = options?.Value ?? new LoginRateLimitOptions();

    private static string ClientIdentifierKey(string clientKey, string identifierKey) =>
        $"login:attempt:{clientKey}:{identifierKey}";

    private static string IdentifierOnlyKey(string identifierKey) =>
        $"login:attempt:id:{identifierKey}";

    public async Task<bool> IsBlockedAsync(
        string clientKey,
        string identifierKey,
        CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var pairValue = await db.StringGetAsync(ClientIdentifierKey(clientKey, identifierKey)).WaitAsync(cancellationToken);
        if (pairValue.HasValue && int.TryParse(pairValue.ToString(), out var pairCount)
            && pairCount >= _options.MaxAttemptsPerClientAndIdentifier)
        {
            return true;
        }

        var idValue = await db.StringGetAsync(IdentifierOnlyKey(identifierKey)).WaitAsync(cancellationToken);
        return idValue.HasValue && int.TryParse(idValue.ToString(), out var idCount)
            && idCount >= _options.MaxAttemptsPerIdentifier;
    }

    public async Task RecordFailureAsync(
        string clientKey,
        string identifierKey,
        CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var window = TimeSpan.FromMinutes(_options.WindowMinutes);

        var pairKey = ClientIdentifierKey(clientKey, identifierKey);
        var pairCount = await db.StringIncrementAsync(pairKey).WaitAsync(cancellationToken);
        if (pairCount == 1)
        {
            await db.KeyExpireAsync(pairKey, window).WaitAsync(cancellationToken);
        }

        var idKey = IdentifierOnlyKey(identifierKey);
        var idCount = await db.StringIncrementAsync(idKey).WaitAsync(cancellationToken);
        if (idCount == 1)
        {
            await db.KeyExpireAsync(idKey, window).WaitAsync(cancellationToken);
        }
    }

    public async Task ResetAsync(
        string clientKey,
        string identifierKey,
        CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(ClientIdentifierKey(clientKey, identifierKey)).WaitAsync(cancellationToken);
    }
}
