using AlMomiaa.Domain.Catalog;

namespace AlMomiaa.Domain.Sales;

public sealed class SalesReturnItem
{
    private SalesReturnItem() { }

    public Guid Id { get; private set; }
    public Guid SalesReturnId { get; private set; }
    public SalesReturn SalesReturn { get; private init; } = null!;
    public Guid MasterProductId { get; private set; }
    public MasterProduct MasterProduct { get; private init; } = null!;
    public Guid ProductUnitId { get; private set; }
    public ProductUnit ProductUnit { get; private init; } = null!;
    public decimal ReturnedQty { get; private set; }
    public bool IsSellableRestock { get; private set; } = true;
    public decimal UnitPrice { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    public static SalesReturnItem Create(
        Guid salesReturnId,
        Guid masterProductId,
        Guid productUnitId,
        decimal returnedQty,
        bool isSellableRestock,
        decimal unitPrice,
        decimal vatAmount,
        decimal lineTotal) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SalesReturnId = salesReturnId,
            MasterProductId = masterProductId,
            ProductUnitId = productUnitId,
            ReturnedQty = returnedQty,
            IsSellableRestock = isSellableRestock,
            UnitPrice = unitPrice,
            VatAmount = vatAmount,
            LineTotal = lineTotal
        };
}
