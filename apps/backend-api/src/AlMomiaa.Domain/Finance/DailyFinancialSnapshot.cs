namespace AlMomiaa.Domain.Finance;

public sealed class DailyFinancialSnapshot
{
    private DailyFinancialSnapshot() { }

    public DateOnly SnapshotDate { get; private set; }
    public decimal TotalMainSafeCash { get; private set; }
    public decimal TotalRepCustodyCash { get; private set; }
    public decimal TotalBankBalance { get; private set; }
    public decimal TotalReceivables { get; private set; }
    public decimal TotalPayables { get; private set; }
    public decimal TotalInventoryValuation { get; private set; }
    public decimal DailyGrossSales { get; private set; }
    public decimal DailyGrossProfit { get; private set; }
    public decimal DailyCollectionsTotal { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
