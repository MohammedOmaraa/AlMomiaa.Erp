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
}
