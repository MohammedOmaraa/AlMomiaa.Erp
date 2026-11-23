using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Common.Behaviors;
using AlMomiaa.Infrastructure.Authentication;
using AlMomiaa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AlMomiaa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AlMomiaaDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SqlServer"),
                sql => sql.MigrationsAssembly(typeof(AlMomiaaDbContext).Assembly.FullName)));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<LoginRateLimitOptions>(configuration.GetSection(LoginRateLimitOptions.SectionName));

        services.AddSingleton(TimeProvider.System);

        var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString");
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException("Redis:ConnectionString configuration is required.");
            }

            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisOptions);
        });

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ISecurityStampStore, RedisSecurityStampStore>();
        services.AddSingleton<ILoginAttemptLimiter, LoginAttemptLimiter>();

        services.AddScoped<IIdempotencyStore, PassthroughIdempotencyStore>();
        services.AddScoped<IRequestPermissionChecker, DefaultPermissionChecker>();
        services.AddScoped<IRequestAuditSink, LoggingRequestAuditSink>();
        services.AddScoped<ITransactionBoundary, PassthroughTransactionBoundary>();

        return services;
    }
}
