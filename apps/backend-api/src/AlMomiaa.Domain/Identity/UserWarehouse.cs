namespace AlMomiaa.Domain.Identity;

public sealed class UserWarehouse
{
    private UserWarehouse()
    {
    }

    public Guid UserId { get; private set; }
    public User User { get; private init; } = null!;
    public int WarehouseId { get; private set; }

    public static UserWarehouse Create(Guid userId, int warehouseId) => new()
    {
        UserId = userId,
        WarehouseId = warehouseId
    };
}
