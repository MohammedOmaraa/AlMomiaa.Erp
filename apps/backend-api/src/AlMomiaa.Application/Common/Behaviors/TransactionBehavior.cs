using MediatR;

namespace AlMomiaa.Application.Common.Behaviors;

public interface ITransactionBoundary
{
    Task<TResponse> ExecuteAsync<TResponse>(Func<Task<TResponse>> operation, CancellationToken cancellationToken);
}

public sealed class TransactionBehavior<TRequest, TResponse>(ITransactionBoundary transactionBoundary) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        transactionBoundary.ExecuteAsync(() => next(), cancellationToken);
}
