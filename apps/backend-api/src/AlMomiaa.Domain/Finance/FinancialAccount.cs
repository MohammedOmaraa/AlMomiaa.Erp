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
}
