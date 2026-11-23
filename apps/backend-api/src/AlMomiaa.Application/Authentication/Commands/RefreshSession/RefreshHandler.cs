using AlMomiaa.Application.Authentication.Abstractions;
using MediatR;

namespace AlMomiaa.Application.Authentication.Commands.RefreshSession;

public sealed class RefreshHandler(IAuthenticationService authenticationService)
    : IRequestHandler<RefreshSessionCommand, RefreshSessionResult>
{
    public Task<RefreshSessionResult> Handle(RefreshSessionCommand request, CancellationToken cancellationToken) =>
        authenticationService.RefreshAsync(request.RefreshToken, cancellationToken);
}
