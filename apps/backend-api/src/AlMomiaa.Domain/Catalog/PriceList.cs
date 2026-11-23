namespace AlMomiaa.Domain.Catalog;

public sealed class PriceList
{
    private PriceList() { }

    public int Id { get; private set; }
    public string PriceListCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public ICollection<PriceListItem> Items { get; private init; } = new List<PriceListItem>();
}
