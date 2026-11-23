namespace AlMomiaa.Domain.Identity;

public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public int RoleId { get; private set; }
    public Role Role { get; private init; } = null!;
    public string PermissionClaim { get; private set; } = string.Empty;

    public static RolePermission Create(int roleId, string permissionClaim)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionClaim);

        return new RolePermission
        {
            RoleId = roleId,
            PermissionClaim = permissionClaim.Trim()
        };
    }
}
