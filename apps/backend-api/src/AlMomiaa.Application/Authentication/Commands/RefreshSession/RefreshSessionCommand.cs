using AlMomiaa.Application.Authentication.Contracts;
using MediatR;

namespace AlMomiaa.Application.Authentication.Commands.RefreshSession;

public sealed record RefreshSessionCommand(string? RefreshToken) : IRequest<RefreshSessionResult>;

public sealed record RefreshSessionResult(
    AuthenticationResponse? Response,
    bool Succeeded,
    string? NewRefreshToken = null,
    DateTimeOffset? RefreshExpiresAtUtc = null);
