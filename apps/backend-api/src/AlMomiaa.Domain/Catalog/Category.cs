namespace AlMomiaa.Domain.Catalog;

public sealed class Category
{
    private Category() { }

    public int Id { get; private set; }
    public int? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private init; }
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public ICollection<Category> SubCategories { get; private init; } = new List<Category>();

    public static Category Create(int? parentCategoryId, string nameAr, string? nameEn, int displayOrder = 0) =>
        new()
        {
            ParentCategoryId = parentCategoryId,
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            DisplayOrder = displayOrder,
            IsActive = true
        };
}
