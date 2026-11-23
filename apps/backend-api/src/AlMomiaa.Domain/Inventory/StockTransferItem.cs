using AlMomiaa.Domain.Catalog;

namespace AlMomiaa.Domain.Inventory;

public sealed class StockTransferItem
{
    private StockTransferItem() { }

    public Guid Id { get; private set; }
    public Guid StockTransferId { get; private set; }
    public StockTransfer StockTransfer { get; private init; } = null!;
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public decimal RequestedQty { get; private set; }
    public decimal DispatchedQty { get; private set; }
    public decimal? ReceivedQty { get; private set; }
}
