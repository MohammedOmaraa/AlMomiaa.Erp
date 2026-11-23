using AlMomiaa.Domain.Inventory;

namespace AlMomiaa.Domain.Sales;

public sealed class ZatcaDeviceSequence
{
    private ZatcaDeviceSequence() { }

    public string EgsDeviceId { get; private set; } = string.Empty;
    public int? WarehouseId { get; private set; }
    public Warehouse? Warehouse { get; private init; }
    public long CurrentIcv { get; private set; }
    public string LastInvoiceHash { get; private set; } = string.Empty;
    public string CertificateSerial { get; private set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; private set; }
}
