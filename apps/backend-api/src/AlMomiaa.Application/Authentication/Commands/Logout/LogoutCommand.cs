using MediatR;

namespace AlMomiaa.Application.Authentication.Commands.Logout;

public sealed record LogoutCommand(string? RefreshToken) : IRequest<Unit>;
