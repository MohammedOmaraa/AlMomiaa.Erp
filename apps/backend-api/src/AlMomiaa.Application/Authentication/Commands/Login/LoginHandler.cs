using AlMomiaa.Application.Authentication.Abstractions;
using MediatR;

namespace AlMomiaa.Application.Authentication.Commands.Login;

public sealed class LoginHandler(IAuthenticationService authenticationService)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        authenticationService.LoginAsync(request.Request, request.ClientIp, cancellationToken);
}
