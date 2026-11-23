using AlMomiaa.Domain.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMomiaa.Infrastructure.Persistence.Configurations.Procurement;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", "proc");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SupplierCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.SupplierCode).IsUnique().HasDatabaseName("UQ_Suppliers_SupplierCode");

        builder.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(200);
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired().HasDefaultValue("Saudi Arabia");
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.VatNumber).HasMaxLength(20);
        builder.Property(x => x.CurrentPayableBalance).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
    }
}

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", "proc");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PoNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.PoNumber).IsUnique().HasDatabaseName("UQ_PurchaseOrders_PoNumber");

        builder.Property(x => x.OrderType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.TotalAmountSAR).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetWarehouse)
            .WithMany()
            .HasForeignKey(x => x.TargetWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems", "proc");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.OrderedQty).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.ReceivedQty).HasPrecision(18, 3).HasDefaultValue(0.000m);
        builder.Property(x => x.UnitCostSAR).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.LineTotalSAR)
            .HasPrecision(18, 2)
            .HasComputedColumnSql("(CAST([OrderedQty] * [UnitCostSAR] AS DECIMAL(18,2)))", stored: true);

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MasterProduct)
            .WithMany()
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("Containers", "proc");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ContainerNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BillOfLading).HasMaxLength(100);
        builder.Property(x => x.ShippingLine).HasMaxLength(100);
        builder.Property(x => x.PortOfArrival).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EstimatedArrivalDate).IsRequired();
        builder.Property(x => x.ActualArrivalDate);
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SupplierGoodsCostSAR).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.FreightAndCustomsCostSAR).HasPrecision(18, 2).HasDefaultValue(0.00m);
        builder.Property(x => x.TotalLandedCostSAR)
            .HasPrecision(18, 2)
            .HasComputedColumnSql("([SupplierGoodsCostSAR] + [FreightAndCustomsCostSAR])", stored: true);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("GoodsReceipts", "proc");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.GrnNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.GrnNumber).IsUnique().HasDatabaseName("UQ_GoodsReceipts_GrnNumber");
        builder.Property(x => x.ReceivedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Container)
            .WithMany()
            .HasForeignKey(x => x.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReceivedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GoodsReceiptItemConfiguration : IEntityTypeConfiguration<GoodsReceiptItem>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptItem> builder)
    {
        builder.ToTable("GoodsReceiptItems", "proc");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReceivedQty).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.AllocatedLandedUnitCostSAR).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.LotNumber).HasMaxLength(50);
        builder.Property(x => x.ExpiryDate);

        builder.HasOne(x => x.GoodsReceipt)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MasterProduct)
            .WithMany()
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
