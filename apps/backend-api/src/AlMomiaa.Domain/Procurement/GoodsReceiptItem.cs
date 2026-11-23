using AlMomiaa.Domain.Catalog;

namespace AlMomiaa.Domain.Procurement;

public sealed class GoodsReceiptItem
{
    private GoodsReceiptItem() { }

    public Guid Id { get; private set; }
    public Guid GoodsReceiptId { get; private set; }
    public GoodsReceipt GoodsReceipt { get; private init; } = null!;
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public decimal ReceivedQty { get; private set; }
    public decimal AllocatedLandedUnitCostSAR { get; private set; }
    public string? LotNumber { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
}
