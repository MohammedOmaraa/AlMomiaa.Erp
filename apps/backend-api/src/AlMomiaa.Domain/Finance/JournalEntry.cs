using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.Finance;

public sealed class JournalEntry
{
    private JournalEntry() { }

    public Guid Id { get; private set; }
    public string EntryNumber { get; private set; } = string.Empty;
    public int FiscalPeriodId { get; private set; }
    public FiscalPeriod FiscalPeriod { get; private init; } = null!;
    public DateTime PostingDateUtc { get; private set; }
    public DateTime OriginalValueDateUtc { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public string DescriptionAr { get; private set; } = string.Empty;
    public Guid CreatedByUserId { get; private set; }
    public User CreatedByUser { get; private init; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public ICollection<JournalLine> Lines { get; private init; } = new List<JournalLine>();

    public static JournalEntry PostBalanced(
        string entryNumber,
        FiscalPeriod fiscalPeriod,
        DateTime postingDateUtc,
        DateTime originalValueDateUtc,
        string referenceType,
        Guid referenceId,
        string descriptionAr,
        Guid createdByUserId,
        IEnumerable<(Guid FinancialAccountId, Guid? CustomerId, Guid? SupplierId, decimal Debit, decimal Credit)> lines)
    {
        fiscalPeriod.EnsureOpen();

        var entryId = Guid.CreateVersion7();
        var materialized = lines.ToList();
        if (materialized.Count < 2)
            throw new BusinessRuleException("A double-entry journal entry requires at least 2 lines.");

        var totalDebit = materialized.Sum(l => l.Debit);
        var totalCredit = materialized.Sum(l => l.Credit);
        if (totalDebit <= 0 || totalDebit != totalCredit)
            throw new BusinessRuleException($"Journal entry is unbalanced (Debit={totalDebit}, Credit={totalCredit}).");

        var entry = new JournalEntry
        {
            Id = entryId,
            EntryNumber = entryNumber.Trim().ToUpperInvariant(),
            FiscalPeriodId = fiscalPeriod.Id,
            PostingDateUtc = postingDateUtc,
            OriginalValueDateUtc = originalValueDateUtc,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            DescriptionAr = descriptionAr.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = postingDateUtc
        };

        foreach (var line in materialized)
        {
            entry.Lines.Add(JournalLine.Create(
                entryId,
                line.FinancialAccountId,
                line.CustomerId,
                line.SupplierId,
                line.Debit,
                line.Credit,
                postingDateUtc));
        }

        return entry;
    }
}
