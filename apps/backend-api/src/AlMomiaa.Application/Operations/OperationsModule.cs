using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Operations;

public sealed record ExecutiveDashboardDto(
    DateOnly SnapshotDate,
    decimal TotalMainSafeCash,
    decimal TotalRepCustodyCash,
    decimal TotalBankBalance,
    decimal TotalReceivables,
    decimal TotalPayables,
    decimal TotalInventoryValuation,
    int PendingOrdersCount,
    int LowStockItemsCount);

public sealed record RoleDashboardDto(string RoleView, Dictionary<string, decimal> Metrics, DateTime GeneratedAtUtc);

public sealed record AuditLogDto(long Id, Guid UserId, string UserRole, string ActionName, string EntityName, string EntityId, string? OldValuesJson, string? NewValuesJson, string? IpAddress, DateTime TimestampUtc);

public sealed record GetExecutiveDashboardQuery : IRequest<ExecutiveDashboardDto>;
public sealed record GetRoleDashboardQuery(string RoleView) : IRequest<RoleDashboardDto>;
public sealed record GetAuditLogsQuery(string? EntityName, Guid? UserId, string? Cursor, int PageSize = 50) : IRequest<PagedResult<AuditLogDto>>;

public sealed record GpsPingItemRequest(decimal Latitude, decimal Longitude, decimal? AccuracyMeters, Guid? NearCustomerId, DateTime RecordedAtUtc);
public sealed record RecordGpsPingsRequest(GpsPingItemRequest[] Pings);
public sealed record RecordGpsPingsCommand(RecordGpsPingsRequest Request) : IRequest<int>;

public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, 250);
        RuleFor(x => x.EntityName).MaximumLength(100);
    }
}

public sealed class RecordGpsPingsCommandValidator : AbstractValidator<RecordGpsPingsCommand>
{
    public RecordGpsPingsCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Pings).NotEmpty().Must(p => p.Length <= 100).WithMessage("Maximum 100 GPS pings per batch.");
        RuleForEach(x => x.Request.Pings).ChildRules(ping =>
        {
            ping.RuleFor(p => p.Latitude).InclusiveBetween(-90m, 90m);
            ping.RuleFor(p => p.Longitude).InclusiveBetween(-180m, 180m);
            ping.RuleFor(p => p.AccuracyMeters).GreaterThanOrEqualTo(0m).When(p => p.AccuracyMeters.HasValue);
        });
    }
}

public sealed class GetExecutiveDashboardHandler(IOperationsModuleService service) : IRequestHandler<GetExecutiveDashboardQuery, ExecutiveDashboardDto>
{
    public Task<ExecutiveDashboardDto> Handle(GetExecutiveDashboardQuery request, CancellationToken ct) => service.GetExecutiveDashboardAsync(ct);
}
public sealed class GetRoleDashboardHandler(IOperationsModuleService service) : IRequestHandler<GetRoleDashboardQuery, RoleDashboardDto>
{
    public Task<RoleDashboardDto> Handle(GetRoleDashboardQuery request, CancellationToken ct) => service.GetRoleDashboardAsync(request.RoleView, ct);
}
public sealed class GetAuditLogsHandler(IOperationsModuleService service) : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct) => service.GetAuditLogsAsync(request, ct);
}
public sealed class RecordGpsPingsHandler(IOperationsModuleService service) : IRequestHandler<RecordGpsPingsCommand, int>
{
    public Task<int> Handle(RecordGpsPingsCommand request, CancellationToken ct) => service.RecordGpsPingsAsync(request, ct);
}
