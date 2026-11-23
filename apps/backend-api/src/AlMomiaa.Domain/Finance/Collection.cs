using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.Finance;

public sealed class Collection
{
    private Collection() { }

    public Guid Id { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private init; } = null!;
    public Guid RepId { get; private set; }
    public User Rep { get; private init; } = null!;
    public string PaymentMethod { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public bool IsCustomerConfirmed { get; private set; }
    public DateTime? CustomerConfirmedAtUtc { get; private set; }
    public string? PaymentProofImageUrl { get; private set; }
    public string? PaymentProofImageHash { get; private set; }
    public string? BankReferenceNumber { get; private set; }
    public Guid? TargetFinancialAccountId { get; private set; }
    public FinancialAccount? TargetFinancialAccount { get; private init; }
    public Guid? VerifiedByAccountantId { get; private set; }
    public User? VerifiedByAccountant { get; private init; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public DateTime OriginalValueDateUtc { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public ICollection<CollectionAllocation> Allocations { get; private init; } = new List<CollectionAllocation>();
}
