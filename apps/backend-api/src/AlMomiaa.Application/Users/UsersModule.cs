using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace AlMomiaa.Application.Users;

public sealed record UserSummaryDto(
    Guid Id,
    string EmployeeCode,
    string FullNameAr,
    string? FullNameEn,
    string Phone,
    string? Email,
    string? RepType,
    int? DefaultWarehouseId,
    bool IsActive,
    DateTime CreatedAtUtc,
    string[] RoleCodes,
    int[] AssignedWarehouseIds);

public sealed record RoleDto(int Id, string RoleCode, string NameAr, string NameEn, string[] Permissions);

public sealed record GetUsersQuery(string? Search, string? RoleCode, bool? IsActive, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<UserSummaryDto>>;

public sealed record CreateUserRequest(
    string EmployeeCode,
    string FullNameAr,
    string? FullNameEn,
    string Phone,
    string? Email,
    string Password,
    string? RepType,
    int? DefaultWarehouseId,
    int[] RoleIds,
    int[]? AssignedWarehouseIds,
    bool CreateMobileVan = false,
    string? VehiclePlateNumber = null);

public sealed record CreateUserCommand(CreateUserRequest Request) : IRequest<UserSummaryDto>;

public sealed record UpdateUserStatusRequest(bool IsActive);
public sealed record UpdateUserStatusCommand(Guid UserId, bool IsActive) : IRequest<UserSummaryDto>;

public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public sealed record UpdateRolePermissionsRequest(string[] Permissions);
public sealed record UpdateRolePermissionsCommand(int RoleId, string[] Permissions) : IRequest<RoleDto>;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 250);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.RoleCode).MaximumLength(30);
    }
}

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly string[] ValidRepTypes = ["Wholesale", "Retail", "CashVan"];

    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.EmployeeCode)
            .NotEmpty()
            .MaximumLength(30)
            .Matches("^[A-Za-z0-9\\-_]+$")
            .WithMessage("EmployeeCode may contain only letters, digits, hyphens, and underscores.");

        RuleFor(x => x.Request.FullNameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Request.FullNameEn).MaximumLength(150);
        RuleFor(x => x.Request.Phone)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^\\+?[0-9\\s\\-()]{9,20}$")
            .WithMessage("Phone must be a valid phone number.");

        RuleFor(x => x.Request.Email)
            .EmailAddress()
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Email));

        RuleFor(x => x.Request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.Request.RepType)
            .Must(v => ValidRepTypes.Contains(v))
            .WithMessage("RepType must be one of: Wholesale, Retail, CashVan.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.RepType));

        RuleFor(x => x.Request.RoleIds)
            .NotEmpty()
            .WithMessage("At least one role must be assigned.");

        RuleFor(x => x.Request.VehiclePlateNumber).MaximumLength(30);
    }
}

public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0);
        RuleFor(x => x.Permissions).NotNull();
        RuleForEach(x => x.Permissions).NotEmpty().MaximumLength(100);
    }
}

public sealed class GetUsersHandler(IIamModuleService service) : IRequestHandler<GetUsersQuery, PagedResult<UserSummaryDto>>
{
    public Task<PagedResult<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken ct) => service.GetUsersAsync(request, ct);
}

public sealed class CreateUserHandler(IIamModuleService service) : IRequestHandler<CreateUserCommand, UserSummaryDto>
{
    public Task<UserSummaryDto> Handle(CreateUserCommand request, CancellationToken ct) => service.CreateUserAsync(request, ct);
}

public sealed class UpdateUserStatusHandler(IIamModuleService service) : IRequestHandler<UpdateUserStatusCommand, UserSummaryDto>
{
    public Task<UserSummaryDto> Handle(UpdateUserStatusCommand request, CancellationToken ct) => service.UpdateUserStatusAsync(request, ct);
}

public sealed class GetRolesHandler(IIamModuleService service) : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken ct) => service.GetRolesAsync(ct);
}

public sealed class UpdateRolePermissionsHandler(IIamModuleService service) : IRequestHandler<UpdateRolePermissionsCommand, RoleDto>
{
    public Task<RoleDto> Handle(UpdateRolePermissionsCommand request, CancellationToken ct) => service.UpdateRolePermissionsAsync(request, ct);
}
