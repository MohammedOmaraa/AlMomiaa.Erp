using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Procurement;

namespace AlMomiaa.Domain.Finance;

public sealed class JournalLine
{
    private JournalLine() { }

    public long Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public JournalEntry JournalEntry { get; private init; } = null!;
    public Guid FinancialAccountId { get; private set; }
    public FinancialAccount FinancialAccount { get; private init; } = null!;
    public Guid? CustomerId { get; private set; }
    public Customer? Customer { get; private init; }
    public Guid? SupplierId { get; private set; }
    public Supplier? Supplier { get; private init; }
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static JournalLine Create(
        Guid journalEntryId,
        Guid financialAccountId,
        Guid? customerId,
        Guid? supplierId,
        decimal debitAmount,
        decimal creditAmount,
        DateTime createdAtUtc)
    {
        if (debitAmount < 0 || creditAmount < 0 || (debitAmount == 0 && creditAmount == 0))
            throw new BusinessRuleException("Journal line must have non-negative debit or credit greater than zero.");

        return new JournalLine
        {
            JournalEntryId = journalEntryId,
            FinancialAccountId = financialAccountId,
            CustomerId = customerId,
            SupplierId = supplierId,
            DebitAmount = Math.Round(debitAmount, 2, MidpointRounding.AwayFromZero),
            CreditAmount = Math.Round(creditAmount, 2, MidpointRounding.AwayFromZero),
            CreatedAtUtc = createdAtUtc
        };
    }
}
