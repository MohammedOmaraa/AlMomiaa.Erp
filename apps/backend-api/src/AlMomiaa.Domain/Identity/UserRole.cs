namespace AlMomiaa.Domain.Identity;

public sealed class UserRole
{
    private UserRole()
    {
    }

    public Guid UserId { get; private set; }
    public User User { get; private init; } = null!;
    public int RoleId { get; private set; }
    public Role Role { get; private init; } = null!;

    public static UserRole Create(Guid userId, int roleId) => new()
    {
        UserId = userId,
        RoleId = roleId
    };
}
