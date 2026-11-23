using AlMomiaa.Application.Authentication.Abstractions;
using MediatR;

namespace AlMomiaa.Application.Authentication.Commands.Logout;

public sealed class LogoutHandler(IAuthenticationService authenticationService)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await authenticationService.LogoutAsync(request.RefreshToken, cancellationToken);
        return Unit.Value;
    }
}
