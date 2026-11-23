using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AlMomiaa.Application.Authentication.Abstractions;
using AlMomiaa.Application.Authentication.Contracts;
using AlMomiaa.Domain.Identity;
using AlMomiaa.Infrastructure.Authentication;
using AlMomiaa.Infrastructure.Persistence;
using AlMomiaa.WebApi.Controllers;
using AlMomiaa.WebApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AlMomiaa.AuthenticationTests;

public sealed class AuthenticationEndToEndTests
{
    private static async Task<User> SeedUserAsync(
        AuthWebApplicationFactory factory,
        string employeeCode = "EMP-1001",
        string phone = "+966501234567",
        string password = "StrongPassword#2026",
        bool isActive = true,
        int roleId = 3, // SALES_MGR
        string? repType = null,
        int? defaultWarehouseId = 2,
        int[]? assignedWarehouses = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = User.Create(
            employeeCode: employeeCode,
            fullNameAr: "أحمد السبيعي",
            fullNameEn: "Ahmed Al-Subaie",
            phone: phone,
            email: "ahmed@almomiaa.sa",
            passwordHash: hasher.HashPassword(password),
            repType: repType,
            defaultWarehouseId: defaultWarehouseId,
            isActive: isActive,
            createdAtUtc: factory.Clock.GetUtcNow().UtcDateTime);

        user.AssignRole(roleId);
        foreach (var warehouseId in assignedWarehouses ?? [2, 5])
        {
            user.AssignWarehouse(warehouseId);
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static string ExtractCookieHeader(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies), "Expected Set-Cookie header.");
        return cookies.Single(c => c.StartsWith($"{AuthController.RefreshCookieName}=", StringComparison.Ordinal));
    }

    private static string ExtractCookieValue(string setCookieHeader)
    {
        var firstSegment = setCookieHeader.Split(';')[0];
        return firstSegment[(AuthController.RefreshCookieName.Length + 1)..];
    }

    [Fact]
    public async Task Canonical_role_and_permission_seed_is_idempotent_and_seeds_no_users()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        // Run seeder a second time to verify idempotency.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
            await IdentitySeedData.EnsureCanonicalRolesAndPermissionsAsync(db);

