using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;

namespace AlMomiaa.Domain.Procurement;

public sealed class PurchaseOrder
{
    private PurchaseOrder() { }

    public Guid Id { get; private set; }
    public string PoNumber { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private init; } = null!;
    public int TargetWarehouseId { get; private set; }
    public Warehouse TargetWarehouse { get; private init; } = null!;
    public string OrderType { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public decimal TotalAmountSAR { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public User? ApprovedByUser { get; private init; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public ICollection<PurchaseOrderItem> Items { get; private init; } = new List<PurchaseOrderItem>();
}
