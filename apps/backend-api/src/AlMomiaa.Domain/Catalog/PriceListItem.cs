namespace AlMomiaa.Domain.Catalog;

public sealed class PriceListItem
{
    private PriceListItem() { }

    public Guid Id { get; private set; }
    public int PriceListId { get; private set; }
    public PriceList PriceList { get; private init; } = null!;
    public Guid ProductUnitId { get; private set; }
    public ProductUnit ProductUnit { get; private init; } = null!;
    public decimal MinQuantity { get; private set; } = 1.000m;
    public decimal TierUnitPrice { get; private set; }
}
