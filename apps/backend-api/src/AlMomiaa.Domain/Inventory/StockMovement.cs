using AlMomiaa.Domain.Catalog;

namespace AlMomiaa.Domain.Inventory;

public sealed class StockMovement
{
    private StockMovement() { }

    public long Id { get; private set; }
    public int WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private init; } = null!;
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public string MovementType { get; private set; } = string.Empty;
    public decimal QuantityDelta { get; private set; }
    public decimal PhysicalBalanceAfter { get; private set; }
    public decimal UnitCost { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public string? LotNumber { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
