using AlMomiaa.Application.Authentication.Commands.ChangePassword;
using AlMomiaa.Application.Authentication.Commands.Login;
using AlMomiaa.Application.Authentication.Commands.Logout;
using AlMomiaa.Application.Authentication.Commands.RefreshSession;
using AlMomiaa.Application.Authentication.Contracts;
using AlMomiaa.Application.Common.Abstractions;
using AlMomiaa.Domain.Identity;
using AlMomiaa.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMomiaa.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IMediator mediator,
    ITrustedOriginValidator originValidator,
    ICurrentUserService currentUser) : ControllerBase
{
    public const string RefreshCookieName = "almomiaa_refresh";
    public const string RefreshCookiePath = "/api/v1/auth";

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [EndpointSummary("Authenticate an employee by phone number or employee code")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await mediator.Send(new LoginCommand(request, clientIp), cancellationToken);

        if (result.Response is null)
        {
            return result.Failure switch
            {
                LoginFailure.TooManyAttempts => StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many login attempts",
                        Detail = "Too many failed sign-in attempts. Please wait and try again.",
                        Type = "https://api.almomiaa.sa/errors/rate-limited",
                        Instance = HttpContext.Request.Path
                    }),
                _ => Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Authentication failed",
                    Detail = "The identifier or password is incorrect.",
                    Type = "https://api.almomiaa.sa/errors/unauthorized",
                    Instance = HttpContext.Request.Path
                })
            };
        }

        SetRefreshCookie(
            result.RefreshToken!,
            result.RefreshExpiresAtUtc ?? DateTimeOffset.UtcNow.Add(RefreshTokenSession.MaxSessionLifetime));

        return Ok(result.Response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [EndpointSummary("Atomically rotate the HttpOnly refresh cookie and issue a new 10-minute access JWT")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!originValidator.IsTrustedOrigin(Request))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Untrusted origin",
                    Detail = "Cross-site authentication requests are not permitted.",
                    Type = "https://api.almomiaa.sa/errors/untrusted-origin",
                    Instance = HttpContext.Request.Path
                });
        }

        var rawCookie = Request.Cookies[RefreshCookieName];
        var result = await mediator.Send(new RefreshSessionCommand(rawCookie), cancellationToken);

        if (!result.Succeeded || result.Response is null || string.IsNullOrEmpty(result.NewRefreshToken))
        {
            ClearRefreshCookie();
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Session invalid",
                Detail = "Your session has expired or is no longer valid. Please sign in again.",
                Type = "https://api.almomiaa.sa/errors/refresh-invalid",
                Instance = HttpContext.Request.Path
            });
        }

        SetRefreshCookie(
            result.NewRefreshToken,
            result.RefreshExpiresAtUtc ?? DateTimeOffset.UtcNow.Add(RefreshTokenSession.MaxSessionLifetime));

        return Ok(result.Response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Idempotently revoke the active refresh-session family and clear the refresh cookie")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!originValidator.IsTrustedOrigin(Request))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Untrusted origin",
                    Detail = "Cross-site authentication requests are not permitted.",
                    Type = "https://api.almomiaa.sa/errors/untrusted-origin",
                    Instance = HttpContext.Request.Path
                });
        }

        var rawCookie = Request.Cookies[RefreshCookieName];
        await mediator.Send(new LogoutCommand(rawCookie), cancellationToken);
        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Change the signed-in employee's password, rotate SecurityStamp, revoke all sessions, and clear refresh cookie")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!originValidator.IsTrustedOrigin(Request))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Untrusted origin",
                    Detail = "Cross-site authentication requests are not permitted.",
                    Type = "https://api.almomiaa.sa/errors/untrusted-origin",
                    Instance = HttpContext.Request.Path
                });
        }

        if (currentUser.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        await mediator.Send(new ChangePasswordCommand(currentUser.UserId, request), cancellationToken);
        ClearRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(string token, DateTimeOffset expiresAtUtc)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = expiresAtUtc,
            IsEssential = true
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Append(RefreshCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = DateTimeOffset.UnixEpoch,
            MaxAge = TimeSpan.Zero,
            IsEssential = true
        });
    }
}
