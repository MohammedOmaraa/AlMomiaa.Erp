using AlMomiaa.Domain.Common;
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

    public static Collection CreateCash(
        string receiptNumber,
        Guid customerId,
        Guid repId,
        decimal amount,
        Guid repCustodyAccountId,
        Guid idempotencyKey,
        DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ReceiptNumber = receiptNumber.Trim().ToUpperInvariant(),
            CustomerId = customerId,
            RepId = repId,
            PaymentMethod = "Cash",
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            Status = "InRepCustody",
            IsCustomerConfirmed = false,
            TargetFinancialAccountId = repCustodyAccountId,
            OriginalValueDateUtc = createdAtUtc,
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = createdAtUtc
        };

    public static Collection CreateBankTransfer(
        string receiptNumber,
        Guid customerId,
        Guid repId,
        decimal amount,
        string bankReferenceNumber,
        string paymentProofImageUrl,
        string paymentProofImageHash,
        DateTime originalValueDateUtc,
        Guid idempotencyKey,
        DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ReceiptNumber = receiptNumber.Trim().ToUpperInvariant(),
            CustomerId = customerId,
            RepId = repId,
            PaymentMethod = "BankTransfer",
            Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
            Status = "PendingBankVerification",
            IsCustomerConfirmed = false,
            BankReferenceNumber = bankReferenceNumber.Trim(),
            PaymentProofImageUrl = paymentProofImageUrl.Trim(),
            PaymentProofImageHash = paymentProofImageHash.Trim().ToLowerInvariant(),
            OriginalValueDateUtc = originalValueDateUtc,
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = createdAtUtc
        };

    public void ConfirmByCustomer(DateTime confirmedAtUtc)
    {
        IsCustomerConfirmed = true;
        CustomerConfirmedAtUtc ??= confirmedAtUtc;
    }

    public void DepositToTreasury(Guid targetTreasuryAccountId, Guid accountantUserId, DateTime verifiedAtUtc)
    {
        if (PaymentMethod != "Cash" || Status != "InRepCustody")
            throw new BusinessRuleException($"Only Cash collections InRepCustody can be handed over to Treasury (current: {PaymentMethod}/{Status}).");

        Status = "DepositedToTreasury";
        TargetFinancialAccountId = targetTreasuryAccountId;
        VerifiedByAccountantId = accountantUserId;
        VerifiedAtUtc = verifiedAtUtc;
    }

    public void VerifyBankTransfer(bool approve, Guid targetBankAccountId, Guid accountantUserId, DateTime verifiedAtUtc)
    {
        if (PaymentMethod != "BankTransfer" || Status != "PendingBankVerification")
            throw new BusinessRuleException($"Only PendingBankVerification bank transfers can be verified (current: {PaymentMethod}/{Status}).");

        Status = approve ? "VerifiedInBank" : "Rejected";
        TargetFinancialAccountId = approve ? targetBankAccountId : null;
        VerifiedByAccountantId = accountantUserId;
        VerifiedAtUtc = verifiedAtUtc;
    }
}
