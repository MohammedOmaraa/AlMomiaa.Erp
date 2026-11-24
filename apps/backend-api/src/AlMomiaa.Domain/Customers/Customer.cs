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

    public static Customer Create(
        string customerCode,
        string nameAr,
        string? nameEn,
        string phone,
        string address,
        string city,
        string category,
        string? vatNumber,
        int gradeId,
        int? priceListId,
        int? customPaymentDays,
        decimal creditLimit,
        Guid assignedRepId,
        decimal? latitude,
        decimal? longitude) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CustomerCode = customerCode.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            Phone = phone.Trim(),
            Address = address.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? "Riyadh" : city.Trim(),
            Category = category.Trim(),
            VatNumber = string.IsNullOrWhiteSpace(vatNumber) ? null : vatNumber.Trim(),
            GradeId = gradeId,
            PriceListId = priceListId,
            CustomPaymentDays = customPaymentDays,
            CreditLimit = creditLimit,
            CurrentBalance = 0.00m,
            AssignedRepId = assignedRepId,
            Latitude = latitude,
            Longitude = longitude,
            IsActive = true
        };

    public void Debit(decimal amount) => CurrentBalance = Math.Round(CurrentBalance + amount, 2, MidpointRounding.AwayFromZero);
    public void Credit(decimal amount) => CurrentBalance = Math.Round(CurrentBalance - amount, 2, MidpointRounding.AwayFromZero);

    public void AssignRepresentative(Guid repId)
    {
        if (repId == Guid.Empty)
        {
            throw new ArgumentException("AssignedRepId is required.", nameof(repId));
        }

        AssignedRepId = repId;
    }
}
