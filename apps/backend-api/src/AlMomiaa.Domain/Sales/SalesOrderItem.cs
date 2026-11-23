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

    public static SalesOrderItem Create(
        Guid salesOrderId,
        Guid masterProductId,
        Guid productUnitId,
        decimal conversionFactor,
        decimal orderedQty,
        decimal standardTierPrice,
        decimal unitPrice,
        bool priceOverridePending,
        decimal unitCostSnapshot,
        decimal vatAmount,
        decimal lineTotal) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SalesOrderId = salesOrderId,
            MasterProductId = masterProductId,
            ProductUnitId = productUnitId,
            ConversionFactor = conversionFactor,
            OrderedQty = orderedQty,
            ApprovedQty = orderedQty,
            StandardTierPrice = standardTierPrice,
            UnitPrice = unitPrice,
            PriceOverridePending = priceOverridePending,
            UnitCostSnapshot = unitCostSnapshot,
            VatAmount = vatAmount,
            LineTotal = lineTotal
        };

    public void UpdateByManager(decimal approvedQty, decimal unitPrice, bool priceOverridePending, decimal vatAmount, decimal lineTotal)
    {
        ApprovedQty = approvedQty;
        UnitPrice = unitPrice;
        PriceOverridePending = priceOverridePending;
        VatAmount = vatAmount;
        LineTotal = lineTotal;
    }

    public void ApprovePriceOverride(Guid approverUserId)
    {
        PriceOverridePending = false;
        PriceOverrideApprovedByUserId = approverUserId;
    }

    public void ConfirmShipped(decimal shippedQty) => ShippedQty = shippedQty;
}
