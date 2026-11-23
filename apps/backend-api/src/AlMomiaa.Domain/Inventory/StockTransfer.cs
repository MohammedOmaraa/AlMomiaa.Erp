using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.Inventory;

public sealed class StockTransfer
{
    private StockTransfer() { }

    public Guid Id { get; private set; }
    public string TransferNumber { get; private set; } = string.Empty;
    public int SourceWarehouseId { get; private set; }
    public Warehouse SourceWarehouse { get; private init; } = null!;
    public int DestinationWarehouseId { get; private set; }
    public Warehouse DestinationWarehouse { get; private init; } = null!;
    public string Status { get; private set; } = string.Empty;
    public Guid DispatchedByUserId { get; private set; }
    public User DispatchedByUser { get; private init; } = null!;
    public DateTime DispatchedAtUtc { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public User? ReceivedByUser { get; private init; }
    public DateTime? ReceivedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public ICollection<StockTransferItem> Items { get; private init; } = new List<StockTransferItem>();
}
