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

    public static ZatcaDeviceSequence Create(
        string egsDeviceId,
        int? warehouseId,
        string initialPreviousHash,
        string certificateSerial,
        DateTime updatedAtUtc) =>
        new()
        {
            EgsDeviceId = egsDeviceId.Trim(),
            WarehouseId = warehouseId,
            CurrentIcv = 0,
            LastInvoiceHash = initialPreviousHash,
            CertificateSerial = certificateSerial.Trim(),
            UpdatedAtUtc = updatedAtUtc
        };

    public (long NextIcv, string PreviousHash) AllocateNext(string newXmlHash, DateTime updatedAtUtc)
    {
        var previousHash = LastInvoiceHash;
        CurrentIcv += 1;
        LastInvoiceHash = newXmlHash;
        UpdatedAtUtc = updatedAtUtc;
        return (CurrentIcv, previousHash);
    }
}
