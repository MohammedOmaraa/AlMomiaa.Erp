using AlMomiaa.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMomiaa.Infrastructure.Persistence.Configurations.Inventory;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses", "inv");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WarehouseCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.WarehouseCode).IsUnique().HasDatabaseName("UQ_Warehouses_WarehouseCode");

        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.WarehouseType).HasMaxLength(30).IsRequired().HasDefaultValue("MainWarehouse");
        builder.Property(x => x.VehiclePlateNumber).HasMaxLength(30);
        builder.Property(x => x.City).HasMaxLength(100).IsRequired().HasDefaultValue("Riyadh");
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.AssignedRep)
            .WithMany()
            .HasForeignKey(x => x.AssignedRepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks", "inv", table =>
        {
            table.HasCheckConstraint(
                "CK_WarehouseStocks_NonNegative",
                "[PhysicalQty] >= 0 AND [ReservedQty] >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PhysicalQty).HasPrecision(18, 3).HasDefaultValue(0.000m);
        builder.Property(x => x.ReservedQty).HasPrecision(18, 3).HasDefaultValue(0.000m);
        builder.Property(x => x.IncomingQty).HasPrecision(18, 3).HasDefaultValue(0.000m);
        builder.Property(x => x.AvailableQty)
            .HasPrecision(18, 3)
            .HasComputedColumnSql("([PhysicalQty] - [ReservedQty])", stored: true);
        builder.Property(x => x.MinReorderQty).HasPrecision(18, 3).HasDefaultValue(10.000m);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.WarehouseId, x.MasterProductId })
            .IsUnique()
            .HasDatabaseName("UQ_Warehouse_Product");

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MasterProduct)
            .WithMany()
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", "inv");
        builder.HasKey(x => new { x.CreatedAtUtc, x.Id })
            .HasName("PK_StockMovements")
            .IsClustered();

        builder.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn();
        builder.Property(x => x.MovementType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.QuantityDelta).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.PhysicalBalanceAfter).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.UnitCost).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReferenceId).IsRequired();
        builder.Property(x => x.LotNumber).HasMaxLength(50);
        builder.Property(x => x.ExpiryDate);
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MasterProduct)
            .WithMany()
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers", "inv");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TransferNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.TransferNumber).IsUnique().HasDatabaseName("UQ_StockTransfers_TransferNumber");

        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DispatchedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.ReceivedAtUtc);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasOne(x => x.SourceWarehouse)
            .WithMany()
            .HasForeignKey(x => x.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(x => x.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DispatchedByUser)
            .WithMany()
            .HasForeignKey(x => x.DispatchedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReceivedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockTransferItemConfiguration : IEntityTypeConfiguration<StockTransferItem>
{
    public void Configure(EntityTypeBuilder<StockTransferItem> builder)
    {
        builder.ToTable("StockTransferItems", "inv");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.RequestedQty).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.DispatchedQty).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.ReceivedQty).HasPrecision(18, 3);

        builder.HasOne(x => x.StockTransfer)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.StockTransferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MasterProduct)
            .WithMany()
            .HasForeignKey(x => x.MasterProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
