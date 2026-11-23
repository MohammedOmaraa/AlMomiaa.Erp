using AlMomiaa.Domain.SystemOps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMomiaa.Infrastructure.Persistence.Configurations.SystemOps;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "ops");
        builder.HasKey(x => new { x.TimestampUtc, x.Id })
            .HasName("PK_AuditLogs")
            .IsClustered();

        builder.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.UserRole).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ActionName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OldValuesJson);
        builder.Property(x => x.NewValuesJson);
        builder.Property(x => x.IpAddress).HasMaxLength(45).IsUnicode(false);
        builder.Property(x => x.TimestampUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "ops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MessageType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.ProcessedAtUtc);
        builder.Property(x => x.RetryCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LastError).HasMaxLength(1000);

        builder.HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc })
            .HasFilter("[ProcessedAtUtc] IS NULL")
            .HasDatabaseName("IX_Outbox_Unprocessed");
    }
}

public sealed class GpsRouteHistoryConfiguration : IEntityTypeConfiguration<GpsRouteHistory>
{
    public void Configure(EntityTypeBuilder<GpsRouteHistory> builder)
    {
        builder.ToTable("GpsRouteHistory", "ops");
        builder.HasKey(x => new { x.RecordedAtUtc, x.Id })
            .HasName("PK_GpsRouteHistory")
            .IsClustered();

        builder.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn();
        builder.Property(x => x.Latitude).HasPrecision(9, 6).IsRequired();
        builder.Property(x => x.Longitude).HasPrecision(9, 6).IsRequired();
        builder.Property(x => x.AccuracyMeters).HasPrecision(6, 2);
        builder.Property(x => x.NearCustomerId);
        builder.Property(x => x.RecordedAtUtc).IsRequired();

        builder.HasOne(x => x.Rep)
            .WithMany()
            .HasForeignKey(x => x.RepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
