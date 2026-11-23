namespace AlMomiaa.Domain.Identity;

public sealed class Role
{
    private Role()
    {
    }

    public int Id { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public ICollection<RolePermission> Permissions { get; private init; } = new List<RolePermission>();

    public static Role Create(int id, string roleCode, string nameAr, string nameEn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameEn);

        return new Role
        {
            Id = id,
            RoleCode = roleCode.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = nameEn.Trim()
        };
    }
}
