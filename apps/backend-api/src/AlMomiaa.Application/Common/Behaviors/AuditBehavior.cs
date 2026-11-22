using MediatR;

namespace AlMomiaa.Application.Common.Behaviors;

public interface IRequestAuditSink
{
    Task RecordAsync(Type requestType, CancellationToken cancellationToken);
}

public sealed class AuditBehavior<TRequest, TResponse>(IRequestAuditSink auditSink) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        await auditSink.RecordAsync(typeof(TRequest), cancellationToken);
        return response;
    }
}
