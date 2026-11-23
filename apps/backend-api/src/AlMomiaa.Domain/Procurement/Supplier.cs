namespace AlMomiaa.Domain.Procurement;

public sealed class Supplier
{
    private Supplier() { }

    public Guid Id { get; private set; }
    public string SupplierCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public string Country { get; private set; } = "Saudi Arabia";
    public string? Phone { get; private set; }
    public string? VatNumber { get; private set; }
    public decimal CurrentPayableBalance { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
}
