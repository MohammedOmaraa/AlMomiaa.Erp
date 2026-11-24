using System.Text;
using AlMomiaa.Application;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Domain.Common;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Infrastructure;
using AlMomiaa.Infrastructure.Authentication;
using AlMomiaa.Infrastructure.Persistence;
using AlMomiaa.WebApi.Hubs;
using AlMomiaa.WebApi.Middleware;
using AlMomiaa.WebApi.Security;
using AlMomiaa.WebApi.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var issuer = jwtSection["Issuer"] ?? string.Empty;
var audience = jwtSection["Audience"] ?? string.Empty;
var signingKey = jwtSection["SigningKey"] ?? string.Empty;

var configuredOrigins = builder.Configuration
    .GetSection($"{CorsSecurityOptions.SectionName}:AllowedOrigins")
    .Get<string[]>() ?? [];

if (configuredOrigins.Any(origin => string.Equals(origin?.Trim(), "*", StringComparison.Ordinal)))
{
    throw new InvalidOperationException("Wildcard CORS origins cannot be combined with credentialed authentication.");
}

if (string.IsNullOrWhiteSpace(issuer)
    || string.IsNullOrWhiteSpace(audience)
    || string.IsNullOrWhiteSpace(signingKey)
    || Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException(
        "Auth:Jwt configuration (Issuer, Audience, and a SigningKey of at least 32 UTF-8 bytes) is required. " +
        "In Development, configure them via 'dotnet user-secrets set \"Auth:Jwt:SigningKey\" \"<32+ chars>\"' or environment variables.");
}

if (builder.Environment.IsProduction() && configuredOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must be explicitly configured in production.");
}

builder.Services.Configure<CorsSecurityOptions>(builder.Configuration.GetSection(CorsSecurityOptions.SectionName));
builder.Services.AddSingleton<ITrustedOriginValidator, TrustedOriginValidator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddSignalR();
builder.Services.AddSingleton<IOperationsRealtimeNotifier, SignalROperationsRealtimeNotifier>();

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more request fields are invalid.",
            Type = "https://api.almomiaa.sa/errors/validation",
            Instance = context.HttpContext.Request.Path
        });
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AlMomiaa ERP API",
        Version = "v1",
        Description = "AlMomiaa Enterprise ERP & DMS REST API (8 Core Modules, ZATCA Phase 2, Double-Entry Ledger)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Short-lived (10-minute) Bearer JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("TrustedFrontend", policy =>
    {
        if (configuredOrigins.Length > 0)
        {
            policy.WithOrigins(configuredOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    var allPermissions = CanonicalRolesAndPermissions.All
        .SelectMany(role => role.Permissions)
        .Distinct(StringComparer.Ordinal);

    foreach (var permission in allPermissions)
    {
        options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
    }

    options.AddPolicy("WarehousesRead", policy => policy.RequireAssertion(ctx =>
        ctx.User.HasClaim("permission", CanonicalRolesAndPermissions.Permissions.InventoryView)
        || ctx.User.HasClaim("permission", CanonicalRolesAndPermissions.Permissions.OrdersCreate)
        || ctx.User.HasClaim("permission", CanonicalRolesAndPermissions.Permissions.OrdersApprove)
        || ctx.User.HasClaim("permission", CanonicalRolesAndPermissions.Permissions.DashboardsInventory)));
});

var app = builder.Build();

if (app.Environment.IsDevelopment()
    && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ALMOMIAA_DEV_ADMIN_CODE")))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DevelopmentAdminBootstrapper.EnsureDevAdminFromEnvironmentAsync(db, hasher, logger);
}

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "application/problem+json";

        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more request fields failed validation.",
                Type = "https://api.almomiaa.sa/errors/validation",
                Instance = context.Request.Path
            };
            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        if (exception is ForbiddenDomainException forbiddenException)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access forbidden",
                Detail = forbiddenException.Message,
                Type = "https://api.almomiaa.sa/errors/forbidden",
                Instance = context.Request.Path
            });
            return;
        }

        if (exception is NotFoundException notFoundException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = notFoundException.Message,
                Type = "https://api.almomiaa.sa/errors/not-found",
                Instance = context.Request.Path
            });
            return;
        }

        if (exception is ConflictException conflictException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Resource conflict",
                Detail = conflictException.Message,
                Type = $"https://api.almomiaa.sa/errors/{conflictException.ErrorCode}",
                Instance = context.Request.Path
            });
            return;
        }

        if (exception is DbUpdateConcurrencyException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict",
                Detail = "The record was modified by another operation. Reload the latest version and retry.",
                Type = "https://api.almomiaa.sa/errors/concurrency-conflict",
                Instance = context.Request.Path
            });
            return;
        }

        if (exception is BusinessRuleException ruleException)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Business rule violation",
                Detail = ruleException.Message,
                Type = $"https://api.almomiaa.sa/errors/{ruleException.ErrorCode}",
                Instance = context.Request.Path
            });
            return;
        }

        if (exception is SecurityStampUnavailableException)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Authentication service unavailable",
                Detail = "Unable to verify session revocation state at this time.",
                Type = "https://api.almomiaa.sa/errors/service-unavailable",
                Instance = context.Request.Path
            });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "The request could not be completed.",
            Type = "https://api.almomiaa.sa/errors/internal",
            Instance = context.Request.Path
        });
    });
});

app.UseCors("TrustedFrontend");
app.UseAuthentication();
app.UseMiddleware<SecurityStampMiddleware>();
app.UseAuthorization();

app.UseSwagger(options =>
{
    options.RouteTemplate = "openapi/{documentName}.json";
});
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "AlMomiaa ERP API v1");
});

app.MapControllers();
app.MapHub<OperationsHub>("/hubs/operations").RequireCors("TrustedFrontend");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

app.Run();

public partial class Program;
