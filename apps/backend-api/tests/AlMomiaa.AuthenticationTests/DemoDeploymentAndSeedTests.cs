using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Infrastructure;
using AlMomiaa.Infrastructure.Persistence;
using AlMomiaa.WebApi.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AlMomiaa.AuthenticationTests;

public sealed class DemoDeploymentAndSeedTests
{
    [Fact]
    public async Task DemoDataSeeder_SeedsIdempotently_WithSingleWarehouseKeeper_AndMultiWarehouseProducts()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // First seed run
        await DemoDataSeeder.SeedAsync(db, hasher, "DemoStrong#2026", NullLogger.Instance);
        // Second seed run must be strictly idempotent
        await DemoDataSeeder.SeedAsync(db, hasher, "DemoStrong#2026", NullLogger.Instance);

        Assert.True(await db.Warehouses.CountAsync(w => w.WarehouseCode == "WH-RYD-MAIN" || w.WarehouseCode == "WH-JED-BR" || w.WarehouseCode == "VAN-RYD-01") == 3);

        var demoUserCodes = new[] { "ADMIN-DEMO", "MGR-DEMO", "REP-DEMO", "WHK-DEMO" };
        var demoUsers = await db.Users
            .Include(u => u.Warehouses)
            .Include(u => u.Roles)
            .Where(u => demoUserCodes.Contains(u.EmployeeCode))
            .ToListAsync();

        Assert.Equal(4, demoUsers.Count);

        var whKeeper = Assert.Single(demoUsers, u => u.EmployeeCode == "WHK-DEMO");
        Assert.Single(whKeeper.Warehouses);
        Assert.Equal(whKeeper.Warehouses.First().WarehouseId, whKeeper.DefaultWarehouseId);

        var repUser = Assert.Single(demoUsers, u => u.EmployeeCode == "REP-DEMO");
        var repCustomers = await db.Customers.Where(c => c.AssignedRepId == repUser.Id).ToListAsync();
        Assert.Equal(3, repCustomers.Count);

        var demoProducts = await db.MasterProducts
            .Include(p => p.Units)
            .Where(p => p.SKU.StartsWith("ALM-"))
            .ToListAsync();
        Assert.Equal(4, demoProducts.Count);
        Assert.All(demoProducts, p => Assert.Equal(2, p.Units.Count));

        // Each demo product has stock split across 2 warehouses (WH-RYD-MAIN + WH-JED-BR)
        var productIds = demoProducts.Select(p => p.Id).ToArray();
        var stockCount = await db.WarehouseStocks.CountAsync(s => productIds.Contains(s.MasterProductId));
        Assert.Equal(8, stockCount);

        Assert.True(await db.ZatcaDeviceSequences.AnyAsync(z => z.EgsDeviceId == "EGS-DEMO-01"));
        Assert.True(await db.GpsRouteHistories.AnyAsync(g => g.RepId == repUser.Id));
    }

    [Fact]
    public async Task DemoDataSeeder_RejectsMissingOrWeakDefaultPassword()
    {
        await using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DemoDataSeeder.SeedAsync(db, hasher, "", NullLogger.Instance));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DemoDataSeeder.SeedAsync(db, hasher, "short", NullLogger.Instance));
    }

    [Fact]
    public void CorsSecurityOptions_ParsesAllowedOriginsCsv_AndDeduplicates()
    {
        var options = new CorsSecurityOptions
        {
            AllowedOrigins = ["https://erp.almomiaa.sa/"],
            AllowedOriginsCsv = "https://erp.almomiaa.sa, https://rep.almomiaa.sa/ ; https://admin.almomiaa.sa"
        };

        var resolved = options.ResolveAllowedOrigins();
        Assert.Equal(3, resolved.Length);
        Assert.Contains("https://erp.almomiaa.sa", resolved);
        Assert.Contains("https://rep.almomiaa.sa", resolved);
        Assert.Contains("https://admin.almomiaa.sa", resolved);
    }

    [Fact]
    public void DependencyInjection_EnforcesFailClosedRedisByDefault_AndAllowsOptInInMemoryFallback()
    {
        var configFailClosed = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = "Server=localhost;Database=AlMomiaa;Trusted_Connection=true;",
                ["Redis:ConnectionString"] = "",
                ["Redis:AllowInMemoryFallback"] = "false"
            })
            .Build();

        var servicesFailClosed = new ServiceCollection();
        servicesFailClosed.AddInfrastructure(configFailClosed, isDevelopment: false);
        using var spFailClosed = servicesFailClosed.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => spFailClosed.GetRequiredService<ISecurityStampStore>());

        var configOptIn = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = "Server=localhost;Database=AlMomiaa;Trusted_Connection=true;",
                ["Redis:ConnectionString"] = "",
                ["Redis:AllowInMemoryFallback"] = "true"
            })
            .Build();

        var servicesOptIn = new ServiceCollection();
        servicesOptIn.AddInfrastructure(configOptIn, isDevelopment: false);
        using var spOptIn = servicesOptIn.BuildServiceProvider();
        var stampStore = spOptIn.GetRequiredService<ISecurityStampStore>();
        Assert.NotNull(stampStore);
    }
}
