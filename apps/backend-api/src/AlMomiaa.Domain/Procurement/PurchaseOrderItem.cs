using AlMomiaa.Domain.Catalog;

namespace AlMomiaa.Domain.Procurement;

public sealed class PurchaseOrderItem
{
    private PurchaseOrderItem() { }

    public Guid Id { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public PurchaseOrder PurchaseOrder { get; private init; } = null!;
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public decimal OrderedQty { get; private set; }
    public decimal ReceivedQty { get; private set; }
    public decimal UnitCostSAR { get; private set; }
    public decimal LineTotalSAR { get; private set; }

    public static PurchaseOrderItem Create(Guid purchaseOrderId, Guid masterProductId, decimal orderedQty, decimal unitCostSar) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            PurchaseOrderId = purchaseOrderId,
            MasterProductId = masterProductId,
            OrderedQty = orderedQty,
            ReceivedQty = 0m,
            UnitCostSAR = unitCostSar
        };

    public void AddReceived(decimal qty) => ReceivedQty += qty;
}
