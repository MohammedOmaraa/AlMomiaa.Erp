using AlMomiaa.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlMomiaa.Infrastructure.Persistence;

public sealed class AlMomiaaDbContext(DbContextOptions<AlMomiaaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserWarehouse> UserWarehouses => Set<UserWarehouse>();
    public DbSet<RefreshTokenSession> RefreshTokenSessions => Set<RefreshTokenSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("id");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlMomiaaDbContext).Assembly);

        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            modelBuilder.Entity<RefreshTokenSession>()
                .Property(x => x.RowVersion)
                .HasDefaultValueSql("randomblob(8)");
        }
    }
}
