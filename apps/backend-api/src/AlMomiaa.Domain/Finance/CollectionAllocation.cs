using AlMomiaa.Domain.Sales;

namespace AlMomiaa.Domain.Finance;

public sealed class CollectionAllocation
{
    private CollectionAllocation() { }

    public Guid Id { get; private set; }
    public Guid CollectionId { get; private set; }
    public Collection Collection { get; private init; } = null!;
    public Guid InvoiceId { get; private set; }
    public Invoice Invoice { get; private init; } = null!;
    public decimal AllocatedAmount { get; private set; }

    public static CollectionAllocation Create(Guid collectionId, Guid invoiceId, decimal allocatedAmount) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CollectionId = collectionId,
            InvoiceId = invoiceId,
            AllocatedAmount = Math.Round(allocatedAmount, 2, MidpointRounding.AwayFromZero)
        };
}
