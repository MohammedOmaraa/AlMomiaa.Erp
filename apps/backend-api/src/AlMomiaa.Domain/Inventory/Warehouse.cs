using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.Inventory;

public sealed class Warehouse
{
    private Warehouse() { }

    public int Id { get; private set; }
    public string WarehouseCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public string WarehouseType { get; private set; } = "MainWarehouse";
    public Guid? AssignedRepId { get; private set; }
    public User? AssignedRep { get; private init; }
    public string? VehiclePlateNumber { get; private set; }
    public string City { get; private set; } = "Riyadh";
    public bool IsActive { get; private set; } = true;
}
