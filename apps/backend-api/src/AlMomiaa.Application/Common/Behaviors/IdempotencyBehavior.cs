using MediatR;

namespace AlMomiaa.Application.Common.Behaviors;

public interface IIdempotentRequest
{
    string IdempotencyKey { get; }
}

public interface IIdempotencyStore
{
    Task<TResponse> ExecuteAsync<TResponse>(string key, Func<CancellationToken, Task<TResponse>> operation, CancellationToken cancellationToken);
}

public sealed class IdempotencyBehavior<TRequest, TResponse>(IIdempotencyStore store) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        request is IIdempotentRequest idempotent
            ? store.ExecuteAsync(idempotent.IdempotencyKey, _ => next(), cancellationToken)
            : next();
}
