using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Application.Common.Behaviors;
using AlMomiaa.Infrastructure.Authentication;
using AlMomiaa.Infrastructure.Persistence;
using AlMomiaa.Infrastructure.Persistence.Services;
using AlMomiaa.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AlMomiaa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment = false)
    {
        services.AddDbContext<AlMomiaaDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SqlServer"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(AlMomiaaDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<LoginRateLimitOptions>(configuration.GetSection(LoginRateLimitOptions.SectionName));

        services.AddSingleton(TimeProvider.System);

        var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString");
        var allowInMemoryFallback = configuration.GetValue<bool>("Redis:AllowInMemoryFallback", false);
        if ((isDevelopment || allowInMemoryFallback) && string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<ISecurityStampStore, InMemoryDevSecurityStampStore>();
            services.AddSingleton<ILoginAttemptLimiter, InMemoryDevLoginAttemptLimiter>();
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                if (string.IsNullOrWhiteSpace(redisConnectionString))
                {
                    throw new InvalidOperationException(
                        "Redis:ConnectionString configuration is required in non-Development environments unless Redis:AllowInMemoryFallback=true is explicitly enabled for single-instance demo hosting.");
                }

                var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
                redisOptions.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(redisOptions);
            });

            services.AddSingleton<ISecurityStampStore, RedisSecurityStampStore>();
            services.AddSingleton<ILoginAttemptLimiter, LoginAttemptLimiter>();
        }

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IIamModuleService, IamModuleService>();
        services.AddScoped<ICustomersModuleService, CustomersModuleService>();
        services.AddScoped<ICatalogModuleService, CatalogModuleService>();
        services.AddScoped<IInventoryModuleService, InventoryModuleService>();
        services.AddScoped<IProcurementModuleService, ProcurementModuleService>();
        services.AddScoped<ISalesModuleService, SalesModuleService>();
        services.AddScoped<IFinanceModuleService, FinanceModuleService>();
        services.AddScoped<IOperationsModuleService, OperationsModuleService>();
        services.AddScoped<INotificationsModuleService, NotificationsModuleService>();

        services.AddScoped<IIdempotencyStore, PassthroughIdempotencyStore>();
        services.AddScoped<IRequestPermissionChecker, DefaultPermissionChecker>();
        services.AddScoped<IRequestAuditSink, LoggingRequestAuditSink>();
        services.AddScoped<ITransactionBoundary, PassthroughTransactionBoundary>();
        services.AddSingleton<IOperationsRealtimeNotifier, NoOpOperationsRealtimeNotifier>();

        return services;
    }
}
