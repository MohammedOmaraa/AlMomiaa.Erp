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

    public static GoodsReceiptItem Create(
        Guid goodsReceiptId,
        Guid masterProductId,
        decimal receivedQty,
        decimal allocatedLandedUnitCostSar,
        string? lotNumber,
        DateOnly? expiryDate) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            GoodsReceiptId = goodsReceiptId,
            MasterProductId = masterProductId,
            ReceivedQty = receivedQty,
            AllocatedLandedUnitCostSAR = allocatedLandedUnitCostSar,
            LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber.Trim(),
            ExpiryDate = expiryDate
        };
}
