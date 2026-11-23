using AlMomiaa.Application.Authentication.Commands.Login;
using AlMomiaa.Application.Authentication.Commands.RefreshSession;
using AlMomiaa.Application.Authentication.Contracts;

namespace AlMomiaa.Application.Authentication.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool Verify(string hash, string password);
}

public interface IJwtTokenService
{
    string CreateAccessToken(AuthenticatedUserDto user, Guid securityStamp);
}

public interface ILoginAttemptLimiter
{
    Task<bool> IsBlockedAsync(string clientKey, string identifierKey, CancellationToken cancellationToken);
    Task RecordFailureAsync(string clientKey, string identifierKey, CancellationToken cancellationToken);
    Task ResetAsync(string clientKey, string identifierKey, CancellationToken cancellationToken);
}

public interface ISecurityStampStore
{
    Task<string?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task SetAsync(Guid userId, Guid securityStamp, CancellationToken cancellationToken);
    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(LoginRequest request, string clientIp, CancellationToken cancellationToken);
    Task<RefreshSessionResult> RefreshAsync(string? rawRefreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string? rawRefreshToken, CancellationToken cancellationToken);
}

public sealed class SecurityStampUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
