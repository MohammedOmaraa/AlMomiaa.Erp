namespace AlMomiaa.Application.Common.Abstractions;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> RoleCodes { get; }
    IReadOnlyList<string> Permissions { get; }
    string? RepType { get; }
    int? DefaultWarehouseId { get; }
    IReadOnlyList<int> AssignedWarehouseIds { get; }
    string IpAddress { get; }
    bool IsInRole(string roleCode);
    bool HasPermission(string permission);
}

public static class CurrentUserServiceExtensions
{
    public static bool HasRole(this ICurrentUserService user, string roleCode) =>
        user.IsInRole(roleCode);

    public static bool HasAnyRole(this ICurrentUserService user, params string[] roleCodes) =>
        roleCodes.Any(user.IsInRole);
}
