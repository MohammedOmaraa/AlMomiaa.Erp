using AlMomiaa.Application.Authentication.Contracts;
using MediatR;

namespace AlMomiaa.Application.Authentication.Commands.Login;

public enum LoginFailure
{
    None = 0,
    Unauthorized = 1,
    TooManyAttempts = 2
}

public sealed record LoginCommand(LoginRequest Request, string ClientIp) : IRequest<LoginResult>;

public sealed record LoginResult(
    AuthenticationResponse? Response,
    LoginFailure Failure,
    string? RefreshToken = null,
    DateTimeOffset? RefreshExpiresAtUtc = null);
