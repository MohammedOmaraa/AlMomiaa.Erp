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

    public static MasterProduct Create(
        string sku,
        string barcode,
        string nameAr,
        string? nameEn,
        int categoryId,
        int brandId,
        string baseUnit,
        decimal weightedAvgCost,
        decimal wholesaleSellingPrice,
        decimal retailSellingPrice,
        decimal minAllowedSellingPrice,
        decimal vatRate = 15.00m) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SKU = sku.Trim().ToUpperInvariant(),
            Barcode = barcode.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            CategoryId = categoryId,
            BrandId = brandId,
            BaseUnit = baseUnit.Trim(),
            WeightedAvgCost = weightedAvgCost,
            WholesaleSellingPrice = wholesaleSellingPrice,
            RetailSellingPrice = retailSellingPrice,
            MinAllowedSellingPrice = minAllowedSellingPrice,
            VatRate = vatRate,
            IsActive = true
        };

    public void RecalculateWeightedAvgCost(decimal existingTotalQty, decimal incomingQty, decimal incomingLandedUnitCost)
    {
        var totalQty = existingTotalQty + incomingQty;
        if (totalQty <= 0)
        {
            WeightedAvgCost = Math.Round(incomingLandedUnitCost, 4, MidpointRounding.AwayFromZero);
            return;
        }

        var existingValue = existingTotalQty * WeightedAvgCost;
        var incomingValue = incomingQty * incomingLandedUnitCost;
        WeightedAvgCost = Math.Round((existingValue + incomingValue) / totalQty, 4, MidpointRounding.AwayFromZero);
    }
}
