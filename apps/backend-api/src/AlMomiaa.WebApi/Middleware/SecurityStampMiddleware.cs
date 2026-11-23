using System.Security.Cryptography;
using System.Text;
using AlMomiaa.Application.Authentication.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Middleware;

public sealed class SecurityStampMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISecurityStampStore securityStampStore)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("UserId")?.Value;
            var stampClaim = context.User.FindFirst("SecurityStamp")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(stampClaim))
            {
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Session invalid",
                    "Authentication token is missing required security context.",
                    "https://api.almomiaa.sa/errors/unauthorized");
                return;
            }

            string? cachedStamp;
            try
            {
                cachedStamp = await securityStampStore.GetAsync(userId, context.RequestAborted);
            }
            catch (SecurityStampUnavailableException)
            {
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status503ServiceUnavailable,
                    "Authentication service unavailable",
                    "Unable to verify session revocation state at this time.",
                    "https://api.almomiaa.sa/errors/service-unavailable");
                return;
            }

            if (string.IsNullOrWhiteSpace(cachedStamp) || !FixedTimeEquals(cachedStamp, stampClaim))
            {
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Session revoked",
                    "Your session is no longer valid. Please sign in again.",
                    "https://api.almomiaa.sa/errors/session-revoked");
                return;
            }
        }

        await next(context);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string type)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = context.Request.Path
        }, context.RequestAborted);
    }
}
