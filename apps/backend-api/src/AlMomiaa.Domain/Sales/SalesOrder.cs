using AlMomiaa.Domain.Common;
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

    public static SalesOrder Submit(
        string orderNumber,
        string orderChannel,
        Guid customerId,
        Guid repId,
        int warehouseId,
        decimal customerBalanceAtOrder,
        decimal customerCreditLimitAtOrder,
        bool exceededCreditWarning,
        bool requiresFinanceCoApproval,
        bool hasStockShortage,
        bool hasPriceOverridePending,
        decimal subTotal,
        decimal vatAmount,
        decimal totalAmount,
        Guid idempotencyKey,
        DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            OrderChannel = orderChannel,
            CustomerId = customerId,
            RepId = repId,
            WarehouseId = warehouseId,
            Status = "PendingApproval",
            CustomerBalanceAtOrder = customerBalanceAtOrder,
            CustomerCreditLimitAtOrder = customerCreditLimitAtOrder,
            ExceededCreditWarning = exceededCreditWarning,
            RequiresFinanceCoApproval = requiresFinanceCoApproval,
            HasStockShortage = hasStockShortage,
            HasPriceOverridePending = hasPriceOverridePending,
            ReservationExpiresAtUtc = createdAtUtc.AddHours(24),
            ReservationState = hasStockShortage ? "SoftDemandWarning" : "Locked",
            SubTotal = subTotal,
            VatAmount = vatAmount,
            TotalAmount = totalAmount,
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = createdAtUtc
        };

    public void EnsureEditableByManager(bool callerHasEditSubmittedPermission)
    {
        if (!callerHasEditSubmittedPermission)
        {
            throw new ForbiddenDomainException("Sales Representatives cannot edit an order after submission. Only a Sales Manager or Super Admin may modify submitted orders.");
        }

        if (Status is not ("PendingApproval" or "Approved"))
        {
            throw new BusinessRuleException($"Order '{OrderNumber}' cannot be edited in status '{Status}'.");
        }
    }

    public void RecordManagerEdit(
        Guid managerUserId,
        DateTime editedAtUtc,
        decimal subTotal,
        decimal vatAmount,
        decimal totalAmount,
        bool hasStockShortage,
        bool hasPriceOverridePending)
    {
        SubTotal = subTotal;
        VatAmount = vatAmount;
        TotalAmount = totalAmount;
        HasStockShortage = hasStockShortage;
        HasPriceOverridePending = hasPriceOverridePending;
        ReservationState = hasStockShortage ? "SoftDemandWarning" : "Locked";
        EditedByManagerId = managerUserId;
        EditedAtUtc = editedAtUtc;
    }

    public void ApprovePriceOverrides(Guid approverUserId)
    {
        if (Status is not ("PendingApproval" or "Approved"))
        {
            throw new BusinessRuleException($"Cannot override prices for order in status '{Status}'.");
        }

        foreach (var item in Items.Where(i => i.PriceOverridePending))
        {
            item.ApprovePriceOverride(approverUserId);
        }

        HasPriceOverridePending = false;
    }

    public void CoApproveCredit(Guid financeUserId, DateTime approvedAtUtc)
    {
        if (!RequiresFinanceCoApproval)
        {
            throw new BusinessRuleException("Order does not require finance credit co-approval.");
        }

        if (Status is not "PendingApproval")
        {
            throw new BusinessRuleException($"Cannot co-approve credit for order in status '{Status}'.");
        }

        FinanceCoApprovedByUserId = financeUserId;
        FinanceCoApprovedAtUtc = approvedAtUtc;
    }

    public void Approve(Guid managerUserId, DateTime approvedAtUtc)
    {
        if (Status is not "PendingApproval")
        {
            throw new BusinessRuleException($"Only PendingApproval orders can be approved (current status: {Status}).");
        }

        if (RequiresFinanceCoApproval && FinanceCoApprovedByUserId is null)
        {
            throw new BusinessRuleException("Order requires Finance/Executive credit co-approval before final approval (MR-03).");
        }

        if (HasPriceOverridePending || Items.Any(i => i.PriceOverridePending))
        {
            throw new BusinessRuleException("Order has unresolved price override lines below tier/floor price (MR-02).");
        }

        if (HasStockShortage)
        {
            throw new BusinessRuleException("Order has an unresolved stock shortage. Adjust quantities or replenish warehouse stock first (RISK-03).");
        }

        Status = "Approved";
        ApprovedByManagerId = managerUserId;
        ApprovedAtUtc = approvedAtUtc;
    }

    public void Reject(Guid managerUserId, string reason, DateTime rejectedAtUtc)
    {
        if (Status is not ("PendingApproval" or "Approved"))
        {
            throw new BusinessRuleException($"Order in status '{Status}' cannot be rejected.");
        }

        Status = "Rejected";
        RejectionReason = reason.Trim();
        ReservationState = "Released";
        ApprovedByManagerId = managerUserId;
        ApprovedAtUtc = rejectedAtUtc;
    }

    public void MarkShippedAndInvoiced()
    {
        if (Status is not ("Approved" or "PreparingShipment"))
        {
            throw new BusinessRuleException($"Order must be Approved before confirming shipment (current status: {Status}).");
        }

        Status = "ShippedAndInvoiced";
        ReservationState = "Fulfilled";
    }
}
