using AlMomiaa.Domain.Catalog;
using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.Customers;

public sealed class Customer
{
    private Customer() { }

    public Guid Id { get; private set; }
    public string CustomerCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = "Riyadh";
    public string Category { get; private set; } = string.Empty;
    public string? VatNumber { get; private set; }
    public int GradeId { get; private set; }
    public CustomerGrade Grade { get; private init; } = null!;
    public int? PriceListId { get; private set; }
    public PriceList? PriceList { get; private init; }
    public int? CustomPaymentDays { get; private set; }
    public decimal CreditLimit { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public Guid AssignedRepId { get; private set; }
    public User AssignedRep { get; private init; } = null!;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
}
