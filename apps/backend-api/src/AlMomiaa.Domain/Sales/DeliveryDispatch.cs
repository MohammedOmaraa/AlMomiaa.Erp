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

    public static DeliveryDispatch Create(
        string dispatchNumber,
        int warehouseId,
        Guid driverOrRepId,
        string? vehiclePlateNumber,
        Guid createdByUserId,
        DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            DispatchNumber = dispatchNumber.Trim().ToUpperInvariant(),
            WarehouseId = warehouseId,
            DriverOrRepId = driverOrRepId,
            VehiclePlateNumber = string.IsNullOrWhiteSpace(vehiclePlateNumber) ? null : vehiclePlateNumber.Trim(),
            Status = "PreparedAtWarehouse",
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = createdAtUtc
        };

    public void RefreshStatus(DateTime utcNow)
    {
        if (Items.Count > 0 && Items.All(i => i.DeliveryStatus == "Delivered"))
        {
            Status = "Completed";
            CompletedAtUtc ??= utcNow;
            DispatchedAtUtc ??= utcNow;
        }
        else if (Items.Any(i => i.DeliveryStatus is "OutForDelivery" or "Delivered"))
        {
            Status = "OutForDelivery";
            DispatchedAtUtc ??= utcNow;
        }
    }
}
