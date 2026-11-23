namespace AlMomiaa.Domain.Catalog;

public sealed class MasterProduct
{
    private MasterProduct() { }

    public Guid Id { get; private set; }
    public string SKU { get; private set; } = string.Empty;
    public string Barcode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public int CategoryId { get; private set; }
    public Category Category { get; private init; } = null!;
    public int BrandId { get; private set; }
    public Brand Brand { get; private init; } = null!;
    public string BaseUnit { get; private set; } = string.Empty;
    public decimal WeightedAvgCost { get; private set; }
    public decimal WholesaleSellingPrice { get; private set; }
    public decimal RetailSellingPrice { get; private set; }
    public decimal MinAllowedSellingPrice { get; private set; }
    public decimal VatRate { get; private set; } = 15.00m;
    public bool IsActive { get; private set; } = true;
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public ICollection<ProductUnit> Units { get; private init; } = new List<ProductUnit>();
    public ICollection<SupplierProduct> SupplierProducts { get; private init; } = new List<SupplierProduct>();
}
