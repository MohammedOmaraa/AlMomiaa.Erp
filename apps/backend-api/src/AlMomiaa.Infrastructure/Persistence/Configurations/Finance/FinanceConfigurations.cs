using AlMomiaa.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMomiaa.Infrastructure.Persistence.Configurations.Finance;

public sealed class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.ToTable("FiscalPeriods", "fin", table =>
        {
            table.HasCheckConstraint("CK_FiscalPeriods_Month", "[FiscalMonth] BETWEEN 1 AND 12");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.FiscalYear).IsRequired();
        builder.Property(x => x.FiscalMonth).IsRequired();
        builder.Property(x => x.PeriodStartUtc).IsRequired();
        builder.Property(x => x.PeriodEndUtc).IsRequired();
        builder.Property(x => x.IsClosed).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.FiscalYear, x.FiscalMonth })
            .IsUnique()
            .HasDatabaseName("UQ_FiscalPeriod_YearMonth");

        builder.HasOne(x => x.ClosedByUser)
            .WithMany()
            .HasForeignKey(x => x.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("FinancialAccounts", "fin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AccountCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.AccountCode).IsUnique().HasDatabaseName("UQ_FinancialAccounts_AccountCode");

        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.AccountType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(100);
        builder.Property(x => x.Iban).HasMaxLength(50);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.OwnerRepId)
            .IsUnique()
            .HasFilter("[OwnerRepId] IS NOT NULL")
            .HasDatabaseName("UX_FinancialAccounts_OwnerRep");

        builder.HasOne(x => x.OwnerRep)
            .WithMany()
            .HasForeignKey(x => x.OwnerRepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("Collections", "fin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReceiptNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.ReceiptNumber).IsUnique().HasDatabaseName("UQ_Collections_ReceiptNumber");

        builder.Property(x => x.PaymentMethod).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(35).IsRequired();
        builder.Property(x => x.IsCustomerConfirmed).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.PaymentProofImageUrl).HasMaxLength(500);
        builder.Property(x => x.PaymentProofImageHash).HasMaxLength(64).IsUnicode(false);
        builder.Property(x => x.BankReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.OriginalValueDateUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.IdempotencyKey).IsRequired();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UQ_Collections_IdempotencyKey");
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.PaymentProofImageHash)
            .IsUnique()
            .HasFilter("[PaymentProofImageHash] IS NOT NULL AND [Status] <> 'Rejected'")
            .HasDatabaseName("UX_Collections_ProofHash");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Rep)
            .WithMany()
            .HasForeignKey(x => x.RepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetFinancialAccount)
            .WithMany()
            .HasForeignKey(x => x.TargetFinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VerifiedByAccountant)
            .WithMany()
            .HasForeignKey(x => x.VerifiedByAccountantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CollectionAllocationConfiguration : IEntityTypeConfiguration<CollectionAllocation>
{
    public void Configure(EntityTypeBuilder<CollectionAllocation> builder)
    {
        builder.ToTable("CollectionAllocations", "fin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.AllocatedAmount).HasPrecision(18, 2).IsRequired();

        builder.HasOne(x => x.Collection)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.CollectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries", "fin");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EntryNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.EntryNumber).IsUnique().HasDatabaseName("UQ_JournalEntries_EntryNumber");

        builder.Property(x => x.PostingDateUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.OriginalValueDateUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.ReferenceType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReferenceId).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.FiscalPeriod)
            .WithMany()
            .HasForeignKey(x => x.FiscalPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("JournalLines", "fin", table =>
        {
            table.HasCheckConstraint(
                "CK_JournalLines_NonNegative",
                "[DebitAmount] >= 0 AND [CreditAmount] >= 0 AND ([DebitAmount] > 0 OR [CreditAmount] > 0)");
        });

        builder.HasKey(x => new { x.CreatedAtUtc, x.Id })
            .HasName("PK_JournalLines")
            .IsClustered();

        builder.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn();
        builder.Property(x => x.DebitAmount).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.CreditAmount).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.JournalEntry)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FinancialAccount)
            .WithMany()
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DailyFinancialSnapshotConfiguration : IEntityTypeConfiguration<DailyFinancialSnapshot>
{
    public void Configure(EntityTypeBuilder<DailyFinancialSnapshot> builder)
    {
        builder.ToTable("DailyFinancialSnapshots", "fin");
        builder.HasKey(x => x.SnapshotDate);

        builder.Property(x => x.TotalMainSafeCash).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.TotalRepCustodyCash).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.TotalBankBalance).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.TotalReceivables).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.TotalPayables).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.TotalInventoryValuation).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.DailyGrossSales).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.DailyGrossProfit).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.DailyCollectionsTotal).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.UpdatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
