namespace AlMomiaa.Domain.Catalog;

public sealed class Brand
{
    private Brand() { }

    public int Id { get; private set; }
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Brand Create(string nameAr, string? nameEn) =>
        new()
        {
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            IsActive = true
        };
}
