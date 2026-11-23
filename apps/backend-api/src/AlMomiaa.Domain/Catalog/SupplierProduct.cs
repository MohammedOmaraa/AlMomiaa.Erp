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

    public static SupplierProduct Create(
        Guid masterProductId,
        Guid supplierId,
        string? supplierSku,
        string? supplierBarcode,
        decimal lastPurchasePrice,
        int? leadTimeDays,
        bool isPreferredSupplier,
        DateTime updatedAtUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            MasterProductId = masterProductId,
            SupplierId = supplierId,
            SupplierSku = string.IsNullOrWhiteSpace(supplierSku) ? null : supplierSku.Trim(),
            SupplierBarcode = string.IsNullOrWhiteSpace(supplierBarcode) ? null : supplierBarcode.Trim(),
            LastPurchasePrice = lastPurchasePrice,
            LeadTimeDays = leadTimeDays,
            IsPreferredSupplier = isPreferredSupplier,
            UpdatedAtUtc = updatedAtUtc
        };
}
