namespace AlMomiaa.Domain.Procurement;

public sealed class Container
{
    private Container() { }

    public Guid Id { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public PurchaseOrder PurchaseOrder { get; private init; } = null!;
    public string ContainerNumber { get; private set; } = string.Empty;
    public string? BillOfLading { get; private set; }
    public string? ShippingLine { get; private set; }
    public string PortOfArrival { get; private set; } = string.Empty;
    public DateOnly EstimatedArrivalDate { get; private set; }
    public DateOnly? ActualArrivalDate { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public decimal SupplierGoodsCostSAR { get; private set; }
    public decimal FreightAndCustomsCostSAR { get; private set; }
    public decimal TotalLandedCostSAR { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
}
