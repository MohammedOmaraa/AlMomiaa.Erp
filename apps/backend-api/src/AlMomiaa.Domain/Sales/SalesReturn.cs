using AlMomiaa.Domain.Customers;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Domain.Inventory;

namespace AlMomiaa.Domain.Sales;

public sealed class SalesReturn
{
    private SalesReturn() { }

    public Guid Id { get; private set; }
    public string CreditNoteNumber { get; private set; } = string.Empty;
    public Guid OriginalInvoiceId { get; private set; }
    public Invoice OriginalInvoice { get; private init; } = null!;
    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private init; } = null!;
    public int WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private init; } = null!;
    public string ReturnReason { get; private set; } = string.Empty;
    public decimal SubTotal { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string EgsDeviceId { get; private set; } = string.Empty;
    public ZatcaDeviceSequence ZatcaDeviceSequence { get; private init; } = null!;
    public long ZatcaIcv { get; private set; }
    public Guid ZatcaUuid { get; private set; }
    public string ZatcaXmlHash { get; private set; } = string.Empty;
    public string ZatcaQrCodeBase64 { get; private set; } = string.Empty;
    public string ZatcaSubmissionStatus { get; private set; } = "Pending";
    public Guid ApprovedByUserId { get; private set; }
    public User ApprovedByUser { get; private init; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public ICollection<SalesReturnItem> Items { get; private init; } = new List<SalesReturnItem>();

    public static SalesReturn Create(
        string creditNoteNumber,
        Guid originalInvoiceId,
        Guid customerId,
        int warehouseId,
        string returnReason,
        decimal subTotal,
        decimal vatAmount,
        decimal totalAmount,
        string egsDeviceId,
        long zatcaIcv,
        Guid zatcaUuid,
        string zatcaXmlHash,
        string zatcaQrCodeBase64,
        Guid approvedByUserId,
        DateTime createdAtUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CreditNoteNumber = creditNoteNumber.Trim().ToUpperInvariant(),
            OriginalInvoiceId = originalInvoiceId,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            ReturnReason = returnReason.Trim(),
            SubTotal = subTotal,
            VatAmount = vatAmount,
            TotalAmount = totalAmount,
            EgsDeviceId = egsDeviceId,
            ZatcaIcv = zatcaIcv,
            ZatcaUuid = zatcaUuid,
            ZatcaXmlHash = zatcaXmlHash,
            ZatcaQrCodeBase64 = zatcaQrCodeBase64,
            ZatcaSubmissionStatus = "Pending",
            ApprovedByUserId = approvedByUserId,
            CreatedAtUtc = createdAtUtc
        };
}
