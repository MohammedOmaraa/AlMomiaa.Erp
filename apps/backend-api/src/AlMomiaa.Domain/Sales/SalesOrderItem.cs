using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.Sales;

public sealed class SalesOrderItem
{
    private SalesOrderItem() { }

    public Guid Id { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public SalesOrder SalesOrder { get; private init; } = null!;
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public Guid ProductUnitId { get; private set; }
    public ProductUnit ProductUnit { get; private init; } = null!;
    public decimal ConversionFactor { get; private set; } = 1.0000m;
    public decimal OrderedQty { get; private set; }
    public decimal ApprovedQty { get; private set; }
    public decimal? ShippedQty { get; private set; }
    public decimal StandardTierPrice { get; private set; }
    public decimal UnitPrice { get; private set; }
    public bool PriceOverridePending { get; private set; }
    public Guid? PriceOverrideApprovedByUserId { get; private set; }
    public User? PriceOverrideApprovedByUser { get; private init; }
    public decimal UnitCostSnapshot { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal LineTotal { get; private set; }
}
