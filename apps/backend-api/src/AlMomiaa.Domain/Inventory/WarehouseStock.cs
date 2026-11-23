using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Common;

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

    public decimal ComputedAvailableQty => PhysicalQty - ReservedQty;

    public static WarehouseStock Create(int warehouseId, Guid masterProductId, decimal minReorderQty = 10.000m) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            WarehouseId = warehouseId,
            MasterProductId = masterProductId,
            PhysicalQty = 0m,
            ReservedQty = 0m,
            IncomingQty = 0m,
            MinReorderQty = minReorderQty
        };

    public void AddIncoming(decimal qty) => IncomingQty += qty;

    public void ReceiveGoods(decimal qty, decimal incomingDecrement)
    {
        PhysicalQty += qty;
        IncomingQty = Math.Max(0m, IncomingQty - incomingDecrement);
    }

    public void Reserve(decimal qty)
    {
        if (qty < 0) throw new BusinessRuleException("Reserved quantity cannot be negative.");
        ReservedQty += qty;
    }

    public void AdjustReservation(decimal delta)
    {
        var next = ReservedQty + delta;
        ReservedQty = next < 0 ? 0m : next;
    }

    public void DeductForTransfer(decimal qty)
    {
        if (ComputedAvailableQty < qty)
        {
            throw new BusinessRuleException($"Insufficient available stock for transfer. Available: {ComputedAvailableQty}, requested: {qty}.");
        }

        PhysicalQty -= qty;
    }

    public void DeductForShipment(decimal shippedQty, decimal reservedToRelease)
    {
        if (PhysicalQty < shippedQty)
        {
            throw new BusinessRuleException($"Insufficient physical stock to ship. Physical: {PhysicalQty}, shipped: {shippedQty}.");
        }

        PhysicalQty -= shippedQty;
        ReservedQty = Math.Max(0m, ReservedQty - reservedToRelease);
    }

    public void RestockReturn(decimal qty) => PhysicalQty += qty;

    public void SetPhysicalCount(decimal countedPhysicalQty)
    {
        if (countedPhysicalQty < 0)
            throw new BusinessRuleException("Counted physical quantity cannot be negative.");
        if (countedPhysicalQty < ReservedQty)
            throw new BusinessRuleException($"Counted physical quantity ({countedPhysicalQty}) cannot be lower than reserved quantity ({ReservedQty}).");

        PhysicalQty = countedPhysicalQty;
    }
}
