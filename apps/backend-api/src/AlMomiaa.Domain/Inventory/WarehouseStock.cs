using AlMomiaa.Domain.Catalog;

namespace AlMomiaa.Domain.Inventory;

public sealed class WarehouseStock
{
    private WarehouseStock() { }

    public Guid Id { get; private set; }
    public int WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private init; } = null!;
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public decimal PhysicalQty { get; private set; }
    public decimal ReservedQty { get; private set; }
    public decimal IncomingQty { get; private set; }
    public decimal AvailableQty { get; private set; }
    public decimal MinReorderQty { get; private set; } = 10.000m;
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
}
