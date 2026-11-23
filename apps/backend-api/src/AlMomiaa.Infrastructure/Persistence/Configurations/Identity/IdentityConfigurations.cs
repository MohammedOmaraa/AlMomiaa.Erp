using AlMomiaa.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMomiaa.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "id");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EmployeeCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.EmployeeCode).IsUnique().HasDatabaseName("UQ_Users_EmployeeCode");

        builder.Property(x => x.FullNameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.FullNameEn).HasMaxLength(150);

        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Phone).IsUnique().HasDatabaseName("UQ_Users_Phone");

        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RepType).HasMaxLength(20);
        builder.Property(x => x.DefaultWarehouseId);
        builder.Property(x => x.SecurityStamp).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "id");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.RoleCode).IsUnique().HasDatabaseName("UQ_Roles_RoleCode");

        builder.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(100).IsRequired();

        builder.HasData(CanonicalRolesAndPermissions.All
            .Select(r => Role.Create(r.Id, r.RoleCode, r.NameAr, r.NameEn)));
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", "id");
        builder.HasKey(x => new { x.RoleId, x.PermissionClaim }).HasName("PK_RolePermissions");

        builder.Property(x => x.PermissionClaim).HasMaxLength(100).IsRequired();

        builder.HasOne(x => x.Role)
            .WithMany(x => x.Permissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(CanonicalRolesAndPermissions.All
            .SelectMany(r => r.Permissions.Select(p => RolePermission.Create(r.Id, p))));
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "id");
        builder.HasKey(x => new { x.UserId, x.RoleId }).HasName("PK_UserRoles");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserWarehouseConfiguration : IEntityTypeConfiguration<UserWarehouse>
{
    public void Configure(EntityTypeBuilder<UserWarehouse> builder)
    {
        builder.ToTable("UserWarehouses", "id");
        builder.HasKey(x => new { x.UserId, x.WarehouseId }).HasName("PK_UserWarehouses");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Warehouses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenSessionConfiguration : IEntityTypeConfiguration<RefreshTokenSession>
{
    public void Configure(EntityTypeBuilder<RefreshTokenSession> builder)
    {
        builder.ToTable("RefreshTokenSessions", "id");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.FamilyId).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.IssuedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.RevokedAtUtc);
        builder.Property(x => x.ReplacedBySessionId);
        builder.Property(x => x.RevocationReason).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokenSessions_TokenHash");

        builder.HasIndex(x => x.FamilyId)
            .HasDatabaseName("IX_RefreshTokenSessions_FamilyId");

        builder.HasIndex(x => new { x.UserId, x.RevokedAtUtc, x.ExpiresAtUtc })
            .HasDatabaseName("IX_RefreshTokenSessions_User_Active");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
