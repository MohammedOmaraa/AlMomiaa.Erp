using System.Collections.Concurrent;
using AlMomiaa.Application.Authentication.Abstractions;
using Microsoft.Extensions.Options;

namespace AlMomiaa.Infrastructure.Authentication;

/// <summary>
/// Development-only in-memory security stamp store.
/// Never registered in Production/Staging where Redis is mandatory and fail-closed.
/// </summary>
public sealed class InMemoryDevSecurityStampStore : ISecurityStampStore
{
    private readonly ConcurrentDictionary<Guid, string> _stamps = new();

    public Task<string?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_stamps.TryGetValue(userId, out var stamp) ? stamp : null);

    public Task SetAsync(Guid userId, Guid securityStamp, CancellationToken cancellationToken)
    {
        _stamps[userId] = securityStamp.ToString("D");
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken)
    {
        _stamps.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Development-only in-memory login rate limiter.
/// Never registered in Production/Staging.
/// </summary>
public sealed class InMemoryDevLoginAttemptLimiter(IOptions<LoginRateLimitOptions>? options = null) : ILoginAttemptLimiter
{
    private readonly LoginRateLimitOptions _options = options?.Value ?? new LoginRateLimitOptions();
    private readonly ConcurrentDictionary<string, (int Count, DateTime ExpiresAtUtc)> _pairAttempts = new();
    private readonly ConcurrentDictionary<string, (int Count, DateTime ExpiresAtUtc)> _idAttempts = new();

    public Task<bool> IsBlockedAsync(string clientKey, string identifierKey, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var pairKey = $"{clientKey}:{identifierKey}";
        if (_pairAttempts.TryGetValue(pairKey, out var pair) && pair.ExpiresAtUtc > now && pair.Count >= _options.MaxAttemptsPerClientAndIdentifier)
        {
            return Task.FromResult(true);
        }

        if (_idAttempts.TryGetValue(identifierKey, out var id) && id.ExpiresAtUtc > now && id.Count >= _options.MaxAttemptsPerIdentifier)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task RecordFailureAsync(string clientKey, string identifierKey, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.WindowMinutes);
        var pairKey = $"{clientKey}:{identifierKey}";

        _pairAttempts.AddOrUpdate(
            pairKey,
            _ => (1, expires),
            (_, existing) => existing.ExpiresAtUtc <= now ? (1, expires) : (existing.Count + 1, existing.ExpiresAtUtc));

        _idAttempts.AddOrUpdate(
            identifierKey,
            _ => (1, expires),
            (_, existing) => existing.ExpiresAtUtc <= now ? (1, expires) : (existing.Count + 1, existing.ExpiresAtUtc));

        return Task.CompletedTask;
    }

    public Task ResetAsync(string clientKey, string identifierKey, CancellationToken cancellationToken)
    {
        _pairAttempts.TryRemove($"{clientKey}:{identifierKey}", out _);
        return Task.CompletedTask;
    }
}
