using AlMomiaa.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMomiaa.Infrastructure.Persistence.Configurations.Customers;

public sealed class CustomerGradeConfiguration : IEntityTypeConfiguration<CustomerGrade>
{
    public void Configure(EntityTypeBuilder<CustomerGrade> builder)
    {
        builder.ToTable("CustomerGrades", "cust");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GradeCode)
            .HasColumnType("char(1)")
            .HasMaxLength(1)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(x => x.GradeCode).IsUnique().HasDatabaseName("UQ_CustomerGrades_GradeCode");

        builder.Property(x => x.DefaultPaymentDays).IsRequired();
        builder.Property(x => x.AllowCredit).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.UpdatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasData(CanonicalCustomerGrades.All);
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "cust");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CustomerCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.CustomerCode).IsUnique().HasDatabaseName("UQ_Customers_CustomerCode");

        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired().HasDefaultValue("Riyadh");
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.Property(x => x.VatNumber).HasMaxLength(15);

        builder.Property(x => x.CustomPaymentDays);
        builder.Property(x => x.CreditLimit).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasOne(x => x.Grade)
            .WithMany()
            .HasForeignKey(x => x.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PriceList)
            .WithMany()
            .HasForeignKey(x => x.PriceListId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedRep)
            .WithMany()
            .HasForeignKey(x => x.AssignedRepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AssignedRepId)
            .HasDatabaseName("IX_Customers_AssignedRep")
            .IncludeProperties(x => new
            {
                x.NameAr,
                x.GradeId,
                x.PriceListId,
                x.CreditLimit,
                x.CurrentBalance
            });
    }
}
