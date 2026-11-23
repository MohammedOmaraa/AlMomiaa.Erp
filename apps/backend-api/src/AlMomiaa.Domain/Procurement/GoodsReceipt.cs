using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;

namespace AlMomiaa.Domain.Procurement;

public sealed class GoodsReceipt
{
    private GoodsReceipt() { }

    public Guid Id { get; private set; }
    public string GrnNumber { get; private set; } = string.Empty;
    public Guid PurchaseOrderId { get; private set; }
    public PurchaseOrder PurchaseOrder { get; private init; } = null!;
    public Guid? ContainerId { get; private set; }
    public Container? Container { get; private init; }
    public int WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private init; } = null!;
    public Guid ReceivedByUserId { get; private set; }
    public User ReceivedByUser { get; private init; } = null!;
    public DateTime ReceivedAtUtc { get; private set; }
    public ICollection<GoodsReceiptItem> Items { get; private init; } = new List<GoodsReceiptItem>();
}
