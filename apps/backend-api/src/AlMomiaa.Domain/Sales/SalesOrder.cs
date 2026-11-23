using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;

namespace AlMomiaa.Domain.Sales;

public sealed class SalesOrder
{
    private SalesOrder() { }

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string OrderChannel { get; private set; } = "PreSales";
    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private init; } = null!;
    public Guid RepId { get; private set; }
    public User Rep { get; private init; } = null!;
    public int WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private init; } = null!;
    public string Status { get; private set; } = string.Empty;
    public decimal CustomerBalanceAtOrder { get; private set; }
    public decimal CustomerCreditLimitAtOrder { get; private set; }
    public bool ExceededCreditWarning { get; private set; }
    public bool RequiresFinanceCoApproval { get; private set; }
    public Guid? FinanceCoApprovedByUserId { get; private set; }
    public User? FinanceCoApprovedByUser { get; private init; }
    public DateTime? FinanceCoApprovedAtUtc { get; private set; }
    public bool HasStockShortage { get; private set; }
    public bool HasPriceOverridePending { get; private set; }
    public DateTime? ReservationExpiresAtUtc { get; private set; }
    public string ReservationState { get; private set; } = "Locked";
    public decimal SubTotal { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid? ApprovedByManagerId { get; private set; }
    public User? ApprovedByManager { get; private init; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? EditedByManagerId { get; private set; }
    public User? EditedByManager { get; private init; }
    public DateTime? EditedAtUtc { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public ICollection<SalesOrderItem> Items { get; private init; } = new List<SalesOrderItem>();
}
