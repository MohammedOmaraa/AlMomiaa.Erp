using MediatR;

namespace AlMomiaa.Application.Common.Behaviors;

public interface IPermissionRequest
{
    string RequiredPermission { get; }
}

public interface IRequestPermissionChecker
{
    Task EnsureAuthorizedAsync(string permission, CancellationToken cancellationToken);
}

public sealed class AuthorizationBehavior<TRequest, TResponse>(IRequestPermissionChecker permissionChecker) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IPermissionRequest permissionRequest)
        {
            await permissionChecker.EnsureAuthorizedAsync(permissionRequest.RequiredPermission, cancellationToken);
        }

        return await next();
    }
}
