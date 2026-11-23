using AlMomiaa.Domain.Identity;

namespace AlMomiaa.Domain.Finance;

public sealed class FinancialAccount
{
    private FinancialAccount() { }

    public Guid Id { get; private set; }
    public string AccountCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public string AccountType { get; private set; } = string.Empty;
    public Guid? OwnerRepId { get; private set; }
    public User? OwnerRep { get; private init; }
    public string? BankName { get; private set; }
    public string? Iban { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public static FinancialAccount Create(
        string accountCode,
        string nameAr,
        string? nameEn,
        string accountType,
        Guid? ownerRepId = null,
        string? bankName = null,
        string? iban = null) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            AccountCode = accountCode.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            AccountType = accountType.Trim(),
            OwnerRepId = ownerRepId,
            BankName = string.IsNullOrWhiteSpace(bankName) ? null : bankName.Trim(),
            Iban = string.IsNullOrWhiteSpace(iban) ? null : iban.Trim().ToUpperInvariant(),
            CurrentBalance = 0.00m,
            IsActive = true
        };

    public void AdjustBalance(decimal delta) =>
        CurrentBalance = Math.Round(CurrentBalance + delta, 2, MidpointRounding.AwayFromZero);
}
