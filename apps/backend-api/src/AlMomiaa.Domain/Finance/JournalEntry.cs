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
}
