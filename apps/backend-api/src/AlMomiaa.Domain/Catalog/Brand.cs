namespace AlMomiaa.Domain.Catalog;

public sealed class Brand
{
    private Brand() { }

    public int Id { get; private set; }
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public bool IsActive { get; private set; } = true;
}
