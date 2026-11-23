using AlMomiaa.Domain.Procurement;

namespace AlMomiaa.Domain.Catalog;

public sealed class SupplierProduct
{
    private SupplierProduct() { }

    public Guid Id { get; private set; }
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private init; } = null!;
    public string? SupplierSku { get; private set; }
    public string? SupplierBarcode { get; private set; }
    public decimal LastPurchasePrice { get; private set; }
    public int? LeadTimeDays { get; private set; }
    public bool IsPreferredSupplier { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
