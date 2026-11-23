using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Customers;

public sealed record CustomerGradeDto(int Id, string GradeCode, int DefaultPaymentDays, bool AllowCredit, DateTime UpdatedAtUtc);

public sealed record CustomerDto(
    Guid Id,
    string CustomerCode,
    string NameAr,
    string? NameEn,
    string Phone,
    string Address,
    string City,
    string Category,
    string? VatNumber,
    int GradeId,
    string GradeCode,
    int? PriceListId,
    int? CustomPaymentDays,
    decimal CreditLimit,
    decimal CurrentBalance,
    Guid AssignedRepId,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive);

public sealed record RecentCustomerOrderDto(Guid Id, string OrderNumber, string Status, decimal TotalAmount, DateTime CreatedAtUtc);

public sealed record CustomerCreditStatusDto(
    Guid CustomerId,
    string CustomerCode,
    string NameAr,
    string GradeCode,
    bool AllowCredit,
    int EffectivePaymentDays,
    int? PriceListId,
    decimal CreditLimit,
    decimal CurrentBalance,
    decimal OverdueDebt,
    IReadOnlyList<RecentCustomerOrderDto> RecentOrders);

public sealed record GetCustomersQuery(string? Search, int? GradeId, string? City, bool? IsActive, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<CustomerDto>>;

public sealed record GetCustomerCreditStatusQuery(Guid CustomerId) : IRequest<CustomerCreditStatusDto>;

public sealed record CreateCustomerRequest(
    string CustomerCode,
    string NameAr,
    string? NameEn,
    string Phone,
    string Address,
    string? City,
    string Category,
    string? VatNumber,
    int GradeId,
    int? PriceListId,
    int? CustomPaymentDays,
    decimal CreditLimit,
    Guid AssignedRepId,
    decimal? Latitude,
    decimal? Longitude);

public sealed record CreateCustomerCommand(CreateCustomerRequest Request) : IRequest<CustomerDto>;

public sealed record GetCustomerGradesQuery : IRequest<IReadOnlyList<CustomerGradeDto>>;

public sealed record UpdateCustomerGradeItem(string GradeCode, int DefaultPaymentDays, bool AllowCredit);
public sealed record UpdateCustomerGradesRequest(UpdateCustomerGradeItem[] Grades);
public sealed record UpdateCustomerGradesCommand(UpdateCustomerGradesRequest Request) : IRequest<IReadOnlyList<CustomerGradeDto>>;

public sealed class GetCustomersQueryValidator : AbstractValidator<GetCustomersQuery>
{
    public GetCustomersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 250);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
    }
}

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    private static readonly string[] ValidCategories = ["Market", "Store", "Restaurant", "Supermarket", "Wholesale"];

    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.CustomerCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.NameEn).MaximumLength(200);
        RuleFor(x => x.Request.Phone).NotEmpty().MaximumLength(20).Matches("^\\+?[0-9\\s\\-()]{9,20}$");
        RuleFor(x => x.Request.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.City).MaximumLength(100);
        RuleFor(x => x.Request.Category)
            .NotEmpty()
            .MaximumLength(50)
            .Must(c => ValidCategories.Contains(c))
            .WithMessage("Category must be one of: Market, Store, Restaurant, Supermarket, Wholesale.");

        RuleFor(x => x.Request.VatNumber)
            .Matches("^[0-9]{15}$")
            .WithMessage("Saudi VAT number must be exactly 15 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.VatNumber));

        RuleFor(x => x.Request.GradeId).GreaterThan(0);
        RuleFor(x => x.Request.CustomPaymentDays).InclusiveBetween(0, 365).When(x => x.Request.CustomPaymentDays.HasValue);
        RuleFor(x => x.Request.CreditLimit).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Request.AssignedRepId).NotEmpty();
        RuleFor(x => x.Request.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Request.Latitude.HasValue);
        RuleFor(x => x.Request.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Request.Longitude.HasValue);
    }
}

public sealed class UpdateCustomerGradesCommandValidator : AbstractValidator<UpdateCustomerGradesCommand>
{
    private static readonly string[] ValidGrades = ["A", "B", "C", "D"];

    public UpdateCustomerGradesCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Grades).NotEmpty();
        RuleForEach(x => x.Request.Grades).ChildRules(grade =>
        {
            grade.RuleFor(g => g.GradeCode).NotEmpty().Must(c => ValidGrades.Contains(c.Trim().ToUpperInvariant()))
                .WithMessage("GradeCode must be A, B, C, or D.");
            grade.RuleFor(g => g.DefaultPaymentDays).InclusiveBetween(0, 365);
        });
    }
}

public sealed class GetCustomersHandler(ICustomersModuleService service) : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken ct) => service.GetCustomersAsync(request, ct);
}

public sealed class GetCustomerCreditStatusHandler(ICustomersModuleService service) : IRequestHandler<GetCustomerCreditStatusQuery, CustomerCreditStatusDto>
{
    public Task<CustomerCreditStatusDto> Handle(GetCustomerCreditStatusQuery request, CancellationToken ct) => service.GetCustomerCreditStatusAsync(request.CustomerId, ct);
}

public sealed class CreateCustomerHandler(ICustomersModuleService service) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken ct) => service.CreateCustomerAsync(request, ct);
}

public sealed class GetCustomerGradesHandler(ICustomersModuleService service) : IRequestHandler<GetCustomerGradesQuery, IReadOnlyList<CustomerGradeDto>>
{
    public Task<IReadOnlyList<CustomerGradeDto>> Handle(GetCustomerGradesQuery request, CancellationToken ct) => service.GetGradesAsync(ct);
}

public sealed class UpdateCustomerGradesHandler(ICustomersModuleService service) : IRequestHandler<UpdateCustomerGradesCommand, IReadOnlyList<CustomerGradeDto>>
{
    public Task<IReadOnlyList<CustomerGradeDto>> Handle(UpdateCustomerGradesCommand request, CancellationToken ct) => service.UpdateGradesAsync(request, ct);
}
