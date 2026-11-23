namespace AlMomiaa.Application.Authentication.Contracts;

public sealed record AuthenticatedUserDto(
    Guid Id,
    string EmployeeCode,
    string FullNameAr,
    string? FullNameEn,
    string[] RoleCodes,
    string[] Permissions,
    string? RepType,
    int? DefaultWarehouseId,
    int[] AssignedWarehouseIds);
