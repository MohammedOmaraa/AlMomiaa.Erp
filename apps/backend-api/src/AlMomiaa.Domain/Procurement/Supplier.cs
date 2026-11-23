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

    public static Supplier Create(string supplierCode, string nameAr, string? nameEn, string country, string? phone, string? vatNumber) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SupplierCode = supplierCode.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            Country = string.IsNullOrWhiteSpace(country) ? "Saudi Arabia" : country.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            VatNumber = string.IsNullOrWhiteSpace(vatNumber) ? null : vatNumber.Trim(),
            CurrentPayableBalance = 0.00m,
            IsActive = true
        };

    public void AddPayable(decimal amount) => CurrentPayableBalance = Math.Round(CurrentPayableBalance + amount, 2, MidpointRounding.AwayFromZero);
}
