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
}
