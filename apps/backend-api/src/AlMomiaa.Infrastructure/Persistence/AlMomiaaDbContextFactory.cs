using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlMomiaa.Infrastructure.Persistence;

public sealed class AlMomiaaDbContextFactory : IDesignTimeDbContextFactory<AlMomiaaDbContext>
{
    public AlMomiaaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ALMOMIAA_DESIGNTIME_SQLSERVER")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
            ?? "Server=localhost;Database=AlMomiaa;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AlMomiaaDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(AlMomiaaDbContext).Assembly.FullName));

        return new AlMomiaaDbContext(optionsBuilder.Options);
    }
}
