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
}
