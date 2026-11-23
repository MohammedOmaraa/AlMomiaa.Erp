using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Customers;

namespace AlMomiaa.Domain.Sales;

public sealed class Invoice
{
    private Invoice() { }

    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid SalesOrderId { get; private set; }
    public SalesOrder SalesOrder { get; private init; } = null!;
    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private init; } = null!;
    public string InvoiceType { get; private set; } = string.Empty;
    public DateTime IssueDateUtc { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingBalance { get; private set; }
    public string PaymentStatus { get; private set; } = "Unpaid";
    public string? UploadedInvoiceCopyUrl { get; private set; }
    public string EgsDeviceId { get; private set; } = string.Empty;
    public ZatcaDeviceSequence ZatcaDeviceSequence { get; private init; } = null!;
    public long ZatcaIcv { get; private set; }
    public Guid ZatcaUuid { get; private set; }
    public string ZatcaPreviousInvoiceHash { get; private set; } = string.Empty;
    public string ZatcaXmlHash { get; private set; } = string.Empty;
    public string ZatcaQrCodeBase64 { get; private set; } = string.Empty;
    public string ZatcaSubmissionStatus { get; private set; } = "Pending";
    public string? ZatcaXmlStorageUrl { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public static Invoice Create(
        string invoiceNumber,
        Guid salesOrderId,
        Guid customerId,
        string invoiceType,
        DateTime issueDateUtc,
        DateTime dueDateUtc,
        decimal subTotal,
        decimal vatAmount,
        decimal totalAmount,
        string egsDeviceId,
        long zatcaIcv,
        Guid zatcaUuid,
        string zatcaPreviousInvoiceHash,
        string zatcaXmlHash,
        string zatcaQrCodeBase64) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            InvoiceNumber = invoiceNumber.Trim().ToUpperInvariant(),
            SalesOrderId = salesOrderId,
            CustomerId = customerId,
            InvoiceType = invoiceType,
            IssueDateUtc = issueDateUtc,
            DueDateUtc = dueDateUtc,
            SubTotal = subTotal,
            VatAmount = vatAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0.00m,
            PaymentStatus = "Unpaid",
            EgsDeviceId = egsDeviceId,
            ZatcaIcv = zatcaIcv,
            ZatcaUuid = zatcaUuid,
            ZatcaPreviousInvoiceHash = zatcaPreviousInvoiceHash,
            ZatcaXmlHash = zatcaXmlHash,
            ZatcaQrCodeBase64 = zatcaQrCodeBase64,
            ZatcaSubmissionStatus = "Pending"
        };

    public void AttachCopyUrl(string url) => UploadedInvoiceCopyUrl = url.Trim();

    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0) throw new BusinessRuleException("Allocated payment amount must be positive.");
        var remaining = TotalAmount - PaidAmount;
        if (amount > remaining)
            throw new BusinessRuleException($"Allocated amount ({amount}) exceeds invoice remaining balance ({remaining}).");

        PaidAmount = Math.Round(PaidAmount + amount, 2, MidpointRounding.AwayFromZero);
        PaymentStatus = PaidAmount >= TotalAmount ? "Paid" : "PartiallyPaid";
    }
}
