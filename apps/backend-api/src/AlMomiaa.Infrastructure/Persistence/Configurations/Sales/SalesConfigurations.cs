using AlMomiaa.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMomiaa.Infrastructure.Persistence.Configurations.Sales;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders", "sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.OrderNumber).IsUnique().HasDatabaseName("UQ_SalesOrders_OrderNumber");

        builder.Property(x => x.OrderChannel).HasMaxLength(20).IsRequired().HasDefaultValue("PreSales");
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CustomerBalanceAtOrder).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CustomerCreditLimitAtOrder).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.ExceededCreditWarning).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RequiresFinanceCoApproval).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasStockShortage).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasPriceOverridePending).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ReservationState).HasMaxLength(25).IsRequired().HasDefaultValue("Locked");
        builder.Property(x => x.SubTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.VatAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.IdempotencyKey).IsRequired();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UQ_SalesOrders_IdempotencyKey");
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_SalesOrders_Status_Created");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Rep)
            .WithMany()
            .HasForeignKey(x => x.RepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FinanceCoApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.FinanceCoApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByManager)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EditedByManager)
            .WithMany()
            .HasForeignKey(x => x.EditedByManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalesOrderItemConfiguration : IEntityTypeConfiguration<SalesOrderItem>
{
    public void Configure(EntityTypeBuilder<SalesOrderItem> builder)
    {
        builder.ToTable("SalesOrderItems", "sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ConversionFactor).HasPrecision(18, 4).HasDefaultValue(1.0000m);
        builder.Property(x => x.OrderedQty).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.ApprovedQty).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.ShippedQty).HasPrecision(18, 3);
        builder.Property(x => x.StandardTierPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PriceOverridePending).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.UnitCostSnapshot).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.VatAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.LineTotal).HasPrecision(18, 2).IsRequired();

        builder.HasOne(x => x.SalesOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MasterProduct)
            .WithMany()
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PriceOverrideApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.PriceOverrideApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ZatcaDeviceSequenceConfiguration : IEntityTypeConfiguration<ZatcaDeviceSequence>
{
    public void Configure(EntityTypeBuilder<ZatcaDeviceSequence> builder)
    {
        builder.ToTable("ZatcaDeviceSequences", "sales");
        builder.HasKey(x => x.EgsDeviceId);

        builder.Property(x => x.EgsDeviceId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CurrentIcv).IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.LastInvoiceHash).HasMaxLength(250).IsRequired();
        builder.Property(x => x.CertificateSerial).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", "sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.InvoiceNumber).IsUnique().HasDatabaseName("UQ_Invoices_InvoiceNumber");

        builder.HasIndex(x => x.SalesOrderId).IsUnique().HasDatabaseName("UQ_Invoices_SalesOrderId");

        builder.Property(x => x.InvoiceType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IssueDateUtc).IsRequired();
        builder.Property(x => x.DueDateUtc).IsRequired();
        builder.Property(x => x.SubTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.VatAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.RemainingBalance)
            .HasPrecision(18, 2)
            .HasComputedColumnSql("([TotalAmount] - [PaidAmount])", stored: true);
        builder.Property(x => x.PaymentStatus).HasMaxLength(20).IsRequired().HasDefaultValue("Unpaid");
        builder.Property(x => x.UploadedInvoiceCopyUrl).HasMaxLength(500);
        builder.Property(x => x.EgsDeviceId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ZatcaIcv).IsRequired();
        builder.Property(x => x.ZatcaUuid).IsRequired();
        builder.HasIndex(x => x.ZatcaUuid).IsUnique().HasDatabaseName("UQ_Invoices_ZatcaUuid");
        builder.Property(x => x.ZatcaPreviousInvoiceHash).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ZatcaXmlHash).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ZatcaQrCodeBase64).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ZatcaSubmissionStatus).HasMaxLength(30).IsRequired().HasDefaultValue("Pending");
        builder.Property(x => x.ZatcaXmlStorageUrl).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.EgsDeviceId, x.ZatcaIcv })
            .IsUnique()
            .HasDatabaseName("UQ_Zatca_Device_Icv");

        builder.HasIndex(x => new { x.CustomerId, x.PaymentStatus, x.DueDateUtc })
            .HasDatabaseName("IX_Invoices_Customer_Status");

        builder.HasOne(x => x.SalesOrder)
            .WithOne()
            .HasForeignKey<Invoice>(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ZatcaDeviceSequence)
            .WithMany()
            .HasForeignKey(x => x.EgsDeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns", "sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CreditNoteNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.CreditNoteNumber).IsUnique().HasDatabaseName("UQ_SalesReturns_CreditNoteNumber");

        builder.Property(x => x.ReturnReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SubTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.VatAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.EgsDeviceId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ZatcaIcv).IsRequired();
        builder.Property(x => x.ZatcaUuid).IsRequired();
        builder.HasIndex(x => x.ZatcaUuid).IsUnique().HasDatabaseName("UQ_SalesReturns_ZatcaUuid");
        builder.Property(x => x.ZatcaXmlHash).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ZatcaQrCodeBase64).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ZatcaSubmissionStatus).HasMaxLength(30).IsRequired().HasDefaultValue("Pending");
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.OriginalInvoice)
            .WithMany()
            .HasForeignKey(x => x.OriginalInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ZatcaDeviceSequence)
            .WithMany()
            .HasForeignKey(x => x.EgsDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalesReturnItemConfiguration : IEntityTypeConfiguration<SalesReturnItem>
{
    public void Configure(EntityTypeBuilder<SalesReturnItem> builder)
    {
        builder.ToTable("SalesReturnItems", "sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReturnedQty).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.IsSellableRestock).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.VatAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.LineTotal).HasPrecision(18, 2).IsRequired();

        builder.HasOne(x => x.SalesReturn)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MasterProduct)
            .WithMany()
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DeliveryDispatchConfiguration : IEntityTypeConfiguration<DeliveryDispatch>
{
    public void Configure(EntityTypeBuilder<DeliveryDispatch> builder)
    {
        builder.ToTable("DeliveryDispatches", "sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.DispatchNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.DispatchNumber).IsUnique().HasDatabaseName("UQ_DeliveryDispatches_DispatchNumber");

        builder.Property(x => x.VehiclePlateNumber).HasMaxLength(30);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired().HasDefaultValue("PreparedAtWarehouse");
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DriverOrRep)
            .WithMany()
            .HasForeignKey(x => x.DriverOrRepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DeliveryDispatchItemConfiguration : IEntityTypeConfiguration<DeliveryDispatchItem>
{
    public void Configure(EntityTypeBuilder<DeliveryDispatchItem> builder)
    {
        builder.ToTable("DeliveryDispatchItems", "sales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.DeliveryStatus).HasMaxLength(30).IsRequired().HasDefaultValue("PreparedAtWarehouse");
        builder.Property(x => x.SignedPodImageUrl).HasMaxLength(500);
        builder.Property(x => x.DeliveryNotes).HasMaxLength(500);

        builder.HasIndex(x => new { x.DeliveryDispatchId, x.InvoiceId })
            .IsUnique()
            .HasDatabaseName("UQ_Dispatch_Invoice");

        builder.HasOne(x => x.DeliveryDispatch)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.DeliveryDispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Invoice)
            .WithMany()
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
