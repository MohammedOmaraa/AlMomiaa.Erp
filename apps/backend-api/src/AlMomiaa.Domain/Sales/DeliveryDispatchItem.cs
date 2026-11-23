namespace AlMomiaa.Domain.Sales;

public sealed class DeliveryDispatchItem
{
    private DeliveryDispatchItem() { }

    public Guid Id { get; private set; }
    public Guid DeliveryDispatchId { get; private set; }
    public DeliveryDispatch DeliveryDispatch { get; private init; } = null!;
    public Guid InvoiceId { get; private set; }
    public Invoice Invoice { get; private init; } = null!;
    public string DeliveryStatus { get; private set; } = "PreparedAtWarehouse";
    public DateTime? DeliveredAtUtc { get; private set; }
    public string? SignedPodImageUrl { get; private set; }
    public string? DeliveryNotes { get; private set; }
}
