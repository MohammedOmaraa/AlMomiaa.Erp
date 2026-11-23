using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;

namespace AlMomiaa.Domain.Sales;

public sealed class DeliveryDispatch
{
    private DeliveryDispatch() { }

    public Guid Id { get; private set; }
    public string DispatchNumber { get; private set; } = string.Empty;
    public int WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private init; } = null!;
    public Guid DriverOrRepId { get; private set; }
    public User DriverOrRep { get; private init; } = null!;
    public string? VehiclePlateNumber { get; private set; }
    public string Status { get; private set; } = "PreparedAtWarehouse";
    public DateTime? DispatchedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public User CreatedByUser { get; private init; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public ICollection<DeliveryDispatchItem> Items { get; private init; } = new List<DeliveryDispatchItem>();
}