            Assert.Equal(7, await db.Roles.CountAsync());
            Assert.Equal(0, await db.Users.CountAsync());
            Assert.True(await db.RolePermissions.AnyAsync(p => p.PermissionClaim == CanonicalRolesAndPermissions.Permissions.OrdersEditSubmitted));
        }
    }

    [Fact]
    public async Task Login_by_employee_code_and_phone_returns_jwt_claims_and_hardened_refresh_cookie()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = await SeedUserAsync(factory, employeeCode: "EMP-2001", phone: "+966551112233", repType: "Wholesale", defaultWarehouseId: 4, assignedWarehouses: [4, 7]);
        using var client = factory.CreateClient();

        // 1. Login by employee code (case-insensitive)
        var codeResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("emp-2001", "StrongPassword#2026"));
        Assert.Equal(HttpStatusCode.OK, codeResponse.StatusCode);

        var rawBody = await codeResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("refreshToken", rawBody, StringComparison.OrdinalIgnoreCase);

        var payload = JsonSerializer.Deserialize<AuthenticationResponse>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(payload);
        Assert.Equal("Bearer", payload.TokenType);
        Assert.Equal(600, payload.ExpiresIn);
        Assert.Equal(user.Id, payload.User.Id);
        Assert.Equal("EMP-2001", payload.User.EmployeeCode);
        Assert.Contains("SALES_MGR", payload.User.RoleCodes);
        Assert.Contains("orders:edit_submitted", payload.User.Permissions);
        Assert.Equal("Wholesale", payload.User.RepType);
        Assert.Equal(4, payload.User.DefaultWarehouseId);
        Assert.Equal([4, 7], payload.User.AssignedWarehouseIds);

        // Verify JWT claims and exact 10-minute lifetime
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.AccessToken);
        Assert.Equal(TimeSpan.FromMinutes(10), jwt.ValidTo - jwt.ValidFrom);
        Assert.Equal(user.Id.ToString("D"), jwt.Claims.First(c => c.Type == "UserId").Value);
        Assert.Equal(user.SecurityStamp.ToString("D"), jwt.Claims.First(c => c.Type == "SecurityStamp").Value);
        Assert.Equal("SALES_MGR", jwt.Claims.First(c => c.Type == "RoleCodes").Value);
        Assert.Equal("Wholesale", jwt.Claims.First(c => c.Type == "RepType").Value);
        Assert.Equal("4", jwt.Claims.First(c => c.Type == "DefaultWarehouseId").Value);
        Assert.Equal("4,7", jwt.Claims.First(c => c.Type == "AssignedWarehouseIds").Value);
        Assert.Contains(jwt.Claims, c => c.Type == "permission" && c.Value == "orders:edit_submitted");

        // Verify Set-Cookie security attributes
        var setCookie = ExtractCookieHeader(codeResponse);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", setCookie, StringComparison.OrdinalIgnoreCase);

        var rawToken = ExtractCookieValue(setCookie);
        Assert.False(string.IsNullOrWhiteSpace(rawToken));

        // Verify DB stores ONLY hash, never raw token
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlMomiaaDbContext>();
            var session = await db.RefreshTokenSessions.SingleAsync();
            Assert.NotEqual(rawToken, session.TokenHash);
            Assert.Equal(AuthenticationService.HashToken(rawToken), session.TokenHash);
            Assert.Equal(TimeSpan.FromDays(14), session.ExpiresAtUtc - session.IssuedAtUtc);
        }

        // 2. Login by phone number (with formatting whitespace)
        var phoneResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("+966 55 111 2233", "StrongPassword#2026"));
        Assert.Equal(HttpStatusCode.OK, phoneResponse.StatusCode);
    }

    [Fact]
    public async Task Unknown_user_wrong_password_and_inactive_user_return_identical_generic_unauthorized_problem()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedUserAsync(factory, employeeCode: "EMP-3001", phone: "+966500000001", isActive: true);
        await SeedUserAsync(factory, employeeCode: "EMP-3002", phone: "+966500000002", isActive: false);
        using var client = factory.CreateClient();

        var unknown = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-9999", "WrongPassword!"));
        var wrongPwd = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-3001", "WrongPassword!"));
        var inactive = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-3002", "StrongPassword#2026"));

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPwd.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, inactive.StatusCode);

        var unknownBody = await unknown.Content.ReadAsStringAsync();
        var wrongPwdBody = await wrongPwd.Content.ReadAsStringAsync();
        var inactiveBody = await inactive.Content.ReadAsStringAsync();

        Assert.Equal(unknownBody, wrongPwdBody);
        Assert.Equal(wrongPwdBody, inactiveBody);
        Assert.DoesNotContain("inactive", inactiveBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not found", unknownBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_throttling_blocks_excessive_failed_attempts_with_429()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedUserAsync(factory, employeeCode: "EMP-4001", phone: "+966500000004");
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var attempt = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-4001", "BadPassword!"));
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-4001", "StrongPassword#2026"));
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_token_atomically_and_replaying_old_token_revokes_entire_family()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedUserAsync(factory, employeeCode: "EMP-5001", phone: "+966500000005");
        using var client = factory.CreateClient();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-5001", "StrongPassword#2026"));
        var token1 = ExtractCookieValue(ExtractCookieHeader(loginResp));

        // 1. Valid refresh rotates token1 -> token2 and preserves full role/permission context
        using var refreshReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq1.Headers.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);
        refreshReq1.Headers.Add("Cookie", $"{AuthController.RefreshCookieName}={token1}");
        var refreshResp1 = await client.SendAsync(refreshReq1);
        Assert.Equal(HttpStatusCode.OK, refreshResp1.StatusCode);

        var refreshedPayload = await refreshResp1.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(refreshedPayload);
        Assert.Contains("SALES_MGR", refreshedPayload.User.RoleCodes);
        Assert.Contains("orders:edit_submitted", refreshedPayload.User.Permissions);
        Assert.Equal([2, 5], refreshedPayload.User.AssignedWarehouseIds);

        var token2 = ExtractCookieValue(ExtractCookieHeader(refreshResp1));
        Assert.NotEqual(token1, token2);

        // 2. Replay old token1 -> must fail with 401, clear cookie, AND revoke token2's family!
        using var replayReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        replayReq.Headers.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);
        replayReq.Headers.Add("Cookie", $"{AuthController.RefreshCookieName}={token1}");
        var replayResp = await client.SendAsync(replayReq);
        Assert.Equal(HttpStatusCode.Unauthorized, replayResp.StatusCode);
        Assert.Contains("max-age=0", ExtractCookieHeader(replayResp), StringComparison.OrdinalIgnoreCase);

        // 3. Previously issued token2 in same family is now revoked too!
        using var refreshReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq2.Headers.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);
        refreshReq2.Headers.Add("Cookie", $"{AuthController.RefreshCookieName}={token2}");
        var refreshResp2 = await client.SendAsync(refreshReq2);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp2.StatusCode);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected_and_clears_cookie()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedUserAsync(factory, employeeCode: "EMP-6001", phone: "+966500000006");
        using var client = factory.CreateClient();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-6001", "StrongPassword#2026"));
        var token = ExtractCookieValue(ExtractCookieHeader(loginResp));

        // Advance clock past 14-day max session lifetime
        factory.Clock.Advance(TimeSpan.FromDays(14) + TimeSpan.FromMinutes(1));

        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);
        refreshReq.Headers.Add("Cookie", $"{AuthController.RefreshCookieName}={token}");
        var refreshResp = await client.SendAsync(refreshReq);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
        Assert.Contains("max-age=0", ExtractCookieHeader(refreshResp), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_is_idempotent_revokes_family_and_always_clears_cookie()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedUserAsync(factory, employeeCode: "EMP-7001", phone: "+966500000007");
        using var client = factory.CreateClient();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-7001", "StrongPassword#2026"));
        var token = ExtractCookieValue(ExtractCookieHeader(loginResp));

        using var logoutReq1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq1.Headers.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);
        logoutReq1.Headers.Add("Cookie", $"{AuthController.RefreshCookieName}={token}");
        var logoutResp1 = await client.SendAsync(logoutReq1);
        Assert.Equal(HttpStatusCode.NoContent, logoutResp1.StatusCode);
        Assert.Contains("max-age=0", ExtractCookieHeader(logoutResp1), StringComparison.OrdinalIgnoreCase);

        // Idempotent second logout without valid cookie still returns 204 and clears cookie
        using var logoutReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq2.Headers.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);
        var logoutResp2 = await client.SendAsync(logoutReq2);
        Assert.Equal(HttpStatusCode.NoContent, logoutResp2.StatusCode);

        // Refresh using revoked token now fails
        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);
        refreshReq.Headers.Add("Cookie", $"{AuthController.RefreshCookieName}={token}");
        var refreshResp = await client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }

    [Fact]
    public async Task Security_stamp_invalidation_blocks_refresh_and_middleware_fails_closed_when_redis_unavailable()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        var user = await SeedUserAsync(factory, employeeCode: "EMP-8001", phone: "+966500000008");
        using var client = factory.CreateClient();

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("EMP-8001", "StrongPassword#2026"));
        var token = ExtractCookieValue(ExtractCookieHeader(loginResp));

        // 1. Middleware accepts valid authenticated principal when Redis stamp matches
        var nextCalled = false;
        var middleware = new SecurityStampMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var validContext = new DefaultHttpContext();
        validContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("UserId", user.Id.ToString("D")),
            new Claim("SecurityStamp", user.SecurityStamp.ToString("D"))
        ], "Bearer"));

        await middleware.InvokeAsync(validContext, factory.StampStore);
        Assert.True(nextCalled);

        // 2. Middleware fails closed (503) when Redis is unavailable
        factory.StampStore.SimulateUnavailable = true;
        var unavailableContext = new DefaultHttpContext();
        unavailableContext.Response.Body = new MemoryStream();
        unavailableContext.User = validContext.User;
        await middleware.InvokeAsync(unavailableContext, factory.StampStore);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailableContext.Response.StatusCode);
        factory.StampStore.SimulateUnavailable = false;

        // 3. Invalidating stamp immediately rejects both middleware (401) and refresh (401)
        await factory.StampStore.InvalidateAsync(user.Id, CancellationToken.None);

        var revokedContext = new DefaultHttpContext();
        revokedContext.Response.Body = new MemoryStream();
        revokedContext.User = validContext.User;
        await middleware.InvokeAsync(revokedContext, factory.StampStore);
        Assert.Equal(StatusCodes.Status401Unauthorized, revokedContext.Response.StatusCode);

        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Origin", AuthWebApplicationFactory.TrustedOrigin);
        refreshReq.Headers.Add("Cookie", $"{AuthController.RefreshCookieName}={token}");
        var refreshResp = await client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResp.StatusCode);
    }

    [Fact]
    public async Task Untrusted_cross_site_origin_is_rejected_on_cookie_authenticated_refresh_and_logout()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Origin", "https://evil.example");
        var refreshResp = await client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.Forbidden, refreshResp.StatusCode);

        using var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq.Headers.Add("Origin", "https://evil.example");
        var logoutResp = await client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.Forbidden, logoutResp.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_publishes_auth_routes_and_contracts()
    {
        using var factory = new AuthWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var openApiResponse = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);

        var openApiJson = await openApiResponse.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/auth/login", openApiJson);
        Assert.Contains("/api/v1/auth/refresh", openApiJson);
        Assert.Contains("/api/v1/auth/logout", openApiJson);
        Assert.Contains("AuthenticationResponse", openApiJson);
        Assert.Contains("AuthenticatedUserDto", openApiJson);
        Assert.Contains("LoginRequest", openApiJson);
    }
}
