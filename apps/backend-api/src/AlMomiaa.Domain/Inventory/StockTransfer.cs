using AlMomiaa.Domain.Common;
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

    public static StockTransfer CreateInTransit(
        string transferNumber,
        int sourceWarehouseId,
        int destinationWarehouseId,
        Guid dispatchedByUserId,
        DateTime dispatchedAtUtc)
    {
        if (sourceWarehouseId == destinationWarehouseId)
            throw new BusinessRuleException("Source and destination warehouses must be different.");

        return new StockTransfer
        {
            Id = Guid.CreateVersion7(),
            TransferNumber = transferNumber.Trim().ToUpperInvariant(),
            SourceWarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,
            Status = "InTransit",
            DispatchedByUserId = dispatchedByUserId,
            DispatchedAtUtc = dispatchedAtUtc
        };
    }

    public void MarkReceived(Guid receivedByUserId, DateTime receivedAtUtc)
    {
        if (!string.Equals(Status, "InTransit", StringComparison.Ordinal))
            throw new BusinessRuleException($"Only InTransit transfers can be received (current status: {Status}).");

        Status = "Received";
        ReceivedByUserId = receivedByUserId;
        ReceivedAtUtc = receivedAtUtc;
    }
}
