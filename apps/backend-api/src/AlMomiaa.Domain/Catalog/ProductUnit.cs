namespace AlMomiaa.Domain.Catalog;

public sealed class ProductUnit
{
    private ProductUnit() { }

    public Guid Id { get; private set; }
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public string UnitNameAr { get; private set; } = string.Empty;
    public string UnitNameEn { get; private set; } = string.Empty;
    public decimal ConversionFactor { get; private set; } = 1.0000m;
    public string Barcode { get; private set; } = string.Empty;
    public decimal WholesalePrice { get; private set; }
    public decimal RetailPrice { get; private set; }
    public decimal MinAllowedPrice { get; private set; }
    public bool IsDefaultUnit { get; private set; } = true;
}
