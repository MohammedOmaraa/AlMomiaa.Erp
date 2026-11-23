using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.Finance;

public sealed class FiscalPeriod
{
    private FiscalPeriod() { }

    public int Id { get; private set; }
    public int FiscalYear { get; private set; }
    public int FiscalMonth { get; private set; }
    public DateTime PeriodStartUtc { get; private set; }
    public DateTime PeriodEndUtc { get; private set; }
    public bool IsClosed { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public User? ClosedByUser { get; private init; }
    public DateTime? ClosedAtUtc { get; private set; }

    public static FiscalPeriod Create(int fiscalYear, int fiscalMonth)
    {
        if (fiscalMonth is < 1 or > 12)
            throw new BusinessRuleException("Fiscal month must be between 1 and 12.");

        var start = new DateTime(fiscalYear, fiscalMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        return new FiscalPeriod
        {
            FiscalYear = fiscalYear,
            FiscalMonth = fiscalMonth,
            PeriodStartUtc = start,
            PeriodEndUtc = end,
            IsClosed = false
        };
    }

    public void EnsureOpen()
    {
        if (IsClosed)
            throw new BusinessRuleException($"Fiscal period {FiscalYear}-{FiscalMonth:00} is closed and locked against financial postings (MR-05).");
    }

    public void Close(Guid closedByUserId, DateTime closedAtUtc)
    {
        if (IsClosed)
            throw new BusinessRuleException($"Fiscal period {FiscalYear}-{FiscalMonth:00} is already closed.");

        IsClosed = true;
        ClosedByUserId = closedByUserId;
        ClosedAtUtc = closedAtUtc;
    }
}
