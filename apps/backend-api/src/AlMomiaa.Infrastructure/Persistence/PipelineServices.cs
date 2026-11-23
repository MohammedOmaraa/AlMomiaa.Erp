using AlMomiaa.Application.Common.Behaviors;
using Microsoft.Extensions.Logging;

namespace AlMomiaa.Infrastructure.Persistence;

public sealed class PassthroughIdempotencyStore : IIdempotencyStore
{
    public Task<TResponse> ExecuteAsync<TResponse>(
        string key,
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken) =>
        operation(cancellationToken);
}

public sealed class DefaultPermissionChecker : IRequestPermissionChecker
{
    public Task EnsureAuthorizedAsync(string permission, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class LoggingRequestAuditSink(ILogger<LoggingRequestAuditSink> logger) : IRequestAuditSink
{
    public Task RecordAsync(Type requestType, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handled request {RequestType}", requestType.Name);
        return Task.CompletedTask;
    }
}

public sealed class PassthroughTransactionBoundary : ITransactionBoundary
{
    public Task<TResponse> ExecuteAsync<TResponse>(
        Func<Task<TResponse>> operation,
        CancellationToken cancellationToken) =>
        operation();
}
