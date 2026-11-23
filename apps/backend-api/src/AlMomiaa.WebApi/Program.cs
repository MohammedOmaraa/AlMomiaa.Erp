using System.Text;
using AlMomiaa.Application;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Infrastructure;
using AlMomiaa.Infrastructure.Authentication;
using AlMomiaa.WebApi.Middleware;
using AlMomiaa.WebApi.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
        "Auth:Jwt configuration (Issuer, Audience, and a SigningKey of at least 32 UTF-8 bytes) is required.");
}

if (builder.Environment.IsProduction() && configuredOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must be explicitly configured in production.");
}

builder.Services.Configure<CorsSecurityOptions>(builder.Configuration.GetSection(CorsSecurityOptions.SectionName));
builder.Services.AddSingleton<ITrustedOriginValidator, TrustedOriginValidator>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
        Description = "AlMomiaa Enterprise ERP & DMS REST API"
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
});

var app = builder.Build();

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
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

app.Run();

public partial class Program;
