using AlMomiaa.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMomiaa.Infrastructure.Persistence.Configurations.Catalog;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "cat");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.ParentCategory)
            .WithMany(x => x.SubCategories)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands", "cat");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
    }
}

public sealed class MasterProductConfiguration : IEntityTypeConfiguration<MasterProduct>
{
    public void Configure(EntityTypeBuilder<MasterProduct> builder)
    {
        builder.ToTable("MasterProducts", "cat");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SKU).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.SKU).IsUnique().HasDatabaseName("UQ_MasterProducts_SKU");

        builder.Property(x => x.Barcode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Barcode).IsUnique().HasDatabaseName("UQ_MasterProducts_Barcode");

        builder.Property(x => x.NameAr).HasMaxLength(250).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(250);
        builder.Property(x => x.BaseUnit).HasMaxLength(30).IsRequired();

        builder.Property(x => x.WeightedAvgCost).HasPrecision(18, 4).HasDefaultValue(0.0000m);
        builder.Property(x => x.WholesaleSellingPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.RetailSellingPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.MinAllowedSellingPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.VatRate).HasPrecision(5, 2).HasDefaultValue(15.00m);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Brand)
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CategoryId)
            .HasDatabaseName("IX_MasterProducts_Category")
            .IncludeProperties(x => new
            {
                x.SKU,
                x.NameAr,
                x.WholesaleSellingPrice,
                x.RetailSellingPrice,
                x.MinAllowedSellingPrice
            });
    }
}

public sealed class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.ToTable("ProductUnits", "cat");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UnitNameAr).HasMaxLength(50).IsRequired();
        builder.Property(x => x.UnitNameEn).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ConversionFactor).HasPrecision(18, 4).HasDefaultValue(1.0000m);
        builder.Property(x => x.Barcode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Barcode).IsUnique().HasDatabaseName("UQ_ProductUnits_Barcode");

        builder.Property(x => x.WholesalePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.RetailPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.MinAllowedPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.IsDefaultUnit).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.MasterProduct)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("PriceLists", "cat");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PriceListCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.PriceListCode).IsUnique().HasDatabaseName("UQ_PriceLists_PriceListCode");

        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.IsDefault).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.EffectiveFromUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.EffectiveToUtc);
    }
}

public sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("PriceListItems", "cat");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MinQuantity).HasPrecision(18, 3).HasDefaultValue(1.000m);
        builder.Property(x => x.TierUnitPrice).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(x => new { x.PriceListId, x.ProductUnitId, x.MinQuantity })
            .IsUnique()
            .HasDatabaseName("UQ_PriceList_Unit_MinQty");

        builder.HasOne(x => x.PriceList)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PriceListId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SupplierProductConfiguration : IEntityTypeConfiguration<SupplierProduct>
{
    public void Configure(EntityTypeBuilder<SupplierProduct> builder)
    {
        builder.ToTable("SupplierProducts", "cat");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SupplierSku).HasMaxLength(50);
        builder.Property(x => x.SupplierBarcode).HasMaxLength(50);
        builder.Property(x => x.LastPurchasePrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.LeadTimeDays);
        builder.Property(x => x.IsPreferredSupplier).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.UpdatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => new { x.SupplierId, x.MasterProductId })
            .IsUnique()
            .HasDatabaseName("UQ_Supplier_MasterProduct");

        builder.HasOne(x => x.MasterProduct)
            .WithMany(x => x.SupplierProducts)
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .HasConstraintName("FK_SupplierProducts_Supplier")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
