using System.Collections.Concurrent;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlMomiaa.AuthenticationTests;

public sealed class InMemorySecurityStampStore : ISecurityStampStore
{
    private readonly ConcurrentDictionary<Guid, string> _stamps = new();
    public bool SimulateUnavailable { get; set; }

    public Task<string?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (SimulateUnavailable)
        {
            throw new SecurityStampUnavailableException("Simulated Redis outage.");
        }

        return Task.FromResult(_stamps.TryGetValue(userId, out var stamp) ? stamp : null);
    }

    public Task SetAsync(Guid userId, Guid securityStamp, CancellationToken cancellationToken)
    {
        if (SimulateUnavailable)
        {
            throw new SecurityStampUnavailableException("Simulated Redis outage.");
        }

        _stamps[userId] = securityStamp.ToString("D");
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (SimulateUnavailable)
        {
            throw new SecurityStampUnavailableException("Simulated Redis outage.");
        }

        _stamps.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryLoginAttemptLimiter : ILoginAttemptLimiter
{
    private readonly ConcurrentDictionary<string, int> _pairCounts = new();
    private readonly ConcurrentDictionary<string, int> _idCounts = new();
    public int MaxPerPair { get; set; } = 5;
    public int MaxPerIdentifier { get; set; } = 20;

    public Task<bool> IsBlockedAsync(string clientKey, string identifierKey, CancellationToken cancellationToken)
    {
        var pairBlocked = _pairCounts.TryGetValue($"{clientKey}:{identifierKey}", out var p) && p >= MaxPerPair;
        var idBlocked = _idCounts.TryGetValue(identifierKey, out var i) && i >= MaxPerIdentifier;
        return Task.FromResult(pairBlocked || idBlocked);
    }

    public Task RecordFailureAsync(string clientKey, string identifierKey, CancellationToken cancellationToken)
    {
        _pairCounts.AddOrUpdate($"{clientKey}:{identifierKey}", 1, (_, count) => count + 1);
        _idCounts.AddOrUpdate(identifierKey, 1, (_, count) => count + 1);
        return Task.CompletedTask;
    }

    public Task ResetAsync(string clientKey, string identifierKey, CancellationToken cancellationToken)
    {
        _pairCounts.TryRemove($"{clientKey}:{identifierKey}", out _);
        return Task.CompletedTask;
    }
}

public sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestIssuer = "https://test.api.almomiaa.sa";
    public const string TestAudience = "almomiaa-erp-test";
    public const string TestSigningKey = "TestOnlySigningKey_AlMomiaaERP_2026_AtLeast32BytesLong!";
    public const string TrustedOrigin = "https://erp.almomiaa.sa";

    private readonly SqliteConnection _sqliteConnection;

    public InMemorySecurityStampStore StampStore { get; } = new();
    public InMemoryLoginAttemptLimiter AttemptLimiter { get; } = new();
    public MutableTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public AuthWebApplicationFactory()
    {
        _sqliteConnection = new SqliteConnection("Data Source=:memory:");
        _sqliteConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:Jwt:Issuer", TestIssuer);
        builder.UseSetting("Auth:Jwt:Audience", TestAudience);
        builder.UseSetting("Auth:Jwt:SigningKey", TestSigningKey);
        builder.UseSetting("Auth:Jwt:AccessTokenMinutes", "10");
        builder.UseSetting("Cors:AllowedOrigins:0", TrustedOrigin);
        builder.UseSetting("Redis:ConnectionString", "localhost:6379");
        builder.UseSetting("ConnectionStrings:SqlServer", "Server=.;Database=UnusedInSqliteTests;");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AlMomiaaDbContext>>();
            services.RemoveAll<DbContextOptions<AlMomiaaDbContext>>();
            services.RemoveAll<AlMomiaaDbContext>();
            services.AddDbContext<AlMomiaaDbContext>(options => options.UseSqlite(_sqliteConnection));

            services.RemoveAll<ISecurityStampStore>();
            services.AddSingleton<ISecurityStampStore>(StampStore);

            services.RemoveAll<ILoginAttemptLimiter>();
            services.AddSingleton<ILoginAttemptLimiter>(AttemptLimiter);

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER IF NOT EXISTS TR_RefreshTokenSessions_RowVersion
            AFTER UPDATE ON RefreshTokenSessions
            BEGIN
                UPDATE RefreshTokenSessions SET RowVersion = randomblob(8) WHERE rowid = NEW.rowid;
            END;
            """);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT OR IGNORE INTO Warehouses (Id, WarehouseCode, NameAr, NameEn, WarehouseType, City, IsActive) VALUES
                (1, 'WH-01', 'المستودع الرئيسي', 'Main Warehouse', 'MainWarehouse', 'Riyadh', 1),
                (2, 'WH-02', 'مستودع الفرع', 'Branch Warehouse', 'BranchWarehouse', 'Riyadh', 1),
                (4, 'WH-04', 'مستودع جدة', 'Jeddah Warehouse', 'BranchWarehouse', 'Jeddah', 1),
                (5, 'WH-05', 'مستودع الدمام', 'Dammam Warehouse', 'BranchWarehouse', 'Dammam', 1),
                (7, 'WH-07', 'سيارة التوزيع', 'Mobile Van 1', 'MobileVan', 'Riyadh', 1);
            """);
        await IdentitySeedData.EnsureCanonicalRolesAndPermissionsAsync(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _sqliteConnection.Dispose();
        }
    }
}
